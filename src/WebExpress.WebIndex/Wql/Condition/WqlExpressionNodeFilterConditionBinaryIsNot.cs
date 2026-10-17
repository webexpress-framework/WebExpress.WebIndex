namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Represents a binary "is not" condition in a WQL expression node.
    /// Supports <c>attribute is not null</c> and, on a boolean attribute, <c>attribute is not true/false</c>,
    /// which keeps the items whose attribute is null.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionBinaryIsNot<TIndexItem> : WqlExpressionNodeFilterConditionBinaryComparison<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns false, since a null or boolean test has no use for text options.
        /// </summary>
        protected internal override bool AcceptsOptions => false;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WqlExpressionNodeFilterConditionBinaryIsNot()
            : base("is not", WqlComparisonOperator.NotEqual)
        {
        }
    }
}
