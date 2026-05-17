using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Defines the basic functionality of an index document.
    /// </summary>
    public interface IIndexDocument : IDisposable
    {
        /// <summary>
        /// Event that is triggered when the schema has changed.
        /// </summary>
        event EventHandler<IndexSchemaMigrationEventArgs> SchemaChanged;
    }

    /// <summary>
    /// Defines the functionality of an index document for a specific type of index item.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item. This type parameter must implement the IIndexItem interface.</typeparam>
    public interface IIndexDocument<TIndexItem> : IIndexDocument where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the document store.
        /// </summary>
        IIndexDocumentStore<TIndexItem> DocumentStore { get; }

        /// <summary>
        /// Gets the index field names.
        /// </summary>
        IEnumerable<IndexFieldData> Fields { get; }

        /// <summary>
        /// Gets all documents from the index.
        /// </summary>
        IEnumerable<TIndexItem> All { get; }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        void Add(TIndexItem item);

        /// <summary>
        /// Performs an asynchronous addition of an item in the index.
        /// </summary>
        /// <param name="item">The data to be added to the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(TIndexItem item);

        /// <summary>
        /// Updates a item in the index.
        /// </summary>
        /// <typeparam name="T">The data type. This must have the IIndexItem interface.</typeparam>
        /// <param name="item">The data to be updated to the index.</param>
        void Update(TIndexItem item);

        /// <summary>
        /// Performs an asynchronous update of an item in the index.
        /// </summary>
        /// <typeparam name="T">The data type. This must have the IIndexItem interface.</typeparam>
        /// <param name="item">The data to be updated to the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(TIndexItem item);

        /// <summary>
        /// The data to be removed from the index.
        /// </summary>
        /// <param name="item">The data to be removed from the index.</param>
        void Remove(TIndexItem item);

        /// <summary>
        /// Removes an item from the index asynchronously.
        /// </summary>
        /// <param name="item">The data to be removed from the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(TIndexItem item);

        /// <summary>
        /// Returns the number of items.
        /// </summary>
        /// <returns>The number of items.</returns>
        uint Count();

        /// <summary>
        /// Performs an asynchronous determination of the number of elements.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with the number of items.</returns>
        Task<uint> CountAsync();

        /// <summary>
        /// Drop all index documents of type T.
        /// </summary>
        void Drop();

        /// <summary>
        /// Asynchronously drops all index documents of type T.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DropAsync();

        /// <summary>
        /// Removed all data from the index.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removed all data from the index asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearAsync();

        /// <summary>
        /// Returns an index field based on its name.
        /// </summary>
        /// <param name="field">The field that makes up the index.</param>
        /// <returns>The index field or null.</returns>
        IIndexReverse<TIndexItem> GetReverseIndex(IndexFieldData field);
    }
}
