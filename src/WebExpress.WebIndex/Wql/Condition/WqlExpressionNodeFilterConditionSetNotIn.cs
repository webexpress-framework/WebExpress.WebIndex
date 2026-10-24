using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition for a "not in" set test (<c>NOT IN</c>): matches items whose attribute is
    /// none of the values in the given set. Items whose attribute is null remain in the result.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionSetNotIn<TIndexItem> : WqlExpressionNodeFilterConditionSet<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionSetNotIn()
            : base("not in")
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
            return TryRetrieveAnyEqual(indexDocument, out var ids)
                ? Complement(indexDocument, ids)
                : Scan(indexDocument);
        }

        /// <summary>
        /// Builds a LINQ expression representing a "NOT IN" set-membership comparison
        /// between the attribute expression and the list of parameter values.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>The negated disjunction of the equalities with every value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Not(ToAnyEqualExpression(param));
        }
    }
}
