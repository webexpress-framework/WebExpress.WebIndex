using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a node in a term trie where each character is stored as a separate node.
    /// Except for the root node, the nodes have a fixed-size on-disk layout and are stored
    /// in the reverse index's data area. A complete term can reference a posting tree with
    /// per-document positions and frequency data.
    /// </summary>
    /// <param name="context">The index storage context.</param>
    /// <param name="addr">The address of the node segment.</param>
    [SegmentCached]
    public class IndexStorageSegmentTermNode(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr)
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the on-disk size of the node segment.
        /// </summary>
        public const uint SegmentSize = sizeof(uint) + sizeof(ulong) + sizeof(ulong) + sizeof(uint) + sizeof(ulong);

        /// <summary>
        /// Gets or sets the character of the node (0 indicates the root node).
        /// </summary>
        public char Character { get; set; }

        /// <summary>
        /// Gets or sets the address of the sibling node.
        /// </summary>
        public ulong SiblingAddr { get; set; }

        /// <summary>
        /// Gets or sets the address of the first child node.
        /// </summary>
        public ulong ChildAddr { get; set; }

        /// <summary>
        /// Gets or sets the number of postings (documents) for the term ending at this node.
        /// </summary>
        public uint Frequency { get; set; }

        /// <summary>
        /// Gets the address of the root of the posting tree or 0 if none.
        /// </summary>
        public ulong PostingAddr { get; private set; }

        /// <summary>
        /// Gets the list of child nodes.
        /// </summary>
        public IEnumerable<IndexStorageSegmentTermNode> Children
        {
            get
            {
                if (ChildAddr == 0)
                {
                    yield break;
                }

                var addr = ChildAddr;

                while (addr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentTermNode>(addr, Context);
                    addr = item.SiblingAddr;

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this node is the root.
        /// </summary>
        public bool IsRoot => Character == 0;

        /// <summary>
        /// Traverses the trie in pre-order and returns the nodes.
        /// </summary>
        public IEnumerable<IndexStorageSegmentTermNode> PreOrder
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
        /// Gets all terms and their leaf nodes reachable from this node.
        /// </summary>
        public IEnumerable<(string, IndexStorageSegmentTermNode)> Terms
        {
            get
            {
                foreach (var child in Children)
                {
                    if (child.PostingAddr != 0)
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

                if (ChildAddr != 0)
                {
                    yield break;
                }

                yield return (Character.ToString(), this);
            }
        }

        /// <summary>
        /// Gets all document ids from all postings reachable from this node.
        /// </summary>
        public IEnumerable<Guid> All => Terms.SelectMany(x => x.Item2.Posting?.All ?? []);

        /// <summary>
        /// Gets the root of the posting tree or null.
        /// </summary>
        public IndexStorageSegmentPostingNode Posting
        {
            get
            {
                if (PostingAddr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentPostingNode>(PostingAddr, Context);
                    return item;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the leaf node corresponding to the given subterm, or null if not found.
        /// </summary>
        /// <param name="subterm">The remaining subterm to traverse; decreases one character per level.</param>
        public IndexStorageSegmentTermNode this[string subterm]
        {
            get
            {
                if (subterm is null)
                {
                    return this;
                }

                var first = subterm.FirstOrDefault();
                var next = subterm.Length > 1 ? subterm[1..] : null;

                // find nodes
                foreach (var child in Children)
                {
                    if (first == child.Character)
                    {
                        // recursive descent
                        return child[next];
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Adds the given subterm to the trie and returns the leaf node where the term ends.
        /// </summary>
        /// <param name="subterm">The remaining subterm to add; decreases one character per level.</param>
        /// <returns>The leaf node corresponding to the full term.</returns>
        public IndexStorageSegmentTermNode Add(string subterm)
        {
            lock (_guard)
            {
                if (subterm is null)
                {
                    return this;
                }

                var first = subterm.FirstOrDefault();
                var next = subterm.Length > 1 ? subterm[1..] : null;

                // find existing nodes
                foreach (var child in Children)
                {
                    if (first == child.Character)
                    {
                        // recursive descent
                        return child.Add(next);
                    }
                }

                // add new node
                var node = new IndexStorageSegmentTermNode(Context, Context.Allocator.Alloc(SegmentSize))
                {
                    Character = first
                };

                AddChild(node);

                return node.Add(next);
            }
        }

        /// <summary>
        /// Adds a posting (document id) to this term. Creates the posting tree if missing.
        /// </summary>
        /// <param name="id">The document id to add.</param>
        /// <returns>The posting node segment added or found.</returns>
        public IndexStorageSegmentPostingNode AddPosting(Guid id)
        {
            var item = default(IndexStorageSegmentPostingNode);

            lock (_guard)
            {
                if (PostingAddr == 0)
                {
                    PostingAddr = Context.Allocator.Alloc(IndexStorageSegmentPostingNode.SegmentSize);
                    item = new IndexStorageSegmentPostingNode(Context, PostingAddr)
                    {
                        DocumentID = id
                    };

                    Frequency++;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else
                {
                    if (Posting.Insert(id, out IndexStorageSegmentPostingNode node))
                    {
                        Frequency++;

                        Context.IndexFile.Write(this);
                    }

                    item = node;
                }
            }

            return item;
        }

        /// <summary>
        /// Removes a posting (document id) from this term's posting tree.
        /// Handles all cases including root replacement with inorder successor.
        /// </summary>
        /// <param name="id">The document id to remove.</param>
        /// <returns>True if removed; otherwise false.</returns>
        public bool RemovePosting(Guid id)
        {
            if (PostingAddr == 0)
            {
                return false;
            }

            lock (_guard)
            {
                if (PostingAddr == 0)
                {
                    return false;
                }

                var root = Posting;

                if (id.CompareTo(root.DocumentID) < 0)
                {
                    if (root.Left?.Remove(id, root, IndexStorageBinaryTreeDirection.Left) ?? false)
                    {
                        Frequency--;
                        Context.IndexFile.Write(this);
                        return true;
                    }

                    return false;
                }
                else if (id.CompareTo(root.DocumentID) > 0)
                {
                    if (root.Right?.Remove(id, root, IndexStorageBinaryTreeDirection.Right) ?? false)
                    {
                        Frequency--;
                        Context.IndexFile.Write(this);
                        return true;
                    }

                    return false;
                }

                // node with only one child or no child
                if (root.LeftAddr == 0 || root.RightAddr == 0)
                {
                    PostingAddr = root.LeftAddr != 0 ? root.LeftAddr : root.RightAddr;

                    root.RemovePositions();
                    Context.Allocator.Free(root);

                    Frequency--;
                    Context.IndexFile.Write(this);

                    return true;
                }

                // node with two children: replace root with inorder successor (leftmost of right subtree)
                var rightRoot = root.Right;
                var leftmostPack = rightRoot.LeftmostChild;
                var successor = leftmostPack.Leftmost as IndexStorageSegmentPostingNode;

                var oldLeft = root.LeftAddr;
                var oldRight = root.RightAddr;

                // detach successor from its parent: parent.left becomes successor.right
                if (leftmostPack.Parent is IndexStorageSegmentPostingNode successorParent)
                {
                    successorParent.LeftAddr = successor.RightAddr;
                    Context.IndexFile.Write(successorParent);
                }

                // transplant successor in place of root
                successor.LeftAddr = oldLeft;

                // if successor is not the immediate right child, hook up old right subtree
                if (successor.Addr != oldRight)
                {
                    successor.RightAddr = oldRight;
                }

                Context.IndexFile.Write(successor);

                // update head to new root of posting tree
                PostingAddr = successor.Addr;

                // free old root
                root.RemovePositions();
                Context.Allocator.Free(root);

                Frequency--;
                Context.IndexFile.Write(this);

                return true;
            }
        }

        /// <summary>
        /// Adds a child node to the current node; maintains sibling ordering by character.
        /// Returns the inserted or existing child.
        /// </summary>
        /// <param name="node">The child node to add.</param>
        /// <returns>The inserted or existing child node.</returns>
        private IndexStorageSegmentTermNode AddChild(IndexStorageSegmentTermNode node)
        {
            lock (_guard)
            {
                if (ChildAddr == 0)
                {
                    ChildAddr = node.Addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(node);
                }
                else
                {
                    // check whether it exists (ordered insert by character)
                    var last = default(IndexStorageSegmentTermNode);
                    var count = 0U;

                    foreach (var i in Children)
                    {
                        var compare = i.Character.CompareTo(node.Character);

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

                    if (last is null)
                    {
                        // insert at the beginning
                        var tempAddr = ChildAddr;
                        ChildAddr = node.Addr;
                        node.SiblingAddr = tempAddr;

                        Context.IndexFile.Write(this);
                        Context.IndexFile.Write(node);
                    }
                    else
                    {
                        // insert in the correct place
                        var tempAddr = last.SiblingAddr;
                        last.SiblingAddr = node.Addr;
                        node.SiblingAddr = tempAddr;

                        Context.IndexFile.Write(last);
                        Context.IndexFile.Write(node);
                    }
                }

                return node;
            }
        }

        /// <summary>
        /// Retrieves all document ids for the given term using the provided options.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>An enumeration of document ids.</returns>
        public virtual IEnumerable<Guid> Retrieve(string term, IndexRetrieveOptions options)
        {
            foreach (var posting in GetPostings(term))
            {
                yield return posting.DocumentID;
            }
        }

        /// <summary>
        /// Retrieves the posting items for the given term.
        /// </summary>
        /// <param name="term">The search term.</param>
        /// <returns>An enumeration of posting nodes.</returns>
        internal virtual IEnumerable<IndexStorageSegmentPostingNode> GetPostings(string term)
        {
            foreach (var node in GetLeafs(term))
            {
                foreach (var posting in node.Posting?.PreOrder ?? [])
                {
                    yield return posting;
                }
            }
        }

        /// <summary>
        /// Returns the leaf nodes matching the given (possibly wildcard) term.
        /// Supports '?' for single-character and '*' for multi-character wildcards.
        /// </summary>
        /// <param name="term">The (sub)term to match; decreases one character per level.</param>
        /// <returns>An enumeration of matching leaf nodes.</returns>
        public virtual IEnumerable<IndexStorageSegmentTermNode> GetLeafs(string term)
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
                        {
                            // find nodes
                            foreach (var child in Children)
                            {
                                foreach (var node in child.GetLeafs(next))
                                {
                                    yield return node;
                                }
                            }
                            break;
                        }
                    case '*':
                        {
                            // escape regex special chars before expanding wildcards
                            var escaped = Regex.Escape(next ?? string.Empty);
                            var pattern = escaped.Replace("\\*", ".*").Replace("\\?", ".");
                            foreach (var termTuple in Terms)
                            {
                                if (Regex.IsMatch(termTuple.Item1, pattern, RegexOptions.CultureInvariant))
                                {
                                    yield return termTuple.Item2;
                                }
                            }
                            break;
                        }
                    default:
                        {
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
        }

        /// <summary>
        /// Reads the node from the storage medium.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        public override void Read(BinaryReader reader)
        {
            Character = (char)reader.ReadUInt32();
            SiblingAddr = reader.ReadUInt64();
            ChildAddr = reader.ReadUInt64();
            Frequency = reader.ReadUInt32();
            PostingAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the node to the storage medium.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write((uint)Character);
            writer.Write(SiblingAddr);
            writer.Write(ChildAddr);
            writer.Write(Frequency);
            writer.Write(PostingAddr);
        }

        /// <summary>
        /// Compares this instance with another node by character.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>A signed integer indicating relative order.</returns>
        /// <exception cref="System.ArgumentException">Thrown if obj is of different type.</exception>
        public int CompareTo(object obj)
        {
            if (obj is IndexStorageSegmentTermNode item)
            {
                return Character.CompareTo(item.Character);
            }

            throw new ArgumentException("Object is of different type");
        }

        /// <summary>
        /// Returns a string representation of the node.
        /// </summary>
        /// <returns>ROOT for the root or the character.</returns>
        public override string ToString()
        {
            if (IsRoot)
            {
                return "ROOT";
            }

            return $"{Character}";
        }
    }
}