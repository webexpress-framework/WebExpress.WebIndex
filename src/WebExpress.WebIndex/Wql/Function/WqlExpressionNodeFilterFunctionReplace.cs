using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that replaces every occurrence of a text within another text.
    /// Usage: <c>replace(Phone, '-', '') = '0301234'</c>. The search ignores case, like every
    /// text comparison of WQL.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionReplace<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionReplace()
            : base("replace", 3, 3)
        {
        }

        /// <summary>
        /// Replaces the occurrences of the search text.
        /// </summary>
        /// <param name="arguments">The text, the text searched for and its replacement.</param>
        /// <returns>The text with all occurrences replaced, or null for a null text.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var text = ToText(Argument(arguments, 0));
            var search = ToText(Argument(arguments, 1));

            if (text is null || string.IsNullOrEmpty(search))
            {
                return text;
            }

            return text.Replace(search, ToText(Argument(arguments, 2)) ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
