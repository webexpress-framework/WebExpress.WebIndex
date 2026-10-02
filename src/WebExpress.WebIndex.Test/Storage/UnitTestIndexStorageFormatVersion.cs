using System.Globalization;
using System.Reflection;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Test.Storage
{
    /// <summary>
    /// Tests how the on-disk reverse indexes deal with files of another format version.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestIndexStorageFormatVersion : IDisposable
    {
        private readonly IndexContext _context = new();

        /// <summary>
        /// Initializes a new instance of the class with an index directory of its own.
        /// </summary>
        public UnitTestIndexStorageFormatVersion()
        {
            _context.IndexDirectory = Path.Combine(_context.IndexDirectory, Guid.NewGuid().ToString());
        }

        /// <summary>
        /// The reverse index files are written in format version 2, the one whose posting
        /// nodes carry their subtree height.
        /// </summary>
        [Fact]
        public void ReverseIndexesWriteCurrentVersion()
        {
            // arrange + act
            using (var manager = CreateManager())
            {
                manager.Insert(new FormatDocument() { Id = Guid.NewGuid(), Text = "alpha", Count = 5 });
            }

            // validation
            Assert.Equal((byte)2, IndexStorageSegmentHeader.ReadVersion(ReverseFile("wrt"), "wrt"));
            Assert.Equal((byte)2, IndexStorageSegmentHeader.ReadVersion(ReverseFile("wrn"), "wrn"));
        }

        /// <summary>
        /// A reverse index file of an older format is never read with the current layout: it is
        /// discarded and restored from the document store, so the index answers as before.
        /// </summary>
        [Theory]
        [InlineData("wrt", "Text = 'alpha'")]
        [InlineData("wrn", "Count = 5")]
        public void OutdatedReverseIndexIsRebuiltFromDocumentStore(string extension, string wql)
        {
            // arrange
            var alpha = new FormatDocument() { Id = Guid.NewGuid(), Text = "alpha", Count = 5 };

            using (var manager = CreateManager())
            {
                manager.Insert(alpha);
                manager.Insert(new FormatDocument() { Id = Guid.NewGuid(), Text = "beta", Count = 7 });
            }

            SetVersion(ReverseFile(extension), 1);

            // act
            List<FormatDocument> items;

            using (var reopened = CreateManager())
            {
                items = [.. reopened.Retrieve<FormatDocument>(wql)];
            }

            // validation
            Assert.Equal(alpha.Id, Assert.Single(items).Id);
            Assert.Equal((byte)2, IndexStorageSegmentHeader.ReadVersion(ReverseFile(extension), extension));
        }

        /// <summary>
        /// A file that carries another identifier is no index of the expected kind; it is left
        /// alone rather than deleted, and opening the index reports the mismatch.
        /// </summary>
        [Fact]
        public void ForeignFileIsNotDiscarded()
        {
            // arrange
            Directory.CreateDirectory(_context.IndexDirectory);
            var file = Path.Combine(_context.IndexDirectory, $"{nameof(FormatDocument)}.{nameof(FormatDocument.Text)}.wrt");
            File.WriteAllBytes(file, [(byte)'x', (byte)'y', (byte)'z', 1, 0, 0, 0, 0]);

            // act + validation
            var exception = Assert.ThrowsAny<Exception>(() => CreateManager());
            Assert.IsAssignableFrom<IOException>(exception is TargetInvocationException tie ? tie.InnerException : exception);
            Assert.True(File.Exists(file));
        }

        /// <summary>
        /// Opens a storage index manager on the test directory with the test document registered.
        /// </summary>
        /// <returns>The index manager.</returns>
        private WebIndex.IndexManager CreateManager()
        {
            var manager = new IndexManagerTest();
            typeof(IndexManagerTest)
                .GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, [_context]);

            try
            {
                manager.Create<FormatDocument>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);
            }
            catch
            {
                manager.Dispose();
                throw;
            }

            return manager;
        }

        /// <summary>
        /// Returns the path of the reverse index file with the given extension.
        /// </summary>
        /// <param name="extension">The extension: wrt for the text field, wrn for the numeric one.</param>
        /// <returns>The file path.</returns>
        private string ReverseFile(string extension)
        {
            var field = extension == "wrn" ? nameof(FormatDocument.Count) : nameof(FormatDocument.Text);

            return Path.Combine(_context.IndexDirectory, $"{nameof(FormatDocument)}.{field}.{extension}");
        }

        /// <summary>
        /// Overwrites the version byte of an index file header, as an older release would have written it.
        /// </summary>
        /// <param name="file">The index file.</param>
        /// <param name="version">The version to write.</param>
        private static void SetVersion(string file, byte version)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Write);
            stream.Position = 3;
            stream.WriteByte(version);
        }

        /// <summary>
        /// Removes the index directory.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(_context.IndexDirectory))
            {
                Directory.Delete(_context.IndexDirectory, true);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// A document with a text field and a numeric field, so both reverse index kinds exist.
        /// </summary>
        public class FormatDocument : IIndexItem
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            [IndexIgnore]
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the text.
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Gets or sets the count.
            /// </summary>
            public long Count { get; set; }
        }
    }
}
