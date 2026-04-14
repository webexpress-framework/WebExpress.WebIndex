using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// The document store.
    /// Key: The id of the item.
    /// Value: The item.
    /// </summary>
    public class IndexMemoryDocumentStore<TIndexItem> : Dictionary<Guid, TIndexItem>, IIndexDocumentStore<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets all items.
        /// </summary>
        public IEnumerable<TIndexItem> All => Values;

        /// <summary>
        /// Gets the predicted capacity (number of items to store) of the document store.
        /// </summary>
        public new uint Capacity => (uint)base.Count;

        /// <summary>
        /// Gets the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <param name="capacity">The predicted capacity (number of items to store) of the document store.</param>
        public IndexMemoryDocumentStore(IIndexContext context, uint capacity)
            : base((int)capacity)
        {
            Context = context;
        }

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(TIndexItem item)
        {
            if (!ContainsKey(item.Id))
            {
                Add(item.Id, item);
            }
        }

        /// <summary>
        /// Update an item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Update(TIndexItem item)
        {
            if (!ContainsKey(item.Id))
            {
                Add(item);

                return;
            }

            Delete(item);
            Add(item);
        }

        /// <summary>
        /// Remove an item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Delete(TIndexItem item)
        {
            if (ContainsKey(item.Id))
            {
                Remove(item.Id, out _);
            }
        }

        /// <summary>
        /// Returns the number of items.
        /// </summary>
        /// <returns>The number of items.</returns>
        public new uint Count()
        {
            return (uint)base.Count;
        }

        /// <summary>
        /// Drop the index document store.
        /// </summary>
        public void Drop()
        {
        }

        /// <summary>
        /// Returns the item.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <returns>The item.</returns>
        public TIndexItem GetItem(Guid id)
        {
            if (ContainsKey(id))
            {
                return this[id];
            }

            return default;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, 
        /// or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
