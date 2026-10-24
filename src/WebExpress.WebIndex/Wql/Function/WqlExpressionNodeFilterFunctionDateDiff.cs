using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that counts the units between two points in time.
    /// Usage: <c>datediff('day', Date, now()) &gt; 30</c>. The arguments follow SQL's
    /// <c>DATEDIFF</c>: the unit (year, month, day, hour, minute or second), the start and the
    /// end. As in SQL, the unit boundaries crossed are counted, not the whole units elapsed.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionDateDiff<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDateDiff()
            : base("datediff", 3, 3)
        {
        }

        /// <summary>
        /// Counts the units between start and end.
        /// </summary>
        /// <param name="arguments">The unit, the start and the end.</param>
        /// <returns>The number of unit boundaries crossed, negative if the end lies before the start, or null if an argument is invalid.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var start = ToDateTime(Argument(arguments, 1));
            var end = ToDateTime(Argument(arguments, 2));

            if (!WqlDateUnits.TryParse(Argument(arguments, 0), out var unit) || !start.HasValue || !end.HasValue)
            {
                return null;
            }

            return WqlDateUnits.Difference(start.Value, end.Value, unit);
        }
    }
}
