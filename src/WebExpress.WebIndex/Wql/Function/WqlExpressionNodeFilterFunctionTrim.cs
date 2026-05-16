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
        private static readonly System.Reflection.MethodInfo EvaluateMethod = typeof(WqlExpressionNodeFilterFunctionTrim<TIndexItem>)
            .GetMethod(nameof(Evaluate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("Failed to resolve trim() runtime evaluator.");

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
        /// Builds a LINQ expression that returns the trimmed first parameter value.
        /// </summary>
        /// <param name="param">The parameter expression representing the current index item.</param>
        /// <returns>An expression that trims the first parameter value at query evaluation time.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var valueExpression = Parameters?.FirstOrDefault()?.ToExpression(param) ?? Expression.Constant(null);
            return Expression.Call(EvaluateMethod, Expression.Convert(valueExpression, typeof(object)));
        }

        private static string Evaluate(object value)
        {
            return value?.ToString()?.Trim();
        }
    }
}
