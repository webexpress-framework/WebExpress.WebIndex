using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Wql;
using WebExpress.WebIndex.Wql.Function;

[assembly: InternalsVisibleTo("WebExpress.WebIndex.Test")]

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Provides the primary entry point for CRUD operations on index documents and
    /// for registering WQL functions and token processing pipeline stages.
    /// Ensures thread-safe access to documents and robust async operations.
    /// </summary>
    public abstract class IndexManager : IDisposable
    {
        private readonly HashSet<Type> _wqlFunctions = [];
        private readonly Dictionary<Type, IIndexDocument> _documents = [];
        private readonly Lock _syncDocs = new();
        private readonly Lock _syncWql = new();
        private bool _disposed;

        /// <summary>
        /// Raised when a schema change requires migration.
        /// </summary>
        public event EventHandler<IndexSchemaMigrationEventArgs> SchemaChanged;

        /// <summary>
        /// Gets a snapshot enumeration of the registered WQL functions.
        /// </summary>
        public IEnumerable<Type> WqlFunctions
        {
            get
            {
                lock (_syncWql)
                {
                    return [.. _wqlFunctions];
                }
            }
        }

        /// <summary>
        /// Gets the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Gets the token analyzer instance used for token processing.
        /// </summary>
        protected IndexTokenAnalyzer TokenAnalyzer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the IndexManager class.
        /// </summary>
        protected IndexManager()
        {
        }

        /// <summary>
        /// Initializes the manager with the provided context and prepares directories and analyzers.
        /// </summary>
        /// <param name="context">The index context to use for file paths and configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when context or its IndexDirectory is not provided.</exception>
        protected void Initialization(IIndexContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.IndexDirectory))
            {
                throw new ArgumentNullException(nameof(context), "Index directory must be provided.");
            }

            Context = context;

            if (!Directory.Exists(Context.IndexDirectory))
            {
                Directory.CreateDirectory(Context.IndexDirectory);
            }

            TokenAnalyzer = new IndexTokenAnalyzer(context);
        }

        /// <summary>
        /// Registers a token processing pipeline stage.
        /// </summary>
        /// <param name="pipeStage">The pipe stage to register.</param>
        public void RegisterPipeState(IIndexPipeStage pipeStage)
        {
            if (pipeStage is null)
            {
                return;
            }

            if (TokenAnalyzer is null)
            {
                return;
            }

            TokenAnalyzer.Register(pipeStage);
        }

        /// <summary>
        /// Removes a token processing pipeline stage.
        /// </summary>
        /// <param name="pipeStage">The pipe stage to remove.</param>
        public void RemovePipeState(IIndexPipeStage pipeStage)
        {
            if (pipeStage is null)
            {
                return;
            }

            if (TokenAnalyzer is null)
            {
                return;
            }

            TokenAnalyzer.Remove(pipeStage);
        }

        /// <summary>
        /// Registers a WQL function type for later use in the parser.
        /// </summary>
        /// <typeparam name="TFunction">The WQL function type implementing IWqlExpressionNodeFilterFunction.</typeparam>
        public void RegisterWqlFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new()
        {
            lock (_syncWql)
            {
                _wqlFunctions.Add(typeof(TFunction));
            }
        }

        /// <summary>
        /// Unregisters a previously added WQL function type.
        /// </summary>
        /// <typeparam name="TFunction">The WQL function type implementing IWqlExpressionNodeFilterFunction.</typeparam>
        public void RemoveWqlFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new()
        {
            lock (_syncWql)
            {
                _wqlFunctions.Remove(typeof(TFunction));
            }
        }

        /// <summary>
        /// Adds the provided items to the index document (reindex operation).
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="items">The items to add.</param>
        public void ReIndex<TIndexItem>(IEnumerable<TIndexItem> items)
            where TIndexItem : IIndexItem
        {
            if (items is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            foreach (var item in items)
            {
                document.Add(item);
            }
        }

        /// <summary>
        /// Adds the provided items asynchronously to the index document with optional progress and cancellation.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="items">The items to add.</param>
        /// <param name="progress">Optional progress reporter (0..100).</param>
        /// <param name="token">Optional cancellation token.</param>
        public async Task ReIndexAsync<TIndexItem>(IEnumerable<TIndexItem> items, IProgress<int> progress = null, CancellationToken token = default)
            where TIndexItem : IIndexItem
        {
            if (items is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            var list = items as IList<TIndexItem> ?? [.. items];
            var count = list.Count;

            if (count == 0)
            {
                progress?.Report(100);
                return;
            }

            int i = 0;

            foreach (var item in list)
            {
                if (token.IsCancellationRequested)
                {
                    // do not throw on cancellation; exit gracefully to satisfy test expectations
                    return;
                }

                await document.AddAsync(item).ConfigureAwait(false);

                if (progress is not null)
                {
                    // compute percentage based on completed items
                    var percent = (int)Math.Round(((i + 1) / (double)count) * 100.0, MidpointRounding.AwayFromZero);
                    percent = Math.Max(0, Math.Min(100, percent));
                    progress.Report(percent);
                }

                i++;
            }
        }

        /// <summary>
        /// Creates and registers a new index document for the given type and culture.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="culture">The culture to use for tokenization.</param>
        /// <param name="type">The index storage type (default: memory).</param>
        public void Create<TIndexItem>(CultureInfo culture, IndexType type = IndexType.Memory)
            where TIndexItem : IIndexItem
        {
            culture ??= CultureInfo.InvariantCulture;

            lock (_syncDocs)
            {
                if (_documents.ContainsKey(typeof(TIndexItem)))
                {
                    return;
                }

                var context = new IndexDocumemntContext(Context, TokenAnalyzer);
                var document = new IndexDocument<TIndexItem>(context, type, culture);
                document.SchemaChanged += OnSchemaChanged;

                _documents.Add(typeof(TIndexItem), document);
            }
        }

        /// <summary>
        /// Closes and disposes the index document for TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public void Close<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            lock (_syncDocs)
            {
                if (_documents.Remove(typeof(TIndexItem), out IIndexDocument document))
                {
                    document.SchemaChanged -= OnSchemaChanged;
                    document.Dispose();
                }
            }
        }

        /// <summary>
        /// Asynchronously closes and disposes the index document for TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public async Task CloseAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            IIndexDocument document = null;

            lock (_syncDocs)
            {
                if (_documents.Remove(typeof(TIndexItem), out document))
                {
                    document.SchemaChanged -= OnSchemaChanged;
                }
            }

            if (document is not null)
            {
                await Task.Run(() => document.Dispose()).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Drops (deletes) the on-disk structures for the document of TIndexItem and unregisters it.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public void Drop<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            IIndexDocument<TIndexItem> document = null;

            lock (_syncDocs)
            {
                if (_documents.Remove(typeof(TIndexItem), out IIndexDocument baseDoc))
                {
                    document = baseDoc as IIndexDocument<TIndexItem>;
                }
            }

            if (document is not null)
            {
                document.Drop();
                document.SchemaChanged -= OnSchemaChanged;
            }
        }

        /// <summary>
        /// Asynchronously drops the on-disk structures for the document of TIndexItem and unregisters it.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public async Task DropAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            IIndexDocument<TIndexItem> document = null;

            lock (_syncDocs)
            {
                if (_documents.Remove(typeof(TIndexItem), out IIndexDocument baseDoc))
                {
                    document = baseDoc as IIndexDocument<TIndexItem>;
                    if (document is not null)
                    {
                        document.SchemaChanged -= OnSchemaChanged;
                    }
                }
            }

            if (document is not null)
            {
                await document.DropAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Inserts a single item into its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to insert.</param>
        public void Insert<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (item is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            document.Add(item);
        }

        /// <summary>
        /// Asynchronously inserts a single item into its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to insert.</param>
        public async Task InsertAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (item is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            await document.AddAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an item in its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to update.</param>
        public void Update<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (item is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            document.Update(item);
        }

        /// <summary>
        /// Asynchronously updates an item in its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to update.</param>
        public async Task UpdateAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (item is null)
            {
                return;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            await document.UpdateAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the number of items in the document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public uint Count<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return 0;
            }

            return document.Count();
        }

        /// <summary>
        /// Asynchronously returns the number of items in the document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public async Task<uint> CountAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return 0;
            }

            return await document.CountAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Removes an item with the given id from its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="id">The unique id of the item to remove.</param>
        public void Delete<TIndexItem>(Guid id)
            where TIndexItem : IIndexItem
        {
            var item = All<TIndexItem>().FirstOrDefault(x => x.Id == id);
            Delete(item);
        }

        /// <summary>
        /// Removes the provided item from its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to remove.</param>
        public void Delete<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            document.Remove(item);
        }

        /// <summary>
        /// Asynchronously removes an item by id from its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="id">The unique id of the item to remove.</param>
        public async Task DeleteAsync<TIndexItem>(Guid id)
            where TIndexItem : IIndexItem
        {
            var item = All<TIndexItem>().FirstOrDefault(x => x.Id == id);
            await DeleteAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously removes an item from its document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="item">The item to remove.</param>
        public async Task DeleteAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            await document.RemoveAsync(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears all data from the document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public void Clear<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            document.Clear();
        }

        /// <summary>
        /// Asynchronously clears all data from the document.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public async Task ClearAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return;
            }

            await document.ClearAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and returns a WQL statement instance for the given query string.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="wql">The WQL query string.</param>
        public IWqlStatement<TIndexItem> Retrieve<TIndexItem>(string wql)
            where TIndexItem : IIndexItem
        {
            if (string.IsNullOrWhiteSpace(wql))
            {
                return null;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return null;
            }

            var parser = new WqlParser<TIndexItem>(document);

            foreach (var function in WqlFunctions)
            {
                var method = typeof(WqlParser<TIndexItem>).GetMethod("RegisterFunction");
                if (method is null)
                {
                    continue;
                }

                var generic = method.MakeGenericMethod(function);
                generic.Invoke(parser, null);
            }

            return parser.Parse(wql);
        }

        /// <summary>
        /// Asynchronously parses and returns a WQL statement instance for the given query string.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        /// <param name="wql">The WQL query string.</param>
        public async Task<IWqlStatement<TIndexItem>> RetrieveAsync<TIndexItem>(string wql)
            where TIndexItem : IIndexItem
        {
            if (string.IsNullOrWhiteSpace(wql))
            {
                return null;
            }

            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return null;
            }

            return await Task.Run(() =>
            {
                var parser = new WqlParser<TIndexItem>(document);

                foreach (var function in WqlFunctions)
                {
                    var method = typeof(WqlParser<TIndexItem>).GetMethod("RegisterFunction");
                    if (method is null)
                    {
                        continue;
                    }

                    var generic = method.MakeGenericMethod(function);
                    generic.Invoke(parser, null);
                }

                return parser.Parse(wql);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns all items from the document as an enumerable.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public IEnumerable<TIndexItem> All<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            var document = GetIndexDocument<TIndexItem>();
            if (document is null)
            {
                return [];
            }

            return document.All;
        }

        /// <summary>
        /// Returns the registered document instance for the given item type, or null if not registered.
        /// </summary>
        /// <typeparam name="TIndexItem">The item type implementing IIndexItem.</typeparam>
        public IIndexDocument<TIndexItem> GetIndexDocument<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            lock (_syncDocs)
            {
                if (_documents.TryGetValue(typeof(TIndexItem), out IIndexDocument res))
                {
                    return res as IIndexDocument<TIndexItem>;
                }
            }

            return null;
        }

        /// <summary>
        /// Handles the schema changed event and forwards it to subscribers.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The event data.</param>
        protected virtual void OnSchemaChanged(object sender, IndexSchemaMigrationEventArgs e)
        {
            SchemaChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Releases managed resources and disposes all registered documents and the token analyzer.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            List<IIndexDocument> snapshot;

            lock (_syncDocs)
            {
                snapshot = [.. _documents.Values];
                _documents.Clear();
            }

            foreach (var document in snapshot)
            {
                try
                {
                    document.Dispose();
                }
                catch
                {
                    // swallow dispose exceptions to avoid teardown failures
                }
            }

            try
            {
                TokenAnalyzer?.Dispose();
            }
            catch
            {
                // swallow to avoid teardown failures
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}