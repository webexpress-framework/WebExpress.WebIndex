using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition for a negated word search (<c>!~</c>): matches items whose text
    /// attribute does not contain the given words as <c>~</c> would find them. Items without
    /// a text remain in the result, since they contain no word.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryNotLike<TIndexItem> : WqlExpressionNodeFilterConditionBinaryLike<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryNotLike()
            : base("!~")
        {
        }

        /// <summary>
        /// Applies the filter condition to the index as the complement of the word search.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>
        /// A sequence of data identifiers that satisfy the filter condition.
        /// </returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            // a scan already evaluates the negated predicate, only index matches need the complement
            return TryRetrieveMatches(indexDocument, out var ids)
                ? Complement(indexDocument, ids)
                : Scan(indexDocument);
        }

        /// <summary>
        /// Builds the negated word search over the left-hand side.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>A boolean expression.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Not(base.ToExpression(param));
        }
    }
}
