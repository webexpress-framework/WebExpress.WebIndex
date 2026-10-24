namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary "is" condition in a WQL expression node.
    /// Supports <c>attribute is null</c> and, on a boolean attribute, <c>attribute is true/false</c>;
    /// the parser rejects every other value.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryIs<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns false, since a null or boolean test has no use for text options.
        /// </summary>
        protected internal override bool AcceptsOptions => false;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryIs()
            : base("is", WqlComparisonOperator.Equal)
        {
        }
    }
}
