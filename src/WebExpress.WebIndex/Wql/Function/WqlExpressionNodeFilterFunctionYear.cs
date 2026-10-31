using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the year of the current date, optionally offset.
    /// Usage: <c>attribute = year()</c> or <c>attribute = year(-1)</c>. A missing or
    /// non-numeric offset counts as no shift.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionYear<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionYear()
            : base("year", 0, 1)
        {
        }

        /// <summary>
        /// Computes the year of the current date, shifted by the offset in years.
        /// </summary>
        /// <param name="arguments">The optional offset in years.</param>
        /// <returns>The year as a double value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var year = DateTime.Now.Year;
            var offset = ToNumber(Argument(arguments, 0));

            if (offset.HasValue)
            {
                year += Convert.ToInt32(offset.Value);
            }

            return (double)year;
        }
    }
}
