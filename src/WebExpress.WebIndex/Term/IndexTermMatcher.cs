using System;
using System.Collections.Generic;
using System.Linq;
using WebExpress.WebIndex.Utility;

namespace WebExpress.WebIndex.Term
{
    /// <summary>
    /// Matches the tokens of a query against the terms of a reverse term index.
    /// </summary>
    /// <remarks>
    /// Without a distance and outside of a phrase search every term has to occur somewhere in a
    /// document. A phrase search requires the terms in query order, each within the distance of
    /// the place the previous one leads to expect it; a proximity search - a distance without a
    /// phrase - requires each term within the distance of the previous one. A similarity applies
    /// to every term in all three, so a fuzzy query does not turn exact once a distance is added.
    /// The result is cut to the maximum number only when it is complete: cutting earlier would
    /// drop documents that a later term would have confirmed.
    /// The index in memory and the one on disk follow these rules alike and differ only in how
    /// their term trees hand out postings, so the rules are kept here once and each tree is
    /// reached through the delegates.
    /// </remarks>
    /// <typeparam name="TPosting">The posting type of the term tree.</typeparam>
    /// <param name="vocabulary">Returns every term of the index.</param>
    /// <param name="postings">Returns the postings of a term.</param>
    /// <param name="documentOf">Returns the document id of a posting.</param>
    /// <param name="positionsOf">Returns the positions at which the term of a posting occurs in its document.</param>
    /// <param name="retrieve">Returns the document ids of a term, with wildcards resolved by the term tree.</param>
    internal sealed class IndexTermMatcher<TPosting>
    (
        Func<IEnumerable<string>> vocabulary,
        Func<string, IEnumerable<TPosting>> postings,
        Func<TPosting, Guid> documentOf,
        Func<TPosting, IEnumerable<uint>> positionsOf,
        Func<string, IndexRetrieveOptions, IEnumerable<Guid>> retrieve
    )
        where TPosting : class
    {
        /// <summary>
        /// Retrieves the documents that match the tokens of a query.
        /// </summary>
        /// <param name="tokens">The tokens of the query.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>A distinct set of matching document ids.</returns>
        public IEnumerable<Guid> Retrieve(IEnumerable<IndexTermToken> tokens, IndexRetrieveOptions options)
        {
            var query = tokens
                .Where(x => !string.IsNullOrEmpty(x.Value?.ToString()))
                .ToList();

            if (query.Count == 0)
            {
                return [];
            }

            var documents = options.Method == IndexRetrieveMethod.Phrase || options.Distance > 0
                ? RetrievePositional(query, options)
                : RetrieveAll(query, options);

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

            // the postings of every token are read from the term tree once per query; the search
            // below tries many placements per document and would otherwise walk them at each step
            var occurrences = tokens
                .Select(x => Occurrences(ResolveTerms(x.Value.ToString(), options)))
                .ToList();

            var documents = new HashSet<Guid>();

            foreach (var (document, starts) in occurrences[0])
            {
                // whether the tokens from an index on can follow a position does not depend on
                // the path that led there, so a placement that failed once is not tried again
                var failed = new HashSet<(int, uint)>();

                if (starts.Any(start => IsArranged(start, 1)))
                {
                    documents.Add(document);
                }

                bool IsArranged(uint position, int index)
                {
                    if (index >= tokens.Count)
                    {
                        return true;
                    }

                    if (failed.Contains((index, position)) ||
                        !occurrences[index].TryGetValue(document, out var candidates))
                    {
                        return false;
                    }

                    var (lower, upper) = Window(tokens, index, position, phrase, options.Distance);

                    foreach (var next in candidates)
                    {
                        if (next > upper)
                        {
                            break;
                        }

                        if (next >= lower && IsArranged(next, index + 1))
                        {
                            return true;
                        }
                    }

                    failed.Add((index, position));

                    return false;
                }
            }

            return documents;
        }

        /// <summary>
        /// Collects the positions at which any of the given terms occurs, per document.
        /// </summary>
        /// <param name="terms">The index terms a token stands for.</param>
        /// <returns>The ascending positions per document id.</returns>
        private Dictionary<Guid, uint[]> Occurrences(IReadOnlyList<string> terms)
        {
            var occurrences = new Dictionary<Guid, SortedSet<uint>>();

            foreach (var posting in terms.SelectMany(postings).Where(x => x is not null))
            {
                var document = documentOf(posting);

                if (!occurrences.TryGetValue(document, out var positions))
                {
                    positions = [];
                    occurrences.Add(document, positions);
                }

                positions.UnionWith(positionsOf(posting) ?? []);
            }

            return occurrences.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }

        /// <summary>
        /// Returns the range of positions in which the token at the given index may follow the
        /// previous token of the query.
        /// </summary>
        /// <param name="tokens">The tokens of the query.</param>
        /// <param name="index">The index of the token to place next.</param>
        /// <param name="position">The position at which the previous token occurs.</param>
        /// <param name="phrase">True for a phrase search, false for a proximity search.</param>
        /// <param name="distance">The allowed distance.</param>
        /// <returns>The lowest and the highest permitted position.</returns>
        private static (uint Lower, uint Upper) Window(IReadOnlyList<IndexTermToken> tokens, int index, uint position, bool phrase, uint distance)
        {
            if (phrase)
            {
                // the next token is expected as far behind the previous one as in the query
                var gap = tokens[index].Position >= tokens[index - 1].Position
                    ? tokens[index].Position - tokens[index - 1].Position
                    : 0u;

                return (Clamp(position + (ulong)gap), Clamp(position + (ulong)gap + distance));
            }

            return (position >= distance ? position - distance : 0u, Clamp(position + (ulong)distance));
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

                return [.. vocabulary().Where(x => IndexFuzzy.CalculateLevenshteinSimilarity(term, x) >= threshold)];
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
                return ResolveTerms(term, options)
                    .SelectMany(postings)
                    .Where(x => x is not null)
                    .Select(documentOf);
            }

            return retrieve(term, options);
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
