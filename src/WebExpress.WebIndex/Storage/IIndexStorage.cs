namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Interface for identifying an index that is kept in the file system.
    /// </summary>
    public interface IIndexStorage
    {
        /// <summary>
        /// Gets the file name for the reverse index.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the reverse index file.
        /// </summary>
        IndexStorageFile IndexFile { get; }

        /// <summary>
        /// Gets the header.
        /// </summary>
        IndexStorageSegmentHeader Header { get; }

        /// <summary>
        /// Gets the memory manager.
        /// </summary>
        IndexStorageSegmentAllocator Allocator { get; }

        /// <summary>
        /// Gets the statistical values that can be help to optimize the index.
        /// </summary>
        IndexStorageSegmentStatistic Statistic { get; }
    }
}