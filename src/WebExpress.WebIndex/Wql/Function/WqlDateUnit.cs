using System;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// The units in which <c>dateadd</c> shifts and <c>datediff</c> measures a point in time.
    /// </summary>
    internal enum WqlDateUnit
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    /// <summary>
    /// Reads and applies the date units of the WQL date functions.
    /// </summary>
    internal static class WqlDateUnits
    {
        /// <summary>
        /// Reads a unit from its name, without regard to case.
        /// </summary>
        /// <param name="value">The name of the unit, such as <c>day</c>.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>True if the name denotes a unit, otherwise false.</returns>
        public static bool TryParse(object value, out WqlDateUnit unit)
        {
            unit = default;

            // a number would otherwise be read as the ordinal of a unit
            return value is string name &&
                !int.TryParse(name, out _) &&
                Enum.TryParse(name.Trim(), true, out unit) &&
                Enum.IsDefined(unit);
        }

        /// <summary>
        /// Shifts a point in time by a number of units.
        /// </summary>
        /// <param name="date">The point in time.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="amount">The number of units; a fraction is truncated.</param>
        /// <returns>The shifted point in time, or null if it lies outside the range of a date.</returns>
        public static DateTime? Add(DateTime date, WqlDateUnit unit, double amount)
        {
            var count = Math.Truncate(amount);

            try
            {
                return unit switch
                {
                    WqlDateUnit.Year => date.AddYears(checked((int)count)),
                    WqlDateUnit.Month => date.AddMonths(checked((int)count)),
                    WqlDateUnit.Day => date.AddDays(count),
                    WqlDateUnit.Hour => date.AddHours(count),
                    WqlDateUnit.Minute => date.AddMinutes(count),
                    _ => date.AddSeconds(count)
                };
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Counts the unit boundaries crossed between two points in time, as SQL counts them:
        /// from 31 December to 1 January is one year, although only a day has passed.
        /// </summary>
        /// <param name="start">The earlier point in time.</param>
        /// <param name="end">The later point in time; an earlier one yields a negative count.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>The number of boundaries crossed.</returns>
        public static double Difference(DateTime start, DateTime end, WqlDateUnit unit)
        {
            return unit switch
            {
                WqlDateUnit.Year => end.Year - start.Year,
                WqlDateUnit.Month => (end.Year - start.Year) * 12 + end.Month - start.Month,
                WqlDateUnit.Day => Truncate(end, TimeSpan.TicksPerDay) - Truncate(start, TimeSpan.TicksPerDay),
                WqlDateUnit.Hour => Truncate(end, TimeSpan.TicksPerHour) - Truncate(start, TimeSpan.TicksPerHour),
                WqlDateUnit.Minute => Truncate(end, TimeSpan.TicksPerMinute) - Truncate(start, TimeSpan.TicksPerMinute),
                _ => Truncate(end, TimeSpan.TicksPerSecond) - Truncate(start, TimeSpan.TicksPerSecond)
            };
        }

        /// <summary>
        /// Returns the number of whole units since the start of the calendar.
        /// </summary>
        /// <param name="date">The point in time.</param>
        /// <param name="ticksPerUnit">The length of the unit in ticks.</param>
        /// <returns>The number of whole units.</returns>
        private static long Truncate(DateTime date, long ticksPerUnit)
        {
            return date.Ticks / ticksPerUnit;
        }
    }
}
