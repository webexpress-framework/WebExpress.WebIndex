using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that rounds a number, optionally to a number of decimal places.
    /// Usage: <c>round(Price) = 13</c> or <c>round(Price, 1) = 12.5</c>. A midpoint is rounded
    /// away from zero, as in SQL, not to the nearest even number as .NET does by default.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionRound<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionRound()
            : base("round", 1, 2)
        {
        }

        /// <summary>
        /// Rounds the number.
        /// </summary>
        /// <param name="arguments">The number and the optional number of decimal places, between 0 and 15.</param>
        /// <returns>The rounded number, or null if an argument is no number.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var number = ToNumber(Argument(arguments, 0));
            var digits = arguments.Count > 1 ? ToNumber(Argument(arguments, 1)) : 0;

            if (!number.HasValue || !digits.HasValue)
            {
                return null;
            }

            return Math.Round(number.Value, (int)Math.Clamp(Math.Truncate(digits.Value), 0, 15), MidpointRounding.AwayFromZero);
        }
    }
}
