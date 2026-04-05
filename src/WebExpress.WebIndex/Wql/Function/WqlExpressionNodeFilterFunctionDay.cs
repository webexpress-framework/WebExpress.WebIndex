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
        /// by a specified number of days, evaluated at query execution time.
        /// </summary>
        /// <param name="param">
        /// The parameter expression representing the index item in the generated
        /// expression tree. This function does not use the parameter directly,
        /// but it is required to satisfy the expression node contract.
        /// </param>
        /// <returns>
        /// An expression that reads <c>DateTime.Today</c> (and optionally calls 
        /// <c>AddDays</c>) each time the expression is evaluated, rather than 
        /// freezing the date at query construction time.
        /// </returns>
        public override Expression ToExpression(ParameterExpression param)
        {
            // DateTime.Today == DateTime.Now.Date
            var todayProperty = typeof(DateTime).GetProperty(nameof(DateTime.Today))
                ?? throw new InvalidOperationException("DateTime.Today property not found.");
            Expression dateExpr = Expression.Property(null, todayProperty);

            // if a day offset parameter was provided, build AddDays call
            var parameters = Parameters?.Select(x => x.GetValue());
            var offsetParam = parameters?.FirstOrDefault();

            if (offsetParam is not null)
            {
                var addDaysMethod = typeof(DateTime).GetMethod(nameof(DateTime.AddDays), [typeof(double)])
                    ?? throw new InvalidOperationException("DateTime.AddDays method not found.");
                var daysConstant = Expression.Constant(Convert.ToDouble(offsetParam));
                dateExpr = Expression.Call(dateExpr, addDaysMethod, daysConstant);
            }

            return dateExpr;
        }
    }
}