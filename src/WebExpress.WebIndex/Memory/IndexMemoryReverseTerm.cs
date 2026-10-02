using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Utility;

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
        /// Adds an item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        public override void Add(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item)?.ToString();
            var terms = Context.TokenAnalyzer.Analyze(value, Culture);

            Add(item, terms);
        }

        /// <summary>
        /// Adds an item to the index.
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
        /// Retrieves documents for a given input and retrieval options.
        /// </summary>
        /// <remarks>
        /// Without a distance and outside of a phrase search every term has to occur somewhere in a
        /// document. A phrase search requires the terms in query order, each within the distance of
        /// the place the previous one leads to expect it; a proximity search - a distance without a
        /// phrase - requires each term within the distance of the previous one. A similarity applies
        /// to every term in all three, so a fuzzy query does not turn exact once a distance is added.
        /// The result is cut to the maximum number only when it is complete: cutting earlier would
        /// drop documents that a later term would have confirmed.
        /// </remarks>
        /// <param name="input">The input text.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>A distinct set of matching document ids.</returns>
        public override IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options)
        {
            var tokens = Context.TokenAnalyzer.Analyze(input?.ToString(), Culture, true)
                .Where(x => !string.IsNullOrEmpty(x.Value?.ToString()))
                .ToList();

            if (tokens.Count == 0)
            {
                return [];
            }

            var documents = options.Method == IndexRetrieveMethod.Phrase || options.Distance > 0
                ? RetrievePositional(tokens, options)
                : RetrieveAll(tokens, options);

            return documents.Take((int)Math.Min(options.MaxResults, int.MaxValue));
        }

        /// <summary>
        /// Returns the documents that contain every token, wherever it occurs.
        /// </summary>
        /// <param name="tokens">The tokens of the query.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>The matching document ids.</returns>
        private HashSet<Guid> RetrieveAll(IReadOnlyList<IndexTermToken> tokens, IndexRetrieveOptions options)
        {
            HashSet<Guid> documents = null;

            foreach (var token in tokens)
            {
                var matches = new HashSet<Guid>(RetrieveTerm(token.Value.ToString(), options));

                if (documents is null)
                {
                    documents = matches;
                }
                else
                {
                    documents.IntersectWith(matches);
                }

                if (documents.Count == 0)
                {
                    break;
                }
            }

            return documents ?? [];
        }

        /// <summary>
        /// Returns the documents in which the tokens occur in the arrangement a phrase or a
        /// proximity search asks for. Every occurrence of the first token is a possible start and
        /// every occurrence of a following token within reach a possible continuation; all of them
        /// are tried, because the first one that fits the window may lead nowhere while a later
        /// one completes the match.
        /// </summary>
        /// <param name="tokens">The tokens of the query.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>The matching document ids.</returns>
        private HashSet<Guid> RetrievePositional(IReadOnlyList<IndexTermToken> tokens, IndexRetrieveOptions options)
        {
            var phrase = options.Method == IndexRetrieveMethod.Phrase;
            var terms = tokens.Select(x => ResolveTerms(x.Value.ToString(), options)).ToList();
            var documents = new HashSet<Guid>();

            foreach (var term in terms[0])
            {
                foreach (var posting in Root.GetPostings(term))
                {
                    if (posting is null || documents.Contains(posting.DocumentId))
                    {
                        continue;
                    }

                    foreach (var position in Positions(posting))
                    {
                        if (IsArranged(posting.DocumentId, position, 1, phrase, options.Distance, tokens, terms))
                        {
                            documents.Add(posting.DocumentId);
                            break;
                        }
                    }
                }
            }

            return documents;
        }

        /// <summary>
        /// Determines whether the tokens from the given index on follow the occurrence at the given
        /// position in the arrangement of a phrase or a proximity search.
        /// </summary>
        /// <param name="document">The document id.</param>
        /// <param name="position">The position at which the previous token occurs.</param>
        /// <param name="index">The index of the token to place next.</param>
        /// <param name="phrase">True for a phrase search, false for a proximity search.</param>
        /// <param name="distance">The allowed distance.</param>
        /// <param name="tokens">The tokens of the query.</param>
        /// <param name="terms">The index terms each token stands for.</param>
        /// <returns>True if the remaining tokens can be placed, otherwise false.</returns>
        private bool IsArranged(Guid document, uint position, int index, bool phrase, uint distance, IReadOnlyList<IndexTermToken> tokens, IReadOnlyList<IReadOnlyList<string>> terms)
        {
            if (index >= tokens.Count)
            {
                return true;
            }

            uint lower;
            uint upper;

            if (phrase)
            {
                // the next token is expected as far behind the previous one as in the query
                var gap = tokens[index].Position >= tokens[index - 1].Position
                    ? tokens[index].Position - tokens[index - 1].Position
                    : 0u;

                lower = Clamp(position + (ulong)gap);
                upper = Clamp(position + (ulong)gap + distance);
            }
            else
            {
                lower = position >= distance ? position - distance : 0u;
                upper = Clamp(position + (ulong)distance);
            }

            foreach (var term in terms[index])
            {
                foreach (var posting in Root.GetPostings(term).Where(x => x?.DocumentId == document))
                {
                    foreach (var next in Positions(posting).Where(x => x >= lower && x <= upper))
                    {
                        if (IsArranged(document, next, index + 1, phrase, distance, tokens, terms))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the index terms a query term stands for: the term itself - wildcards are resolved
        /// by the term tree - or, with a similarity, every term of the vocabulary similar enough.
        /// </summary>
        /// <param name="term">The (normalized) query term.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>The index terms.</returns>
        private IReadOnlyList<string> ResolveTerms(string term, IndexRetrieveOptions options)
        {
            if (options.Similarity is > 0 and < 100)
            {
                var threshold = options.Similarity / 100.0;

                return [.. Root.Terms
                    .Select(x => x.Item1)
                    .Where(x => IndexFuzzy.CalculateLevenshteinSimilarity(term, x) >= threshold)];
            }

            return [term];
        }

        /// <summary>
        /// Returns the document ids for a single term. When a similarity threshold
        /// is set, every term of the vocabulary whose Levenshtein similarity reaches
        /// the threshold contributes its documents (fuzzy search).
        /// </summary>
        /// <param name="term">The (normalized) search term.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>An enumeration of matching document ids.</returns>
        private IEnumerable<Guid> RetrieveTerm(string term, IndexRetrieveOptions options)
        {
            if (options.Similarity is > 0 and < 100)
            {
                return ResolveTerms(term, options).SelectMany(x => Root.GetPostings(x)).Select(x => x.DocumentId);
            }

            return Root.Retrieve(term, options);
        }

        /// <summary>
        /// Returns the positions of a posting.
        /// </summary>
        /// <param name="posting">The posting.</param>
        /// <returns>The positions at which the term occurs in the document.</returns>
        private static IEnumerable<uint> Positions(IndexMemorySegmentPosting posting)
        {
            return posting.Positions ?? [];
        }

        /// <summary>
        /// Limits a position to the range of a position.
        /// </summary>
        /// <param name="value">The computed position.</param>
        /// <returns>The position, capped at the largest one.</returns>
        private static uint Clamp(ulong value)
        {
            return value > uint.MaxValue ? uint.MaxValue : (uint)value;
        }
    }
}
