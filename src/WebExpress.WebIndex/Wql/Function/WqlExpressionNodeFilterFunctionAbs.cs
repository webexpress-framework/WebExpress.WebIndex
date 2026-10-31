using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the absolute value of a number.
    /// Usage: <c>abs(Balance) &gt; 100</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionAbs<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double?);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionAbs()
            : base("abs", 1, 1)
        {
        }

        /// <summary>
        /// Computes the absolute value.
        /// </summary>
        /// <param name="arguments">The number.</param>
        /// <returns>The absolute value, or null if the argument is no number.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var number = ToNumber(Argument(arguments, 0));

            return number.HasValue ? Math.Abs(number.Value) : null;
        }
    }
}
