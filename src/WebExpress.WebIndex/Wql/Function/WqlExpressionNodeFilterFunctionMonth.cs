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
        /// Builds a LINQ expression that returns the current month at query evaluation time.
        /// </summary>
        /// <param name="param">The parameter expression (unused for this function).</param>
        /// <returns>An expression that evaluates the current month, optionally offset by months.</returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            var nowProperty = typeof(DateTime).GetProperty(nameof(DateTime.Now))
                ?? throw new InvalidOperationException("Failed to resolve DateTime.Now property during month() expression tree construction.");
            var monthProperty = typeof(DateTime).GetProperty(nameof(DateTime.Month))
                ?? throw new InvalidOperationException("Failed to resolve DateTime.Month property during month() expression tree construction.");
            var addMonthsMethod = typeof(DateTime).GetMethod(nameof(DateTime.AddMonths), [typeof(int)])
                ?? throw new InvalidOperationException("Failed to resolve DateTime.AddMonths method during month() expression tree construction.");

            Expression dateExpression = Expression.Property(null, nowProperty);
            var offset = Parameters?.Select(x => x.GetValue()).FirstOrDefault();

            if (offset is not null)
            {
                dateExpression = Expression.Call(dateExpression, addMonthsMethod, Expression.Constant(Convert.ToInt32(offset)));
            }

            return Expression.Convert(Expression.Property(dateExpression, monthProperty), typeof(double));
        }
    }
}
