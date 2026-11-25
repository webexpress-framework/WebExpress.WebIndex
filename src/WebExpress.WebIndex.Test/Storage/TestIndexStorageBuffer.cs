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
            // arrange
            var segment = new TestSegment(0);

            // act
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
            // arrange
            var segment = new TestSegment(3);
            _buffer.Write(segment);

            // act
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
            // arrange
            var segment = new TestSegment(4);
            _buffer.Write(segment);

            // act
            _buffer.Dispose();

            // validation
            Assert.True(segment.Written);
        }

        /// <summary>
        /// A segment written to an address whose free-list record is still cached - the record
        /// the allocator read to hand the address out - is what the next read of the address
        /// returns, not the bytes that are still on disk.
        /// </summary>
        [Fact]
        public void WriteToAReusedAddressShadowsTheCachedFreeRecord()
        {
            // arrange
            var context = new IndexStorageContextMock();
            _buffer.Write(new IndexStorageSegmentFree(context, 0) { SuccessorAddr = 99 });
            _buffer.Flush();
            _buffer.Read<IndexStorageSegmentFree>(0, context);

            var position = new IndexStorageSegmentPosition(context, 0) { Position = 7 };

            // act
            _buffer.Write(position);
            var result = _buffer.Read<IndexStorageSegmentPosition>(0, context);

            // validation
            Assert.Same(position, result);
        }

        /// <summary>
        /// A new segment written to an address replaces a cached segment of the same type there.
        /// </summary>
        [Fact]
        public void WriteReplacesAStaleCachedSegment()
        {
            // arrange
            var context = new IndexStorageContextMock();
            _buffer.Write(new IndexStorageSegmentPosition(context, 0) { Position = 1 });
            _buffer.Flush();
            _buffer.Read<IndexStorageSegmentPosition>(0, context);

            var position = new IndexStorageSegmentPosition(context, 0) { Position = 2 };

            // act
            _buffer.Write(position);
            var result = _buffer.Read<IndexStorageSegmentPosition>(0, context);

            // validation
            Assert.Same(position, result);
        }

        /// <summary>
        /// Under pressure the cache drops the segments that were not used for the longest time
        /// and keeps the ones just read.
        /// </summary>
        [Fact]
        public void EvictionKeepsTheRecentlyUsedSegments()
        {
            WithMaxCachedSegments(10, buffer =>
            {
                // arrange
                var context = new IndexStorageContextMock();
                var cold = Enumerable.Range(0, 10).Select(i => buffer.Read<IndexStorageSegmentPosition>(Addr(i), context)).ToList();

                for (var i = 0; i < 5; i++)
                {
                    buffer.ReduceLifetimeAndRemoveExpiredSegments();
                }

                var hot = Enumerable.Range(10, 5).Select(i => buffer.Read<IndexStorageSegmentPosition>(Addr(i), context)).ToList();

                // act
                buffer.ReduceLifetimeAndRemoveExpiredSegments();

                // validation
                Assert.Equal(5, buffer.ReadCacheCount);
                Assert.All(hot, x => Assert.Same(x, buffer.Read<IndexStorageSegmentPosition>(x.Addr, context)));
                Assert.NotSame(cold[0], buffer.Read<IndexStorageSegmentPosition>(cold[0].Addr, context));
            });
        }

        /// <summary>
        /// Segments read within one tick carry equal counters and give no order to evict by;
        /// the cache still shrinks below its limit instead of growing without bound.
        /// </summary>
        [Fact]
        public void EvictionBoundsTheCacheWhenCountersAreEqual()
        {
            WithMaxCachedSegments(10, buffer =>
            {
                // arrange
                var context = new IndexStorageContextMock();

                for (var i = 0; i < 20; i++)
                {
                    buffer.Read<IndexStorageSegmentPosition>(Addr(i), context);
                }

                // act
                buffer.ReduceLifetimeAndRemoveExpiredSegments();

                // validation
                Assert.Equal(8, buffer.ReadCacheCount);
            });
        }

        /// <summary>
        /// An idle entry stays the oldest: its counter stops at zero rather than wrapping
        /// around to the highest, freshest value.
        /// </summary>
        [Fact]
        public void AgeStopsAtZero()
        {
            // arrange
            var item = new IndexStorageBufferItem(new TestSegment(0));

            // act
            for (var i = 0; i <= IndexStorageBufferItem.Lifetime; i++)
            {
                item.Age();
            }

            // validation
            Assert.Equal(0u, item.Counter);
        }

        /// <summary>
        /// Returns the address of the i-th position segment in the test file.
        /// </summary>
        /// <param name="i">The index of the segment.</param>
        /// <returns>The address.</returns>
        private static ulong Addr(int i)
        {
            return (ulong)i * IndexStorageSegmentPosition.SegmentSize;
        }

        /// <summary>
        /// Runs an action on a buffer of its own, whose maintenance timer never fires and whose
        /// file holds 20 position segments, with the cache limit lowered for the duration.
        /// </summary>
        /// <param name="max">The cache limit.</param>
        /// <param name="action">The action.</param>
        private void WithMaxCachedSegments(uint max, Action<IndexStorageBuffer> action)
        {
            var context = new IndexStorageContextMock();

            for (var i = 0; i < 20; i++)
            {
                _buffer.Write(new IndexStorageSegmentPosition(context, Addr(i)) { Position = (uint)i });
            }

            _buffer.Flush();

            var maxCachedSegments = IndexStorageBuffer.MaxCachedSegments;
            var interval = IndexStorageBuffer.MaintenanceIntervalMs;
            IndexStorageBuffer buffer;

            try
            {
                IndexStorageBuffer.MaintenanceIntervalMs = Timeout.Infinite;
                buffer = new IndexStorageBuffer(_file);
            }
            finally
            {
                IndexStorageBuffer.MaintenanceIntervalMs = interval;
            }

            try
            {
                IndexStorageBuffer.MaxCachedSegments = max;
                action(buffer);
            }
            finally
            {
                IndexStorageBuffer.MaxCachedSegments = maxCachedSegments;
                buffer.Dispose();
            }
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