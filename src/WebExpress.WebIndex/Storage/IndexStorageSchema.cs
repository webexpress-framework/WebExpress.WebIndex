using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a schema file stored on disk for the given index item type.
    /// Generates a deterministic schema representation and migrates the stored schema when it changes.
    /// </summary>
    /// <typeparam name="TIndexItem">The index item type, must implement IIndexItem and be non-nullable.</typeparam>
    public class IndexStorageSchema<TIndexItem> : IIndexSchema<TIndexItem>
        where TIndexItem : IIndexItem
    {
        // file extension for schema files
        private const string _extension = "ws";

        // deterministic serializer options
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        // case insensitive deserializer options for compatibility
        private static readonly JsonSerializerOptions _jsonDeserializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Gets the file name of the schema file.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Gets the index fields discovered for the type parameter.
        /// </summary>
        public IEnumerable<IndexFieldData> Fields
        {
            get
            {
                return GetFieldData(typeof(TIndexItem));
            }
        }

        /// <summary>
        /// Initializes a new instance of the schema manager.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context or its IndexDirectory is null/empty.</exception>
        public IndexStorageSchema(IIndexContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.IndexDirectory))
            {
                throw new ArgumentNullException(nameof(context), "Index directory must be provided.");
            }

            Context = context;

            // ensure directory exists
            Directory.CreateDirectory(Context.IndexDirectory);

            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{_extension}");

            if (!File.Exists(FileName))
            {
                Write();
            }
        }

        /// <summary>
        /// Determines whether the schema has changed compared to the stored one.
        /// </summary>
        /// <returns>True when the schema has changed; otherwise false.</returns>
        public bool HasSchemaChanged()
        {
            if (!File.Exists(FileName))
            {
                return true;
            }

            var current = BuildSchemaModel();
            var stored = ReadSchemaModel();

            if (stored is null)
            {
                // unreadable or incompatible stored schema -> trigger migration
                return true;
            }

            NormalizeSchemaModel(current);
            NormalizeSchemaModel(stored);

            // compare schema models structurally
            if (!string.Equals(current.Type, stored.Type, StringComparison.Ordinal))
            {
                return true;
            }

            if (current.Fields.Count != stored.Fields.Count)
            {
                return true;
            }

            for (int i = 0; i < current.Fields.Count; i++)
            {
                var a = current.Fields[i];
                var b = stored.Fields[i];

                if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal) ||
                    !string.Equals(a.Type, b.Type, StringComparison.Ordinal) ||
                    a.Ignore != b.Ignore ||
                    a.Abstract != b.Abstract)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Migrates the stored schema file if it differs from the current schema.
        /// </summary>
        public void Migrate()
        {
            if (HasSchemaChanged())
            {
                Write();
            }
        }

        /// <summary>
        /// Writes the current schema to disk atomically.
        /// </summary>
        private void Write()
        {
            var schema = BuildSchemaModel();
            NormalizeSchemaModel(schema);

            var json = JsonSerializer.Serialize(schema, _jsonSerializerOptions);

            // ensure directory exists (defensive)
            var dir = Path.GetDirectoryName(FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // atomic write via temp file
            var tmp = FileName + ".tmp";
            try
            {
                File.WriteAllText(tmp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                File.Replace(tmp, FileName, null);
            }
            catch
            {
                // fallback to non-atomic write on failure
                try
                {
                    File.WriteAllText(FileName, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
                catch
                {
                    // swallow to avoid throwing during initialization paths
                }
                finally
                {
                    // cleanup
                    try
                    {
                        if (File.Exists(tmp))
                        {
                            File.Delete(tmp);
                        }
                    }
                    catch
                    {
                        // ignore cleanup failures
                    }
                }
            }
        }

        /// <summary>
        /// Reads and deserializes the stored schema model from disk.
        /// Returns null when unreadable or incompatible.
        /// </summary>
        private SchemaModel ReadSchemaModel()
        {
            try
            {
                var json = File.ReadAllText(FileName);
                var model = JsonSerializer.Deserialize<SchemaModel>(json, _jsonDeserializerOptions);

                return model;
            }
            catch
            {
                // unreadable or incompatible schema file
                return null;
            }
        }

        /// <summary>
        /// Returns a deterministic schema model for the current type.
        /// </summary>
        private SchemaModel BuildSchemaModel()
        {
            var objectType = typeof(TIndexItem);

            // build fields with deterministic type names and flags
            var fields = Fields
                .Select(x => new FieldModel
                {
                    Name = x.Name,
                    Type = GetStableTypeName(x.PropertyInfo),
                    Ignore = !x.Enabled,
                    Abstract = x.PropertyInfo.GetMethod?.IsAbstract ?? false
                })
                .ToList();

            // wrap into schema model
            return new SchemaModel
            {
                Type = objectType.FullName ?? objectType.Name,
                Fields = fields
            };
        }

        /// <summary>
        /// Normalizes a schema model: sorts fields by name to ensure deterministic order.
        /// </summary>
        private static void NormalizeSchemaModel(SchemaModel model)
        {
            if (model is null)
            {
                return;
            }

            model.Fields = [.. (model.Fields ?? [])
                .OrderBy(f => f.Name, StringComparer.Ordinal)];
        }

        /// <summary>
        /// Produces a stable type name for a property, unwrapping Nullable&lt;T&gt; and classifying common primitives.
        /// </summary>
        private static string GetStableTypeName(PropertyInfo property)
        {
            if (property is null)
            {
                return "Object";
            }

            var t = property.PropertyType;

            // unwrap Nullable<T>
            var underlying = Nullable.GetUnderlyingType(t);
            if (underlying is not null)
            {
                t = underlying;
            }

            if (t.IsPrimitive)
            {
                return t.Name;
            }

            if (t == typeof(string))
            {
                return t.Name;
            }

            if (t == typeof(decimal))
            {
                return t.Name;
            }

            if (t == typeof(Guid))
            {
                return t.Name;
            }

            if (t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan))
            {
                return t.Name;
            }

            if (t.IsEnum)
            {
                return "Enum";
            }

            // collections and arrays
            if (t.IsArray)
            {
                return $"{t.GetElementType()?.Name ?? "Object"}[]";
            }

            // fallback to simple name to keep schema readable
            return t.Name ?? "Object";
        }

        /// <summary>
        /// Recursively retrieves field descriptors for the given type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="prefix">An optional prefix for nested properties.</param>
        /// <param name="processedTypes">Visited types to avoid cycles.</param>
        private static IEnumerable<IndexFieldData> GetFieldData(Type type, string prefix = "", HashSet<Type> processedTypes = null)
        {
            processedTypes ??= [];

            if (processedTypes.Contains(type))
            {
                yield break;
            }

            processedTypes.Add(type);

            // only consider public instance properties; skip indexers
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                // leaf when not a class or is string
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                {
                    yield return new IndexFieldData
                    {
                        Name = propertyName,
                        Type = property.PropertyType,
                        PropertyInfo = property
                    };
                }

                // recurse into nested classes (excluding string)
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    foreach (var subProperty in GetFieldData(property.PropertyType, propertyName, processedTypes))
                    {
                        yield return subProperty;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the schema file from disk.
        /// </summary>
        public void Drop()
        {
            Dispose();

            try
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
            }
            catch
            {
                // ignore file system errors during drop
            }
        }

        /// <summary>
        /// Disposes this instance (no unmanaged resources).
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Simple schema model used for deterministic serialization and comparison.
        /// </summary>
        private sealed class SchemaModel
        {
            public string Type { get; set; } = string.Empty;
            public List<FieldModel> Fields { get; set; } = [];
        }

        /// <summary>
        /// Field descriptor used within the schema model.
        /// </summary>
        private sealed class FieldModel
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = "Object";
            public bool Ignore { get; set; }
            public bool Abstract { get; set; }
        }
    }
}