namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary filter condition that checks if a property value is less than or equal to a specified value.
    /// Texts are ordered without regard to case; a null value is never smaller or equal.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryLessThanOrEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryLessThanOrEqual()
            : base("<=", WqlComparisonOperator.LessThanOrEqual)
        {
        }
    }
}
