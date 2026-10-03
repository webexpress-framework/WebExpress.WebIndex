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
        private readonly IndexTermMatcher<IndexMemorySegmentPosting> _matcher;

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
            _matcher = new
            (
                () => Root.Terms.Select(x => x.Item1),
                term => Root.GetPostings(term),
                posting => posting.DocumentId,
                posting => posting.Positions,
                (term, options) => Root.Retrieve(term, options)
            );
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
