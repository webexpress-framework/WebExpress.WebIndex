using System;
using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that joins its arguments into one text.
    /// Usage: <c>concat(FirstName, ' ', LastName) = 'Ada Lovelace'</c>. A null argument
    /// contributes nothing, as in SQL's <c>CONCAT</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionConcat<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the type of the computed value.
        /// </summary>
        public override Type ReturnType => typeof(string);

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionConcat()
            : base("concat", 1, int.MaxValue)
        {
        }

        /// <summary>
        /// Joins the texts of the arguments.
        /// </summary>
        /// <param name="arguments">The parts.</param>
        /// <returns>The joined text.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return string.Concat(arguments.Select(ToText));
        }
    }
}
