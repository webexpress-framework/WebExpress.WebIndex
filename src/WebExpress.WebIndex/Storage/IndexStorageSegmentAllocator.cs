using System;
using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides a mechanism to reserve and free space on the storage medium.
    /// Stores and maintains the next-free address used by concrete allocators.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the allocator segment.
    /// </remarks>
    /// <param name="context">The storage context of the index.</param>
    /// <param name="addr">The absolute address of this allocator segment.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public abstract class IndexStorageSegmentAllocator(IndexStorageContext context, ulong addr)
        : IndexStorageSegment(context ?? throw new ArgumentNullException(nameof(context)), addr)
    {
        /// <summary>
        /// Gets the on-disk size of this allocator segment.
        /// </summary>
        public const uint SegmentSize = sizeof(ulong);

        /// <summary>
        /// Gets or sets the next free address where new segments can be allocated.
        /// </summary>
        public ulong NextFreeAddr { get; protected set; } = 0ul;

        /// <summary>
        /// Initializes the allocator segment either by loading from file or by writing its current state.
        /// When writing a new allocator, the file's next-free pointer is adopted if it is ahead.
        /// When reading from file, the file pointer is synchronized forward if needed.
        /// </summary>
        /// <param name="initializationFromFile">If true, reads allocator from file; otherwise writes the current state.</param>
        /// <exception cref="InvalidOperationException">Thrown when the index file is not available.</exception>
        public virtual void Initialization(bool initializationFromFile)
        {
            if (Context?.IndexFile == null)
            {
                throw new InvalidOperationException("Index file is not available in the provided context.");
            }

            if (initializationFromFile)
            {
                Context.IndexFile.Read(this);

                // keep the file's next-free pointer at least as large as the allocator's value
                if (Context.IndexFile.NextFreeAddr < NextFreeAddr)
                {
                    Context.IndexFile.NextFreeAddr = NextFreeAddr;
                }
            }
            else
            {
                // adopt the file's pointer if it is ahead
                if (Context.IndexFile.NextFreeAddr > NextFreeAddr)
                {
                    NextFreeAddr = Context.IndexFile.NextFreeAddr;
                }

                Context.IndexFile.Write(this);
            }
        }

        /// <summary>
        /// Allocates a continuous region of the given size and returns its starting address.
        /// </summary>
        /// <param name="size">The number of bytes to reserve.</param>
        /// <returns>The starting address of the reserved storage area.</returns>
        public abstract ulong Alloc(uint size);

        /// <summary>
        /// Frees a previously allocated segment, returning its space to the appropriate free list.
        /// </summary>
        /// <param name="segment">The segment to be freed.</param>
        public abstract void Free(IIndexStorageSegment segment);

        /// <summary>
        /// Reads the allocator's state from the storage medium.
        /// </summary>
        /// <param name="reader">The binary reader used for I/O operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>"
        public override void Read(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            NextFreeAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the allocator's state to the storage medium.
        /// </summary>
        /// <param name="writer">The binary writer used for I/O operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>"
        public override void Write(BinaryWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.Write(NextFreeAddr);
        }

        /// <summary>
        /// Returns a diagnostic string for the allocator.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}; NextFreeAddr={NextFreeAddr}";
        }
    }
}