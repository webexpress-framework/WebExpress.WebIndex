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
        private static readonly System.Reflection.MethodInfo EvaluateMethod = typeof(WqlExpressionNodeFilterFunctionUpper<TIndexItem>)
            .GetMethod(nameof(Evaluate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("Failed to resolve upper() runtime evaluator.");

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
        /// <param name="param">The parameter expression representing the current index item.</param>
        /// <returns>An expression that uppercases the first parameter value at query evaluation time.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var valueExpression = Parameters?.FirstOrDefault()?.ToExpression(param) ?? Expression.Constant(null);
            return Expression.Call(EvaluateMethod, Expression.Convert(valueExpression, typeof(object)));
        }

        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        private static string Evaluate(object value)
        {
            return value?.ToString()?.ToUpperInvariant();
        }
    }
}
