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
        /// Builds a LINQ expression that returns the current year at query evaluation time.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>An expression that evaluates the current year, optionally offset by years.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var nowProperty = typeof(DateTime).GetProperty(nameof(DateTime.Now))
                ?? throw new InvalidOperationException("DateTime.Now property not found.");
            var yearProperty = typeof(DateTime).GetProperty(nameof(DateTime.Year))
                ?? throw new InvalidOperationException("DateTime.Year property not found.");
            var addYearsMethod = typeof(DateTime).GetMethod(nameof(DateTime.AddYears), [typeof(int)])
                ?? throw new InvalidOperationException("DateTime.AddYears method not found.");

            Expression dateExpression = Expression.Property(null, nowProperty);
            var offset = Parameters?.Select(x => x.GetValue()).FirstOrDefault();

            if (offset is not null)
            {
                dateExpression = Expression.Call(dateExpression, addYearsMethod, Expression.Constant(Convert.ToInt32(offset)));
            }

            return Expression.Convert(Expression.Property(dateExpression, yearProperty), typeof(double));
        }
    }
}
