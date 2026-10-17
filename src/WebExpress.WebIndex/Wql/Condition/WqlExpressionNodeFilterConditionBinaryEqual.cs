namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition for an equality test (<c>=</c>): matches items whose attribute equals the
    /// given value. On a text attribute the value is searched as a phrase, so that
    /// <c>Text = 'lorem ipsum'</c> finds the words in this order anywhere in the text.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryEqual()
            : base("=", WqlComparisonOperator.Equal)
        {
        }
    }
}
