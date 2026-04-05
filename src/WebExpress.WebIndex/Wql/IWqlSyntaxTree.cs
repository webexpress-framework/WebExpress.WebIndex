namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents the syntax tree for a WQL query and provides access to the root nodes.
    /// </summary>
    /// <typeparam name="TIndexItem">The index item type for the syntax tree nodes.</typeparam>
    public interface IWqlSyntaxTree<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the filter node of the syntax tree.
        /// </summary>
        IWqlExpressionNode<TIndexItem> Filter { get; }

        /// <summary>
        /// Returns the order node of the syntax tree.
        /// </summary>
        IWqlExpressionNode<TIndexItem> Order { get; }

        /// <summary>
        /// Returns the partitioning node of the syntax tree.
        /// </summary>
        IWqlExpressionNode<TIndexItem> Partitioning { get; }
    }
}