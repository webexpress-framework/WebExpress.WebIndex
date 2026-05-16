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
        private static readonly System.Reflection.MethodInfo EvaluateMethod = typeof(WqlExpressionNodeFilterFunctionLen<TIndexItem>)
            .GetMethod(nameof(Evaluate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("Failed to resolve len() runtime evaluator.");

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
        /// Builds a LINQ expression that returns the length of the first parameter value.
        /// </summary>
        /// <param name="param">The parameter expression representing the current index item.</param>
        /// <returns>An expression that evaluates the string length at query evaluation time.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var valueExpression = Parameters?.FirstOrDefault()?.ToExpression(param) ?? Expression.Constant(null);
            return Expression.Call(EvaluateMethod, Expression.Convert(valueExpression, typeof(object)));
        }

        private static double Evaluate(object value)
        {
            return (double)(value?.ToString()?.Length ?? 0);
        }
    }
}
