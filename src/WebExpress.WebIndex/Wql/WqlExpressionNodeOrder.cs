using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the order expression node of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeOrder<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the order attribute expressions.
        /// </summary>
        public IReadOnlyList<WqlExpressionNodeOrderAttribute<TIndexItem>> Attributes { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeOrder()
        {
        }

        /// <summary>
        /// Applies the filter to the unfiltered data object.
        /// </summary>
        /// <param name="unfiltered">The unfiltered data.</param>
        /// <returns>The filtered data.</returns>
        public IQueryable<TIndexItem> Apply(IQueryable<TIndexItem> unfiltered)
        {
            var filtered = unfiltered;

            foreach (var attribute in Attributes)
            {
                filtered = attribute.Apply(filtered);
            }

            return filtered.AsQueryable();
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("order by {0}", string.Join(", ", Attributes)).Trim();
        }
    }
}