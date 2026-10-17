using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The base of the WQL conditions that test a text for a part of characters: <c>starts with</c>,
    /// <c>ends with</c> and <c>contains</c>. Unlike the word search they match any sequence of
    /// characters, also inside a word, without regard to case. A reverse index knows words
    /// only, so these conditions always scan the document store.
    /// </summary>
    /// <param name="token">The operator.</param>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterConditionBinarySubstring<TIndexItem>(string token) : WqlExpressionNodeFilterConditionBinary<TIndexItem>(token)
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns false, since a part of characters has no words to be similar or near.
        /// </summary>
        protected internal override bool AcceptsOptions => false;

        /// <summary>
        /// Returns true, since a part of characters exists in text only.
        /// </summary>
        protected internal override bool RequiresText => true;

        /// <summary>
        /// Applies the filter condition by scanning the document store.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>
        /// A sequence of data identifiers that satisfy the filter condition.
        /// </returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            return Scan(indexDocument);
        }

        /// <summary>
        /// Builds the test of the left-hand side for the part.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>A boolean expression that is false if either side is null.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            if (Function is null)
            {
                ArgumentNullException.ThrowIfNull(Attribute);
            }

            ArgumentNullException.ThrowIfNull(Parameter);

            return Test(LeftToExpression(param), Parameter.ToOperand(param, typeof(string), Culture));
        }

        /// <summary>
        /// Builds the test itself.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="part">The part searched for.</param>
        /// <returns>A boolean expression.</returns>
        protected abstract Expression Test(Expression text, Expression part);
    }
}
