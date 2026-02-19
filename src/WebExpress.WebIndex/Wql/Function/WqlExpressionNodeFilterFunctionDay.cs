using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// Describes the function expression of a wql statement.
    /// Returns the current date.
    /// </summary>
    public class WqlExpressionNodeFilterFunctionDay<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDay()
            : base("day")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        public override object Execute()
        {
            var parameters = Parameters.Select(x => x.GetValue());
            var param = parameters.FirstOrDefault();

            if (param is not null)
            {
                return DateTime.Now.Date.AddDays(Convert.ToDouble(param));
            }

            return DateTime.Now.Date;
        }

        /// <summary>
        /// Builds a LINQ expression representing the result of the <c>day()</c> function, 
        /// which returns the current date optionally offset
        /// by a specified number of days.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree. This function does not use the parameter directly,
        /// but it is required to satisfy the expression node contract.
        /// </param>
        /// <returns>
        /// A constant expression containing the computed date value.
        /// </returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            // evaluate the function using the existing execute logic
            var result = Execute();

            // wrap the result in a constant expression
            return Expression.Constant(result, typeof(DateTime));
        }
    }
}