using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition that checks if a property value is greater than a specified value.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryGreaterThan<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryGreaterThan()
            : base(">")
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
            // get attribute
            var attribute = indexDocument.Fields
                .FirstOrDefault(x => x.Name.Equals(Attribute.Name, StringComparison.OrdinalIgnoreCase));

            if (attribute == null || Parameter == null)
            {
                return [];
            }

            // get the reverse index for the attribute
            var reverseIndex = indexDocument.GetReverseIndex(attribute);

            // get the value for comparison
            var value = Parameter.GetValue();

            // get all matching guids where the attribute value is greater than the given value
            return reverseIndex?.Retrieve(value, new IndexRetrieveOptions
            {
                Method = IndexRetrieveMethod.GreaterThan,
                Distance = Options.Distance ?? 0
            }) ?? [];
        }

        /// <summary>
        /// Builds a LINQ expression representing a "greater than" comparison between 
        /// the attribute expression and the parameter expression.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Property &gt; value</c>).
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

            return Expression.GreaterThan(left, right);
        }
    }
}
