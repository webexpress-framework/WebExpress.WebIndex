using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that shifts a point in time by a number of units.
    /// Usage: <c>dateadd('month', 1, Date) &gt; now()</c>. The arguments follow SQL's
    /// <c>DATEADD</c>: the unit (year, month, day, hour, minute or second), the number of
    /// units, which is truncated to an integer, and the point in time.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionDateAdd<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(DateTime?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDateAdd()
            : base("dateadd", 3, 3)
        {
        }

        /// <summary>
        /// Shifts the point in time.
        /// </summary>
        /// <param name="arguments">The unit, the number of units and the point in time.</param>
        /// <returns>The shifted point in time, or null if an argument is invalid or the result lies outside the range of a date.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var amount = ToNumber(Argument(arguments, 1));
            var date = ToDateTime(Argument(arguments, 2));

            if (!WqlDateUnits.TryParse(Argument(arguments, 0), out var unit) || !amount.HasValue || !date.HasValue)
            {
                return null;
            }

            return WqlDateUnits.Add(date.Value, unit, amount.Value);
        }
    }
}
