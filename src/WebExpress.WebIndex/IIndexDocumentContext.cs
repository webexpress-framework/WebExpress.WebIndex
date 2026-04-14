using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// The context of an indexed document.
    /// </summary>
    public interface IIndexDocumemntContext : IIndexContext
    {
        /// <summary>
        /// Gets the token analyzer that is valid in the context of the IndexDocument.
        /// </summary>
        IndexTokenAnalyzer TokenAnalyzer { get; }
    }
}
