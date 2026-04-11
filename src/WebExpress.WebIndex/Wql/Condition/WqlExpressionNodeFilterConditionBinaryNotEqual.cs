using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary not-equal condition in a WQL expression node.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryNotEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryNotEqual()
            : base("!=")
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
            var attribute = indexDocument.Fields
                .FirstOrDefault(x => x.Name.Equals(Attribute.Name, StringComparison.OrdinalIgnoreCase));

            if (attribute == null || Parameter == null)
            {
                return [];
            }

            var reverseIndex = indexDocument?.GetReverseIndex(attribute);
            var value = Parameter.GetValue()?.ToString();

            // get matching ids for the value and then exclude them from all ids
            var matchingIds = reverseIndex?.Retrieve(value, new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.Phrase,
                Distance = Options.Distance.HasValue ? Options.Distance.Value : 0
            }) ?? [];

            return indexDocument.All
                .Select(x => x.Id)
                .Except(matchingIds);
        }

        /// <summary>
        /// Builds a LINQ expression representing a not-equal comparison between the
        /// attribute expression and the parameter expression.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Attribute != value</c>).
        /// </param>
        /// <returns>
        /// A binary expression, comparing the attribute value to the parameter value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <c>Attribute</c> or <c>Parameter</c> is <c>null</c>.
        /// </exception>
        public override Expression ToExpression(ParameterExpression param)
        {
            ArgumentNullException.ThrowIfNull(Attribute);
            ArgumentNullException.ThrowIfNull(Parameter);

            Expression left = Attribute.ToExpression(param);
            Expression right = Parameter.ToExpression(param);

            return Expression.NotEqual(left, right);
        }
    }
}
