using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        /// Builds the corresponding expression tree for this binary filter.
        /// </summary>
        /// <param name="parameter">The parameter representing the index item in the expression.</param>
        /// <returns>The composed expression matching the binary filter (AND/OR).</returns>
        public override Expression ToExpression(ParameterExpression parameter)
        {
            // recursively get expressions from both subtrees
            var left = LeftFilter.ToExpression(parameter);
            var right = RightFilter.ToExpression(parameter);

            switch (LogicalOperator)
            {
                case WqlExpressionLogicalOperator.And:
                    {
                        return Expression.AndAlso(left, right);
                    }
                case WqlExpressionLogicalOperator.Or:
                    {
                        return Expression.OrElse(left, right);
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unsupported logical operator: {LogicalOperator}");
                    }
            }
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