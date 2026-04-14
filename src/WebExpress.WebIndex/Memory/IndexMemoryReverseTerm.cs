using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Provides a reverse index that manages the data in the main memory.
    /// </summary>
    /// <param name="context">The index context.</param>
    /// <param name="field">The field that makes up the index.</param>
    /// <param name="culture">The culture.</param>
    public class IndexMemoryReverseTerm<TIndexItem> : IndexMemoryReverse<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the root term.
        /// </summary>
        public IndexMemorySegmentTermNode Root { get; private set; } = new();

        /// <summary>
        /// Gets all items.
        /// </summary>
        public override IEnumerable<Guid> All => Root.Terms
            .SelectMany(x => x.Item2.Postings)
            .Select(x => x.DocumentId)
            .Distinct();

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <param name="field">The field that makes up the index.</param>
        /// <param name="culture">The culture.</param>
        public IndexMemoryReverseTerm(IIndexDocumemntContext context, IndexFieldData field, CultureInfo culture)
            : base(context, field, culture)
        {
        }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        public override void Add(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item)?.ToString();
            var terms = Context.TokenAnalyzer.Analyze(value, Culture);

            Add(item, terms);
        }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        /// <param name="terms">The terms to add to the reverse index for the given item.</param>
        public override void Add(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            foreach (var term in terms)
            {
                Root.Add(item.Id, term.Value.ToString(), term.Position);
            }
        }

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the field.</param>
        public override void Delete(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item);
            var terms = Context.TokenAnalyzer.Analyze(value?.ToString(), Culture);

            Delete(item, terms);
        }

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the field.</param>
        /// <param name="terms">The terms to add to the reverse index for the given item.</param>
        public override void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            foreach (var term in terms)
            {
                Root.Remove(term.Value.ToString(), item.Id);
            }
        }

        /// <summary>
        /// Removed all data from the index.
        /// </summary>
        public override void Clear()
        {
            Root = new IndexMemorySegmentTermNode();
        }

        /// <summary>
        /// Drop the reverse index.
        /// </summary>
        public override void Drop()
        {

        }

        /// <summary>
        /// Return all items for a given input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="options">The retrieve options.</param>
        /// <returns>An enumeration of the data ids.</returns>
        public override IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options)
        {
            var tokens = Context.TokenAnalyzer.Analyze(input?.ToString(), Culture, true);
            var distinct = new HashSet<Guid>((int)Math.Min(options.MaxResults, int.MaxValue / 2));
            var count = 0u;

            if (!tokens.Any())
            {
                return distinct;
            }

            switch (options.Method)
            {
                case IndexRetrieveMethod.Phrase:
                    {
                        var firstTerm = tokens.Take(1).FirstOrDefault();
                        var nextTerms = tokens.Skip(1);

                        foreach (var posting in Root.GetPostings(firstTerm.Value.ToString()))
                        {
                            foreach (var position in posting.Positions)
                            {
                                if (CheckForPhraseMatch(posting.DocumentId, position, firstTerm.Position, nextTerms))
                                {
                                    distinct.Add(posting.DocumentId);
                                }
                            }
                        }

                        break;
                    }
                default:
                    {
                        foreach (var document in tokens.Take(1).SelectMany(x => Root.Retrieve(x.Value.ToString(), options)))
                        {
                            if (distinct.Add(document) && count++ >= options.MaxResults)
                            {
                                break;
                            }
                        }

                        foreach (var normalized in tokens.Skip(1))
                        {
                            var temp = new HashSet<Guid>(distinct.Count);

                            foreach (var document in Root.Retrieve(normalized.Value.ToString(), options))
                            {
                                if (distinct.Contains(document) && temp.Add(document))
                                {
                                }
                            }

                            distinct = temp;
                        }

                        break;
                    }
            }

            return distinct;
        }

        /// <summary>
        /// Checks whether there is an exact match.
        /// </summary>
        /// <param name="document">The document id to check.</param>
        /// <param name="position">The position of the term within the document.</param>
        /// <param name="offset">The position within the search term.</param>
        /// <param name="terms">Further following search terms.</param>
        /// <returns>True ff there is an exact match, otherwise false.</returns>
        private bool CheckForPhraseMatch(Guid document, uint position, uint offset, IEnumerable<IndexTermToken> terms)
        {
            if (!terms.Any())
            {
                return true;
            }

            var firstTerm = terms.Take(1).FirstOrDefault();
            var nextTerms = terms.Skip(1);

            foreach (var posting in Root.GetPostings(firstTerm.Value.ToString()).Where(x => x?.DocumentId == document))
            {
                foreach (var pos in posting.Positions.Where(x => x == position + (firstTerm.Position - offset)))
                {
                    return CheckForPhraseMatch(posting.DocumentId, pos, firstTerm.Position, nextTerms);
                }
            }

            return false;
        }
    }
}
