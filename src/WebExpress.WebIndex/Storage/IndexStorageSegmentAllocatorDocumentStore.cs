using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides allocation and deallocation of on-disk segments for the document store.
    /// Manages separate free lists for item and chunk segments and maintains a monotonic allocation pointer.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the allocator.
    /// </remarks>
    /// <param name="context">The storage context.</param>
    public class IndexStorageSegmentAllocatorDocumentStore(IndexStorageContext context) : IndexStorageSegmentAllocator(context, context.IndexFile.Alloc(SegmentSize))
    {
        // guard for allocator state (free lists and next-free pointer)
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the size of the allocator segment on disk.
        /// </summary>
        public new const uint SegmentSize = IndexStorageSegmentAllocator.SegmentSize + sizeof(ulong) + sizeof(ulong);

        /// <summary>
        /// Gets or sets the head address of the free item list.
        /// </summary>
        public ulong FreeItemAddr { get; set; }

        /// <summary>
        /// Gets or sets the head address of the free chunk list.
        /// </summary>
        public ulong FreeChunkAddr { get; set; }

        /// <summary>
        /// Gets a snapshot enumeration of the free item segments.
        /// </summary>
        public IEnumerable<IndexStorageSegmentFree> FreeItemSegments
        {
            get
            {
                // build a snapshot under lock to avoid concurrent modifications during enumeration
                var snapshot = new List<IndexStorageSegmentFree>();
                lock (_guard)
                {
                    if (FreeItemAddr != 0)
                    {
                        var addr = FreeItemAddr;
                        while (addr != 0)
                        {
                            var item = Context.IndexFile.Read<IndexStorageSegmentFree>(addr, Context);
                            if (item is not null)
                            {
                                snapshot.Add(item);
                                addr = item.SuccessorAddr;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                foreach (var item in snapshot)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets a snapshot enumeration of the free chunk segments.
        /// </summary>
        public IEnumerable<IndexStorageSegmentFree> FreeChunkSegments
        {
            get
            {
                // build a snapshot under lock to avoid concurrent modifications during enumeration
                var snapshot = new List<IndexStorageSegmentFree>();
                lock (_guard)
                {
                    if (FreeChunkAddr != 0)
                    {
                        var addr = FreeChunkAddr;
                        while (addr != 0)
                        {
                            var item = Context.IndexFile.Read<IndexStorageSegmentFree>(addr, Context);
                            if (item is not null)
                            {
                                snapshot.Add(item);
                                addr = item.SuccessorAddr;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                foreach (var item in snapshot)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Allocates space of the given size and returns its starting address.
        /// </summary>
        /// <param name="size">The size to allocate.</param>
        /// <returns>The starting address of the reserved storage area.</returns>
        /// <exception cref="IOException">Thrown when the allocator runs out of address space.</exception>
        public override ulong Alloc(uint size)
        {
            lock (_guard)
            {
                if (size == 0)
                {
                    // zero-sized allocations do not advance the pointer
                    return NextFreeAddr;
                }

                switch (size)
                {
                    case IndexStorageSegmentItem.SegmentSize:
                        {
                            if (FreeItemAddr != 0)
                            {
                                var item = Context.IndexFile.Read<IndexStorageSegmentFree>(FreeItemAddr, Context);
                                FreeItemAddr = item.SuccessorAddr;
                                Context.IndexFile.Write(this);
                                return item.Addr;
                            }
                            break;
                        }
                    case IndexStorageSegmentChunk.SegmentSize:
                        {
                            if (FreeChunkAddr != 0)
                            {
                                var item = Context.IndexFile.Read<IndexStorageSegmentFree>(FreeChunkAddr, Context);
                                FreeChunkAddr = item.SuccessorAddr;
                                Context.IndexFile.Write(this);
                                return item.Addr;
                            }
                            break;
                        }
                    default:
                        {
                            // unknown size: fall through to fresh allocation
                            break;
                        }
                }

                // allocate fresh space with overflow guard
                var addr = NextFreeAddr;
                if (ulong.MaxValue - NextFreeAddr < size)
                {
                    throw new IOException("Out of address space for allocator.");
                }

                NextFreeAddr += size;
                Context.IndexFile.Write(this);
                return addr;
            }
        }

        /// <summary>
        /// Frees the space occupied by the specified segment and adds it to the appropriate free list.
        /// </summary>
        /// <param name="segment">The segment to free.</param>
        public override void Free(IIndexStorageSegment segment)
        {
            if (segment is null)
            {
                return;
            }

            var item = new IndexStorageSegmentFree(Context, segment.Addr);

            lock (_guard)
            {
                // ensure any cached copy of the segment is invalidated before reusing the address
                Context.IndexFile.Invalidation(segment);

                if (segment is IndexStorageSegmentItem)
                {
                    var addr = FreeItemAddr;
                    FreeItemAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else if (segment is IndexStorageSegmentChunk)
                {
                    var addr = FreeChunkAddr;
                    FreeChunkAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else
                {
                    // unsupported segment type; ignore silently to keep allocator consistent
                }
            }
        }

        /// <summary>
        /// Reads the allocator metadata from the storage medium.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            FreeItemAddr = reader.ReadUInt64();
            FreeChunkAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the allocator metadata to the storage medium.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(FreeItemAddr);
            writer.Write(FreeChunkAddr);
        }

        /// <summary>
        /// Returns a string that represents the current allocator state.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return $"{Addr}: FreeItems[{string.Join(", ", FreeItemSegments)}];FreeChunks[{string.Join(", ", FreeChunkSegments)}]";
        }
    }
}