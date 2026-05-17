using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a posting node storing a document id in a binary search tree.
    /// </summary>
    /// <remarks>
    /// balancing is not implemented; consider an avl/rb-tree or b+-tree layout for on-disk structures.
    /// </remarks>
    /// <param name="context">The reference to the context of the index.</param>
    /// <param name="addr">The address of the segment.</param>
    public class IndexStorageSegmentPostingNode(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr)
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the on-disk size of the segment.
        /// </summary>
        public const uint SegmentSize = 16 + sizeof(ulong) + sizeof(ulong) + sizeof(ulong);

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
        /// Gets the height of the tree (derived recursively; may be expensive on-disk).
        /// </summary>
        public uint Height
        {
            get
            {
                var leftHeight = Left?.Height ?? 0;
                var rightHeight = Right?.Height ?? 0;

                return Math.Max(leftHeight, rightHeight) + 1;
            }
        }

        /// <summary>
        /// Gets the absolute balance factor of the node.
        /// </summary>
        public uint Balance
        {
            get
            {
                var leftHeight = Left?.Height ?? 0;
                var rightHeight = Right?.Height ?? 0;

                return leftHeight > rightHeight ? leftHeight - rightHeight : rightHeight - leftHeight;
            }
        }

        /// <summary>
        /// Gets the leftmost child in the subtree and its parent.
        /// </summary>
        public dynamic LeftmostChild
        {
            get
            {
                var node = this;
                var parent = null as IndexStorageSegmentPostingNode;

                while (node.Left is not null)
                {
                    parent = node;
                    node = node.Left;
                }

                return new { Leftmost = node, Parent = parent };
            }
        }

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
        /// Inserts a new node with the given document id into the binary tree.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <param name="insert">The inserted or existing posting node segment.</param>
        /// <returns>True if a new node has been inserted, otherwise false.</returns>
        public bool Insert(Guid id, out IndexStorageSegmentPostingNode insert)
        {
            lock (_guard)
            {
                if (id.CompareTo(DocumentID) < 0)
                {
                    if (LeftAddr == 0)
                    {
                        LeftAddr = Context.Allocator.Alloc(SegmentSize);
                        var item = new IndexStorageSegmentPostingNode(Context, LeftAddr)
                        {
                            DocumentID = id
                        };

                        // persist parent pointer update and new node
                        Context.IndexFile.Write(this);
                        Context.IndexFile.Write(item);

                        insert = item;
                        return true;
                    }
                    else
                    {
                        return Left.Insert(id, out insert);
                    }
                }
                else if (id.CompareTo(DocumentID) > 0)
                {
                    if (RightAddr == 0)
                    {
                        RightAddr = Context.Allocator.Alloc(SegmentSize);
                        var item = new IndexStorageSegmentPostingNode(Context, RightAddr)
                        {
                            DocumentID = id
                        };

                        // persist parent pointer update and new node
                        Context.IndexFile.Write(this);
                        Context.IndexFile.Write(item);

                        insert = item;
                        return true;
                    }
                    else
                    {
                        return Right.Insert(id, out insert);
                    }
                }

                insert = this;
                return false;
            }
        }

        /// <summary>
        /// Removes a node with the given data from the binary tree.
        /// </summary>
        /// <param name="id">The document id.</param>
        /// <param name="parent">The parent node.</param>
        /// <param name="direction">The direction from the parent to this node.</param>
        /// <returns>True if a node has been removed, otherwise false.</returns>
        public bool Remove(Guid id, IndexStorageSegmentPostingNode parent, IndexStorageBinaryTreeDirection direction)
        {
            lock (_guard)
            {
                if (id.CompareTo(DocumentID) < 0)
                {
                    return Left?.Remove(id, this, IndexStorageBinaryTreeDirection.Left) ?? false;
                }
                else if (id.CompareTo(DocumentID) > 0)
                {
                    return Right?.Remove(id, this, IndexStorageBinaryTreeDirection.Right) ?? false;
                }

                // node with only one child or no child
                if (LeftAddr == 0 || RightAddr == 0)
                {
                    var childAddr = LeftAddr != 0 ? LeftAddr : RightAddr;

                    switch (direction)
                    {
                        case IndexStorageBinaryTreeDirection.Left:
                            {
                                parent.LeftAddr = childAddr;
                                break;
                            }
                        case IndexStorageBinaryTreeDirection.Right:
                            {
                                parent.RightAddr = childAddr;
                                break;
                            }
                    }

                    // free positions and persist parent
                    RemovePositions();
                    Context.Allocator.Free(this);
                    Context.IndexFile.Write(parent);

                    return true;
                }

                // node with two children: find inorder successor (leftmost of right subtree)
                var rightRoot = Right;
                var leftmostPack = rightRoot.LeftmostChild;
                var successor = leftmostPack.Leftmost as IndexStorageSegmentPostingNode;

                var oldLeft = LeftAddr;
                var oldRight = RightAddr;

                // detach successor from its parent: parent.left becomes successor.right
                if (leftmostPack.Parent is IndexStorageSegmentPostingNode successorParent)
                {
                    successorParent.LeftAddr = successor.RightAddr;
                    Context.IndexFile.Write(successorParent);
                }

                // transplant successor in place of current node
                successor.LeftAddr = oldLeft;

                // if successor is not the immediate right child, hook up the old right subtree
                if (successor.Addr != oldRight)
                {
                    successor.RightAddr = oldRight;
                }

                Context.IndexFile.Write(successor);

                // connect parent to successor
                switch (direction)
                {
                    case IndexStorageBinaryTreeDirection.Left:
                        {
                            parent.LeftAddr = successor.Addr;
                            break;
                        }
                    case IndexStorageBinaryTreeDirection.Right:
                        {
                            parent.RightAddr = successor.Addr;
                            break;
                        }
                }

                Context.IndexFile.Write(parent);

                // free current node’s positions and the node itself
                RemovePositions();
                Context.Allocator.Free(this);

                return true;
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
    }
}