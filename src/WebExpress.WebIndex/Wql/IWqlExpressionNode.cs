
namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Base type for a node in the tree a WQL query is parsed into. Each node represents one part of
    /// the query (a condition, a function, a combination, …); together they form the parsed query
    /// that is evaluated against the index.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {

    }
}