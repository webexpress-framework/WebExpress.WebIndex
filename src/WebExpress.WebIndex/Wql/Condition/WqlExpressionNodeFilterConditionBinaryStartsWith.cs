using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition <c>starts with</c>: matches items whose text begins with the given
    /// characters, as in <c>Code starts with 'ab'</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryStartsWith<TIndexItem> : WqlExpressionNodeFilterConditionBinarySubstring<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryStartsWith()
            : base("starts with")
        {
        }

        /// <summary>
        /// Builds the test whether the text begins with the part.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="part">The beginning searched for.</param>
        /// <returns>A boolean expression.</returns>
        protected override Expression Test(Expression text, Expression part)
        {
            return WqlExpressionBuilder.StartsWith(text, part);
        }
    }
}
