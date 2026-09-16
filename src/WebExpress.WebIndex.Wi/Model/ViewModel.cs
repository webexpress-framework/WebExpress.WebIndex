using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Wi.Converter;

namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// Represents the ViewModel for managing the indexing of project objects.
    /// </summary>
    internal class ViewModel
    {
        /// <summary>
        /// Returns or sets the name of the application.
        /// </summary>
        public string Name { get; private set; } = "wi";

        /// <summary>
        /// Returns the program version.
        /// </summary>
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Manages the indexing of project objects.
        /// </summary>
        public IndexManager IndexManager { get; private set; }

        /// <summary>
        /// Returns or set the current indexfile or the directory.
        /// </summary>
        public string CurrentDirectory { get; set; } = Environment.CurrentDirectory;

        /// <summary>
        /// Returns or set the current indexfile or the directory.
        /// </summary>
        public string CurrentIndexFile { get; set; }

        /// <summary>
        /// Return or sets the current object type.
        /// </summary>
        public ObjectType CurrentObjectType { get; set; }

        /// <summary>
        /// Return or sets the current field.
        /// </summary>
        public Field CurrentIndexField { get; set; }

        /// <summary>
        /// Creates an index file for an object type without fields.
        /// </summary>
        /// <param name="indexFile">The name of the index file.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool CreateIndexFile(string indexFile)
        {
            return CreateIndexFile(new ObjectType() { Name = indexFile });
        }

        /// <summary>
        /// Creates an index file for the given object type in the current directory.
        /// </summary>
        /// <param name="objectType">The object type the index holds.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool CreateIndexFile(ObjectType objectType)
        {
            CurrentObjectType = objectType;
            CurrentIndexFile = Path.Combine(CurrentDirectory, $"{objectType.Name}.ws");

            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var context = new IndexContext { IndexDirectory = CurrentDirectory };
            IndexManager = new IndexManager();

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManager).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(IndexManager, [context]);

            IndexManager.Create(runtimeClass, CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            return true;
        }

        /// <summary>
        /// Opens the specified index file.
        /// </summary>
        /// <param name="indexFile">The full path to the index file.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool OpenIndexFile(string indexFile)
        {
            CurrentDirectory = Path.GetDirectoryName(indexFile);
            CurrentIndexFile = indexFile;

            var schema = File.ReadAllText(CurrentIndexFile);
            var options = new JsonSerializerOptions { Converters = { new FieldTypeConverter() } };
            CurrentObjectType = JsonSerializer.Deserialize<ObjectType>(schema, options);

            var runtimeClass = CurrentObjectType.BuildRuntimeClass();

            var context = new IndexContext { IndexDirectory = CurrentDirectory };
            IndexManager = new IndexManager();

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManager).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(IndexManager, [context]);

            IndexManager.Create(runtimeClass, CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            return true;
        }

        /// <summary>
        /// Opens the specified index field.
        /// </summary>
        /// <param name="indexField">The the index field.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool OpenIndexField(Field indexField)
        {
            CurrentIndexField = indexField;

            return true;
        }

        /// <summary>
        /// Close the current index file.
        /// </summary>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool CloseIndexFile()
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            IndexManager.Close(runtimeClass);
            CurrentIndexFile = null;

            CurrentObjectType = null;

            return true;
        }

        /// <summary>
        /// Drop the current index file.
        /// </summary>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool DropIndexFile()
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            IndexManager.Drop(runtimeClass);
            CurrentIndexFile = null;

            CurrentObjectType = null;

            return true;
        }

        /// <summary>
        /// Inserts an item built from the given field values into the current index.
        /// </summary>
        /// <param name="values">The field values, see <see cref="ParseValues"/>.</param>
        /// <returns>The id of the new item.</returns>
        public Guid Insert(string values)
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var item = Activator.CreateInstance(runtimeClass);
            var id = Guid.NewGuid();

            runtimeClass.GetProperty("Id").SetValue(item, id);
            Assign(item, ParseValues(runtimeClass, values));
            IndexManager.Insert(runtimeClass, item);

            return id;
        }

        /// <summary>
        /// Changes the given field values of an item of the current index.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <param name="values">The field values, see <see cref="ParseValues"/>.</param>
        /// <returns>True when the item exists, otherwise false.</returns>
        public bool Update(Guid id, string values)
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var item = FindItem(runtimeClass, id);

            if (item is null)
            {
                return false;
            }

            Assign(item, ParseValues(runtimeClass, values));
            IndexManager.Update(runtimeClass, item);

            return true;
        }

        /// <summary>
        /// Removes an item from the current index.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <returns>True when the item existed, otherwise false.</returns>
        public bool Delete(Guid id)
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();

            if (FindItem(runtimeClass, id) is null)
            {
                return false;
            }

            IndexManager.Delete(runtimeClass, id);

            return true;
        }

        /// <summary>
        /// Writes the current index - its object type and every item - into a file.
        /// </summary>
        /// <param name="file">The path of the export file.</param>
        /// <returns>The number of exported items.</returns>
        public int Export(string file)
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var properties = runtimeClass.GetProperties();
            var dump = new IndexDump
            {
                Name = CurrentObjectType.Name,
                Fields = CurrentObjectType.Fields,
                Items = [.. CurrentObjectType.All.Select(item => properties.ToDictionary
                (
                    x => x.Name,
                    x => JsonSerializer.SerializeToElement(x.GetValue(item), x.PropertyType)
                ))]
            };

            File.WriteAllText(file, JsonSerializer.Serialize(dump, DumpOptions));

            return dump.Items.Count;
        }

        /// <summary>
        /// Creates an index from an export file in the current directory and fills it with the
        /// exported items; the index is the open one afterwards.
        /// </summary>
        /// <param name="file">The path of the export file.</param>
        /// <returns>The number of imported items.</returns>
        public int Import(string file)
        {
            var dump = JsonSerializer.Deserialize<IndexDump>(File.ReadAllText(file), DumpOptions);

            if (string.IsNullOrWhiteSpace(dump?.Name))
            {
                throw new FormatException("The export file names no object type.");
            }

            CreateIndexFile(new ObjectType() { Name = dump.Name, Fields = [.. dump.Fields] });

            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var properties = runtimeClass.GetProperties().ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var values in dump.Items)
            {
                var item = Activator.CreateInstance(runtimeClass);

                foreach (var (name, value) in values)
                {
                    if (properties.TryGetValue(name, out var property))
                    {
                        property.SetValue(item, value.Deserialize(property.PropertyType));
                    }
                }

                // an item without an id would collide with every other such item; a fresh
                // one keeps the import going, the item is just no longer the exported one
                if ((Guid)properties["Id"].GetValue(item) == Guid.Empty)
                {
                    properties["Id"].SetValue(item, Guid.NewGuid());
                }

                IndexManager.Insert(runtimeClass, item);
            }

            return dump.Items.Count;
        }

        /// <summary>
        /// Returns the index terms.
        /// </summary>
        /// <returns>The index terms</returns>
        public IEnumerable<(string, uint, uint, uint, IEnumerable<Guid>)> GetIndexTerms()
        {
            var runtimeClass = CurrentObjectType.BuildRuntimeClass();
            var document = IndexManager.GetIndexDocument(runtimeClass);
            var fieldProperty = runtimeClass.GetProperty(CurrentIndexField?.Name);
            var fieldData = new IndexFieldData(fieldProperty);
            var methodInfo = document.GetType().GetMethod("GetReverseIndex");
            var reverseIndex = methodInfo.Invoke(document, [fieldData]);
            var termProperty = reverseIndex.GetType().GetProperty("Term");
            var term = termProperty.GetValue(reverseIndex) as IndexStorageSegmentTerm;

            return term.Terms.Select(x =>
            (
                x.Item1,
                x.Item2.Frequency,
                x.Item2.Posting.Height,
                x.Item2.Posting.Balance,
                x.Item2.Posting.PreOrder.Select(y => y.DocumentID)
            ));
        }

        /// <summary>
        /// The serializer options of the export file: readable, and with the field types under
        /// the names the tool shows.
        /// </summary>
        private static JsonSerializerOptions DumpOptions { get; } = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new FieldTypeConverter() }
        };

        /// <summary>
        /// Reads a list of field values as typed on the command line.
        /// </summary>
        /// <remarks>
        /// Values are separated by commas. A value is either positional - in the order the
        /// fields are listed, leaving out the id, which is never typed - or named as
        /// <c>field=value</c>, and a value may be quoted to carry a comma or surrounding
        /// blanks. Both forms may be mixed, so a single field of a wide object can be changed
        /// without spelling out the others.
        /// </remarks>
        /// <param name="runtimeClass">The runtime class of the object type.</param>
        /// <param name="values">The typed values.</param>
        /// <returns>The values converted to the property types, keyed by property.</returns>
        /// <exception cref="FormatException">Thrown when a value does not fit its field or
        /// there are more values than fields.</exception>
        private static Dictionary<PropertyInfo, object> ParseValues(Type runtimeClass, string values)
        {
            var fields = runtimeClass.GetProperties()
                .Where(x => !x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var result = new Dictionary<PropertyInfo, object>();
            var position = 0;

            foreach (var token in SplitValues(values ?? ""))
            {
                var separator = token.IndexOf('=');
                var field = separator > 0
                    ? fields.FirstOrDefault(x => x.Name.Equals(token[..separator].Trim(), StringComparison.OrdinalIgnoreCase))
                    : null;
                var text = field is not null ? token[(separator + 1)..] : token;

                if (field is null)
                {
                    if (position >= fields.Count)
                    {
                        throw new FormatException($"More values than fields: '{token}' has no field to go to.");
                    }

                    field = fields[position++];
                }

                result[field] = ConvertValue(Unquote(text), field);
            }

            return result;
        }

        /// <summary>
        /// Splits the typed values at the commas that are not inside quotes.
        /// </summary>
        /// <param name="values">The typed values.</param>
        /// <returns>The trimmed tokens.</returns>
        private static IEnumerable<string> SplitValues(string values)
        {
            var token = new StringBuilder();
            var quote = '\0';

            foreach (var c in values)
            {
                if (quote != '\0')
                {
                    if (c == quote)
                    {
                        quote = '\0';
                    }

                    token.Append(c);
                }
                else if (c is '"' or '\'')
                {
                    quote = c;
                    token.Append(c);
                }
                else if (c == ',')
                {
                    yield return token.ToString().Trim();
                    token.Clear();
                }
                else
                {
                    token.Append(c);
                }
            }

            if (token.Length > 0)
            {
                yield return token.ToString().Trim();
            }
        }

        /// <summary>
        /// Removes one pair of matching quotes around a value.
        /// </summary>
        /// <param name="text">The value as typed.</param>
        /// <returns>The value without its quotes.</returns>
        private static string Unquote(string text)
        {
            text = text.Trim();

            return text.Length >= 2 && (text[0] is '"' or '\'') && text[^1] == text[0]
                ? text[1..^1]
                : text;
        }

        /// <summary>
        /// Converts one typed value to the type of its field.
        /// </summary>
        /// <param name="text">The value as typed.</param>
        /// <param name="field">The field the value goes to.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="FormatException">Thrown when the value does not fit the field.</exception>
        private static object ConvertValue(string text, PropertyInfo field)
        {
            var type = Nullable.GetUnderlyingType(field.PropertyType) ?? field.PropertyType;

            try
            {
                if (type == typeof(string) || type == typeof(object))
                {
                    return text;
                }

                if (type == typeof(Guid))
                {
                    return Guid.Parse(text);
                }

                if (type == typeof(DateTime))
                {
                    return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                }

                return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new FormatException($"'{text}' is not a valid {field.PropertyType.Name} for field '{field.Name}'.", ex);
            }
        }

        /// <summary>
        /// Writes converted values into an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="values">The values keyed by property.</param>
        private static void Assign(object item, Dictionary<PropertyInfo, object> values)
        {
            foreach (var (property, value) in values)
            {
                property.SetValue(item, value);
            }
        }

        /// <summary>
        /// Finds an item of the current index by its id.
        /// </summary>
        /// <param name="runtimeClass">The runtime class of the object type.</param>
        /// <param name="id">The id of the item.</param>
        /// <returns>The item, or null when there is none.</returns>
        private object FindItem(Type runtimeClass, Guid id)
        {
            var idProperty = runtimeClass.GetProperty("Id");

            return IndexManager.All(runtimeClass).FirstOrDefault(x => (Guid)idProperty.GetValue(x) == id);
        }
    }
}
