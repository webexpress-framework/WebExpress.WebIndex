using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the length of a string parameter.
    /// Usage: <c>attribute = len('value')</c> returns 5.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionLen<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionLen()
            : base("len")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The length of the string parameter, or 0 if null.</returns>
        public override object Execute()
        {
            var parameters = Parameters?.Select(x => x.GetValue());
            var param = parameters?.FirstOrDefault();

            return (double)(param?.ToString()?.Length ?? 0);
        }

        /// <summary>
        /// Builds a LINQ expression that returns the length as a constant.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>A constant expression with the string length.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Constant(Execute());
        }
    }
}
