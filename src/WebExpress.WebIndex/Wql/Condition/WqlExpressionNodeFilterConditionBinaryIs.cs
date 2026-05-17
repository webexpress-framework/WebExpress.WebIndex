using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary "is" condition in a WQL expression node.
    /// Supports <c>attribute is null</c> and <c>attribute is true/false</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryIs<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryIs()
            : base("is")
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

            var paramValue = Parameter.GetValue()?.ToString()?.ToLower();

            if (paramValue == "null")
            {
                return indexDocument.All
                    .Where(x => attribute.GetPropertyValue(x) == null)
                    .Select(x => x.Id);
            }

            // for boolean "is true" / "is false"
            var reverseIndex = indexDocument?.GetReverseIndex(attribute);
            return reverseIndex?.Retrieve(paramValue, new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.Phrase,
                Distance = 0
            }) ?? [];
        }

        /// <summary>
        /// Builds a LINQ expression representing an "is" comparison.
        /// For <c>null</c>, generates <c>x.Property == null</c>.
        /// For boolean values, generates <c>x.Property == true/false</c>.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree.
        /// </param>
        /// <returns>
        /// A binary expression comparing the attribute value to the parameter value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <c>Attribute</c> or <c>Parameter</c> is <c>null</c>.
        /// </exception>
        public override Expression ToExpression(ParameterExpression param)
        {
            ArgumentNullException.ThrowIfNull(Attribute);
            ArgumentNullException.ThrowIfNull(Parameter);

            Expression left = Attribute.ToExpression(param);
            var paramValue = Parameter.GetValue()?.ToString()?.ToLower();

            if (paramValue == "null")
            {
                return Expression.Equal(left, Expression.Constant(null, left.Type));
            }

            Expression right = Parameter.ToExpression(param);
            return Expression.Equal(left, right);
        }
    }
}
