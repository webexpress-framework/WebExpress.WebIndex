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
    /// The IndexManager serves as the primary component for interacting with the 
    /// indexing functions (CRUD).
    /// </summary>
    public abstract class IndexManager : IDisposable
    {
        private readonly HashSet<Type> _wqlFunctions = [];

        /// <summary>
        /// Event that is triggered when the schema has changed.
        /// </summary>
        public event EventHandler<IndexSchemaMigrationEventArgs> SchemaChanged;

        /// <summary>
        /// Returns an enumeration of the existing index documents.
        /// </summary>
        private Dictionary<Type, IIndexDocument> Documents { get; } = [];

        /// <summary>
        /// Returns the collection of registered WQL functions.
        /// </summary>
        public IEnumerable<Type> WqlFunctions => _wqlFunctions;

        /// <summary>
        /// Returns the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Returns the token analyzer.
        /// </summary>
        protected IndexTokenAnalyzer TokenAnalyzer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public IndexManager()
        {
        }

        /// <summary>
        /// Initialization of the IndexManager.
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        protected void Initialization(IIndexContext context)
        {
            Context = context;

            if (!Directory.Exists(Context.IndexDirectory))
            {
                Directory.CreateDirectory(Context.IndexDirectory);
            }

            TokenAnalyzer = new IndexTokenAnalyzer(context);
        }

        /// <summary>
        /// Registers a pipe state for processing the tokens.
        /// </summary>
        /// <param name="pipeState">The pipe stage to add.</param>
        public void RegisterPipeState(IIndexPipeStage pipeStage)
        {
            TokenAnalyzer.Register(pipeStage);
        }

        /// <summary>
        /// Removes a pipe stage from the processing pipeline.
        /// </summary>
        /// <param name="pipeStage">The pipe stage to remove.</param>
        public void RemovePipeState(IIndexPipeStage pipeStage)
        {
            TokenAnalyzer.Remove(pipeStage);
        }

        /// <summary>
        /// Registers a WQL function for an index item type.
        /// </summary>
        /// <typeparam name="TFunction">
        /// The type of the WQL function. This must implement the 
        /// IWqlExpressionNodeFilterFunction interface.
        /// </typeparam>
        /// <typeparam name="TIndexItem">
        /// The type of the index item. This must implement the IIndexItem interface.
        /// </typeparam>
        public void RegisterWqlFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new()
        {
            _wqlFunctions.Add(typeof(TFunction));
        }

        /// <summary>
        /// Removes a registered WQL function for an index item type.
        /// </summary>
        /// <typeparam name="TFunction">
        /// The type of the WQL function. This must implement the 
        /// IWqlExpressionNodeFilterFunction interface.
        /// </typeparam>
        public void RemoveWqlFunction<TFunction>()
            where TFunction : IWqlExpressionNodeFilterFunction, new()
        {
            _wqlFunctions.Remove(typeof(TFunction));
        }

        /// <summary>
        /// Reindexing the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="items">The data to be added to the index.</param>
        public void ReIndex<TIndexItem>(IEnumerable<TIndexItem> items)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                foreach (var item in items)
                {
                    document.Add(item);
                }
                ;
            }
        }

        /// <summary>
        /// Performs an asynchronous reindexing of a collection of index items.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="items">
        /// The collection of items to be re-indexed.
        /// </param>
        /// <param name="progress">
        /// An optional IProgress object that tracks the progress of the re-indexing.
        /// </param>
        /// <param name="token">
        /// An optional CancellationToken that is used to cancel the re-indexing.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReIndexAsync<TIndexItem>(IEnumerable<TIndexItem> items, IProgress<int> progress = null, CancellationToken token = default)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                int i = 0;
                var count = items.Count();

                foreach (var item in items)
                {
                    await document.AddAsync(item);

                    if (progress != null)
                    {
                        var percent = (i++ / (float)count) * 100;
                        progress.Report((int)percent);
                    }

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }
                ;
            }
        }

        /// <summary>
        /// Registers a data type in the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="culture">The culture.</param>
        /// <param name="type">The index type.</param>
        public void Create<TIndexItem>(CultureInfo culture, IndexType type = IndexType.Memory)
            where TIndexItem : IIndexItem
        {
            if (!Documents.ContainsKey(typeof(TIndexItem)))
            {
                var context = new IndexDocumemntContext(Context, TokenAnalyzer);
                var document = new IndexDocument<TIndexItem>(context, type, culture);

                document.SchemaChanged += OnSchemaChanged;
                Documents.Add(typeof(TIndexItem), document);
            }
        }

        /// <summary>
        /// Closes the index file of type TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        public void Close<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() != null)
            {
                Documents.Remove(typeof(TIndexItem), out IIndexDocument document);

                document.SchemaChanged -= OnSchemaChanged;
                document.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously closes the index file of type TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CloseAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() != null)
            {
                await Task.Run(() =>
                {
                    Documents.Remove(typeof(TIndexItem), out IIndexDocument document);

                    document.SchemaChanged -= OnSchemaChanged;
                    document.Dispose();
                });
            }
        }

        /// <summary>
        /// Drops all index documents of type TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        public void Drop<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                Documents.Remove(typeof(TIndexItem), out _);

                document.Drop();
                document.SchemaChanged -= OnSchemaChanged;
            }
        }

        /// <summary>
        /// Asynchronously drops all index documents of type TIndexItem.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DropAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                await Task.Run(() =>
                {
                    Documents.Remove(typeof(TIndexItem), out _);

                    var res = document.DropAsync();
                    document.SchemaChanged -= OnSchemaChanged;

                    res.Wait();
                });
            }
        }

        /// <summary>
        /// Adds a item to the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be added to the index.</param>
        public void Insert<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                document.Add(item);
            }
        }

        /// <summary>
        /// Performs an asynchronous addition of an item in the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be added to the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InsertAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                await document.AddAsync(item);
            }
        }

        /// <summary>
        /// Updates a item in the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be updated to the index.</param>
        public void Update<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                document.Update(item);
            }
        }

        /// <summary>
        /// Performs an asynchronous update of an item in the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be updated to the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                await document.UpdateAsync(item);
            }
        }

        /// <summary>
        /// Returns the number of items.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>The number of items.</returns>
        public uint Count<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                return document.Count();
            }

            return 0;
        }

        /// <summary>
        /// Performs an asynchronous determination of the number of elements.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>A task representing the asynchronous operation with the number of items.</returns>
        public async Task<uint> CountAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                return await document.CountAsync();
            }

            return 0;
        }

        /// <summary>
        /// Removes the specified item from the index.
        /// </summary>
        /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
        /// <param name="id">The unique identifier of the item to remove.</param>
        public void Delete<TIndexItem>(Guid id)
            where TIndexItem : IIndexItem
        {
            var item = All<TIndexItem>()
               .FirstOrDefault(x => x.Id == id);

            Delete(item);
        }

        /// <summary>
        /// Removes an item from the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be removed from the index.</param>
        public void Delete<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                document.Remove(item);
            }
        }

        /// <summary>
        /// Removes an item from the index asynchronously.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be removed from the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync<TIndexItem>(Guid id)
            where TIndexItem : IIndexItem
        {
            var item = All<TIndexItem>()
              .FirstOrDefault(x => x.Id == id);

            await DeleteAsync(item);
        }

        /// <summary>
        /// Removes an item from the index asynchronously.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="item">The data to be removed from the index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync<TIndexItem>(TIndexItem item)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                await document.RemoveAsync(item);
            }
        }

        /// <summary>
        /// Clear all data from index document.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        public void Clear<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                document.Clear();
            }
        }

        /// <summary>
        /// Removed all data from the index asynchronously.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearAsync<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                await document.ClearAsync();
            }
        }

        /// <summary>
        /// Executes a wql statement.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="wql">The wql statement.</param>
        /// <returns>The WQL statement.</returns>
        public IWqlStatement<TIndexItem> Retrieve<TIndexItem>(string wql)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                var parser = new WqlParser<TIndexItem>(document);

                foreach (var function in _wqlFunctions)
                {
                    var method = typeof(WqlParser<TIndexItem>).GetMethod("RegisterFunction").MakeGenericMethod(function);
                    method.Invoke(parser, null);
                }

                return parser.Parse(wql);
            }

            return null;
        }

        /// <summary>
        /// Executes a wql statement asynchronously.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <param name="wql">The wql statement.</param>
        /// <returns>
        /// A task that represents the asynchronous operation using the WQL statement.
        /// </returns>
        public async Task<IWqlStatement<TIndexItem>> RetrieveAsync<TIndexItem>(string wql)
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                return await Task.Run(() =>
                {
                    var parser = new WqlParser<TIndexItem>(document);

                    foreach (var function in _wqlFunctions)
                    {
                        var method = typeof(WqlParser<TIndexItem>).GetMethod("RegisterFunction").MakeGenericMethod(function);
                        method.Invoke(parser, null);
                    }

                    return parser.Parse(wql);
                });
            }

            return null;
        }

        /// <summary>
        /// Returns all documents from the index.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>An enumeration of the documents</returns>
        public IEnumerable<TIndexItem> All<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (GetIndexDocument<TIndexItem>() is IIndexDocument<TIndexItem> document)
            {
                return document.All;
            }

            return [];
        }

        /// <summary>
        /// Returns an index type based on its type.
        /// </summary>
        /// <typeparam name="TIndexItem">
        /// The data type. This must have the IIndexItem interface.
        /// </typeparam>
        /// <returns>The index type or null.</returns>
        public IIndexDocument<TIndexItem> GetIndexDocument<TIndexItem>()
            where TIndexItem : IIndexItem
        {
            if (Documents.TryGetValue(typeof(TIndexItem), out IIndexDocument res))
            {
                return res as IIndexDocument<TIndexItem>;
            }

            return null;
        }

        /// <summary>
        /// Raises the SchemaChanged event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An IndexSchemaMigrationEventArgs that contains the event data.</param>
        protected virtual void OnSchemaChanged(object sender, IndexSchemaMigrationEventArgs e)
        {
            SchemaChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes of the resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            foreach (var document in Documents)
            {
                document.Value.Dispose();
            }

            Documents.Clear();
            TokenAnalyzer?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
