using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the current date, optionally shifted by a number of days.
    /// Usage: <c>attribute = day()</c> or <c>attribute = day(-3)</c>. A missing or non-numeric
    /// offset counts as no shift.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionDay<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(DateTime);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionDay()
            : base("day", 0, 1)
        {
        }

        /// <summary>
        /// Computes the current date, shifted by the offset in days.
        /// </summary>
        /// <param name="arguments">The optional offset in days.</param>
        /// <returns>The date at midnight.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return DateTime.Today.AddDays(ToNumber(Argument(arguments, 0)) ?? 0);
        }
    }
}
