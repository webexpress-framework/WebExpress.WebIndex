using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the month of the current date, optionally offset.
    /// Usage: <c>attribute = month()</c> or <c>attribute = month(-1)</c>. A missing or
    /// non-numeric offset counts as no shift.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionMonth<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionMonth()
            : base("month", 0, 1)
        {
        }

        /// <summary>
        /// Computes the month of the current date, shifted by the offset in months.
        /// </summary>
        /// <param name="arguments">The optional offset in months.</param>
        /// <returns>The month as a double value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var date = DateTime.Now;
            var offset = ToNumber(Argument(arguments, 0));

            if (offset.HasValue)
            {
                date = date.AddMonths(Convert.ToInt32(offset.Value));
            }

            return (double)date.Month;
        }
    }
}
