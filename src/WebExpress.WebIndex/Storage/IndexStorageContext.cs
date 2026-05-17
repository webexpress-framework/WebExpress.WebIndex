namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides a context for managing an index storage.
    /// </summary>
    public class IndexStorageContext
    {
        /// <summary>
        /// Gets or sets the index storage instance.
        /// </summary>
        private IIndexStorage Index { get; set; }

        /// <summary>
        /// Gets the reverse index file.
        /// </summary>
        public IndexStorageFile IndexFile => Index.IndexFile;

        /// <summary>
        /// Gets the header.
        /// </summary>
        public IndexStorageSegmentHeader Header => Index.Header;

        /// <summary>
        /// Gets the memory manager.
        /// </summary>
        public IndexStorageSegmentAllocator Allocator => Index.Allocator;

        /// <summary>
        /// Gets the statistical values that can be help to optimize the index.
        /// </summary>
        public IndexStorageSegmentStatistic Statistic => Index.Statistic;

        /// <summary>
        /// Initializes a new instance of the IndexStorageContext class.
        /// </summary>
        /// <param name="index">The index.</param>
        public IndexStorageContext(IIndexStorage index)
        {
            Index = index;
        }
    }
}
