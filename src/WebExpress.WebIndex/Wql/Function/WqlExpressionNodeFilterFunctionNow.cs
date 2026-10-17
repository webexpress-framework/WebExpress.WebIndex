using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the current date and time.
    /// Usage: <c>attribute &lt; now()</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionNow<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(DateTime);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionNow()
            : base("now", 0, 0)
        {
        }

        /// <summary>
        /// Computes the current date and time.
        /// </summary>
        /// <param name="arguments">No arguments.</param>
        /// <returns>The current local time.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return DateTime.Now;
        }
    }
}
