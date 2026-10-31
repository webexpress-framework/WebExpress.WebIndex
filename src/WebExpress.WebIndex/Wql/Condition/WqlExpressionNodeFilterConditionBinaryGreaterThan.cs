namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a filter condition that checks if a property value is greater than a specified value.
    /// Texts are ordered without regard to case; a null value is never greater.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryGreaterThan<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryGreaterThan()
            : base(">", WqlComparisonOperator.GreaterThan)
        {
        }
    }
}
