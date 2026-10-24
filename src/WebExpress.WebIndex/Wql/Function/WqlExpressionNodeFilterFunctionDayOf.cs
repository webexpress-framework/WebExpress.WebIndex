using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the day of the month of a point in time.
    /// Usage: <c>dayof(Date) = 1</c>. Unlike <c>day()</c>, which returns the current date, it
    /// reads the day of its argument.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionDayOf<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDayOf()
            : base("dayof", 1, 1)
        {
        }

        /// <summary>
        /// Reads the day of the month.
        /// </summary>
        /// <param name="arguments">The point in time.</param>
        /// <returns>The day from 1 to 31, or null if the argument is no date.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var date = ToDateTime(Argument(arguments, 0));

            return date.HasValue ? (double)date.Value.Day : null;
        }
    }
}
