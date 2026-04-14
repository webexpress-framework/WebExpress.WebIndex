using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Defines the basic functionality of an index schema.
    /// </summary>
    public interface IIndexSchema : IDisposable
    {
        /// <summary>
        /// Gets the index context.
        /// </summary>
        IIndexContext Context { get; }

        /// <summary>
        /// Gets the index field data.
        /// </summary>
        IEnumerable<IndexFieldData> Fields { get; }

        /// <summary>
        /// Delete this file from storage.
        /// </summary>
        void Drop();
    }

    /// <summary>
    /// Defines the functionality of an index schema for a specific type of index item.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item. This type parameter must implement the IIndexItem interface.</typeparam>
    public interface IIndexSchema<TIndexItem> : IIndexSchema where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Checks if the schema of the object has changed.
        /// </summary>
        /// <returns>
        /// True if the schema has changed, otherwise false.
        /// </returns>
        /// <remarks>
        /// This function compares the current schema of an object with the stored schema and returns true if there are changes, and false otherwise.
        /// </remarks>
        bool HasSchemaChanged();

        /// <summary>
        /// Migrates the schema if it has changed.
        /// </summary>
        public void Migrate();
    }
}
