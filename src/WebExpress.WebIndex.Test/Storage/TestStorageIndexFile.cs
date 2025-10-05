using WebExpress.WebIndex.Storage;

namespace WebExpress.WebIndex.Test.Storage
{
    /// <summary>
    /// Test class for IndexStorageFile.
    /// </summary>
    public class TestStorageIndexFile : IDisposable
    {
        private readonly string _tempFileName;

        /// <summary>
        /// Initializes a new instance of the class, creating a unique temporary file
        /// for the test run.
        /// </summary>
        /// <remarks>The temporary file is created in the system's temporary directory and is uniquely
        /// named using a GUID to avoid conflicts. This ensures that each test run operates on an isolated
        /// file.</remarks>
        public TestStorageIndexFile()
        {
            // create a unique temp file for each test run
            _tempFileName = Path.Combine(Path.GetTempPath(), $"IndexStorageFileTest_{Guid.NewGuid():N}.dat");
        }

        /// <summary>
        /// Disposes the temp test file after each test.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempFileName))
                {
                    File.Delete(_tempFileName);
                }
            }
            catch
            {
                // ignore errors during test cleanup
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tests that a new IndexStorageFile can be created and basic properties are set.
        /// </summary>
        [Fact]
        public void Create()
        {
            // test execution
            using var storageFile = new IndexStorageFile(_tempFileName);

            // validation
            Assert.Equal(_tempFileName, storageFile.FileName);
            Assert.NotNull(storageFile.FileStream);
            Assert.True(storageFile.FileStream.CanRead);
            Assert.True(storageFile.FileStream.CanWrite);
            Assert.True(File.Exists(_tempFileName));
        }

        /// <summary>
        /// Tests that Alloc returns increasing addresses and NextFreeAddr is updated correctly.
        /// </summary>
        [Fact]
        public void Alloc()
        {
            // preconditions
            using var storageFile = new IndexStorageFile(_tempFileName);

            // test execution
            ulong addr1 = storageFile.Alloc(100);
            ulong addr2 = storageFile.Alloc(200);

            // validation
            Assert.Equal(0ul, addr1);
            Assert.Equal(100ul, addr2);
            Assert.Equal(300ul, storageFile.NextFreeAddr);
        }

        /// <summary>
        /// Tests Delete removes the file from disk.
        /// </summary>
        [Fact]
        public void Delete()
        {
            // preconditions
            using var storageFile = new IndexStorageFile(_tempFileName);

            // test execution
            storageFile.Delete();

            // validation
            Assert.False(File.Exists(_tempFileName));
        }

        /// <summary>
        /// Tests that Dispose closes the stream and buffer.
        /// </summary>
        [Fact]
        public void Closes()
        {
            // preconditions
            var storageFile = new IndexStorageFile(_tempFileName);

            // test execution
            storageFile.Dispose();

            // validation
            Assert.Null(storageFile.FileStream);
        }

        /// <summary>
        /// Tests that Flush calls the buffer's Flush and does not throw if stream is unwritable.
        /// </summary>
        [Fact]
        public void Flush()
        {
            // preconditions
            using var storageFile = new IndexStorageFile(_tempFileName);

            // test execution
            var res = Record.Exception(() => storageFile.Flush());

            // validation
            Assert.Null(res);
        }

        /// <summary>
        /// Tests that Write, Invalidation, and InvalidationAll do not throw with null arguments.
        /// </summary>
        [Fact]
        public void Write()
        {
            // preconditions
            using var storageFile = new IndexStorageFile(_tempFileName);

            // test execution
            var ex1 = Record.Exception(() => storageFile.Write(null));
            var ex2 = Record.Exception(() => storageFile.Invalidation(null));
            var ex3 = Record.Exception(() => storageFile.InvalidationAll());

            // validation
            Assert.Null(ex1);
            Assert.Null(ex2);
            Assert.Null(ex3);
        }
    }
}
