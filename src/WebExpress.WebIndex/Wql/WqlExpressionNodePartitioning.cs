using System;
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
        /// Returns the window that the partitioning functions cut out of the result, merged
        /// in the order they are written: <c>take 10 skip 5</c> keeps the items 6 to 10, while
        /// <c>skip 5 take 10</c> keeps the items 6 to 15. Both execution paths apply this one
        /// window, so they page identically.
        /// </summary>
        public (int Skip, int? Take) Window
        {
            get
            {
                var skip = 0L;
                long? take = null;

                foreach (var function in PartitioningFunctions ?? [])
                {
                    if (function.Operator == WqlExpressionNodePartitioningOperator.Skip)
                    {
                        skip += function.Value;
                        take = take.HasValue ? Math.Max(0, take.Value - function.Value) : null;
                    }
                    else if (function.Operator == WqlExpressionNodePartitioningOperator.Take)
                    {
                        take = take.HasValue ? Math.Min(take.Value, function.Value) : function.Value;
                    }
                }

                return ((int)Math.Min(skip, int.MaxValue), take.HasValue ? (int)take.Value : null);
            }
        }

        /// <summary>
        /// Applies the filter to the unfiltered data object.
        /// </summary>
        /// <param name="unfiltered">The unfiltered data.</param>
        /// <returns>The filtered data.</returns>
        public IQueryable<Q> Apply<Q>(IQueryable<Q> unfiltered)
        {
            var (skip, take) = Window;
            var filtered = unfiltered.Skip(skip);

            if (take.HasValue)
            {
                filtered = filtered.Take(take.Value);
            }

            return filtered;
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