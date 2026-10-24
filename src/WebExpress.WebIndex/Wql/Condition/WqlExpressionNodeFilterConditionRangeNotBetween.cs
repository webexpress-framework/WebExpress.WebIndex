using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition <c>not between</c>: matches items whose attribute lies outside the
    /// bounds. As the complement of <c>between</c>, it keeps the items whose attribute is null.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionRangeNotBetween<TIndexItem> : WqlExpressionNodeFilterConditionRange<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionRangeNotBetween()
            : base("not between")
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
            // a scan already evaluates the negated predicate, only index matches need the complement
            return TryRetrieveWithin(indexDocument, out var ids)
                ? Complement(indexDocument, ids)
                : Scan(indexDocument);
        }

        /// <summary>
        /// Builds the test whether the attribute lies outside the bounds.
        /// </summary>
        /// <param name="param">The parameter expression representing the index item.</param>
        /// <returns>A boolean expression.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Not(ToWithinExpression(param));
        }
    }
}
