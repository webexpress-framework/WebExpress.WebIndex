using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that trims whitespace from a string parameter.
    /// Usage: <c>attribute = trim('  value  ')</c> returns "value".
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionTrim<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionTrim()
            : base("trim")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The trimmed string value.</returns>
        public override object Execute()
        {
            var parameters = Parameters?.Select(x => x.GetValue());
            var param = parameters?.FirstOrDefault();

            return param?.ToString()?.Trim();
        }

        /// <summary>
        /// Builds a LINQ expression that returns the trimmed string as a constant.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>A constant expression with the trimmed string value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Constant(Execute());
        }
    }
}
