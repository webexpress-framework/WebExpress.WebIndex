using System;
using System.Collections.Generic;
using System.Globalization;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides a base class for reverse index implementations persisted on disk.
    /// </summary>
    /// <typeparam name="TIndexItem">The data type implementing IIndexItem.</typeparam>
    /// <remarks>
    /// Initializes a new instance of the reverse index base.
    /// </remarks>
    /// <param name="context">The index document context.</param>
    /// <param name="field">The field definition that builds the index.</param>
    /// <param name="culture">The culture information.</param>
    public abstract class IndexStorageReverse<TIndexItem>(IIndexDocumemntContext context, IndexFieldData field, CultureInfo culture) : IIndexReverse<TIndexItem>, IIndexStorage, IDisposable
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the field definition that builds the index.
        /// </summary>
        protected IndexFieldData Field { get; private set; } = field;

        /// <summary>
        /// Returns the file name for the reverse index.
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// Returns the underlying file for the reverse index.
        /// </summary>
        public IndexStorageFile IndexFile { get; protected set; }

        /// <summary>
        /// Returns the header segment.
        /// </summary>
        public IndexStorageSegmentHeader Header { get; protected set; }

        /// <summary>
        /// Returns the allocator segment.
        /// </summary>
        public IndexStorageSegmentAllocator Allocator { get; protected set; }

        /// <summary>
        /// Returns the statistic segment containing optimization counters.
        /// </summary>
        public IndexStorageSegmentStatistic Statistic { get; protected set; }

        /// <summary>
        /// Returns the index document context.
        /// </summary>
        public IIndexDocumemntContext Context { get; private set; } = context;

        /// <summary>
        /// Returns the culture info used by the index.
        /// </summary>
        public CultureInfo Culture { get; private set; } = culture;

        /// <summary>
        /// Returns all document ids contained in the reverse index.
        /// </summary>
        public abstract IEnumerable<Guid> All { get; }

        /// <summary>
        /// Adds a single item to the index.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public abstract void Add(TIndexItem item);

        /// <summary>
        /// Adds the specified terms of an item to the index.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="terms">The tokenized terms for the given item.</param>
        public abstract void Add(TIndexItem item, IEnumerable<IndexTermToken> terms);

        /// <summary>
        /// Deletes a single item from the index.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        public abstract void Delete(TIndexItem item);

        /// <summary>
        /// Deletes the specified terms of an item from the index.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        /// <param name="terms">The tokenized terms for the given item.</param>
        public abstract void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms);

        /// <summary>
        /// Clears all data from the index and reinitializes structures.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Drops the reverse index and removes persistent storage.
        /// </summary>
        public abstract void Drop();

        /// <summary>
        /// Retrieves all items for a given input according to the provided options.
        /// </summary>
        /// <param name="input">The raw input to analyze.</param>
        /// <param name="options">The retrieval options.</param>
        /// <returns>An enumeration of document ids.</returns>
        public abstract IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options);

        // implements disposable pattern with null guards to avoid null reference on uninitialized IndexFile
        private bool _disposed;

        /// <summary>
        /// Releases the resources used by the reverse index.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">True to release managed resources; otherwise false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (IndexFile is not null)
                {
                    IndexFile.Dispose();
                    IndexFile = null;
                }
            }

            _disposed = true;
        }
    }
}