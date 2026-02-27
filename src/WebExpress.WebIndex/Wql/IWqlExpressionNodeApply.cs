using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Interface of a WQL expression node that can apply.
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