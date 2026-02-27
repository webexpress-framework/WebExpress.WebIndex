using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary equal condition in a WQL expression node.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryEqual()
            : base("=")
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
            // find the relevant attribute by name
            var attribute = indexDocument.Fields
                .FirstOrDefault(x => x.Name.Equals(Attribute.Name, StringComparison.OrdinalIgnoreCase));

            if (attribute == null || Parameter == null)
            {
                return [];
            }

            // get the reverse index for the attribute
            var reverseIndex = indexDocument?.GetReverseIndex(attribute);
            var value = Parameter.GetValue()?.ToString();

            // retrieve matching identifiers if reverse index and value exist
            return reverseIndex?.Retrieve(value, new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.Phrase,
                Distance = Options.Distance.HasValue ? Options.Distance.Value : 0
            }) ?? [];
        }

        /// <summary>
        /// Builds a LINQ expression representing an equality comparison between the 
        /// attribute expression and the parameter expression.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Attribute == value</c>).
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

            return Expression.Equal(left, right);
        }
    }
}
