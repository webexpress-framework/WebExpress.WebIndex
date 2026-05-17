using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Implements a reverse index for terms persisted on disk.
    /// </summary>
    /// <typeparam name="TIndexItem">The data type implementing IIndexItem.</typeparam>
    public class IndexStorageReverseTerm<TIndexItem> : IndexStorageReverse<TIndexItem>, IIndexStorage
        where TIndexItem : IIndexItem
    {
        private readonly string _extentions = "wrt";
        private readonly int _version = 1;

        /// <summary>
        /// Gets the term tree root segment.
        /// </summary>
        public IndexStorageSegmentTerm Term { get; private set; }

        /// <summary>
        /// Gets all document ids contained in the reverse index.
        /// </summary>
        public override IEnumerable<Guid> All => Term.All.Distinct();

        /// <summary>
        /// Initializes a new instance of the reverse term storage.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <param name="field">The index field definition.</param>
        /// <param name="culture">The culture.</param>
        public IndexStorageReverseTerm(IIndexDocumemntContext context, IndexFieldData field, CultureInfo culture)
            : base(context, field, culture)
        {
            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{Field.Name}.{_extentions}");

            var exists = File.Exists(FileName);

            IndexFile = new IndexStorageFile(FileName);
            Header = new IndexStorageSegmentHeader(new IndexStorageContext(this))
            {
                Identifier = _extentions,
                Version = (byte)_version
            };
            Allocator = new IndexStorageSegmentAllocatorReverseIndex(new IndexStorageContext(this));
            Statistic = new IndexStorageSegmentStatistic(new IndexStorageContext(this));
            Term = new IndexStorageSegmentTerm(new IndexStorageContext(this));

            Header.Initialization(exists);
            Statistic.Initialization(exists);
            Term.Initialization(exists);
            Allocator.Initialization(exists);

            IndexFile.Flush();
        }

        /// <summary>
        /// Adds a single item to the reverse index.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public override void Add(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item)?.ToString();
            var terms = Context.TokenAnalyzer.Analyze(value, Culture);

            Add(item, terms);
        }

        /// <summary>
        /// Adds the specified terms of an item to the reverse index.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="terms">The tokenized terms of the item.</param>
        public override void Add(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            foreach (var term in terms)
            {
                // add term posting and position if available
                Term.Add(term.Value.ToString())?
                    .AddPosting(item.Id)?
                    .AddPosition(term.Position);

                Statistic.Count++;
                IndexFile.Write(Statistic);
            }
        }

        /// <summary>
        /// Deletes a single item from the reverse index.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        public override void Delete(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item)?.ToString();
            var terms = Context.TokenAnalyzer.Analyze(value?.ToString(), Culture);

            Delete(item, terms);
        }

        /// <summary>
        /// Deletes the specified terms of an item from the reverse index.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        /// <param name="terms">The tokenized terms of the item.</param>
        public override void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            foreach (var term in terms)
            {
                var node = Term[term.Value.ToString()];

                if (node is not null)
                {
                    if (node.RemovePosting(item.Id))
                    {
                        Statistic.Count--;
                        IndexFile.Write(Statistic);
                    }
                }
            }
        }

        /// <summary>
        /// Clears and reinitializes the reverse index storage.
        /// </summary>
        public override void Clear()
        {
            IndexFile.NextFreeAddr = 0;
            IndexFile.InvalidationAll();
            IndexFile.Flush();

            Header = new IndexStorageSegmentHeader(new IndexStorageContext(this)) { Identifier = _extentions, Version = (byte)_version };
            Allocator = new IndexStorageSegmentAllocatorReverseIndex(new IndexStorageContext(this));
            Statistic = new IndexStorageSegmentStatistic(new IndexStorageContext(this));
            Term = new IndexStorageSegmentTerm(new IndexStorageContext(this));

            Header.Initialization(false);
            Statistic.Initialization(false);
            Term.Initialization(false);
            Allocator.Initialization(false);

            IndexFile.Flush();
        }

        /// <summary>
        /// Drops the reverse index storage file.
        /// </summary>
        public override void Drop()
        {
            IndexFile.Delete();
        }

        /// <summary>
        /// Retrieves documents for a given input and retrieval options.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>A distinct set of matching document ids.</returns>
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
                        var firstValue = firstTerm?.Value?.ToString();

                        if (string.IsNullOrEmpty(firstValue))
                        {
                            return distinct;
                        }

                        var nextTerms = tokens.Skip(1);

                        foreach (var posting in Term.GetPostings(firstValue))
                        {
                            // positions enumeration may be null; guard with empty
                            foreach (var position in posting.Positions ?? [])
                            {
                                if (CheckForPhraseMatch(posting.DocumentID, position.Position, firstTerm.Position, options.Distance, nextTerms))
                                {
                                    distinct.Add(posting.DocumentID);
                                }
                            }
                        }

                        break;
                    }

                default:
                    {
                        if (options.Distance == 0)
                        {
                            // accumulate results for the first token
                            foreach (var document in tokens.Take(1).SelectMany(x => Term.Retrieve(x.Value.ToString(), options)))
                            {
                                if (distinct.Add(document))
                                {
                                    count++;

                                    if (count >= options.MaxResults)
                                    {
                                        break;
                                    }
                                }
                            }

                            // intersect with the remaining tokens
                            foreach (var normalized in tokens.Skip(1))
                            {
                                var temp = new HashSet<Guid>(distinct.Count);

                                foreach (var document in Term.Retrieve(normalized.Value.ToString(), options))
                                {
                                    if (distinct.Contains(document))
                                    {
                                        temp.Add(document);
                                    }
                                }

                                distinct = temp;
                            }
                        }
                        else
                        {
                            var firstTerm = tokens.Take(1).FirstOrDefault();
                            var firstValue = firstTerm?.Value?.ToString();

                            if (string.IsNullOrEmpty(firstValue))
                            {
                                return distinct;
                            }

                            var nextTerms = tokens.Skip(1);

                            foreach (var posting in Term.GetPostings(firstValue))
                            {
                                foreach (var position in posting.Positions ?? [])
                                {
                                    if (CheckForProximityMatch(posting.DocumentID, position.Position, options.Distance, nextTerms))
                                    {
                                        distinct.Add(posting.DocumentID);
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            return distinct;
        }

        /// <summary>
        /// Checks whether the subsequent terms match exactly in phrase order with allowed distance.
        /// </summary>
        /// <param name="document">The document id.</param>
        /// <param name="position">The current absolute position in the document.</param>
        /// <param name="offset">The relative position of the current token in the query.</param>
        /// <param name="distance">The allowed distance tolerance.</param>
        /// <param name="terms">The remaining terms to match.</param>
        /// <returns>True if the phrase chain matches, otherwise false.</returns>
        private bool CheckForPhraseMatch(Guid document, uint position, uint offset, uint distance, IEnumerable<IndexTermToken> terms)
        {
            if (!terms.Any())
            {
                return true;
            }

            var firstTerm = terms.Take(1).FirstOrDefault();
            var nextTerms = terms.Skip(1);

            // compute base offset safely in uint domain
            var baseOffset = firstTerm.Position >= offset ? firstTerm.Position - offset : 0u;

            // compute bounds with overflow protection
            var minU = position + (ulong)baseOffset;
            var maxU = minU + distance;

            var min = minU > uint.MaxValue ? uint.MaxValue : (uint)minU;
            var max = maxU > uint.MaxValue ? uint.MaxValue : (uint)maxU;

            foreach (var posting in Term.GetPostings(firstTerm.Value.ToString()).Where(x => x?.DocumentID == document))
            {
                foreach (var pos in (posting.Positions ?? [])
                    .Where(x => x.Position >= min && x.Position <= max))
                {
                    // recurse with next term starting from the matched absolute position
                    return CheckForPhraseMatch(posting.DocumentID, pos.Position, firstTerm.Position, distance, nextTerms);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether there is a proximity match within a given distance window.
        /// </summary>
        /// <param name="document">The document id.</param>
        /// <param name="position">The absolute position of the previously matched term.</param>
        /// <param name="distance">The allowed distance tolerance.</param>
        /// <param name="terms">The remaining terms to check.</param>
        /// <returns>True if a proximity match is found, otherwise false.</returns>
        private bool CheckForProximityMatch(Guid document, uint position, uint distance, IEnumerable<IndexTermToken> terms)
        {
            if (!terms.Any())
            {
                return true;
            }

            var firstTerm = terms.Take(1).FirstOrDefault();
            var nextTerms = terms.Skip(1);

            // compute uint-safe bounds around current position
            var lower = position >= distance ? position - distance : 0u;
            var upperU = position + (ulong)distance;
            var upper = upperU > uint.MaxValue ? uint.MaxValue : (uint)upperU;

            foreach (var posting in Term.GetPostings(firstTerm.Value.ToString()).Where(x => x?.DocumentID == document))
            {
                foreach (var pos in (posting.Positions ?? [])
                    .Where(x => x.Position >= lower && x.Position <= upper))
                {
                    // recurse with next term starting from the matched absolute position
                    return CheckForProximityMatch(posting.DocumentID, pos.Position, distance, nextTerms);
                }
            }

            return false;
        }
    }
}