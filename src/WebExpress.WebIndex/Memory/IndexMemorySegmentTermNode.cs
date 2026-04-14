using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Represents a tree which is formed from the characters of the terms.
    /// </summary>
    public class IndexMemorySegmentTermNode
    {
        /// <summary>
        /// The character of the node.
        /// </summary>
        public char Character { get; set; }

        /// <summary>
        /// Gets or sets the child nodes of the tree.
        /// </summary>
        public List<IndexMemorySegmentTermNode> Children { get; set; } = [];

        /// <summary>
        /// Gets or sets the postings. This is always on the leaf of an term.
        /// </summary>
        public List<IndexMemorySegmentPosting> Postings { get; set; }

        /// <summary>
        /// Checks whether the current node is the root.
        /// </summary>
        public bool IsRoot => Character == 0;

        /// <summary>
        /// Passes through the tree in pre order.
        /// </summary>
        /// <returns>The tree as a list.</returns>
        public IEnumerable<IndexMemorySegmentTermNode> PreOrder
        {
            get
            {
                yield return this;

                foreach (var child in Children)
                {
                    foreach (var preOrderChild in child.PreOrder)
                    {
                        yield return preOrderChild;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all terms.
        /// </summary>
        public IEnumerable<(string, IndexMemorySegmentTermNode)> Terms
        {
            get
            {
                foreach (var child in Children)
                {
                    if (child.Postings is not null && child.Children.Count != 0)
                    {
                        yield return (Character + child.Character.ToString(), child);
                    }

                    foreach (var term in child.Terms)
                    {
                        if (Character != 0)
                        {
                            yield return (Character + term.Item1, term.Item2);
                        }
                        else
                        {
                            yield return (term.Item1, term.Item2);
                        }
                    }
                }

                if (IsRoot)
                {
                    yield break;
                }

                if (Children.Any())
                {
                    yield break;
                }

                yield return (Character.ToString(), this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal IndexMemorySegmentTermNode()
        {
        }

        /// <summary>
        /// Create tree from term and save item in leaf.
        /// </summary>
        /// <param name="id">The data to be added to the index.</param>
        /// <param name="subterm">A subterm that is shortened by the first character at each tree level.</param>
        /// <param name="position">The position of the term in the input value.</param>
        public void Add(Guid id, string subterm, uint position)
        {
            if (subterm is null)
            {
                // end of recursive descent reached
                Postings ??= [];
                var posting = Postings.FirstOrDefault(x => x.DocumentId.Equals(id));

                if (posting is null)
                {
                    Postings.Add(new IndexMemorySegmentPosting(id, position));
                }
                else if (!posting.Positions.Contains<uint>(position))
                {
                    posting.Positions.Add(position);
                }

                return;
            }

            var children = Children as List<IndexMemorySegmentTermNode>;
            var first = subterm.FirstOrDefault();
            var next = subterm.Length > 1 ? subterm[1..] : null;

            // find existing nodes
            foreach (var child in children)
            {
                if (first == child.Character)
                {
                    // recursive descent
                    child.Add(id, next, position);

                    return;
                }
            }

            // add new node
            var node = new IndexMemorySegmentTermNode() { Character = first };
            children.Add(node);
            node.Add(id, next, position);
        }

        /// <summary>
        /// The data to be removed from the field.
        /// </summary>
        /// <param name="subterm">A subterm that is shortened by the first character at each tree level.</param>
        /// <param name="id">The data to be added to the index.</param>
        public void Remove(string subterm, Guid id)
        {
            if (subterm is null)
            {
                Postings = Postings?.Where(X => !X.DocumentId.Equals(id)).ToList();

                return;
            }

            var first = subterm.FirstOrDefault();
            var next = subterm.Length > 1 ? subterm[1..] : null;

            // find nodes
            foreach (var child in Children)
            {
                if (first == child.Character)
                {
                    // recursive descent
                    child.Remove(next, id);
                }
            }
        }

        /// <summary>
        /// Return all term items for a given term.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="options">The retrieve options.</param>
        /// <returns>An enumeration of data ids of the terms.</returns>
        public virtual IEnumerable<Guid> Retrieve(string term, IndexRetrieveOptions options)
        {
            foreach (var posting in GetPostings(term))
            {
                yield return posting.DocumentId;
            }
        }

        /// <summary>
        /// Return all term posting items for a given term.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <returns>An enumeration of posting items.</returns>
        internal virtual IEnumerable<IndexMemorySegmentPosting> GetPostings(string term)
        {
            foreach (var node in GetLeafs(term))
            {
                foreach (var posting in node.Postings ?? [])
                {
                    yield return posting;
                }
            }
        }

        /// <summary>
        /// Returns the nodes in the tree.
        /// </summary>
        /// <param name="term">A subterm that is shortened by the first character at each tree level.</param>
        /// <returns>An enumeration of leafs of the term.</returns>
        public virtual IEnumerable<IndexMemorySegmentTermNode> GetLeafs(string term)
        {
            if (term is null)
            {
                yield return this;
            }
            else
            {
                var first = term.FirstOrDefault();
                var next = term.Length > 1 ? term[1..] : null;

                switch (first)
                {
                    case '?':
                        // find nodes
                        foreach (var child in Children)
                        {
                            foreach (var node in child.GetLeafs(next))
                            {
                                yield return node;
                            }
                        }
                        break;
                    case '*':
                        var pattern = next?.Replace("*", ".*").Replace("?", ".") ?? ".*";
                        foreach (var termTuple in Terms)
                        {
                            if (Regex.IsMatch(termTuple.Item1, pattern))
                            {
                                yield return termTuple.Item2;
                            }
                        }
                        break;
                    default:
                        // find nodes
                        foreach (var child in Children)
                        {
                            if (first == child.Character)
                            {
                                // recursive descent
                                foreach (var node in child.GetLeafs(next))
                                {
                                    yield return node;
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return $"{Character} → {Postings?.ToString() ?? "null"}";
        }
    }
}
