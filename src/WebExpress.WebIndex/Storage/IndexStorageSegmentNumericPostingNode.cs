using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Each document stored as separate nodes in a binary search tree.
    /// </summary>
    /// <remarks> 
    /// TODO: Implement balanced tree algorithm for optimal performance. 
    /// </remarks>
    /// <typeparam name="T">The data type. This must have the IIndexData interface.</typeparam>
    /// <param name="context">The reference to the context of the index.</param>
    /// <param name="addr">The address of the segment.</param>
    public class IndexStorageSegmentNumericPostingNode(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr)
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = 16 + sizeof(ulong) + sizeof(ulong);

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
        /// Gets the height of the tree.
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
        /// Gets the balance factor of the tree.
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
        /// Gets the leftmost child and his parent.
        /// </summary>
        public dynamic LeftmostChild
        {
            get
            {
                var node = this;
                var parent = null as IndexStorageSegmentNumericPostingNode;

                while (node.Left is not null)
                {
                    parent = node;
                    node = node.Left;
                }

                return new { Leftmost = node, Parent = parent };
            }
        }

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
        /// Gets all document ids.
        /// </summary>
        public IEnumerable<Guid> All => PreOrder
            .Select(x => x.DocumentID);

        /// <summary>
        /// Inserts a new node with the given document id into the binary tree.
        /// </summary>
        /// <param name="id">The document id.</params>
        /// <param name="insert">The posting node segment.</param>
        /// <returns>Ture if a new node has been inserted, otherwise false.</returns>
        public bool Insert(Guid id, out IndexStorageSegmentNumericPostingNode insert)
        {
            lock (_guard)
            {
                if (id.CompareTo(DocumentID) < 0)
                {
                    if (LeftAddr == 0)
                    {
                        LeftAddr = Context.Allocator.Alloc(SegmentSize);
                        var item = new IndexStorageSegmentNumericPostingNode(Context, LeftAddr)
                        {
                            DocumentID = id
                        };

                        // persist new node first to avoid dangling pointers on crash,
                        // then persist the parent's updated pointer
                        Context.IndexFile.Write(item);
                        Context.IndexFile.Write(this);

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
                        var item = new IndexStorageSegmentNumericPostingNode(Context, RightAddr)
                        {
                            DocumentID = id
                        };

                        // persist new node first to avoid dangling pointers on crash,
                        // then persist the parent's updated pointer
                        Context.IndexFile.Write(item);
                        Context.IndexFile.Write(this);

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
        /// <param name="id">The document id.</params>
        /// <param name="parent">The parent node.</param>
        /// <param name="direction"></param>
        /// <returns>Ture if a node has been removed, otherwise false.</returns>
        public bool Remove(Guid id, IndexStorageSegmentNumericPostingNode parent, IndexStorageBinaryTreeDirection direction)
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
                    switch (direction)
                    {
                        case IndexStorageBinaryTreeDirection.Left:
                            parent.LeftAddr = LeftAddr != 0 ? LeftAddr : RightAddr;
                            break;
                        case IndexStorageBinaryTreeDirection.Right:
                            parent.RightAddr = LeftAddr != 0 ? LeftAddr : RightAddr;
                            break;
                    }

                    Context.Allocator.Free(this);

                    Context.IndexFile.Write(parent);

                    return true;
                }

                // node with two children: get the inorder successor (most left child in the right subtree)
                var leftmostChild = Right.LeftmostChild;
                var inorderSuccessor = leftmostChild?.Leftmost;
                var inorderSuccessorParent = leftmostChild?.Parent;

                inorderSuccessor.LeftAddr = LeftAddr;
                inorderSuccessor.RightAddr = inorderSuccessorParent?.Addr ?? 0ul;
                Context.IndexFile.Write(inorderSuccessor);

                if (inorderSuccessorParent is not null)
                {
                    inorderSuccessorParent.LeftAddr = 0ul;
                    Context.IndexFile.Write(inorderSuccessorParent);
                }

                switch (direction)
                {
                    case IndexStorageBinaryTreeDirection.Left:
                        parent.LeftAddr = inorderSuccessor?.Addr ?? 0ul;
                        break;
                    case IndexStorageBinaryTreeDirection.Right:
                        parent.RightAddr = inorderSuccessor?.Addr ?? 0ul;
                        break;
                }

                Context.Allocator.Free(this);

                Context.IndexFile.Write(parent);

                return true;
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
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return $"{DocumentID}";
        }
    }
}