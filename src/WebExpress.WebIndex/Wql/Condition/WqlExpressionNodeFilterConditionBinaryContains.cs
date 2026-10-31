using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition <c>contains</c>: matches items whose text contains the given
    /// characters anywhere, also inside a word, as in <c>Name contains 'ann'</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryContains<TIndexItem> : WqlExpressionNodeFilterConditionBinarySubstring<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryContains()
            : base("contains")
        {
        }

        /// <summary>
        /// Builds the test whether the text contains the part.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="part">The part searched for.</param>
        /// <returns>A boolean expression.</returns>
        protected override Expression Test(Expression text, Expression part)
        {
            return WqlExpressionBuilder.Contains(text, part);
        }
    }
}
