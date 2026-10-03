using System.Text.Json;
using System.Text.Json.Nodes;
using WebExpress.WebIndex.Wi;
using WebExpress.WebIndex.Wi.Model;

namespace WebExpress.WebIndex.Test.Wi
{
    /// <summary>
    /// Tests the field types, the export and the import of the wi command line tool.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestWiImportExport : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"wi-{Guid.NewGuid():N}");
        private readonly string _previousDirectory;

        /// <summary>
        /// Initializes a new instance of the class with an empty index directory of its own.
        /// </summary>
        public UnitTestWiImportExport()
        {
            Directory.CreateDirectory(_directory);
            _previousDirectory = WiApp.ViewModel.CurrentDirectory;
            WiApp.ViewModel.CurrentDirectory = _directory;
        }

        /// <summary>
        /// Every numeric type name the index library writes into its schema comes back as a
        /// field type of the same width, so reopening an index neither narrows its values nor
        /// changes its schema.
        /// </summary>
        [Theory]
        [InlineData("Int16", typeof(short))]
        [InlineData("Int32", typeof(int))]
        [InlineData("Int64", typeof(long))]
        [InlineData("Single", typeof(float))]
        [InlineData("Double", typeof(double))]
        [InlineData("Decimal", typeof(decimal))]
        [InlineData("Integer", typeof(int))]
        [InlineData("Long", typeof(long))]
        public void SchemaTypeNameKeepsItsWidth(string name, Type expected)
        {
            Assert.Equal(expected, FieldTypeExtention.FromStringValue(name).ToType());
        }

        /// <summary>
        /// A display name written into an export reads back as the same field type.
        /// </summary>
        [Theory]
        [InlineData(nameof(FieldType.Short))]
        [InlineData(nameof(FieldType.Int))]
        [InlineData(nameof(FieldType.Long))]
        [InlineData(nameof(FieldType.Float))]
        [InlineData(nameof(FieldType.Double))]
        [InlineData(nameof(FieldType.Decimal))]
        public void DisplayNameRoundTrips(string typeName)
        {
            var type = Enum.Parse<FieldType>(typeName);

            Assert.Equal(type, FieldTypeExtention.FromStringValue(FieldTypeExtention.ToString(type)));
        }

        /// <summary>
        /// Values beyond the range of Int32 and digits beyond the precision of Double survive an
        /// export and an import unchanged.
        /// </summary>
        [Fact]
        public void LongAndDecimalSurviveExportAndImport()
        {
            // arrange
            const long big = long.MaxValue - 7;
            const decimal price = 12345678901234567.89m;
            var file = Path.Combine(_directory, "numbers.json");

            CreateIndex("WiNumbers", (big, price));
            WiApp.ViewModel.Export(file);
            DeleteIndex();

            // act
            var count = WiApp.ViewModel.Import(file);

            // validation
            var item = WiApp.ViewModel.CurrentObjectType.All.Single();
            var runtimeClass = item.GetType();

            Assert.Equal(1, count);
            Assert.Equal(typeof(long), runtimeClass.GetProperty("Big").PropertyType);
            Assert.Equal(typeof(decimal), runtimeClass.GetProperty("Price").PropertyType);
            Assert.Equal(big, runtimeClass.GetProperty("Big").GetValue(item));
            Assert.Equal(price, runtimeClass.GetProperty("Price").GetValue(item));
        }

        /// <summary>
        /// An import into a directory that already holds an index of the exported type is refused
        /// rather than merged into the stored items, which stay untouched.
        /// </summary>
        [Fact]
        public void ImportIntoExistingIndexIsRefused()
        {
            // arrange
            var file = Path.Combine(_directory, "numbers.json");

            CreateIndex("WiNumbers", (1, 1m), (2, 2m));
            WiApp.ViewModel.Export(file);

            // act + validation
            Assert.Throws<IndexExistsException>(() => WiApp.ViewModel.Import(file));
            Assert.Equal(2u, WiApp.ViewModel.CurrentObjectType.Count);
        }

        /// <summary>
        /// With replace, the existing index is deleted first, so the result holds exactly the
        /// exported items and nothing of what was stored before.
        /// </summary>
        [Fact]
        public void ImportWithReplaceResetsExistingIndex()
        {
            // arrange
            var file = Path.Combine(_directory, "numbers.json");

            CreateIndex("WiNumbers", (1, 1m));
            WiApp.ViewModel.Export(file);
            DeleteIndex();
            CreateIndex("WiNumbers", (2, 2m), (3, 3m), (4, 4m));

            // act
            WiApp.ViewModel.Import(file, replace: true);

            // validation
            var item = WiApp.ViewModel.CurrentObjectType.All.Single();
            Assert.Equal(1L, item.GetType().GetProperty("Big").GetValue(item));
        }

        /// <summary>
        /// An export whose values do not fit its own schema fails before anything is deleted, so
        /// a replace that cannot complete leaves the existing index as it was.
        /// </summary>
        [Fact]
        public void FailedImportKeepsExistingIndex()
        {
            // arrange
            var file = Path.Combine(_directory, "numbers.json");

            CreateIndex("WiNumbers", (1, 1m));
            WiApp.ViewModel.Export(file);
            DeleteIndex();

            var dump = JsonNode.Parse(File.ReadAllText(file));
            var item = dump["Items"].AsArray()[0].AsObject();
            var big = item.Single(x => x.Key.Equals("Big", StringComparison.OrdinalIgnoreCase)).Key;
            item[big] = "not a number";
            File.WriteAllText(file, dump.ToJsonString());

            CreateIndex("WiNumbers", (2, 2m), (3, 3m), (4, 4m));

            // act + validation
            Assert.ThrowsAny<JsonException>(() => WiApp.ViewModel.Import(file, replace: true));
            Assert.Equal(3u, WiApp.ViewModel.CurrentObjectType.Count);
        }

        /// <summary>
        /// Creates an index with a long and a decimal field and the given items; it is the open one afterwards.
        /// </summary>
        /// <param name="name">The name of the object type.</param>
        /// <param name="items">The values of the items.</param>
        private static void CreateIndex(string name, params (long Big, decimal Price)[] items)
        {
            WiApp.ViewModel.CreateIndexFile(new ObjectType()
            {
                Name = name,
                Fields =
                [
                    new Field() { Name = "Big", Type = FieldType.Long },
                    new Field() { Name = "Price", Type = FieldType.Decimal }
                ]
            });

            var runtimeClass = WiApp.ViewModel.CurrentObjectType.BuildRuntimeClass();

            foreach (var (big, price) in items)
            {
                var item = Activator.CreateInstance(runtimeClass);
                runtimeClass.GetProperty("Id").SetValue(item, Guid.NewGuid());
                runtimeClass.GetProperty("Big").SetValue(item, big);
                runtimeClass.GetProperty("Price").SetValue(item, price);
                WiApp.ViewModel.IndexManager.Insert(runtimeClass, item);
            }
        }

        /// <summary>
        /// Deletes the open index.
        /// </summary>
        private static void DeleteIndex()
        {
            WiApp.ViewModel.DropIndexFile();
        }

        /// <summary>
        /// Closes the open index and removes the index directory.
        /// </summary>
        public void Dispose()
        {
            if (WiApp.ViewModel.CurrentObjectType is not null)
            {
                WiApp.ViewModel.CloseIndexFile();
            }

            WiApp.ViewModel.CurrentDirectory = _previousDirectory;

            try
            {
                Directory.Delete(_directory, true);
            }
            catch (IOException)
            {
                // a file still held by the index manager is left to the temp directory cleanup
            }

            GC.SuppressFinalize(this);
        }
    }
}
