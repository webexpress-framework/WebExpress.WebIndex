using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that rounds a number down to the next integer.
    /// Usage: <c>floor(Price) = 12</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionFloor<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionFloor()
            : base("floor", 1, 1)
        {
        }

        /// <summary>
        /// Rounds the number down.
        /// </summary>
        /// <param name="arguments">The number.</param>
        /// <returns>The largest integer not greater than the number, or null if the argument is no number.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var number = ToNumber(Argument(arguments, 0));

            return number.HasValue ? Math.Floor(number.Value) : null;
        }
    }
}
