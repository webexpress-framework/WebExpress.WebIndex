using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The base of the WQL conditions that compare the left-hand side with one value:
    /// <c>=</c>, <c>!=</c>, <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>is</c> and
    /// <c>is not</c>. Equality on a text attribute is the phrase search of WQL; every other
    /// comparison is typed against the left-hand side by the <see cref="WqlExpressionBuilder"/>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public abstract class WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the comparison the condition performs.
        /// </summary>
        public WqlComparisonOperator Comparison { get; }

        /// <summary>
        /// Returns true if the condition searches its value as a phrase, which equality and
        /// inequality do on a text attribute - unless the value is the literal null, which
        /// asks whether the attribute holds no text at all.
        /// </summary>
        protected bool IsPhrase => IsTextAttribute &&
            Comparison is WqlComparisonOperator.Equal or WqlComparisonOperator.NotEqual &&
            !(Parameter?.Value?.IsNull ?? false);

        /// <summary>
        /// Returns true if the condition gives a meaning to a similarity or a distance, which
        /// only the phrase search does.
        /// </summary>
        protected internal override bool AcceptsOptions => IsPhrase;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="token">One or more tokens that determine the operation.</param>
        /// <param name="comparison">The comparison the condition performs.</param>
        protected WqlExpressionNodeFilterConditionBinaryComparison(string token, WqlComparisonOperator comparison)
            : base(token)
        {
            Comparison = comparison;
        }

        /// <summary>
        /// Applies the filter condition to the index using the specified attribute
        /// and returns the matching data identifiers. The reverse index is asked where it
        /// knows the answer exactly; every other comparison scans the document store.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>
        /// A sequence of data identifiers that satisfy the filter condition.
        /// </returns>
        public override IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument)
        {
            if (Parameter is not null && Parameter.IsConstant)
            {
                var value = Parameter.GetExactValue();
                var distance = IsPhrase ? Distance : 0;
                var similarity = IsPhrase ? Similarity : 0;

                switch (Comparison)
                {
                    case WqlComparisonOperator.Equal when TryRetrieveEqual(indexDocument, value, distance, similarity, out var ids):
                        return ids;
                    case WqlComparisonOperator.NotEqual when TryRetrieveEqual(indexDocument, value, distance, similarity, out var ids):
                        return Complement(indexDocument, ids);
                    case WqlComparisonOperator.LessThan when TryRetrieveNumber(indexDocument, value, IndexRetrieveMethod.LessThan, out var ids):
                        return ids;
                    case WqlComparisonOperator.LessThanOrEqual when TryRetrieveNumber(indexDocument, value, IndexRetrieveMethod.LessThanOrEqual, out var ids):
                        return ids;
                    case WqlComparisonOperator.GreaterThan when TryRetrieveNumber(indexDocument, value, IndexRetrieveMethod.GreaterThan, out var ids):
                        return ids;
                    case WqlComparisonOperator.GreaterThanOrEqual when TryRetrieveNumber(indexDocument, value, IndexRetrieveMethod.GreaterThanOrEqual, out var ids):
                        return ids;
                }
            }

            return Scan(indexDocument);
        }

        /// <summary>
        /// Builds the comparison of the left-hand side with the parameter.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Attribute == value</c>).
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

            var left = LeftToExpression(param);

            if (IsPhrase)
            {
                var match = WqlExpressionBuilder.MatchText
                (
                    left,
                    Parameter.ToOperand(param, typeof(string), Culture),
                    WqlTextMatchMode.Phrase,
                    Distance,
                    Similarity,
                    Culture
                );

                return Comparison == WqlComparisonOperator.Equal ? match : Expression.Not(match);
            }

            return WqlExpressionBuilder.Compare(left, Comparison, Parameter.ToOperand(param, left.Type, Culture), Culture);
        }
    }
}
