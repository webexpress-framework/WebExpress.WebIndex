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
        // version 2: posting nodes carry the height of their AVL subtree (one byte more per node)
        private readonly byte _version = 2;
        private readonly IndexTermMatcher<IndexStorageSegmentPostingNode> _matcher;

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
            _matcher = new
            (
                () => Term.Terms.Select(x => x.Item1),
                term => Term.GetPostings(term),
                posting => posting.DocumentID,
                posting => posting.Positions?.Select(x => x.Position),
                (term, options) => Term.Retrieve(term, options)
            );

            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{Field.Name}.{_extentions}");

            DiscardOutdatedFile(_extentions, _version);

            var exists = File.Exists(FileName);

            IndexFile = new IndexStorageFile(FileName);
            IndexFile.Initialize(() =>
            {
                Header = new IndexStorageSegmentHeader(new IndexStorageContext(this))
                {
                    Identifier = _extentions,
                    Version = _version
                };
                Allocator = new IndexStorageSegmentAllocatorReverseIndex(new IndexStorageContext(this));
                Statistic = new IndexStorageSegmentStatistic(new IndexStorageContext(this));
                Term = new IndexStorageSegmentTerm(new IndexStorageContext(this));

                Header.Initialization(exists);
                Statistic.Initialization(exists);
                Term.Initialization(exists);
                Allocator.Initialization(exists);
            });
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

            Header = new IndexStorageSegmentHeader(new IndexStorageContext(this)) { Identifier = _extentions, Version = _version };
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
        /// <remarks>
        /// The matching rules are described at <see cref="IndexTermMatcher{TPosting}"/>.
        /// </remarks>
        /// <param name="input">The input text.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>A distinct set of matching document ids.</returns>
        public override IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options)
        {
            return _matcher.Retrieve(Context.TokenAnalyzer.Analyze(input?.ToString(), Culture, true), options);
        }
    }
}
