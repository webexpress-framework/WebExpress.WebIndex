using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns a part of a text.
    /// Usage: <c>substring(Code, 1, 3) = 'abc'</c>. The position counts from 1 as in SQL; a
    /// position before the first character shortens the part by the characters it reaches
    /// in front of the text, and a part beyond the end is cut off there.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionSubstring<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionSubstring()
            : base("substring", 2, 3)
        {
        }

        /// <summary>
        /// Cuts the part out of the text.
        /// </summary>
        /// <param name="arguments">The text, the one-based position and the optional length.</param>
        /// <returns>The part, or null if the text is null or a position or length is invalid.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var text = ToText(Argument(arguments, 0));
            var start = ToNumber(Argument(arguments, 1));
            var length = arguments.Count > 2 ? ToNumber(Argument(arguments, 2)) : text?.Length;

            if (text is null || start is null || length is null || length < 0)
            {
                return null;
            }

            var from = Math.Truncate(start.Value) - 1;
            var to = from + Math.Truncate(length.Value);
            var begin = (int)Math.Clamp(from, 0, text.Length);
            var end = (int)Math.Clamp(to, begin, text.Length);

            return text[begin..end];
        }
    }
}
