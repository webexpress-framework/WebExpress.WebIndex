using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the year of a point in time.
    /// Usage: <c>yearof(Date) = 2024</c>. Unlike <c>year()</c>, which returns the current
    /// year, it reads the year of its argument.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionYearOf<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionYearOf()
            : base("yearof", 1, 1)
        {
        }

        /// <summary>
        /// Reads the year.
        /// </summary>
        /// <param name="arguments">The point in time.</param>
        /// <returns>The year, or null if the argument is no date.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var date = ToDateTime(Argument(arguments, 0));

            return date.HasValue ? (double)date.Value.Year : null;
        }
    }
}
