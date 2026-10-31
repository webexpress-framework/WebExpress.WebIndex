using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that trims whitespace from a string parameter.
    /// Usage: <c>attribute = trim('  value  ')</c> returns "value".
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionTrim<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionTrim()
            : base("trim", 1, 1)
        {
        }

        /// <summary>
        /// Removes leading and trailing whitespace from the text.
        /// </summary>
        /// <param name="arguments">The text.</param>
        /// <returns>The trimmed text, or null for a null value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return ToText(Argument(arguments, 0))?.Trim();
        }
    }
}
