using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the length of a string parameter.
    /// Usage: <c>attribute = len('value')</c> returns 5, <c>len(Name) &gt; 3</c> compares the
    /// length of an attribute. A null value has the length 0.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionLen<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(double);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionLen()
            : base("len", 1, 1)
        {
        }

        /// <summary>
        /// Computes the length of the text.
        /// </summary>
        /// <param name="arguments">The text.</param>
        /// <returns>The length as a double value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return (double)(ToText(Argument(arguments, 0))?.Length ?? 0);
        }
    }
}
