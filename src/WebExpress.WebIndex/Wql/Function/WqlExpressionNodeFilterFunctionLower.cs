using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that converts a string parameter to lowercase.
    /// Usage: <c>attribute = lower('value')</c>
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionLower<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionLower()
            : base("lower", 1, 1)
        {
        }

        /// <summary>
        /// Converts the text to lowercase.
        /// </summary>
        /// <param name="arguments">The text.</param>
        /// <returns>The lowercase text, or null for a null value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return ToText(Argument(arguments, 0))?.ToLowerInvariant();
        }
    }
}
