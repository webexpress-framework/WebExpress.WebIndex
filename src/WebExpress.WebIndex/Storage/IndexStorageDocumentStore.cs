using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Implements a persistent document store segment for items of type TIndexItem, backed by 
    /// a file on disk. Provides add, update, delete, lookup, clear and drop operations with 
    /// chunked storage and gzip compression.
    /// </summary>
    /// <typeparam name="TIndexItem">
    /// The data type. This must implement IIndexItem and must be non-nullable.
    /// </typeparam>
    public class IndexStorageDocumentStore<TIndexItem> : IIndexDocumentStore<TIndexItem>, IIndexStorage
        where TIndexItem : IIndexItem
    {
        private const string Extension = "wds";
        private const int Version = 1;

        /// <summary>
        /// Returns the filename of the storage file.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Returns the underlying storage file abstraction.
        /// </summary>
        public IndexStorageFile IndexFile { get; private set; }

        /// <summary>
        /// Returns the header segment.
        /// </summary>
        public IndexStorageSegmentHeader Header { get; private set; }

        /// <summary>
        /// Returns the hash map segment.
        /// </summary>
        public IndexStorageSegmentHashMap HashMap { get; private set; }

        /// <summary>
        /// Returns the allocator (memory manager) segment.
        /// </summary>
        public IndexStorageSegmentAllocator Allocator { get; private set; }

        /// <summary>
        /// Returns the statistics segment.
        /// </summary>
        public IndexStorageSegmentStatistic Statistic { get; private set; }

        /// <summary>
        /// Returns the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Returns the storage context wrapper.
        /// </summary>
        public IndexStorageContext StorageContext { get; private set; }

        /// <summary>
        /// Enumerates all items by resolving each stored segment to an item instance.
        /// </summary>
        public IEnumerable<TIndexItem> All
        {
            get
            {
                if (HashMap == null)
                {
                    return Enumerable.Empty<TIndexItem>();
                }

                return HashMap.All.Select(GetItem).Where(x => x is not null)!;
            }
        }

        /// <summary>
        /// Gets or sets the predicted capacity (number of items to store).
        /// </summary>
        public uint Capacity { get; set; }

        /// <summary>
        /// Initializes a new instance of the store and materializes or creates storage segments as needed.
        /// </summary>
        /// <param name="context">The index context.</param>
        /// <param name="capacity">The predicted capacity.</param>
        /// <exception cref="ArgumentNullException">Thrown when context or its IndexDirectory is null.</exception>
        public IndexStorageDocumentStore(IIndexContext context, uint capacity)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrWhiteSpace(context.IndexDirectory))
            {
                throw new ArgumentNullException(nameof(context.IndexDirectory), "Index directory must be provided.");
            }

            Capacity = capacity;
            Context = context;

            // ensure target directory exists
            Directory.CreateDirectory(Context.IndexDirectory);

            StorageContext = new IndexStorageContext(this);
            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{Extension}");

            var exists = File.Exists(FileName);

            IndexFile = new IndexStorageFile(FileName);
            Header = new IndexStorageSegmentHeader(StorageContext)
            {
                Identifier = Extension,
                Version = Version
            };
            Allocator = new IndexStorageSegmentAllocatorDocumentStore(StorageContext);
            Statistic = new IndexStorageSegmentStatistic(StorageContext);
            HashMap = new IndexStorageSegmentHashMap(StorageContext, Capacity);

            Header.Initialization(exists);
            Statistic.Initialization(exists);
            HashMap.Initialization(exists);
            Allocator.Initialization(exists);

            IndexFile.Flush();
        }

        /// <summary>
        /// Adds an item to the store by serializing and chunking its payload.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(TIndexItem item)
        {
            if (item is null)
            {
                return;
            }

            var chunkSize = (int)IndexStorageSegmentChunk.ChunkSize;
            var json = JsonSerializer.Serialize(item);
            var bytes = CompressString(json);
            IIndexStorageSegmentChunk previousSegment = null;

            // flag to detect whether we wrote any link update (multi-chunk write path)
            var wroteLinkedSegments = false;

            for (int i = 0; i < bytes.Length; i += chunkSize)
            {
                var chunk = bytes.Skip(i).Take(chunkSize).ToArray();
                IIndexStorageSegmentChunk segment;

                if (i == 0)
                {
                    // first segment is the item header segment
                    segment = new IndexStorageSegmentItem(StorageContext, Allocator.Alloc(IndexStorageSegmentItem.SegmentSize))
                    {
                        Id = item.Id,
                        DataChunk = chunk
                    };

                    // add to hash map; if actually added, update statistics
                    if (HashMap.Add(segment as IndexStorageSegmentItem) == segment)
                    {
                        Statistic.Count++;
                        IndexFile.Write(Statistic);
                    }
                }
                else
                {
                    // subsequent chunk segments
                    segment = new IndexStorageSegmentChunk(StorageContext, Allocator.Alloc(IndexStorageSegmentChunk.SegmentSize))
                    {
                        DataChunk = chunk
                    };

                    // link from previous to current
                    previousSegment.NextChunkAddr = segment.Addr;

                    // write updated previous and new segment
                    IndexFile.Write(previousSegment);
                    IndexFile.Write(segment);
                    wroteLinkedSegments = true;
                }

                previousSegment = segment;
            }

            // write the single (first) segment if there was only one loop iteration (no links written)
            if (!wroteLinkedSegments && previousSegment is not null)
            {
                IndexFile.Write(previousSegment);
            }
        }

        /// <summary>
        /// Updates an existing item by replacing its stored data.
        /// </summary>
        /// <param name="item">The item to update.</param>
        public void Update(TIndexItem item)
        {
            if (item is null)
            {
                return;
            }

            // best-effort delete, then add again
            Delete(item);
            Add(item);
        }

        /// <summary>
        /// Removes all data from the store and reinitializes storage segments.
        /// </summary>
        public void Clear()
        {
            // reset allocator and invalidate on-disk segments
            IndexFile.NextFreeAddr = 0;
            IndexFile.InvalidationAll();
            IndexFile.Flush();

            Header = new IndexStorageSegmentHeader(StorageContext)
            {
                Identifier = Extension,
                Version = Version
            };
            Allocator = new IndexStorageSegmentAllocatorDocumentStore(StorageContext);
            Statistic = new IndexStorageSegmentStatistic(StorageContext);
            HashMap = new IndexStorageSegmentHashMap(StorageContext, Capacity);

            Header.Initialization(false);
            Statistic.Initialization(false);
            HashMap.Initialization(false);
            Allocator.Initialization(false);

            IndexFile.Flush();
        }

        /// <summary>
        /// Deletes a specific item and frees its associated segments.
        /// </summary>
        /// <param name="item">The item to delete.</param>
        public void Delete(TIndexItem item)
        {
            if (item is null)
            {
                return;
            }

            var list = HashMap.GetBucket(item.Id);

            if (!list.Any())
            {
                // nothing to delete
                return;
            }

            var segment = list.FirstOrDefault(x => x.Id == item.Id);

            if (segment == null)
            {
                // nothing to delete
                return;
            }

            // remove from hash map
            HashMap.Remove(segment);

            // free all chunk segments
            foreach (var chunk in segment.ChunkSegments ?? Enumerable.Empty<IndexStorageSegmentChunk>())
            {
                Allocator.Free(chunk);
            }

            // free the root item segment
            Allocator.Free(segment);

            // update statistics
            if (Statistic.Count > 0)
            {
                Statistic.Count--;
                IndexFile.Write(Statistic);
            }
        }

        /// <summary>
        /// Returns the number of stored items.
        /// </summary>
        /// <returns>The number of items.</returns>
        public uint Count()
        {
            return Statistic?.Count ?? 0;
        }

        /// <summary>
        /// Drops (deletes) the storage file from disk.
        /// </summary>
        public void Drop()
        {
            IndexFile?.Delete();
        }

        /// <summary>
        /// Looks up a stored item by id.
        /// </summary>
        /// <param name="id">The unique item id.</param>
        /// <returns>The materialized item or default when not found.</returns>
        public TIndexItem GetItem(Guid id)
        {
            var segment = HashMap.GetBucket(id).FirstOrDefault(x => x.Id == id);
            return GetItem(segment);
        }

        /// <summary>
        /// Materializes an item from its storage segment by following chunk links and decompressing payload.
        /// </summary>
        /// <param name="segment">The item segment.</param>
        /// <returns>The deserialized item or default when unavailable.</returns>
        private TIndexItem GetItem(IndexStorageSegmentItem segment)
        {
            if (segment == null)
            {
                return default!;
            }

            var bytes = new List<byte>();
            var addr = segment.NextChunkAddr;

            if (segment.DataChunk != null && segment.DataChunk.Length > 0)
            {
                bytes.AddRange(segment.DataChunk);
            }

            // follow chunk chain
            while (addr != 0)
            {
                var chunk = IndexFile.Read<IndexStorageSegmentChunk>(addr, StorageContext);
                if (chunk?.DataChunk != null && chunk.DataChunk.Length > 0)
                {
                    bytes.AddRange(chunk.DataChunk);
                }

                addr = chunk?.NextChunkAddr ?? 0;
            }

            if (bytes.Count == 0)
            {
                return default!;
            }

            try
            {
                var json = DecompressString(bytes.ToArray());
                var item = JsonSerializer.Deserialize<TIndexItem>(json);
                return item!;
            }
            catch
            {
                // corrupted payload; treat as missing
                return default!;
            }
        }

        /// <summary>
        /// Releases unmanaged resources and disposes the underlying file.
        /// </summary>
        public void Dispose()
        {
            try
            {
                IndexFile?.Dispose();
            }
            catch
            {
                // swallow dispose errors to avoid teardown failures
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Compresses a UTF-8 string using GZipStream.
        /// </summary>
        /// <param name="input">The string to be compressed.</param>
        /// <returns>The compressed byte array.</returns>
        private static byte[] CompressString(string input)
        {
            using var stream = new MemoryStream();
            // leave stream open so ToArray works after disposing gzip
            using (var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            {
                var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
                // write compressed bytes
                gzip.Write(bytes, 0, bytes.Length);
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Decompresses a GZip byte array to a UTF-8 string.
        /// </summary>
        /// <param name="compressed">The compressed byte array.</param>
        /// <returns>The decompressed string.</returns>
        private static string DecompressString(byte[] compressed)
        {
            if (compressed == null || compressed.Length == 0)
            {
                return string.Empty;
            }

            using var stream = new MemoryStream(compressed);
            using var zip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(zip, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
    }
}