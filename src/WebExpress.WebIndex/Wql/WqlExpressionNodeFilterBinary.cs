using System;
using System.Collections.Generic;
using System.Linq;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the filter expression of a wql statement.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterBinary<TIndexItem> : WqlExpressionNodeFilter<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the left filter expressions.
        /// </summary>
        public WqlExpressionNodeFilter<TIndexItem> LeftFilter { get; internal set; }

        /// <summary>
        /// Returns the logical operator expressions.
        /// </summary>
        public WqlExpressionLogicalOperator LogicalOperator { get; internal set; }

        /// <summary>
        /// Returns the right filter expressions.
        /// </summary>
        public WqlExpressionNodeFilter<TIndexItem> RightFilter { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeFilterBinary()
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IEnumerable<Guid> Apply()
        {
            var filtered = Enumerable.Empty<Guid>();
            var leftFiltered = LeftFilter.Apply();
            var rightFiltered = RightFilter.Apply();

            switch (LogicalOperator)
            {
                case WqlExpressionLogicalOperator.And:
                    filtered = leftFiltered.Intersect(rightFiltered);
                    break;

                case WqlExpressionLogicalOperator.Or:
                    filtered = leftFiltered.Union(rightFiltered);
                    break;
                default:
                    break;
            }

            return filtered;
        }

        /// <summary>
        /// Applies the current filter condition to the specified query and returns the 
        /// resulting query.
        /// </summary>
        /// <param name="query">
        /// The query to which the filter condition will be applied. This parameter must 
        /// not be null.
        /// </param>
        /// <returns>
        /// An <see cref="IQuery{TIndexItem}"/> representing the filtered query if a 
        /// condition exists; otherwise, the original query.
        /// </returns>
        public override IQuery<TIndexItem> Apply(IQuery<TIndexItem> query)
        {
            ArgumentNullException.ThrowIfNull(query);

            // apply the left and right filters to the query
            var leftQuery = LeftFilter?.Apply(query) ?? query;
            var rightQuery = RightFilter?.Apply(query) ?? query;

            switch (LogicalOperator)
            {
                case WqlExpressionLogicalOperator.And:
                    // combine the queries with AND logic
                    return CombineQueriesWithAnd(leftQuery, rightQuery);

                case WqlExpressionLogicalOperator.Or:
                    // combine the queries with OR logic
                    return CombineQueriesWithOr(leftQuery, rightQuery);

                default:
                    throw new InvalidOperationException($"Unsupported logical operator: {LogicalOperator}");
            }
        }

        /// <summary>
        /// Combines two queries using AND logic.
        /// </summary>
        /// <param name="leftQuery">The left query.</param>
        /// <param name="rightQuery">The right query.</param>
        /// <returns>The resulting query.</returns>
        private IQuery<TIndexItem> CombineQueriesWithAnd(IQuery<TIndexItem> leftQuery, IQuery<TIndexItem> rightQuery)
        {
            // merge filters from both queries using AND logic
            var combinedFilters = leftQuery.Filters.Intersect(rightQuery.Filters);
            var newQuery = new Query<TIndexItem>().Where(combinedFilters.ToArray());
            return newQuery;
        }

        /// <summary>
        /// Combines two queries using OR logic.
        /// </summary>
        /// <param name="leftQuery">The left query.</param>
        /// <param name="rightQuery">The right query.</param>
        /// <returns>The resulting query.</returns>
        private IQuery<TIndexItem> CombineQueriesWithOr(IQuery<TIndexItem> leftQuery, IQuery<TIndexItem> rightQuery)
        {
            // merge filters from both queries using OR logic
            var combinedFilters = leftQuery.Filters.Union(rightQuery.Filters);
            var newQuery = new Query<TIndexItem>().Where(combinedFilters.ToArray());
            return newQuery;
        }

        /// <summary>
        /// Converts the filter expression to a string.
        /// </summary>
        /// <returns>The filter expression as a string.</returns>
        public override string ToString()
        {
            return string.Format
            (
                "({0} {1} {2})",
                LeftFilter,
                LogicalOperator.ToString().ToLower(),
                RightFilter
            ).Trim();
        }
    }
}