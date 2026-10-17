using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that converts a string parameter to uppercase.
    /// Usage: <c>attribute = upper('value')</c>
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionUpper<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionUpper()
            : base("upper", 1, 1)
        {
        }

        /// <summary>
        /// Converts the text to uppercase.
        /// </summary>
        /// <param name="arguments">The text.</param>
        /// <returns>The uppercase text, or null for a null value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return ToText(Argument(arguments, 0))?.ToUpperInvariant();
        }
    }
}
