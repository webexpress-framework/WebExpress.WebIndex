using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that rounds a number up to the next integer.
    /// Usage: <c>ceil(Price) = 13</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionCeil<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionCeil()
            : base("ceil", 1, 1)
        {
        }

        /// <summary>
        /// Rounds the number up.
        /// </summary>
        /// <param name="arguments">The number.</param>
        /// <returns>The smallest integer not less than the number, or null if the argument is no number.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var number = ToNumber(Argument(arguments, 0));

            return number.HasValue ? Math.Ceiling(number.Value) : null;
        }
    }
}
