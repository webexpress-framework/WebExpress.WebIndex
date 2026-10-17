using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition for sets in a WQL expression node: matches items whose
    /// attribute equals any of the given values.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionSetIn<TIndexItem> : WqlExpressionNodeFilterConditionSet<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionSetIn()
            : base("in")
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
            return TryRetrieveAnyEqual(indexDocument, out var ids) ? ids : Scan(indexDocument);
        }

        /// <summary>
        /// Builds a LINQ expression representing an "IN" set-membership comparison between
        /// the attribute expression and the list of parameter values.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>The disjunction of the equalities with every value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return ToAnyEqualExpression(param);
        }
    }
}
