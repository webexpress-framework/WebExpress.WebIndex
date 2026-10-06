using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a posting node storing a document id in a self-balancing (AVL) binary search tree.
    /// </summary>
    /// <remarks>
    /// The tree is keyed by document id, and ids arrive in whatever order documents are indexed - for
    /// sequential guids that is sorted order, which turns an unbalanced tree into a list. Every insert
    /// and removal therefore restores the AVL invariant on its way back up, so a lookup never walks
    /// more than about 1.44 log2(n) nodes. The height of a node is stored with it: deriving it would
    /// mean walking the whole subtree on every insert, and a cached copy would not survive eviction
    /// from the segment buffer.
    /// <para>
    /// A rotation changes the root of a subtree, so <see cref="Insert"/> and <see cref="Remove"/>
    /// return the root the caller has to link - the owner of the tree, the term node, keeps the
    /// address of the whole tree's root and follows it the same way.
    /// </para>
    /// </remarks>
    /// <param name="context">The reference to the context of the index.</param>
    /// <param name="addr">The address of the segment.</param>
    public class IndexStorageSegmentPostingNode(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr)
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the on-disk size of the segment.
        /// </summary>
        public const uint SegmentSize = 16 + sizeof(ulong) + sizeof(ulong) + sizeof(ulong) + sizeof(byte);

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
        /// Gets the address of the first position element of a sorted list or 0 if there is no element.
        /// </summary>
        public ulong PositionAddr { get; private set; }

        /// <summary>
        /// Gets the height of the subtree rooted at this node; a leaf has the height 1.
        /// </summary>
        public uint Height { get; private set; } = 1;

        /// <summary>
        /// Gets the left child of the node or null.
        /// </summary>
        public IndexStorageSegmentPostingNode Left
        {
            get
            {
                if (LeftAddr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentPostingNode>(LeftAddr, Context);
                    return item;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the right child of the node or null.
        /// </summary>
        public IndexStorageSegmentPostingNode Right
        {
            get
            {
                if (RightAddr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentPostingNode>(RightAddr, Context);
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
        /// Traverses the tree in pre-order.
        /// </summary>
        public IEnumerable<IndexStorageSegmentPostingNode> PreOrder
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
        /// Gets all document ids in pre-order.
        /// </summary>
        public IEnumerable<Guid> All => PreOrder.Select(x => x.DocumentID);

        /// <summary>
        /// Gets the sorted list of positions or no element.
        /// </summary>
        public IEnumerable<IndexStorageSegmentPosition> Positions
        {
            get
            {
                if (PositionAddr == 0)
                {
                    yield break;
                }

                var addr = PositionAddr;

                while (addr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentPosition>(addr, Context);
                    yield return item;

                    addr = item.SuccessorAddr;
                }
            }
        }

        /// <summary>
        /// Inserts a document id into the subtree rooted at this node and restores the balance.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <param name="insert">The inserted or existing posting node segment.</param>
        /// <param name="inserted">True if a new node has been inserted, false if the id was present.</param>
        /// <returns>The root of the subtree afterwards - another node than this one when a rotation lifted it.</returns>
        public IndexStorageSegmentPostingNode Insert(Guid id, out IndexStorageSegmentPostingNode insert, out bool inserted)
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
        public IndexStorageSegmentPostingNode Remove(Guid id, out bool removed)
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

                    // with at most one child that child takes the place of this node; the
                    // node and its positions are given back to the allocator
                    if (LeftAddr == 0 || RightAddr == 0)
                    {
                        var child = LeftAddr != 0 ? Left : Right;

                        RemovePositions();
                        Context.Allocator.Free(this);

                        return child;
                    }

                    // with two children the inorder successor - the leftmost node of the right
                    // subtree - is moved into this node, positions included, and its former
                    // node is removed from the right subtree. Moving the payload rather than
                    // the node keeps this node as the subtree root, so the parent's link
                    // stays valid; the positions of the removed id travel along to the
                    // successor's node, from where the removal frees them
                    var successor = Right;

                    while (successor.LeftAddr != 0)
                    {
                        successor = successor.Left;
                    }

                    var removedPositions = PositionAddr;

                    DocumentID = successor.DocumentID;
                    PositionAddr = successor.PositionAddr;
                    successor.PositionAddr = removedPositions;
                    Context.IndexFile.Write(successor);

                    RightAddr = Right.Remove(successor.DocumentID, out _)?.Addr ?? 0;
                }

                return Rebalance();
            }
        }

        /// <summary>
        /// Adds a position segment in ascending order. Returns existing position if present.
        /// </summary>
        /// <param name="pos">The position of the term.</param>
        /// <returns>The position segment.</returns>
        public IndexStorageSegmentPosition AddPosition(uint pos)
        {
            var item = default(IndexStorageSegmentPosition);

            lock (_guard)
            {
                if (PositionAddr == 0)
                {
                    PositionAddr = Context.Allocator.Alloc(IndexStorageSegmentPosition.SegmentSize);
                    item = new IndexStorageSegmentPosition(Context, PositionAddr)
                    {
                        Position = pos
                    };

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else
                {
                    // check whether it exists and find insertion point
                    var last = default(IndexStorageSegmentPosition);
                    var count = 0U;

                    foreach (var i in Positions)
                    {
                        var compare = i.Position.CompareTo(pos);

                        if (compare > 0)
                        {
                            break;
                        }
                        else if (compare == 0)
                        {
                            return i;
                        }

                        last = i;
                        count++;
                    }

                    item = new IndexStorageSegmentPosition(Context, Context.Allocator.Alloc(IndexStorageSegmentPosition.SegmentSize))
                    {
                        Position = pos
                    };

                    if (last is null)
                    {
                        // insert at the beginning
                        var tempAddr = PositionAddr;
                        PositionAddr = item.Addr;
                        item.SuccessorAddr = tempAddr;

                        Context.IndexFile.Write(this);
                        Context.IndexFile.Write(item);
                    }
                    else
                    {
                        // insert in the correct place
                        var tempAddr = last.SuccessorAddr;
                        last.SuccessorAddr = item.Addr;
                        item.SuccessorAddr = tempAddr;

                        Context.IndexFile.Write(last);
                        Context.IndexFile.Write(item);
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Removes all position segments and resets the head pointer.
        /// </summary>
        public void RemovePositions()
        {
            if (PositionAddr == 0)
            {
                return;
            }

            lock (_guard)
            {
                foreach (var position in Positions)
                {
                    // remove position segment
                    Context.Allocator.Free(position);
                }

                PositionAddr = 0;
                // persist head reset so readers do not follow freed nodes
                Context.IndexFile.Write(this);
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
            PositionAddr = reader.ReadUInt64();
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
            writer.Write(PositionAddr);
            writer.Write((byte)Height);
        }

        /// <summary>
        /// Compares the current instance to another posting node by document id.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>A signed integer indicating the relative order.</returns>
        /// <exception cref="System.ArgumentException">Obj is not the same type as this instance.</exception>
        public int CompareTo(object obj)
        {
            if (obj is IndexStorageSegmentPostingNode posting)
            {
                return DocumentID.CompareTo(posting.DocumentID);
            }

            throw new ArgumentException("Object is not the same type as this instance.", nameof(obj));
        }

        /// <summary>
        /// Returns the document id as string.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return $"{DocumentID}";
        }

        /// <summary>
        /// Returns the height of a node that may be missing.
        /// </summary>
        /// <param name="node">The node or null.</param>
        /// <returns>The height, 0 for a missing node.</returns>
        private static uint GetHeight(IndexStorageSegmentPostingNode node)
        {
            return node?.Height ?? 0u;
        }

        /// <summary>
        /// Allocates and persists a new leaf for a document id.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <returns>The new node.</returns>
        private IndexStorageSegmentPostingNode CreateLeaf(Guid id)
        {
            var item = new IndexStorageSegmentPostingNode(Context, Context.Allocator.Alloc(SegmentSize))
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
        private IndexStorageSegmentPostingNode Rebalance()
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
        private IndexStorageSegmentPostingNode RotateRight()
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
        private IndexStorageSegmentPostingNode RotateLeft()
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
