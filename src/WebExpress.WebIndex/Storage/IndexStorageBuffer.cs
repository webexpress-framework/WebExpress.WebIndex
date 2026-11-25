using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides a read/write buffer for index storage segments with caching and periodic maintenance.
    /// </summary>
    public class IndexStorageBuffer : IDisposable
    {
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets or sets the maximum upper limit of the cached segments.
        /// </summary>
        public static uint MaxCachedSegments { get; set; } = 50000;

        /// <summary>
        /// Gets or sets the maintenance interval in milliseconds for cache aging and flush operations.
        /// </summary>
        public static int MaintenanceIntervalMs { get; set; } = 500;

        /// <summary>
        /// Provides a buffer for random access of segments (evictable).
        /// </summary>
        private readonly Dictionary<ulong, IndexStorageBufferItem> _readCache;

        /// <summary>
        /// Provides a buffer for random access of imperishable segments (non-evictable).
        /// </summary>
        private readonly Dictionary<ulong, IndexStorageBufferItem> _imperishableCache;

        /// <summary>
        /// Provides a buffer for pending write segments (write-back).
        /// </summary>
        private readonly Dictionary<ulong, IIndexStorageSegment> _writeCache;

        /// <summary>
        /// Provides a reusable buffer holding the addresses scheduled for eviction during
        /// maintenance. Kept as a field so the periodic maintenance path does not allocate
        /// a new collection on every tick.
        /// </summary>
        private readonly List<ulong> _evictionBuffer = [];

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the reader to read from the underlying stream.
        /// </summary>
        private BinaryReader Reader { get; }

        /// <summary>
        /// Gets the writer to write data to the underlying stream.
        /// </summary>
        private BinaryWriter Writer { get; }

        /// <summary>
        /// Gets or sets the timer for periodic maintenance of the caches.
        /// </summary>
        private Timer Timer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexStorageBuffer"/> class.
        /// </summary>
        /// <param name="file">The file wrapper that provides the underlying stream.</param>
        public IndexStorageBuffer(IndexStorageFile file)
        {
            _readCache = new Dictionary<ulong, IndexStorageBufferItem>((int)MaxCachedSegments);
            _imperishableCache = new Dictionary<ulong, IndexStorageBufferItem>((int)MaxCachedSegments);
            _writeCache = new Dictionary<ulong, IIndexStorageSegment>((int)MaxCachedSegments);

            // do not dispose reader/writer here; the underlying FileStream lifecycle is managed by IndexStorageFile
            Reader = new BinaryReader(file.FileStream, Encoding.UTF8);
            Writer = new BinaryWriter(file.FileStream, Encoding.UTF8);

            Timer = new Timer(state =>
            {
                // periodic maintenance: age cache and flush pending writes
                ReduceLifetimeAndRemoveExpiredSegments();
                Flush();
            }, null, MaintenanceIntervalMs, MaintenanceIntervalMs);
        }

        /// <summary>
        /// Keeps storage initialization and its final flush exclusive with other stream operations.
        /// </summary>
        /// <param name="initialization">The action that creates or resets the storage segments.</param>
        internal void Initialize(Action initialization)
        {
            lock (_guard)
            {
                initialization();
                Flush();
            }
        }

        /// <summary>
        /// Reads a segment from the storage medium.
        /// </summary>
        /// <typeparam name="TIndexStorageSegment">The segment type to read.</typeparam>
        /// <param name="addr">The segment address.</param>
        /// <param name="context">The storage/index context.</param>
        /// <returns>The segment instance as read from the storage medium.</returns>
        public TIndexStorageSegment Read<TIndexStorageSegment>(ulong addr, IndexStorageContext context)
            where TIndexStorageSegment : IIndexStorageSegment
        {
            lock (_guard)
            {
                if (GetSegment(addr, out IIndexStorageSegment readCached))
                {
                    if (readCached is TIndexStorageSegment cachedSegment)
                    {
                        return cachedSegment;
                    }
                }

                var segment = IndexSegmentFactory<TIndexStorageSegment>.Create(context, addr);

                Reader.BaseStream.Seek((long)segment.Addr, SeekOrigin.Begin);
                segment.Read(Reader);

                Cache(segment);

                return segment;
            }
        }

        /// <summary>
        /// Reads a segment from the storage medium by an existing segment descriptor.
        /// </summary>
        /// <typeparam name="TIndexStorageSegment">The segment type to read.</typeparam>
        /// <param name="segment">The segment descriptor to be read.</param>
        /// <returns>The segment instance as read from the storage medium.</returns>
        public TIndexStorageSegment Read<TIndexStorageSegment>(IIndexStorageSegment segment)
            where TIndexStorageSegment : IIndexStorageSegment
        {
            lock (_guard)
            {
                if (GetSegment(segment.Addr, out IIndexStorageSegment readCached))
                {
                    return (TIndexStorageSegment)readCached;
                }

                Reader.BaseStream.Seek((long)segment.Addr, SeekOrigin.Begin);
                segment.Read(Reader);

                Cache(segment);
            }

            return (TIndexStorageSegment)segment;
        }

        /// <summary>
        /// Schedules a segment for writing to the underlying storage (write-back).
        /// </summary>
        /// <param name="segment">The segment to write.</param>
        public void Write(IIndexStorageSegment segment)
        {
            if (segment is null)
            {
                return;
            }

            lock (_guard)
            {
                _writeCache[segment.Addr] = segment;

                // a segment written to a reused address replaces what was cached there, e.g. the
                // free-list record the allocator read; left in place, the stale instance would
                // answer the next read instead of the pending write
                if (_readCache.TryGetValue(segment.Addr, out var cached) && !ReferenceEquals(cached.Segment, segment))
                {
                    _readCache.Remove(segment.Addr);
                }

                if (_imperishableCache.TryGetValue(segment.Addr, out var imperishable) && !ReferenceEquals(imperishable.Segment, segment))
                {
                    _imperishableCache.Remove(segment.Addr);
                }
            }
        }

        /// <summary>
        /// Performs cache invalidation for a specific segment.
        /// </summary>
        /// <param name="segment">The segment to be invalidated.</param>
        public void Invalidation(IIndexStorageSegment segment)
        {
            if (segment is null)
            {
                return;
            }

            lock (_guard)
            {
                _readCache.Remove(segment.Addr, out _);
                _imperishableCache.Remove(segment.Addr, out _);
            }
        }

        /// <summary>
        /// Performs cache invalidation for all segments.
        /// </summary>
        public void InvalidationAll()
        {
            lock (_guard)
            {
                _readCache.Clear();
                _imperishableCache.Clear();
                _writeCache.Clear();
            }
        }

        /// <summary>
        /// Caches a segment in the appropriate cache (evictable or imperishable).
        /// </summary>
        /// <param name="segment">The segment to cache.</param>
        private void Cache(IIndexStorageSegment segment)
        {
            var segmentItem = new IndexStorageBufferItem(segment);

            if (segmentItem.Counter < uint.MaxValue)
            {
                if (!_readCache.TryAdd(segment.Addr, segmentItem))
                {
                    _readCache[segment.Addr] = segmentItem;
                }
            }
            else
            {
                if (!_imperishableCache.TryAdd(segment.Addr, segmentItem))
                {
                    _imperishableCache[segment.Addr] = segmentItem;
                }
            }
        }

        /// <summary>
        /// Tries to get a cached segment by its address.
        /// </summary>
        /// <param name="addr">The address of the segment.</param>
        /// <param name="segment">The cached segment or null.</param>
        /// <returns>True if the segment was found in any cache, otherwise false.</returns>
        private bool GetSegment(ulong addr, out IIndexStorageSegment segment)
        {
            // a pending write is newer than anything on disk or in the read caches
            if (_writeCache.TryGetValue(addr, out IIndexStorageSegment pending))
            {
                segment = pending;
                return true;
            }

            if (_readCache.TryGetValue(addr, out IndexStorageBufferItem cached))
            {
                cached.Refresh();
                segment = cached.Segment;
                return true;
            }

            if (_imperishableCache.TryGetValue(addr, out IndexStorageBufferItem imperishableCached))
            {
                imperishableCached.Refresh();
                segment = imperishableCached.Segment;
                return true;
            }

            segment = null;
            return false;
        }

        /// <summary>
        /// Ages cached segments and, above 80% of <see cref="MaxCachedSegments"/>, evicts the
        /// entries that were used least recently. Below that threshold nothing is evicted, to
        /// avoid churn while the cache still has room.
        /// </summary>
        internal void ReduceLifetimeAndRemoveExpiredSegments()
        {
            lock (_guard)
            {
                var count = _readCache.Count;
                if (count == 0)
                {
                    return;
                }

                // aging continues under pressure, otherwise the counters stop telling recently
                // used entries from idle ones exactly when the eviction depends on them. the sum
                // is taken in the same manual pass because the LINQ Average delegate dominated
                // CPU on this timer
                long sum = 0;
                foreach (var item in _readCache.Values)
                {
                    item.Age();
                    sum += item.Counter;
                }

                var threshold = (int)(0.8 * MaxCachedSegments);

                if (count < threshold)
                {
                    return;
                }

                // a refresh resets the counter to the full lifetime and every tick lowers it,
                // so the entries below the average are the cold ones
                var average = (double)sum / count;

                _evictionBuffer.Clear();
                foreach (var kv in _readCache)
                {
                    if (kv.Value.Counter < average)
                    {
                        _evictionBuffer.Add(kv.Key);
                    }
                }

                // equal counters (a burst read within one tick) carry no order to evict by;
                // without this the cache would grow past its limit
                if (count - _evictionBuffer.Count > MaxCachedSegments)
                {
                    foreach (var kv in _readCache)
                    {
                        if (count - _evictionBuffer.Count <= threshold)
                        {
                            break;
                        }

                        if (kv.Value.Counter >= average)
                        {
                            _evictionBuffer.Add(kv.Key);
                        }
                    }
                }

                // survivors are kept by removing the rest in place, which avoids rebuilding and
                // rehashing the whole dictionary on every tick
                foreach (var addr in _evictionBuffer)
                {
                    _readCache.Remove(addr);
                }
            }
        }

        /// <summary>
        /// Gets the number of segments in the evictable read cache.
        /// </summary>
        internal int ReadCacheCount
        {
            get
            {
                lock (_guard)
                {
                    return _readCache.Count;
                }
            }
        }

        /// <summary>
        /// Ensures that all segments scheduled for writing are persisted to the storage device.
        /// </summary>
        public void Flush()
        {
            lock (_guard)
            {
                // take a stable snapshot to avoid mutating during enumeration
                var pending = _writeCache.Values.ToArray();

                foreach (var segment in pending)
                {
                    if (_writeCache.Remove(segment.Addr, out IIndexStorageSegment seq))
                    {
                        Writer.BaseStream.Seek((long)seq.Addr, SeekOrigin.Begin);
                        seq.Write(Writer);
                    }
                }

                // ensure buffered writer pushes data
                Writer.Flush();
            }
        }

        /// <summary>
        /// Releases unmanaged and managed resources and stops the maintenance timer.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            using var waitHandle = new ManualResetEvent(false);

            // stop timer and wait for in-flight callbacks to finish
            Timer?.Dispose(waitHandle);
            waitHandle.WaitOne();

            // final flush to persist any pending writes
            Flush();

            IsDisposed = true;

            GC.SuppressFinalize(this);
        }
    }
}