using System;
using System.Collections.Generic;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Represents a index schema file.
    /// </summary>
    /// <typeparam name="TIndexItem">The data type. This must have the IIndexItem interface.</typeparam>
    public class IndexMemorySchema<TIndexItem> : IIndexSchema<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Gets the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Gets the index field data.
        /// </summary>
        public IEnumerable<IndexFieldData> Fields => GetFieldData(typeof(TIndexItem));

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexSchema"/> class.
        /// </summary>
        /// <param name="context">The index context.</param>
        public IndexMemorySchema(IIndexContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Checks if the schema of the object has changed.
        /// </summary>
        /// <returns>
        /// Returns allways true.
        /// </returns>
        public bool HasSchemaChanged()
        {
            return false;
        }

        /// <summary>
        /// Migrates the schema if it has changed.
        /// </summary>
        public void Migrate()
        {
        }

        /// <summary>
        /// Delete this file from storage.
        /// </summary>
        public void Drop()
        {
            Dispose();
        }

        /// <summary>
        /// Is called to free up resources.
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Recursively retrieves the field names of the specified type.
        /// </summary>
        /// <param name="type">The type whose field names to retrieve.</param>
        /// <param name="prefix">The prefix to prepend to each field name.</param>
        /// <param name="processedTypes">A set of types that have already been processed to avoid circular references.</param>
        /// <returns>An enumerable collection of field names.</returns>
        private static IEnumerable<IndexFieldData> GetFieldData(Type type, string prefix = "", HashSet<Type> processedTypes = null)
        {
            processedTypes ??= [];

            if (processedTypes.Contains(type))
            {
                yield break;
            }

            processedTypes.Add(type);

            foreach (var property in type.GetProperties())
            {
                string propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                {
                    yield return new IndexFieldData
                    {
                        Name = propertyName,
                        Type = property.PropertyType,
                        PropertyInfo = property
                    };
                }

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    foreach (var subProperty in GetFieldData(property.PropertyType, propertyName, processedTypes))
                    {
                        yield return subProperty;
                    }
                }
            }
        }
    }
}