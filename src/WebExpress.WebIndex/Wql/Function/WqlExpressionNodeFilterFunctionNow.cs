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
        /// which returns the current date and time.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree. This function does not use the parameter directly,
        /// but it is required to satisfy the expression node contract.
        /// </param>
        /// <returns>
        /// A constant expression containing the current date and time.
        /// </returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            // evaluate the function using the existing Execute() logic
            var result = Execute();

            // wrap the result in a constant expression
            return Expression.Constant(result, typeof(DateTime));
        }

    }
}