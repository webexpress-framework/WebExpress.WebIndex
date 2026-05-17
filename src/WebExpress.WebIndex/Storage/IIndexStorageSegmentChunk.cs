using System;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a chunk of an index storage segment that can be compared and has an address of the next chunk.
    /// </summary>
    public interface IIndexStorageSegmentChunk : IIndexStorageSegment, IComparable
    {
        /// <summary>
        /// Gets or sets the address of the following chunk item.
        /// </summary>
        ulong NextChunkAddr { get; set; }
    }
}