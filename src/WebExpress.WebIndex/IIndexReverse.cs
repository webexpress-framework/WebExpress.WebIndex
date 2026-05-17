using System;
using System.Collections.Generic;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Reverse index interface.
    /// </summary>
    /// <typeparam name="TIndexItem">The data type. This must have the IIndexData interface.</typeparam>
    public interface IIndexReverse<TIndexItem> : IDisposable where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets all items.
        /// </summary>
        IEnumerable<Guid> All { get; }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        void Add(TIndexItem item);

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        /// <param name="terms">The terms to add to the reverse index for the given item.</param>
        void Add(TIndexItem item, IEnumerable<IndexTermToken> terms);

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the index.</param>
        void Delete(TIndexItem item);

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="terms">The terms to add to the reverse index for the given item.</param>
        void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms);

        /// <summary>
        /// Removed all data from the index.
        /// </summary>
        void Clear();

        /// <summary>
        /// Drop the reverse index.
        /// </summary>
        void Drop();

        /// <summary>
        /// Return all items for a given input.
        /// </summary>
        /// <param name="term">The input.</param>
        /// <param name="options">The retrieve options.</param>
        /// <returns>An enumeration of the data ids.</returns>
        IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options);
    }
}
