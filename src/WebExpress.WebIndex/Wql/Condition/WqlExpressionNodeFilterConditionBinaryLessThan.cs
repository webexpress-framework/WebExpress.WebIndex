namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// The WQL condition for a less-than test (<c>&lt;</c>): matches items whose attribute is smaller
    /// than the given value. Texts are ordered without regard to case; a null value is never smaller.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLessThan<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryLessThan()
            : base("<", WqlComparisonOperator.LessThan)
        {
        }
    }
}
