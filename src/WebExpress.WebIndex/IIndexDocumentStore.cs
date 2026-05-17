using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Interface for a document store that indexes items.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public interface IIndexDocumentStore<TIndexItem> : IDisposable where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets all document items.
        /// </summary>
        IEnumerable<TIndexItem> All { get; }

        /// <summary>
        /// Gets the predicted capacity (number of items to store).
        /// </summary>
        uint Capacity { get; }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <typeparam name="T">The data type. This must have the IIndexData interface.</typeparam>
        /// <param name="item">The data to be added to the index.</param>
        void Add(TIndexItem item);

        /// <summary>
        /// Update an item.
        /// </summary>
        /// <param name="item">The item.</param>
        void Update(TIndexItem item);

        /// <summary>
        /// Removed all data from the document store.
        /// </summary>
        void Clear();

        /// <summary>
        /// The data to be removed from the document store.
        /// </summary>
        /// <typeparam name="T">The data type. This must have the IIndexData interface.</typeparam>
        /// <param name="item">The data to be removed from the document store.</param>
        void Delete(TIndexItem item);

        /// <summary>
        /// Returns the number of items.
        /// </summary>
        /// <returns>The number of items.</returns>
        uint Count();

        /// <summary>
        /// Drop the index document store.
        /// </summary>
        void Drop();

        /// <summary>
        /// Returns the item.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <returns>The item.</returns>
        TIndexItem GetItem(Guid id);
    }
}
