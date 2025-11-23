using System;
using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Represents a tree which is formed from the characters of the numeric values.
    /// </summary>
    public class IndexMemorySegmentNumericNode
    {
        /// <summary>
        /// Returns or sets the numeric value of the node.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Returns or sets the left child node in the tree.
        /// </summary>
        public IndexMemorySegmentNumericNode Left { get; set; }

        /// <summary>
        /// Returns or sets the right child node in the tree.
        /// </summary>
        public IndexMemorySegmentNumericNode Right { get; set; }

        /// <summary>
        /// Returns the height of the node in the tree.
        /// </summary>
        public uint Height { get; private set; }

        /// <summary>
        /// Returns the postings associated with the node.
        /// </summary>
        public IEnumerable<IndexMemorySegmentPosting> Postings { get; private set; } = [];

        /// <summary>
        /// Returns the nodes in a post-order traversal.
        /// </summary>
        public IEnumerable<IndexMemorySegmentNumericNode> PostOrder
        {
            get
            {
                // recurse on the left subtree
                foreach (var n in Left?.PostOrder ?? [])
                {
                    yield return n;
                }

                // recurse on the right subtree
                foreach (var n in Right?.PostOrder ?? [])
                {
                    yield return n;
                }

                yield return this;
            }
        }

        /// <summary>
        /// Returns all document IDs from the postings in the tree.
        /// </summary>
        public IEnumerable<Guid> All => PostOrder.SelectMany(x => x.Postings?.Select(x => x.DocumentId));

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IndexMemorySegmentNumericNode()
        {
            Height = 1;
        }

        /// <summary>
        /// Initializes a new instance of the class with the specified value and postings.
        /// </summary>
        /// <param name="value">The numeric value of the node.</param>
        /// <param name="postings">The postings associated with the node.</param>
        public IndexMemorySegmentNumericNode(decimal value, IEnumerable<IndexMemorySegmentPosting> postings)
        {
            Value = value;
            Postings = postings;
            Height = 1;
        }

        /// <summary>
        /// Returns the balance factor of the node, which is the difference between the heights of the left and right subtrees.
        /// </summary>
        private int BalanceFactor => (int)GetHeight(Left) - (int)GetHeight(Right);

        /// <summary>
        /// Adds a new node with the specified value and balances the tree.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="value">The numeric value to be added to the tree.</param>
        /// <returns>The balanced tree node.</returns>
        public IndexMemorySegmentNumericNode AddAndBalance(Guid id, decimal value)
        {
            Add(id, value);

            return Balance();
        }

        /// <summary>
        /// Retrieves document IDs based on the specified search value and retrieval options.
        /// </summary>
        /// <param name="search">The numeric value to search for.</param>
        /// <param name="options">The options for the search, including method and maximum results.</param>
        /// <returns>An enumerable collection of document IDs that match the search criteria.</returns>
        public virtual IEnumerable<Guid> Retrieve(decimal search, IndexRetrieveOptions options)
        {
            switch (options.Method)
            {
                case IndexRetrieveMethod.Phrase:
                    // searches the binary tree for the value that is equals with the specified value
                    if (Value == search)
                    {
                        foreach (var postting in Postings)
                        {
                            yield return postting.DocumentId;
                        }
                    }

                    if (Left is not null && search < Value)
                    {
                        // recurse on the left subtree
                        foreach (var value in Left.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    if (Right is not null && search > Value)
                    {
                        // recurse on the right subtree
                        foreach (var value in Right.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    break;
                case IndexRetrieveMethod.GratherThan:
                    if (Value > search)
                    {
                        foreach (var postting in Postings)
                        {
                            yield return postting.DocumentId;
                        }
                    }

                    if (Left is not null)
                    {
                        foreach (var value in Left.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    if (Right is not null)
                    {
                        foreach (var value in Right.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    break;
                case IndexRetrieveMethod.GratherThanOrEqual:
                    // searches the binary tree for the largest value that is less or equals than the specified value
                    if (Value >= search)
                    {
                        foreach (var postting in Postings)
                        {
                            yield return postting.DocumentId;
                        }
                    }

                    if (Left is not null)
                    {
                        foreach (var value in Left.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    if (Right is not null)
                    {
                        foreach (var value in Right.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    break;
                case IndexRetrieveMethod.LessThan:
                    // searches the binary tree for the smallest value that is greater than the specified value
                    if (Value < search)
                    {
                        foreach (var postting in Postings)
                        {
                            yield return postting.DocumentId;
                        }
                    }

                    if (Left is not null)
                    {
                        foreach (var value in Left.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    if (Right is not null && search > Value)
                    {
                        foreach (var value in Right.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    break;
                case IndexRetrieveMethod.LessThanOrEqual:
                    // searches the binary tree for the smallest value that is greater or equals than the specified value
                    if (Value <= search)
                    {
                        foreach (var postting in Postings)
                        {
                            yield return postting.DocumentId;
                        }
                    }

                    if (Left is not null)
                    {
                        foreach (var value in Left.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    if (Right is not null && search >= Value)
                    {
                        foreach (var value in Right.Retrieve(search, options))
                        {
                            yield return value;
                        }
                    }

                    break;
                default:
                    yield break;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Value} → {Postings?.ToString() ?? "null"}";
        }

        /// <summary>
        /// Returns the height of the specified node.
        /// </summary>
        /// <param name="node">The node to get the height of.</param>
        /// <returns>The height of the specified node.</returns>
        private static uint GetHeight(IndexMemorySegmentNumericNode node)
        {
            return node?.Height ?? 0u;
        }

        /// <summary>
        /// Updates the height of the current node based on the heights of its children.
        /// </summary>
        private void UpdateHeight()
        {
            Height = Math.Max(GetHeight(Left), GetHeight(Right)) + 1;
        }

        /// <summary>
        /// Performs a right rotation on the current node.
        /// </summary>
        /// <returns>The new root after the rotation.</returns>
        private IndexMemorySegmentNumericNode RotateRight()
        {
            var newRoot = Left;
            Left = newRoot.Right;
            newRoot.Right = this;

            return newRoot;
        }

        /// <summary>
        /// Performs a left rotation on the current node.
        /// </summary>
        /// <returns>The new root after the rotation.</returns>
        private IndexMemorySegmentNumericNode RotateLeft()
        {
            var newRoot = Right;
            Right = newRoot.Left;
            newRoot.Left = this;

            return newRoot;
        }

        /// <summary>
        /// Balances the tree node by performing rotations if necessary.
        /// </summary>
        /// <returns>The balanced tree node.</returns>
        private IndexMemorySegmentNumericNode Balance()
        {
            UpdateHeight();

            if (BalanceFactor > 1)
            {
                if (Left.BalanceFactor < 0)
                {
                    Left = Left.RotateLeft();
                }
                return RotateRight();
            }
            else if (BalanceFactor < -1)
            {
                if (Right.BalanceFactor > 0)
                {
                    Right = Right.RotateRight();
                }
                return RotateLeft();
            }

            return this;
        }

        /// <summary>
        /// Adds a new node with the specified value to the tree.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="value">The numeric value to be added to the tree.</param>
        private void Add(Guid id, decimal value)
        {
            if (value.CompareTo(Value) < 0)
            {
                if (Left is null)
                {
                    Left = new IndexMemorySegmentNumericNode(value, [new IndexMemorySegmentPosting(id, 0)]);
                }
                else
                {
                    Left = Left.AddAndBalance(id, value);
                }
            }
            else if (value.CompareTo(Value) > 0)
            {
                if (Right is null)
                {
                    Right = new IndexMemorySegmentNumericNode(value, [new IndexMemorySegmentPosting(id, 0)]);
                }
                else
                {
                    Right = Right.AddAndBalance(id, value);
                }
            }
            else
            {
                var postingsList = Postings.ToList();
                var posting = postingsList.FirstOrDefault(x => x.DocumentId.Equals(id));

                if (posting is null)
                {
                    postingsList.Add(new IndexMemorySegmentPosting(id, 0));
                }

                Postings = postingsList;
            }
        }
    }
}
