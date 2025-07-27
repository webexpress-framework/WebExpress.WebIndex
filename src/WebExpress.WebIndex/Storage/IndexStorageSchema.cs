using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a index schema file.
    /// </summary>
    /// <typeparam name="TIndexItem">The data type. This must have the IIndexItem interface.</typeparam>
    public class IndexStorageSchema<TIndexItem> : IIndexSchema<TIndexItem>
        where TIndexItem : IIndexItem
    {
        private readonly string _extentions = "ws";
        //private readonly int _version = 1;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
        private static readonly JsonSerializerOptions _jsonDeserializerOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Returns the file name.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Returns the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Return the index field data.
        /// </summary>
        public IEnumerable<IndexFieldData> Fields => GetFieldData(typeof(TIndexItem));

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The index context.</param>
        public IndexStorageSchema(IIndexContext context)
        {
            Context = context;
            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{_extentions}");

            if (!File.Exists(FileName))
            {
                Write();
            }
        }

        /// <summary>
        /// Checks if the schema of the object has changed.
        /// </summary>
        /// <returns>
        /// True if the schema has changed, otherwise false.
        /// </returns>
        /// <remarks>
        /// This function compares the current schema of an object with the stored schema and returns true if there are changes, and false otherwise.
        /// </remarks>
        public bool HasSchemaChanged()
        {
            if (!File.Exists(FileName))
            {
                return true;
            }

            var currentSchema = GetSchema();
            var storedSchema = Read();

            var currentJson = JsonSerializer.Serialize(currentSchema, _jsonSerializerOptions);
            var storedJson = JsonSerializer.Serialize(storedSchema, _jsonSerializerOptions);

            return !currentJson.Equals(storedJson);
        }

        /// <summary>
        /// Migrates the schema if it has changed.
        /// </summary>
        public void Migrate()
        {
            if (HasSchemaChanged())
            {
                Write();
            }
        }

        /// <summary>
        /// Gets the schema of the object.
        /// </summary>
        /// <returns>
        /// The schema of the object as an anonymous type.
        /// </returns>
        private dynamic GetSchema()
        {
            var objectType = typeof(TIndexItem);
            var fields = Fields.Select(x => new
            {
                x.Name,
                Type = GetType(x.PropertyInfo),
                Ignore = !x.Enabled,
                Abstract = x.PropertyInfo.GetGetMethod().IsAbstract
            });
            var schema = new { objectType.Name, Fields = fields };

            return schema;
        }

        /// <summary>
        /// Returns the type.
        /// </summary>
        /// <param name="property">The property info.</param>
        /// <returns>The name of the type.</returns>
        private static string GetType(PropertyInfo property)
        {
            if (property.PropertyType.IsPrimitive)
            {
                return property.PropertyType.Name;
            }
            else if (property.PropertyType == typeof(string))
            {
                return property.PropertyType.Name;
            }
            else if (property.PropertyType == typeof(Guid))
            {
                return property.PropertyType.Name;
            }
            else if (property.PropertyType == typeof(DateTime))
            {
                return property.PropertyType.Name;
            }

            return "Object";
        }

        /// <summary>
        /// Writes the schema of the object to the storage medium.
        /// </summary>
        private void Write()
        {
            var schema = GetSchema();
            var json = JsonSerializer.Serialize(schema, _jsonSerializerOptions);

            File.WriteAllText(FileName, json);
        }

        /// <summary>
        /// Reads the object schema from the storage medium.
        /// </summary>
        /// <returns>
        /// The deserialized schema as an dynamic object.
        /// </returns>
        private dynamic Read()
        {
            var json = File.ReadAllText(FileName);
            var schema = JsonSerializer.Deserialize<dynamic>(json, _jsonDeserializerOptions);

            return schema;
        }

        /// <summary>
        /// Delete this file from storage.
        /// </summary>
        public void Drop()
        {
            Dispose();

            File.Delete(FileName);
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
                var propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

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