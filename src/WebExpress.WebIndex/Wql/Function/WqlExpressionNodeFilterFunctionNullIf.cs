using System.Collections.Generic;
using System.Globalization;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns null if both arguments are equal, otherwise the first one.
    /// Usage: <c>nullif(Status, 'none') is null</c>. Equality follows the comparison of WQL,
    /// so texts are compared without regard to case.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionNullIf<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionNullIf()
            : base("nullif", 2, 2)
        {
        }

        /// <summary>
        /// Replaces the first value by null where it equals the second.
        /// </summary>
        /// <param name="arguments">The value and the value that stands for null.</param>
        /// <returns>Null on equality, otherwise the first value.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            var value = Argument(arguments, 0);

            return WqlExpressionBuilder.CompareValues(value, Argument(arguments, 1), WqlComparisonOperator.Equal, CultureInfo.InvariantCulture)
                ? null
                : value;
        }
    }
}
