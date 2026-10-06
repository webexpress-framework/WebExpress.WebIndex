using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        /// Gets the field definition that builds the index.
        /// </summary>
        protected IndexFieldData Field { get; private set; } = field;

        /// <summary>
        /// Gets the file name for the reverse index.
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// Gets the underlying file for the reverse index.
        /// </summary>
        public IndexStorageFile IndexFile { get; protected set; }

        /// <summary>
        /// Gets the header segment.
        /// </summary>
        public IndexStorageSegmentHeader Header { get; protected set; }

        /// <summary>
        /// Gets the allocator segment.
        /// </summary>
        public IndexStorageSegmentAllocator Allocator { get; protected set; }

        /// <summary>
        /// Gets the statistic segment containing optimization counters.
        /// </summary>
        public IndexStorageSegmentStatistic Statistic { get; protected set; }

        /// <summary>
        /// Gets the index document context.
        /// </summary>
        public IIndexDocumemntContext Context { get; private set; } = context;

        /// <summary>
        /// Gets the culture info used by the index.
        /// </summary>
        public CultureInfo Culture { get; private set; } = culture;

        /// <summary>
        /// Determines whether the index does not hold the items of the document store: its file
        /// was just created, written in an outdated format, or left behind by a rebuild that did
        /// not complete. The index has to be filled again from the document store, which holds
        /// every item - a reverse index is derived data and can always be rebuilt, so a format
        /// change needs no conversion of the old file.
        /// </summary>
        public bool RequiresRebuild { get; private set; }

        /// <summary>
        /// Gets all document ids contained in the reverse index.
        /// </summary>
        public abstract IEnumerable<Guid> All { get; }

        /// <summary>
        /// Gets the path of the file that marks a rebuild in progress. The rebuilt index file
        /// carries the current version from its first byte on, so the version alone cannot tell
        /// a complete rebuild from one that was interrupted; the marker outlives the interruption
        /// and makes the next start discard the partial file.
        /// </summary>
        private string RebuildMarker => $"{FileName}.rebuild";

        /// <summary>
        /// Deletes the index file when it carries the expected identifier but another format
        /// version, or when a rebuild of it did not complete. Reading an outdated file with the
        /// current layout would misplace every segment once the size of a node changed, so it is
        /// never opened. A file with a foreign identifier is left alone; opening it reports the
        /// mismatch.
        /// </summary>
        /// <param name="identifier">The identifier of the index file.</param>
        /// <param name="version">The current format version.</param>
        protected void DiscardOutdatedFile(string identifier, byte version)
        {
            var interrupted = File.Exists(RebuildMarker);

            if (!File.Exists(FileName))
            {
                RequiresRebuild = true;

                return;
            }

            var stored = IndexStorageSegmentHeader.ReadVersion(FileName, identifier);

            if (stored is null)
            {
                return;
            }

            if (interrupted || stored != version)
            {
                // the marker is written before the old file goes, so no moment exists in
                // which an incomplete file could pass for a complete one
                BeginRebuild();
                File.Delete(FileName);
                RequiresRebuild = true;
            }
        }

        /// <summary>
        /// Marks the index as being rebuilt from the document store. Until the rebuild is
        /// completed, an interruption leaves the marker behind and the next start rebuilds again.
        /// </summary>
        public void BeginRebuild()
        {
            if (!File.Exists(RebuildMarker))
            {
                File.WriteAllBytes(RebuildMarker, []);
            }
        }

        /// <summary>
        /// Marks the index as holding every item of the document store again.
        /// </summary>
        public void CompleteRebuild()
        {
            IndexFile?.Flush();
            File.Delete(RebuildMarker);
            RequiresRebuild = false;
        }

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