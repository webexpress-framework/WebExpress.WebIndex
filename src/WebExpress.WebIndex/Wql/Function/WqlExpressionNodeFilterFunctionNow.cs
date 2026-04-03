using System;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// Describes the function expression of a wql statement.
    /// Returns the current date and time.
    /// </summary>
    public class WqlExpressionNodeFilterFunctionNow<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionNow()
            : base("now")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public override object Execute()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// Builds a LINQ expression representing the result of the <c>now()</c> function, 
        /// which returns the current date and time at query evaluation time.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree. This function does not use the parameter directly,
        /// but it is required to satisfy the expression node contract.
        /// </param>
        /// <returns>
        /// A member access expression that reads <c>DateTime.Now</c> each time the 
        /// expression is evaluated, rather than freezing the timestamp at query 
        /// construction time.
        /// </returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var nowProperty = typeof(DateTime).GetProperty(nameof(DateTime.Now))
                ?? throw new InvalidOperationException("DateTime.Now property not found.");
            return Expression.Property(null, nowProperty);
        }

    }
}