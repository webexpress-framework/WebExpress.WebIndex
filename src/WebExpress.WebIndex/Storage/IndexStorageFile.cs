using System;
using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a reverse index or document storage at the file level.
    /// </summary>
    public class IndexStorageFile : IDisposable
    {
        /// <summary>
        /// Gets or sets the maximum upper limit of the cached segments.
        /// </summary>
        public static uint BufferSize { get; set; } = 4 * 1024; // 4096 byte

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the stream for the index file.
        /// </summary>
        internal FileStream FileStream { get; private set; }

        /// <summary>
        /// Gets or sets the buffer for caching segments.
        /// </summary>
        private IndexStorageBuffer Buffer { get; set; }

        /// <summary>
        /// Gets or sets the next free address.
        /// Note: This value is intentionally not inferred from file length on reopen,
        /// because allocator/header segments rely on stable, predefined addresses.
        /// </summary>
        public ulong NextFreeAddr { get; internal set; } = 0ul;

        /// <summary>
        /// Gets a value indicating whether the object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public IndexStorageFile(string fileName)
        {
            FileName = fileName;

            // ensure target directory exists; handle files without directory component
            var dir = Path.GetDirectoryName(FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new FileStreamOptions()
            {
                BufferSize = (int)BufferSize,
                Mode = FileMode.OpenOrCreate,
                Share = FileShare.None,
                Access = FileAccess.ReadWrite
            };

            FileStream = File.Open(FileName, options);
            Buffer = new IndexStorageBuffer(this);
        }

        /// <summary>
        /// Allocates the memory.
        /// </summary>
        /// <param name="size">The size that determines how much memory should be reserved.</param>
        /// <returns>The start address of the reserved storage area.</returns>
        public ulong Alloc(uint size)
        {
            var addr = NextFreeAddr;
            NextFreeAddr += size;

            return addr;
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="addr">The segment address.</param>
        /// <param name="context">The reference to the context of the index.</param>
        /// <typeparam name="TIndexStorageSegment">The type to be read.</typeparam>
        /// <returns>The segment as read from the storage medium.</returns>
        public TIndexStorageSegment Read<TIndexStorageSegment>(ulong addr, IndexStorageContext context)
            where TIndexStorageSegment : IIndexStorageSegment
        {
            return Buffer.Read<TIndexStorageSegment>(addr, context);
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <typeparam name="TIndexStorageSegment">The type to be read.</typeparam>
        /// <returns>The segment as read from the storage medium.</returns>
        public TIndexStorageSegment Read<TIndexStorageSegment>(TIndexStorageSegment segment)
            where TIndexStorageSegment : IIndexStorageSegment
        {
            return Buffer.Read<TIndexStorageSegment>(segment);
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="segment">The segment.</param>
        public void Write(IIndexStorageSegment segment)
        {
            if (segment is null)
            {
                return;
            }

            Buffer.Write(segment);
        }

        /// <summary>
        /// Ensures that all segments in the buffer are written to the storage device.
        /// </summary>
        public void Flush()
        {
            if (!FileStream.CanWrite)
            {
                return;
            }

            Buffer.Flush();
        }

        /// <summary>
        /// Deletes this file from storage.
        /// </summary>
        public void Delete()
        {
            Dispose();

            File.Delete(FileName);
        }

        /// <summary>
        /// Performs cache invalidation for a specific IndexStorageSegment object.
        /// </summary>
        /// <param name="segment">The IndexStorageSegment object to be invalidated.</param>
        public void Invalidation(IIndexStorageSegment segment)
        {
            if (segment is null)
            {
                return;
            }

            Buffer.Invalidation(segment);
        }

        /// <summary>
        /// Performs cache invalidation for all IndexStorageSegment objects.
        /// </summary>
        public void InvalidationAll()
        {
            Buffer.InvalidationAll();
        }

        /// <summary>
        /// Is called to free up resources.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Buffer.Dispose();
            FileStream.Dispose();

            FileStream = null;
            Buffer = null;

            IsDisposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
