using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the month of a point in time.
    /// Usage: <c>monthof(Date) = 12</c>. Unlike <c>month()</c>, which returns the current
    /// month, it reads the month of its argument.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionMonthOf<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionMonthOf()
            : base("monthof", 1, 1)
        {
        }

        /// <summary>
        /// Reads the month.
        /// </summary>
        /// <param name="arguments">The point in time.</param>
        /// <returns>The month from 1 to 12, or null if the argument is no date.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var date = ToDateTime(Argument(arguments, 0));

            return date.HasValue ? (double)date.Value.Month : null;
        }
    }
}
