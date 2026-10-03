namespace WebExpress.WebIndex.Wql.Condition
{
    /// <summary>
    /// Supplies the information a WQL filter condition needs while it is being built or evaluated
    /// (such as the attribute and value it compares). Passed by the parser to the condition node.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeFilterConditionContext<TIndexItem> : IWqlExpressionNodeFilterConditionContext
        where TIndexItem : IIndexItem
    {
    }
}
