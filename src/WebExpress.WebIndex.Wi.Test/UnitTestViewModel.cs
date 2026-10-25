using System.Text.Json;
using WebExpress.WebIndex.Wi.Model;
using Xunit;

namespace WebExpress.WebIndex.Wi.Test
{
    /// <summary>
    /// Exercises isolated storage indexes through the same model used by the console.
    /// </summary>
    public class UnitTestViewModel : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "wi-tests", Guid.NewGuid().ToString("N"));
        private readonly ViewModel _model;

        /// <summary>
        /// Gives each test its own files and manager so tests can run independently.
        /// </summary>
        public UnitTestViewModel()
        {
            Directory.CreateDirectory(_directory);
            _model = new ViewModel { CurrentDirectory = _directory };
        }

        /// <summary>
        /// Verifies typed insert, partial update, lookup, deletion and missing identifiers.
        /// </summary>
        [Fact]
        public void CrudPreservesUnchangedFields()
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("Name='Hello, World', Quantity=7");

            // act
            Assert.True(_model.Update(id, "Quantity=9"));
            var item = Assert.Single(Items());

            // validation
            Assert.Equal("Hello, World", Value(item, "Name"));
            Assert.Equal(9, Value(item, "Quantity"));
            Assert.Equal(id, Value(item, "Id"));
            Assert.True(_model.Delete(id));
            Assert.Empty(Items());
            Assert.False(_model.Update(id, "Quantity=1"));
            Assert.False(_model.Delete(id));
        }

        /// <summary>
        /// Rejects malformed input before any stored item is modified.
        /// </summary>
        /// <param name="values">The malformed field values.</param>
        [Theory]
        [InlineData("Unknown=value")]
        [InlineData("Name='unterminated")]
        [InlineData("Name=changed, Quantity=invalid")]
        [InlineData("Name=one, Name=two")]
        [InlineData("Name=changed, Quantity=9999999999999999")]
        public void InvalidUpdatePreservesItem(string values)
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("original, 7");

            // act
            Assert.Throws<FormatException>(() => _model.Update(id, values));

            // validation
            var item = Assert.Single(Items());
            Assert.Equal("original", Value(item, "Name"));
            Assert.Equal(7, Value(item, "Quantity"));
        }

        /// <summary>
        /// Applies positional values to fields not already assigned by name.
        /// </summary>
        [Fact]
        public void MixedValuesDoNotOverwriteNamedFields()
        {
            // arrange
            CreateIndex();

            // act
            _model.Insert("Name=hello, 7");

            // validation
            var item = Assert.Single(Items());
            Assert.Equal("hello", Value(item, "Name"));
            Assert.Equal(7, Value(item, "Quantity"));
        }

        /// <summary>
        /// Preserves quoted delimiters, escaped quotes and a trailing empty text field.
        /// </summary>
        [Fact]
        public void QuotedValuesAndEmptyTailRoundTrip()
        {
            // arrange
            _model.CreateIndexFile(new ObjectType { Name = "Notes", Fields = [
                new Field { Name = "Title", Type = FieldType.Text },
                new Field { Name = "Body", Type = FieldType.Text }] });

            // act
            _model.Insert("\"say \"\"hello\"\", a=b\",");

            // validation
            var item = Assert.Single(Items());
            Assert.Equal("say \"hello\", a=b", Value(item, "Title"));
            Assert.Equal("", Value(item, "Body"));
        }

        /// <summary>
        /// Confirms storage can be reopened after closing and the field selection is cleared.
        /// </summary>
        [Fact]
        public void CloseReopenPreservesDataAndResetsSelection()
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("saved, 3");
            var file = _model.CurrentIndexFile;
            _model.OpenIndexField(_model.CurrentObjectType.Fields.First());

            // act
            _model.CloseIndexFile();

            // validation
            Assert.Null(_model.CurrentIndexField);
            Assert.Null(_model.CurrentObjectType);
            Assert.True(_model.CloseIndexFile());
            _model.OpenIndexFile(file);
            Assert.Equal(id, Value(Assert.Single(Items()), "Id"));
        }

        /// <summary>
        /// Uses the owning model for export and preserves ids across import.
        /// </summary>
        [Fact]
        public void ExportImportRoundTripsIndependentModel()
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("example, 17");
            var file = Path.Combine(_directory, "dump.json");

            // act
            Assert.Equal(1, _model.Export(file));
            _model.DropIndexFile();
            Assert.Equal(1, _model.Import(file));

            // validation
            var item = Assert.Single(Items());
            Assert.Equal(id, Value(item, "Id"));
            Assert.Equal("example", Value(item, "Name"));
            Assert.Equal(17, Value(item, "Quantity"));
        }

        /// <summary>
        /// Prevents invalid imports from replacing existing data.
        /// </summary>
        /// <param name="items">The invalid serialized items.</param>
        [Theory]
        [InlineData("null")]
        [InlineData("[null]")]
        [InlineData("[{\"Quantity\":\"invalid\"}]")]
        [InlineData("[{\"Unknown\":1}]")]
        [InlineData("[{\"Id\":\"11111111-1111-1111-1111-111111111111\"},{\"Id\":\"11111111-1111-1111-1111-111111111111\"}]")]
        public void InvalidReplacementPreservesExistingData(string items)
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("original, 1");
            var file = Path.Combine(_directory, "invalid.json");
            File.WriteAllText(file, "{\"Type\":\"Products\",\"Fields\":[{\"Name\":\"Quantity\",\"Type\":\"Integer\"}],\"Items\":" + items + "}");

            // act
            var error = Record.Exception(() => _model.Import(file, true));

            // validation
            Assert.True(error is FormatException or JsonException, error?.ToString());
            Assert.Equal(id, Value(Assert.Single(Items()), "Id"));
        }

        /// <summary>
        /// Requires explicit replacement and then replaces all existing items.
        /// </summary>
        [Fact]
        public void ImportRequiresReplacementAndDoesNotMerge()
        {
            // arrange
            CreateIndex();
            _model.Insert("old, 1");
            var file = Path.Combine(_directory, "replacement.json");
            File.WriteAllText(file, "{\"Type\":\"Products\",\"Fields\":[{\"Name\":\"Name\",\"Type\":\"Text\"}],\"Items\":[{\"Name\":\"new\"}]}");

            // act
            Assert.Throws<IndexExistsException>(() => _model.Import(file));
            var count = _model.Import(file, true);

            // validation
            Assert.Equal(1, count);
            Assert.Equal("new", Value(Assert.Single(Items()), "Name"));
        }

        /// <summary>
        /// Releases the active storage before replacing a file name with different Windows casing.
        /// </summary>
        [Fact]
        public void ReplacementReleasesActiveStorage()
        {
            // arrange
            CreateIndex();
            _model.Insert("original, 1");
            var name = OperatingSystem.IsWindows() ? "products" : "Products";
            var file = Path.Combine(_directory, "replacement.json");
            File.WriteAllText(file, JsonSerializer.Serialize(new { Type = name, Fields = Array.Empty<object>(), Items = Array.Empty<object>() }));

            // act
            var count = _model.Import(file, true);

            // validation
            Assert.Equal(0, count);
            Assert.Equal(name, _model.CurrentObjectType.Name);
            Assert.Empty(_model.All);
        }

        /// <summary>
        /// Rejects unsafe schema names before they can address files outside the index directory.
        /// </summary>
        /// <param name="name">The invalid object name.</param>
        [Theory]
        [InlineData("../outside")]
        [InlineData("bad*name")]
        [InlineData("Namespace.Record")]
        [InlineData("")]
        public void CreateRejectsInvalidNames(string name)
        {
            // arrange
            var objectType = new ObjectType { Name = name };

            // act
            var error = Record.Exception(() => _model.CreateIndexFile(objectType));

            // validation
            Assert.IsType<FormatException>(error);
            Assert.Null(_model.CurrentObjectType);
            Assert.Empty(Directory.GetFiles(_directory));
        }

        /// <summary>
        /// Binds valid WQL to the active runtime type and reports parse errors.
        /// </summary>
        [Fact]
        public void WqlFiltersRuntimeItems()
        {
            // arrange
            CreateIndex();
            _model.Insert("first, 1");
            _model.Insert("second, 2");
            var type = _model.CurrentObjectType.BuildRuntimeClass();

            // act
            var statement = _model.IndexManager.Retrieve(type, "Quantity = 2");
            var invalid = _model.IndexManager.Retrieve(type, "Quantity =");

            // validation
            Assert.False(statement.HasErrors);
            Assert.Equal("second", Value(Assert.Single(statement.Apply(type)), "Name"));
            Assert.True(invalid.HasErrors);
            Assert.NotNull(invalid.Error);
            Assert.Empty(invalid.Apply(type));
            Assert.Throws<ArgumentException>(() => statement.Apply(typeof(string)).ToArray());
        }

        /// <summary>
        /// Releases the previous manager when switching indexes and keeps both stores readable.
        /// </summary>
        [Fact]
        public void SwitchingIndexesReleasesPreviousFiles()
        {
            // arrange
            CreateIndex();
            _model.Insert("saved, 1");
            var file = _model.CurrentIndexFile;
            var oldManager = _model.IndexManager;

            // act
            _model.CreateIndexFile("Other");

            // validation
            Assert.Empty(oldManager.DocumentTypes);
            Assert.Equal(0u, _model.Count);
            _model.OpenIndexFile(file);
            Assert.Equal("saved", Value(Assert.Single(_model.All), "Name"));
        }

        /// <summary>
        /// Preserves the active index when a new schema cannot be parsed.
        /// </summary>
        [Fact]
        public void InvalidSchemaPreservesCurrentSelection()
        {
            // arrange
            CreateIndex();
            var selected = _model.CurrentObjectType;
            var file = Path.Combine(_directory, "Broken.ws");
            File.WriteAllText(file, "null");

            // act
            Assert.Throws<FormatException>(() => _model.OpenIndexFile(file));

            // validation
            Assert.Same(selected, _model.CurrentObjectType);
            Assert.Equal(0u, _model.Count);
        }

        /// <summary>
        /// Exercises term inspection and rejects unsupported numeric term details explicitly.
        /// </summary>
        [Fact]
        public void TermInspectionUsesSelectedTextField()
        {
            // arrange
            CreateIndex();
            var id = _model.Insert("aurora, 1");
            _model.OpenIndexField(_model.CurrentObjectType.Fields.First());

            // act
            var terms = _model.GetIndexTerms().ToList();

            // validation
            Assert.Contains(terms, x => x.Item1 == "aurora" && x.Item5.Contains(id));
            _model.OpenIndexField(_model.CurrentObjectType.Fields.Last());
            Assert.Throws<NotSupportedException>(() => _model.GetIndexTerms().ToList());
        }

        /// <summary>
        /// Verifies runtime manager clear and close operations address the selected type.
        /// </summary>
        [Fact]
        public void RuntimeManagerClearAndClose()
        {
            // arrange
            CreateIndex();
            _model.Insert("hello, 1");
            var type = _model.CurrentObjectType.BuildRuntimeClass();

            // act
            _model.IndexManager.Clear(type);

            // validation
            Assert.Equal(0u, _model.Count);
            Assert.Empty(_model.All);
            _model.IndexManager.Close(type);
            Assert.Null(_model.IndexManager.GetIndexDocument(type));
        }

        /// <summary>
        /// Creates the shared schema while keeping each test's storage independent.
        /// </summary>
        private void CreateIndex()
        {
            _model.CreateIndexFile(new ObjectType { Name = "Products", Fields = [
                new Field { Name = "Name", Type = FieldType.Text },
                new Field { Name = "Quantity", Type = FieldType.Int }] });
        }

        /// <summary>
        /// Reads items through the current model's manager.
        /// </summary>
        /// <returns>The active index items.</returns>
        private IEnumerable<object> Items()
        {
            return _model.IndexManager.All(_model.CurrentObjectType.BuildRuntimeClass());
        }

        /// <summary>
        /// Reads a property of a dynamically generated item for assertions.
        /// </summary>
        /// <param name="item">The runtime item.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The stored property value.</returns>
        private static object Value(object item, string name)
        {
            return item.GetType().GetProperty(name).GetValue(item);
        }

        /// <summary>
        /// Releases storage handles before deleting this test's unique directory.
        /// </summary>
        public void Dispose()
        {
            _model.IndexManager?.Dispose();
            Directory.Delete(_directory, true);
        }
    }
}
