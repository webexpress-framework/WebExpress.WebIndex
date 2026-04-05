using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the partitioning expression node of a WQL statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodePartitioning<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the partitioning function expressions.
        /// </summary>
        public IReadOnlyList<WqlExpressionNodePartitioningFunction<TIndexItem>> PartitioningFunctions { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodePartitioning()
        {
        }

        /// <summary>
        /// Applies the filter to the unfiltered data object.
        /// </summary>
        /// <param name="unfiltered">The unfiltered data.</param>
        /// <returns>The filtered data.</returns>
        public IQueryable<Q> Apply<Q>(IQueryable<Q> unfiltered)
        {
            var filtered = unfiltered;

            foreach (var function in PartitioningFunctions)
            {
                filtered = function.Apply(filtered);
            }

            return filtered.AsQueryable();
        }

        /// <summary>
        /// Converts the partitioning expression to a string.
        /// </summary>
        /// <returns>The partitioning expression as a string.</returns>
        public override string ToString()
        {
            return string.Format("{0}", string.Join(" ", PartitioningFunctions)).Trim();
        }
    }
}