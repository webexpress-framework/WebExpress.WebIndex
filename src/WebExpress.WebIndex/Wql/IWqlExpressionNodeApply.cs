using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// A WQL expression node that can be evaluated: its <c>Apply</c> method runs the node against an
    /// index document and returns the ids of the matching items. This is how a parsed query produces results.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IWqlExpressionNodeApply<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Applies the filter to the index.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <returns>The data ids from the index.</returns>
        IEnumerable<Guid> Apply(IIndexDocument<TIndexItem> indexDocument);
    }
}