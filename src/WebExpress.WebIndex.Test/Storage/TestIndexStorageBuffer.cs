using WebExpress.WebIndex.Storage;

namespace WebExpress.WebIndex.Test.Storage
{
    /// <summary>
    /// Test class for IndexStorageBuffer.
    /// Ensures correct caching, writing, invalidation and flush logic.
    /// </summary>
    [Collection("IndexStorageBufferTests")]
    [Trait("Category", "Buffer")]
    public class TestIndexStorageBuffer : IDisposable
    {
        private readonly string _tempFileName;
        private readonly MemoryStream _stream;
        private readonly IndexStorageFile _file;
        private readonly IndexStorageBuffer _buffer;

        /// <summary>
        /// Creates an in-memory IndexStorageFile and buffer for testing.
        /// </summary>
        public TestIndexStorageBuffer()
        {
            // create a unique temp file for each test run
            _tempFileName = Path.Combine(Path.GetTempPath(), $"IndexStorageFileTest_{Guid.NewGuid():N}.dat");
            _stream = new MemoryStream();
            _file = new IndexStorageFile(_tempFileName);
            _buffer = new IndexStorageBuffer(_file);
        }

        /// <summary>
        /// Disposes buffer, file and stream after each test.
        /// </summary>
        public void Dispose()
        {
            _buffer.Dispose();
            _file.Dispose();
            _stream.Dispose();

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
        /// Tests that Write caches a segment and it is available for read.
        /// </summary>
        [Fact]
        public void Write()
        {
            // preconditions
            var segment = new TestSegment(0);

            // test execution
            _buffer.Write(segment);

            // validation
            var result = _buffer.Read<TestSegment>(segment.Addr, new IndexStorageContextMock());
            Assert.Equal(segment, result);
        }

        /// <summary>
        /// Tests that Flush persists scheduled writes.
        /// </summary>
        [Fact]
        public void Flush()
        {
            // preconditions
            var segment = new TestSegment(3);
            _buffer.Write(segment);

            // test execution
            _buffer.Flush();

            // validation
            Assert.True(segment.Written);
        }

        /// <summary>
        /// Tests that Dispose stops the timer and flushes.
        /// </summary>
        [Fact]
        public void Close()
        {
            // preconditions
            var segment = new TestSegment(4);
            _buffer.Write(segment);

            // test execution
            _buffer.Dispose();

            // validation
            Assert.True(segment.Written);
        }

        /// <summary>
        /// Simple mock for a segment.
        /// </summary>
        private class TestSegment(ulong addr) : IIndexStorageSegment
        {
            public ulong Addr { get; } = addr;
            public bool Written { get; private set; }

            public IndexStorageContext Context => throw new NotImplementedException();

            public void Read(BinaryReader reader) { /* nothing */ }
            public void Write(BinaryWriter writer) { Written = true; }
        }

        /// <summary>
        /// Simple mock for IndexStorageContext.
        /// </summary>
        private class IndexStorageContextMock : IndexStorageContext
        {
            public IndexStorageContextMock() : base(null) { }
        }
    }
}