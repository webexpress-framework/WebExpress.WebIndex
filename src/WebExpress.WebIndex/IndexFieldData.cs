using System;
using System.Reflection;
using System.Threading;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Represents the data descriptor for a single index field, including its name, type and property metadata.
    /// Provides a cached, robust accessor for retrieving values from objects, supporting dotted (nested) paths.
    /// </summary>
    public class IndexFieldData
    {
        // cached accessor built lazily and used across calls; independent of concrete runtime object type
        private Func<object, object> _cachedAccessor;
        // lock for lazy initialization of the accessor
        private readonly Lock _sync = new();

        /// <summary>
        /// Gets the field name (supports dotted nested paths like "Address.Street").
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the .NET type of the field.
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// Gets the PropertyInfo of the (leaf) property for this field when available.
        /// For dotted paths this corresponds to the last property in the path.
        /// </summary>
        public PropertyInfo PropertyInfo { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the field is enabled (no IndexIgnoreAttribute is present).
        /// </summary>
        public bool Enabled
        {
            get
            {
                return PropertyInfo?.GetCustomAttribute<IndexIgnoreAttribute>() is null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the IndexFieldData class.
        /// </summary>
        public IndexFieldData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the IndexFieldData class with a specific property.
        /// </summary>
        /// <param name="property">The property information to initialize the index field.</param>
        public IndexFieldData(PropertyInfo property)
        {
            Name = property?.Name;
            Type = property?.PropertyType;
            PropertyInfo = property;
        }

        /// <summary>
        /// Retrieves the value for the configured field from a given object.
        /// Supports nested dotted path names and returns null if any segment is missing or null.
        /// Indexer properties are skipped (returning null).
        /// </summary>
        /// <param name="item">The source object instance.</param>
        /// <returns>The resolved value, or null when not available.</returns>
        public object GetPropertyValue(object item)
        {
            if (item is null)
            {
                return null;
            }

            var accessor = _cachedAccessor;
            if (accessor is null)
            {
                lock (_sync)
                {
                    _cachedAccessor ??= BuildAccessor(Name);

                    accessor = _cachedAccessor;
                }
            }

            return accessor(item);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return $"Field: {Name}";
        }

        /// <summary>
        /// Builds a robust accessor for the given field name and property info.
        /// Uses reflection at runtime to traverse dotted paths and returns null on any mismatch.
        /// </summary>
        /// <param name="name">Field name, may be dotted for nested paths.</param>
        /// <returns>A function that extracts the field value from an object instance.</returns>
        private static Func<object, object> BuildAccessor(string name)
        {
            // normalize path segments; empty or whitespace name yields a null-returning accessor
            var segments = string.IsNullOrWhiteSpace(name) ? [] : name.Split('.');

            if (segments.Length == 0)
            {
                // no usable path
                return _ => null;
            }

            // return a single accessor using reflection; safe for all runtime types
            return (obj) =>
            {
                // current object during traversal
                object current = obj;

                for (int i = 0; i < segments.Length; i++)
                {
                    if (current is null)
                    {
                        return null;
                    }

                    var currentType = current.GetType();
                    var segment = segments[i];

                    // fetch public instance property only; skip indexers
                    var prop = currentType.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public);
                    if (prop is null)
                    {
                        return null;
                    }

                    if (prop.GetIndexParameters().Length != 0)
                    {
                        // indexer properties are not supported as path segments
                        return null;
                    }

                    current = prop.GetValue(current);
                }

                return current;
            };
        }
    }
}