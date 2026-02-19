using System;
using System.Linq;
using System.Linq.Expressions;
using WebExpress.WebIndex.Queries;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary 'LIKE' condition in a WQL expression.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLike<TIndexItem> : WqlExpressionNodeFilterConditionBinary<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="op">The operator.</param>
        public WqlExpressionNodeFilterConditionBinaryLike()
            : base("~")
        {
        }

        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <returns>The data ids from the index.</returns>
        public override IQueryable<Guid> Apply()
        {
            var value = Parameter.GetValue();

            return Attribute.ReverseIndex?.Retrieve(value?.ToString(), new IndexRetrieveOptions()
            {
                Method = IndexRetrieveMethod.Default,
                Distance = Options.Distance ?? 0
            }).AsQueryable();
        }

        /// <summary>
        /// Builds a LINQ expression representing a string-based "LIKE" comparison between 
        /// the attribute expression and the parameter
        /// expression.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree (e.g., <c>x</c> in <c>x => x.Property.Contains(value)</c>).
        /// </param>
        /// <returns>
        /// A method call expression on the attribute value using the parameter value 
        /// as the argument.
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

            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]);

            return Expression.Call(left, containsMethod, right);
        }
    }
}
