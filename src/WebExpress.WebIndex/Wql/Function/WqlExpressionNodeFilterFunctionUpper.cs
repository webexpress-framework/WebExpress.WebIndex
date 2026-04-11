using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that converts a string parameter to uppercase.
    /// Usage: <c>attribute = upper('value')</c>
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionUpper<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionUpper()
            : base("upper")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public override object Execute()
        {
            var parameters = Parameters?.Select(x => x.GetValue());
            var param = parameters?.FirstOrDefault();

            return param?.ToString()?.ToUpperInvariant();
        }

        /// <summary>
        /// Builds a LINQ expression that calls <c>string.ToUpperInvariant()</c> on the
        /// first parameter value.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>A constant expression with the uppercased string value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Constant(Execute());
        }
    }
}
