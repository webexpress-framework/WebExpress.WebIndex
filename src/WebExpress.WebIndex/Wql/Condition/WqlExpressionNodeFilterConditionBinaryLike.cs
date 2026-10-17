using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition for a word search (<c>~</c>): matches items whose text attribute
    /// contains every given word, anywhere and in any order. The words may carry wildcards,
    /// a similarity searches fuzzily and a distance requires each word to stand near the
    /// previous one.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLike<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns true, since the word search applies to text only.
        /// </summary>
        protected internal override bool RequiresText => true;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryLike()
            : base("~")
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with a different operator, for the negated
        /// word search.
        /// </summary>
        /// <param name="token">The operator.</param>
        protected WqlExpressionNodeFilterConditionBinaryLike(string token)
            : base(token)
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
            return TryRetrieveMatches(indexDocument, out var ids) ? ids : Scan(indexDocument);
        }

        /// <summary>
        /// Retrieves the items the word search finds, if the term index can answer it.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="ids">The identifiers of the items containing the words.</param>
        /// <returns>True if the index answered, false if the condition has to scan.</returns>
        protected bool TryRetrieveMatches(IIndexDocument<TIndexItem> indexDocument, out IEnumerable<Guid> ids)
        {
            ids = null;

            return Parameter is not null && Parameter.IsConstant &&
                TryRetrieveWords(indexDocument, Parameter.GetValue(), Distance, Similarity, out ids);
        }

        /// <summary>
        /// Builds the word search over the left-hand side.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>A boolean expression.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <c>Attribute</c> or <c>Parameter</c> is <c>null</c>.
        /// </exception>
        public override Expression ToExpression(ParameterExpression param)
        {
            if (Function is null)
            {
                ArgumentNullException.ThrowIfNull(Attribute);
            }

            ArgumentNullException.ThrowIfNull(Parameter);

            return WqlExpressionBuilder.MatchText
            (
                LeftToExpression(param),
                Parameter.ToOperand(param, typeof(string), Culture),
                WqlTextMatchMode.Words,
                Distance,
                Similarity,
                Culture
            );
        }
    }
}
