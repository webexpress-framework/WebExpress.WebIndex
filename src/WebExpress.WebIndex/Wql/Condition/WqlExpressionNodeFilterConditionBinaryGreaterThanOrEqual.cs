namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition that checks if a property value is greater than or equal to a specified value.
    /// Texts are ordered without regard to case; a null value is never greater or equal.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryGreaterThanOrEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryGreaterThanOrEqual()
            : base(">=", WqlComparisonOperator.GreaterThanOrEqual)
        {
        }
    }
}
