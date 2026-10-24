using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition <c>between</c>: matches items whose attribute lies within the bounds,
    /// both included, as in <c>Date between date('2024-01-01') and date('2024-12-31')</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionRangeBetween<TIndexItem> : WqlExpressionNodeFilterConditionRange<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionRangeBetween()
            : base("between")
        {
        }

        /// <summary>
        /// Applies the filter condition to the index using the specified attribute
        /// and returns the matching data identifiers.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>
        /// A sequence of data identifiers that satisfy the filter condition.
        /// </returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            return TryRetrieveWithin(indexDocument, out var ids) ? ids : Scan(indexDocument);
        }

        /// <summary>
        /// Builds the test whether the attribute lies within the bounds.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>A boolean expression.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return ToWithinExpression(param);
        }
    }
}
