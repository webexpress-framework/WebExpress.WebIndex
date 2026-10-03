using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a posting node of a numeric value: a document id in a self-balancing (AVL) binary search tree.
    /// </summary>
    /// <remarks>
    /// The tree is the same as the one behind a term, see <see cref="IndexStorageSegmentPostingNode"/>,
    /// without the positions: a number occurs once in a field. The height of a node is stored with it,
    /// and <see cref="Insert"/> and <see cref="Remove"/> return the root of the subtree afterwards,
    /// which the numeric node owning the tree follows.
    /// </remarks>
    /// <param name="context">The reference to the context of the index.</param>
    /// <param name="addr">The address of the segment.</param>
    public class IndexStorageSegmentNumericPostingNode(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr)
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = 16 + sizeof(ulong) + sizeof(ulong) + sizeof(byte);

        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        public Guid DocumentID { get; set; }

        /// <summary>
        /// Gets or sets the address of the left child.
        /// </summary>
        public ulong LeftAddr { get; set; }

        /// <summary>
        /// Gets or sets the address of the right child.
        /// </summary>
        public ulong RightAddr { get; set; }

        /// <summary>
        /// Gets the height of the subtree rooted at this node; a leaf has the height 1.
        /// </summary>
        public uint Height { get; private set; } = 1;

        /// <summary>
        /// Gets the left child of the node.
        /// </summary>
        public IndexStorageSegmentNumericPostingNode Left
        {
            get
            {
                if (LeftAddr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentNumericPostingNode>(LeftAddr, Context);
                    return item;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the right child of the node.
        /// </summary>
        public IndexStorageSegmentNumericPostingNode Right
        {
            get
            {
                if (RightAddr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentNumericPostingNode>(RightAddr, Context);
                    return item;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the absolute balance factor of the node, which the AVL invariant keeps at 0 or 1.
        /// </summary>
        public uint Balance
        {
            get
            {
                var factor = BalanceFactor;

                return (uint)Math.Abs(factor);
            }
        }

        /// <summary>
        /// Gets the signed balance factor: the height of the left subtree minus the height of the right one.
        /// </summary>
        private int BalanceFactor => (int)GetHeight(Left) - (int)GetHeight(Right);

        /// <summary>
        /// Passes through the tree in pre order.
        /// </summary>
        /// <returns>The tree as a list.</returns>
        public IEnumerable<IndexStorageSegmentNumericPostingNode> PreOrder
        {
            get
            {
                yield return this;

                // recurse on the left subtree
                foreach (var n in Left?.PreOrder ?? [])
                {
                    yield return n;
                }

                // recurse on the right subtree
                foreach (var n in Right?.PreOrder ?? [])
                {
                    yield return n;
                }
            }
        }

        /// <summary>
        /// Returns all document ids in pre order.
        /// </summary>
        public IEnumerable<Guid> All => PreOrder
            .Select(x => x.DocumentID);

        /// <summary>
        /// Inserts a document id into the subtree rooted at this node and restores the balance.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <param name="insert">The inserted or existing posting node segment.</param>
        /// <param name="inserted">True if a new node has been inserted, false if the id was present.</param>
        /// <returns>The root of the subtree afterwards - another node than this one when a rotation lifted it.</returns>
        public IndexStorageSegmentNumericPostingNode Insert(Guid id, out IndexStorageSegmentNumericPostingNode insert, out bool inserted)
        {
            lock (_guard)
            {
                var compare = id.CompareTo(DocumentID);

                if (compare == 0)
                {
                    insert = this;
                    inserted = false;

                    return this;
                }

                if (compare < 0)
                {
                    if (LeftAddr == 0)
                    {
                        insert = CreateLeaf(id);
                        inserted = true;
                        LeftAddr = insert.Addr;
                    }
                    else
                    {
                        LeftAddr = Left.Insert(id, out insert, out inserted).Addr;
                    }
                }
                else
                {
                    if (RightAddr == 0)
                    {
                        insert = CreateLeaf(id);
                        inserted = true;
                        RightAddr = insert.Addr;
                    }
                    else
                    {
                        RightAddr = Right.Insert(id, out insert, out inserted).Addr;
                    }
                }

                // nothing changed below this node, so its shape and height are as they were
                if (!inserted)
                {
                    return this;
                }

                return Rebalance();
            }
        }

        /// <summary>
        /// Removes a document id from the subtree rooted at this node and restores the balance.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <param name="removed">True if a node has been removed, false if the id was not present.</param>
        /// <returns>The root of the subtree afterwards, or null when the subtree is empty now.</returns>
        public IndexStorageSegmentNumericPostingNode Remove(Guid id, out bool removed)
        {
            lock (_guard)
            {
                var compare = id.CompareTo(DocumentID);

                if (compare < 0)
                {
                    if (LeftAddr == 0)
                    {
                        removed = false;

                        return this;
                    }

                    var left = Left.Remove(id, out removed);

                    if (!removed)
                    {
                        return this;
                    }

                    LeftAddr = left?.Addr ?? 0;
                }
                else if (compare > 0)
                {
                    if (RightAddr == 0)
                    {
                        removed = false;

                        return this;
                    }

                    var right = Right.Remove(id, out removed);

                    if (!removed)
                    {
                        return this;
                    }

                    RightAddr = right?.Addr ?? 0;
                }
                else
                {
                    removed = true;

                    // with at most one child that child takes the place of this node
                    if (LeftAddr == 0 || RightAddr == 0)
                    {
                        var child = LeftAddr != 0 ? Left : Right;

                        Context.Allocator.Free(this);

                        return child;
                    }

                    // with two children the inorder successor - the leftmost node of the right
                    // subtree - is moved into this node and its former node is removed from the
                    // right subtree; moving the id rather than the node keeps this node as the
                    // subtree root, so the parent's link stays valid
                    var successor = Right;

                    while (successor.LeftAddr != 0)
                    {
                        successor = successor.Left;
                    }

                    DocumentID = successor.DocumentID;
                    RightAddr = Right.Remove(successor.DocumentID, out _)?.Addr ?? 0;
                }

                return Rebalance();
            }
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public override void Read(BinaryReader reader)
        {
            var guid = reader.ReadBytes(16);
            LeftAddr = reader.ReadUInt64();
            RightAddr = reader.ReadUInt64();
            Height = reader.ReadByte();
            DocumentID = new Guid(guid);
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(DocumentID.ToByteArray());
            writer.Write(LeftAddr);
            writer.Write(RightAddr);
            writer.Write((byte)Height);
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return $"{DocumentID}";
        }

        /// <summary>
        /// Returns the height of a node that may be missing.
        /// </summary>
        /// <param name="node">The node or null.</param>
        /// <returns>The height, 0 for a missing node.</returns>
        private static uint GetHeight(IndexStorageSegmentNumericPostingNode node)
        {
            return node?.Height ?? 0u;
        }

        /// <summary>
        /// Allocates and persists a new leaf for a document id.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <returns>The new node.</returns>
        private IndexStorageSegmentNumericPostingNode CreateLeaf(Guid id)
        {
            var item = new IndexStorageSegmentNumericPostingNode(Context, Context.Allocator.Alloc(SegmentSize))
            {
                DocumentID = id
            };

            // the leaf is persisted before the link to it, so a crash in between leaves an
            // unreferenced segment rather than a dangling pointer
            Context.IndexFile.Write(item);

            return item;
        }

        /// <summary>
        /// Recomputes the height of this node from its children and persists it.
        /// </summary>
        private void UpdateHeight()
        {
            Height = Math.Max(GetHeight(Left), GetHeight(Right)) + 1;
            Context.IndexFile.Write(this);
        }

        /// <summary>
        /// Restores the AVL invariant at this node after a change in one of its subtrees.
        /// </summary>
        /// <returns>The root of the subtree afterwards.</returns>
        private IndexStorageSegmentNumericPostingNode Rebalance()
        {
            UpdateHeight();

            var factor = BalanceFactor;

            if (factor > 1)
            {
                // left-right case: straighten the left subtree first
                if (Left.BalanceFactor < 0)
                {
                    LeftAddr = Left.RotateLeft().Addr;
                }

                return RotateRight();
            }

            if (factor < -1)
            {
                // right-left case: straighten the right subtree first
                if (Right.BalanceFactor > 0)
                {
                    RightAddr = Right.RotateRight().Addr;
                }

                return RotateLeft();
            }

            return this;
        }

        /// <summary>
        /// Rotates the subtree to the right: the left child becomes its root and this node its right child.
        /// </summary>
        /// <returns>The new root of the subtree.</returns>
        private IndexStorageSegmentNumericPostingNode RotateRight()
        {
            var pivot = Left;

            LeftAddr = pivot.RightAddr;
            pivot.RightAddr = Addr;

            // the lower node first, because the pivot's height is derived from it
            UpdateHeight();
            pivot.UpdateHeight();

            return pivot;
        }

        /// <summary>
        /// Rotates the subtree to the left: the right child becomes its root and this node its left child.
        /// </summary>
        /// <returns>The new root of the subtree.</returns>
        private IndexStorageSegmentNumericPostingNode RotateLeft()
        {
            var pivot = Right;

            RightAddr = pivot.LeftAddr;
            pivot.LeftAddr = Addr;

            // the lower node first, because the pivot's height is derived from it
            UpdateHeight();
            pivot.UpdateHeight();

            return pivot;
        }
    }
}
