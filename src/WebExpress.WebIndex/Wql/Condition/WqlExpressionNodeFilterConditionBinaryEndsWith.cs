using System.Linq.Expressions;

namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition <c>ends with</c>: matches items whose text ends with the given
    /// characters, as in <c>Mail ends with '.org'</c>.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryEndsWith<TIndexItem> : WqlExpressionNodeFilterConditionBinarySubstring<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryEndsWith()
            : base("ends with")
        {
        }

        /// <summary>
        /// Builds the test whether the text ends with the part.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="part">The ending searched for.</param>
        /// <returns>A boolean expression.</returns>
        protected override Expression Test(Expression text, Expression part)
        {
            return WqlExpressionBuilder.EndsWith(text, part);
        }
    }
}
