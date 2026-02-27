using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Represents a structured property path for querying properties using a dot-separated 
    /// string.
    /// </summary>
    public sealed class WqlPropertyPath<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private static readonly char[] Separator = ['.'];

        /// <summary>
        /// Returns the collection of segments represented as a read-only list of strings.
        /// </summary>
        public IEnumerable<string> Segments { get; }

        /// <summary>
        /// Returns the raw, unmodified property path string.
        /// </summary>
        public string Raw { get; }

        /// <summary>
        /// Initializes a new instance of the class using the specified property path string 
        /// and its segments.
        /// </summary>
        /// <param name="raw">
        /// The raw string representation of the property path to be parsed.
        /// </param>
        /// <param name="segments">
        /// A read-only list containing the individual segments of the property path.
        /// </param>
        private WqlPropertyPath(string raw, IEnumerable<string> segments)
        {
            Raw = raw;
            Segments = segments;
        }

        /// <summary>
        /// Parses a raw property path string and returns a structured WqlPropertyPath object 
        /// representing its segments.
        /// </summary>
        /// <param name="raw">
        /// The property path string to parse. Cannot be null, empty, or consist solely of 
        /// whitespace, and must contain at least one valid segment.
        /// </param>
        /// <returns>
        /// A WqlPropertyPath object containing the original property path and its parsed 
        /// segments.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if raw is null, empty, consists only of whitespace, or contains no valid 
        /// segments.
        /// </exception>
        public static WqlPropertyPath<TIndexItem> Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new ArgumentException("Property path cannot be null or empty.", nameof(raw));
            }

            var parts = raw.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 0)
            {
                throw new ArgumentException("Property path contains no valid segments.", nameof(raw));
            }

            var segments = new List<string>();
            var currentType = typeof(TIndexItem);

            foreach (var part in parts)
            {
                if (currentType == null)
                {
                    throw new ArgumentException($"Type information lost for segment '{part}' in '{raw}'");
                }

                // find property using any casing; take the actual member name
                var prop = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"Property '{part}' not found on type {currentType.FullName} when parsing '{raw}'");

                segments.Add(prop.Name); // add member-casing name
                currentType = prop.PropertyType;
            }

            return new WqlPropertyPath<TIndexItem>(raw, segments);
        }

        /// <summary>
        /// Resolves a property from the specified root type by traversing a sequence of 
        /// property segments using the provided binding flags.
        /// </summary>
        /// <param name="rootType">
        /// The type from which the property resolution begins. Cannot be null.
        /// </param>
        /// <param name="flags">
        /// The binding flags used to control the search for properties. Defaults to public 
        /// instance properties, ignoring case.
        /// </param>
        /// <returns>
        /// The PropertyInfo representing the resolved property, or null if any segment in 
        /// the path does not correspond to a valid property.
        /// </returns>
        public PropertyInfo Resolve(Type rootType, BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(rootType);

            var currentType = rootType;
            PropertyInfo currentProperty = null;

            foreach (var segment in Segments)
            {
                currentProperty = currentType.GetProperty(segment, flags);
                if (currentProperty is null)
                {
                    return null;
                }

                currentType = currentProperty.PropertyType;
            }

            return currentProperty;
        }

        /// <summary>
        /// Resolves the value of a specified property path from the given instance using 
        /// reflection.
        /// </summary>
        /// <param name="instance">
        /// The object instance from which to resolve the property values. This parameter 
        /// cannot be null.
        /// </param>
        /// <param name="flags">
        /// The binding flags that specify the type of members to include in the search. 
        /// Defaults to public instance properties, ignoring case.
        /// </param>
        /// <returns>
        /// The resolved value of the property path, or null if any segment in the path is 
        /// not found or if the instance is null.
        /// </returns>
        public object ResolveValue(object instance, BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(instance);

            var current = instance;

            foreach (var segment in Segments)
            {
                if (current is null)
                {
                    return null;
                }

                var prop = current.GetType().GetProperty(segment, flags);
                if (prop is null)
                {
                    return null;
                }

                current = prop.GetValue(current);
            }

            return current;
        }

        /// <summary>
        /// Returns the property path reconstructed from its individual segments.
        /// </summary>
        /// <returns>
        /// A dot-separated string composed of the segments that make up the property path.
        /// </returns>
        public override string ToString()
        {
            return string.Join(".", Segments);
        }
    }
}