using System.Collections.Generic;
using System.Linq;

namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// WQL function that returns the first argument that is not null.
    /// Usage: <c>coalesce(Nickname, FirstName) = 'Ada'</c> or <c>coalesce(Text, '') = ''</c>.
    /// The arguments may differ in type, so the value is compared at evaluation time against
    /// the type of the other operand.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterFunctionCoalesce<TIndexItem> : WqlExpressionNodeFilterFunctionScalar<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterFunctionCoalesce()
            : base("coalesce", 2, int.MaxValue)
        {
        }

        /// <summary>
        /// Selects the first value that is not null.
        /// </summary>
        /// <param name="arguments">The candidates in order of preference.</param>
        /// <returns>The first value that is not null, or null if all are null.</returns>
        protected override object Evaluate(IReadOnlyList<object> arguments)
        {
            return arguments.FirstOrDefault(x => x is not null);
        }
    }
}
