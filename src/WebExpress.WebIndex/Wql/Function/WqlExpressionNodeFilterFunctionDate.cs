using System;
using System.Collections.Generic;
using System.Globalization;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that reads a point in time from an ISO 8601 literal.
    /// Usage: <c>Date &gt;= date('2024-01-31')</c> or <c>Date &lt; date('2024-01-31T12:00')</c>.
    /// Only the ISO form is accepted, so that a statement means the same date on every
    /// machine, whatever its culture.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionDate<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private static readonly string[] Formats =
        [
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        ];

        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(DateTime?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDate()
            : base("date", 1, 1)
        {
        }

        /// <summary>
        /// Reads the point in time.
        /// </summary>
        /// <param name="arguments">The ISO 8601 literal, or a point in time that is passed through.</param>
        /// <returns>The point in time, or null if the argument is no ISO date.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return Argument(arguments, 0) switch
            {
                DateTime date => date,
                string text when DateTime.TryParseExact(text.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date,
                _ => null
            };
        }
    }
}
