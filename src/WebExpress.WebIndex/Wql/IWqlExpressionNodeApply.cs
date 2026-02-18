using System;
using System.Collections.Generic;
using WebExpress.WebIndex.Queries;

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
        /// <returns>The data ids from the index.</returns>
        IEnumerable<Guid> Apply();

        /// <summary>
        /// Applies the current filter condition to the specified query and returns the 
        /// resulting query.
        /// </summary>
        /// <param name="query">
        /// The query to which the filter condition will be applied. This parameter must 
        /// not be null.
        /// </param>
        /// <returns>
        /// An <see cref="IQuery{TIndexItem}"/> representing the filtered query if a 
        /// condition exists; otherwise, the original query.
        /// </returns>
        IQuery<TIndexItem> Apply(IQuery<TIndexItem> query);
    }
}