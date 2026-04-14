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
        private Dictionary<ulong, IndexStorageBufferItem> _readCache;

        /// <summary>
        /// Provides a buffer for random access of imperishable segments (non-evictable).
        /// </summary>
        private readonly Dictionary<ulong, IndexStorageBufferItem> _imperishableCache;

        /// <summary>
        /// Provides a buffer for pending write segments (write-back).
        /// </summary>
        private readonly Dictionary<ulong, IIndexStorageSegment> _writeCache;

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

                var segment = (TIndexStorageSegment)Activator.CreateInstance(typeof(TIndexStorageSegment), context, addr);

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
                if (!_writeCache.TryAdd(segment.Addr, segment))
                {
                    _writeCache[segment.Addr] = segment;
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

            if (_writeCache.TryGetValue(addr, out IIndexStorageSegment res))
            {
                segment = res;
                return true;
            }

            segment = null;
            return false;
        }

        /// <summary>
        /// Ages cached segments and evicts older entries when the cache is above a load threshold.
        /// </summary>
        private void ReduceLifetimeAndRemoveExpiredSegments()
        {
            lock (_guard)
            {
                // under 80% capacity: only age entries, do not remove to avoid churn
                if (_readCache.Count < 0.8 * MaxCachedSegments)
                {
                    foreach (var kv in _readCache)
                    {
                        // increment age
                        kv.Value.IncrementCounter();
                    }
                }
                else
                {
                    // at/over 80%: compute average age and evict items with above-average age
                    var average = _readCache.Count != 0 ? _readCache.Average(x => x.Value.Counter) : 0.0;

                    // build a new dictionary with kept items to avoid mutating during enumeration
                    _readCache = new Dictionary<ulong, IndexStorageBufferItem>(
                        _readCache.Where(x => x.Value.Counter <= average)
                    );
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