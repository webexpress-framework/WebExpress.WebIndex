using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Allocator for reverse index segments (term, posting, position). Manages a free list 
    /// per segment type and maintains a monotonically increasing allocation pointer for fresh 
    /// allocations.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the allocator.
    /// </remarks>
    /// <param name="context">The storage context.</param>
    public class IndexStorageSegmentAllocatorReverseIndex(IndexStorageContext context) : IndexStorageSegmentAllocator(context, context.IndexFile.Alloc(SegmentSize))
    {
        // guard for all allocator mutations (free lists and next-free pointer)
        private readonly Lock _guard = new();

        /// <summary>
        /// Gets the on-disk size of this allocator segment.
        /// </summary>
        public new const uint SegmentSize = IndexStorageSegmentAllocator.SegmentSize + sizeof(ulong) + sizeof(ulong) + sizeof(ulong);

        /// <summary>
        /// Gets or sets the head address of the free list for term segments.
        /// </summary>
        public ulong FreeTermAddr { get; set; }

        /// <summary>
        /// Gets or sets the head address of the free list for posting segments.
        /// </summary>
        public ulong FreePostingAddr { get; set; }

        /// <summary>
        /// Gets or sets the head address of the free list for position segments.
        /// </summary>
        public ulong FreePositionAddr { get; set; }

        /// <summary>
        /// Gets all free term segments (snapshot).
        /// </summary>
        public IEnumerable<IndexStorageSegmentFree> FreeTerms
        {
            get
            {
                return EnumerateFreeListSnapshot(FreeTermAddr);
            }
        }

        /// <summary>
        /// Gets all free posting segments (snapshot).
        /// </summary>
        public IEnumerable<IndexStorageSegmentFree> FreePostings
        {
            get
            {
                return EnumerateFreeListSnapshot(FreePostingAddr);
            }
        }

        /// <summary>
        /// Gets all free position segments (snapshot).
        /// </summary>
        public IEnumerable<IndexStorageSegmentFree> FreePositions
        {
            get
            {
                return EnumerateFreeListSnapshot(FreePositionAddr);
            }
        }

        /// <summary>
        /// Allocates a segment of the requested size from the appropriate free list or from fresh space.
        /// </summary>
        /// <param name="size">
        /// The segment size to allocate (must match one of the known segment sizes).
        /// </param>
        /// <returns>The starting address of the reserved storage area.</returns>
        /// <exception cref="IOException">Thrown when the address space would overflow.</exception>
        public override ulong Alloc(uint size)
        {
            if (size == 0)
            {
                // zero-sized allocations are not meaningful; fall back to current next-free pointer without advancing
                return NextFreeAddr;
            }

            lock (_guard)
            {
                switch (size)
                {
                    case IndexStorageSegmentTermNode.SegmentSize:
                        {
                            if (FreeTermAddr != 0)
                            {
                                var item = Context.IndexFile.Read<IndexStorageSegmentFree>(FreeTermAddr, Context);
                                FreeTermAddr = item.SuccessorAddr;
                                Context.IndexFile.Write(this);
                                return item.Addr;
                            }
                            break;
                        }
                    case IndexStorageSegmentPostingNode.SegmentSize:
                        {
                            if (FreePostingAddr != 0)
                            {
                                var item = Context.IndexFile.Read<IndexStorageSegmentFree>(FreePostingAddr, Context);
                                FreePostingAddr = item.SuccessorAddr;
                                Context.IndexFile.Write(this);
                                return item.Addr;
                            }
                            break;
                        }
                    case IndexStorageSegmentPosition.SegmentSize:
                        {
                            if (FreePositionAddr != 0)
                            {
                                var item = Context.IndexFile.Read<IndexStorageSegmentFree>(FreePositionAddr, Context);
                                FreePositionAddr = item.SuccessorAddr;
                                Context.IndexFile.Write(this);
                                return item.Addr;
                            }
                            break;
                        }
                    default:
                        {
                            // unknown size: treat as fresh allocation block
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
        /// Frees a previously allocated segment by pushing it to the appropriate 
        /// free list head.
        /// </summary>
        /// <param name="segment">The segment to free.</param>
        /// <exception cref="ArgumentNullException">Thrown when segment is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when segment type is not supported by this allocator.
        /// </exception>
        public override void Free(IIndexStorageSegment segment)
        {
            ArgumentNullException.ThrowIfNull(segment);

            // create a free-list record at the segment's address
            var item = new IndexStorageSegmentFree(Context, segment.Addr);

            // mark old content invalid so subsequent reads do not treat it as live
            Context.IndexFile.Invalidation(segment);

            lock (_guard)
            {
                if (segment is IndexStorageSegmentTermNode)
                {
                    var addr = FreeTermAddr;
                    FreeTermAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else if (segment is IndexStorageSegmentPostingNode)
                {
                    var addr = FreePostingAddr;
                    FreePostingAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else if (segment is IndexStorageSegmentNumericPostingNode)
                {
                    var addr = FreePostingAddr;
                    FreePostingAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else if (segment is IndexStorageSegmentPosition)
                {
                    var addr = FreePositionAddr;
                    FreePositionAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else if (segment is IndexStorageSegmentAllocatorReverseIndex)
                {
                    var addr = FreePositionAddr;
                    FreePositionAddr = item.Addr;
                    item.SuccessorAddr = addr;

                    Context.IndexFile.Write(this);
                    Context.IndexFile.Write(item);
                }
                else
                {
                    // unknown segment type cannot be placed on any free list
                    throw new ArgumentException("Unsupported segment type for free operation.", nameof(segment));
                }
            }
        }

        /// <summary>
        /// Reads the allocator record including all free-list heads from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            FreeTermAddr = reader.ReadUInt64();
            FreePostingAddr = reader.ReadUInt64();
            FreePositionAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the allocator record including all free-list heads to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(FreeTermAddr);
            writer.Write(FreePostingAddr);
            writer.Write(FreePositionAddr);
        }

        /// <summary>
        /// Returns a diagnostic string showing the current free-list heads and their contents.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Addr}: FreeTerms[{string.Join(", ", FreeTerms)}];FreePostings[{string.Join(", ", FreePostings)}];FreePositions[{string.Join(", ", FreePositions)}]";
        }

        /// <summary>
        /// Enumerates a free-list snapshot by reading nodes starting from head under the allocator guard.
        /// </summary>
        /// <param name="head">The head address of the list.</param>
        /// <returns>A snapshot list of free nodes.</returns>
        private IEnumerable<IndexStorageSegmentFree> EnumerateFreeListSnapshot(ulong head)
        {
            var snapshot = new List<IndexStorageSegmentFree>();
            lock (_guard)
            {
                if (head == 0)
                {
                    yield break;
                }

                var addr = head;
                while (addr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentFree>(addr, Context);
                    if (item is null)
                    {
                        break;
                    }

                    snapshot.Add(item);

                    addr = item.SuccessorAddr;
                }
            }

            foreach (var item in snapshot)
            {
                yield return item;
            }
        }
    }
}