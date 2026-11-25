using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Provides a reverse index for numeric values that manages the data in the main memory.
    /// Key: The terms.
    /// Value: The index item.
    /// </summary>
    /// <param name="context">The index context.</param>
    /// <param name="field">The field that makes up the index.</param>
    /// <param name="culture">The culture.</param>
    public class IndexMemoryReverseNumeric<TIndexItem> : IndexMemoryReverse<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the root term.
        /// </summary>
        public IndexMemorySegmentNumericNode Numeric { get; private set; } = new();

        /// <summary>
        /// Gets all items.
        /// </summary>
        public override IEnumerable<Guid> All => Numeric.All.Distinct();

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <param name="field">The field that makes up the index.</param>
        /// <param name="culture">The culture.</param>
        public IndexMemoryReverseNumeric(IIndexDocumemntContext context, IndexFieldData field, CultureInfo culture)
            : base(context, field, culture)
        {
        }

        /// <summary>
        /// Adds an item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        public override void Add(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item);
            var terms = Context.TokenAnalyzer.Analyze(Convert.ToString(value, Culture), Culture);

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
                if (IndexNumericConverter.TryToDecimal(term?.Value, Culture, out var value))
                {
                    Numeric = Numeric.AddAndBalance(item.Id, value);
                }
            }
        }

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the field.</param>
        public override void Delete(TIndexItem item)
        {
            var value = Field.GetPropertyValue(item);
            var terms = Context.TokenAnalyzer.Analyze(Convert.ToString(value, Culture), Culture);

            Delete(item, terms);
        }

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the field.</param>
        /// <param name="terms">The terms to add to the reverse index for the given item.</param>
        public override void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            // nothing was removed, so a deleted or changed document was still found by the value it
            // had - a range query returning records that no longer hold it, or no longer exist
            foreach (var term in terms)
            {
                if (IndexNumericConverter.TryToDecimal(term?.Value, Culture, out var value))
                {
                    Numeric.Remove(item.Id, value);
                }
            }
        }

        /// <summary>
        /// Removed all data from the index.
        /// </summary>
        public override void Clear()
        {
            Numeric = new IndexMemorySegmentNumericNode();
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
            if (IndexNumericConverter.TryToDecimal(input, Culture, out var value))
            {
                return options.Method switch
                {
                    IndexRetrieveMethod.Phrase => Numeric.Retrieve(value, options),
                    IndexRetrieveMethod.GreaterThan => Numeric.Retrieve(value, options),
                    IndexRetrieveMethod.GreaterThanOrEqual => Numeric.Retrieve(value, options),
                    IndexRetrieveMethod.LessThan => Numeric.Retrieve(value, options),
                    IndexRetrieveMethod.LessThanOrEqual => Numeric.Retrieve(value, options),
                    _ => []
                };
            }

            return [];
        }
    }
}
