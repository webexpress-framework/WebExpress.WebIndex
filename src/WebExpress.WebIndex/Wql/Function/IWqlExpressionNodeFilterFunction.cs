namespace WebExpress.WebIndex.Wql.Function
{
    /// <summary>
    /// A WQL node representing a callable function used in a query (it exposes the function name and
    /// runs it to produce a value). This non-generic form is the common contract shared by all such functions.
    /// </summary>
    public interface IWqlExpressionNodeFilterFunction
    {
        /// <summary>
        /// Returns the function name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the function.
        /// </summary>
        /// <returns>The return value.</returns>
        object Execute();
    }

    /// <summary>
    /// The strongly typed WQL filter function, bound to a specific index item type so it can take
    /// part in a query expression tree over that type.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IWqlExpressionNodeFilterFunction<TIndexItem> : IWqlExpressionNodeFilterFunction, IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
    }
}
