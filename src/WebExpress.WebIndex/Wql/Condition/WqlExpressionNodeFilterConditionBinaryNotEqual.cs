namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary not-equal condition in a WQL expression node.
    /// It is the complement of <c>=</c>: documents with null or missing attribute values
    /// remain in the result unless they explicitly match the requested value, in the index
    /// as well as in a query.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryNotEqual<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryNotEqual()
            : base("!=", WqlComparisonOperator.NotEqual)
        {
        }
    }
}
