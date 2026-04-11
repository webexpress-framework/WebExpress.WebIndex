using System;
using System.Linq;
using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the year of the current date, optionally offset.
    /// Usage: <c>attribute = year()</c> or <c>attribute = year(-1)</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionYear<TIndexItem> : WqlExpressionNodeFilterFunction<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionYear()
            : base("year")
        {
        }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The year as a double value.</returns>
        public override object Execute()
        {
            var parameters = Parameters?.Select(x => x.GetValue());
            var param = parameters?.FirstOrDefault();

            var year = DateTime.Now.Year;

            if (param is not null)
            {
                year += Convert.ToInt32(param);
            }

            return (double)year;
        }

        /// <summary>
        /// Builds a LINQ expression that returns the year as a constant.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>A constant expression with the year value.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            return Expression.Constant(Execute());
        }
    }
}
