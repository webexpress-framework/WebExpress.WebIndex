using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the month of the current date, optionally offset.
    /// Usage: <c>attribute = month()</c> or <c>attribute = month(-1)</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionMonth<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionMonth()
            : base("month")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The month as a double value.</returns>
        public override object Execute()
        {
            var parameters = Parameters?.Select(x => x.GetValue());
            var param = parameters?.FirstOrDefault();

            var date = DateTime.Now;

            if (param is not null)
            {
                date = date.AddMonths(Convert.ToInt32(param));
            }

            return (double)date.Month;
        }

        /// <summary>
        /// Builds a LINQ expression that returns the month as a constant.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>A constant expression with the month value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Constant(Execute());
        }
    }
}
