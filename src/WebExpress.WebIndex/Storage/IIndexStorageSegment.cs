using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Interface for managing index storage segments.
    /// </summary>
    public interface IIndexStorageSegment
    {
        /// <summary>
        /// Gets the address of the segment.
        /// </summary>
        ulong Addr { get; }

        /// <summary>
        /// Gets the the context of the index.
        /// </summary>
        IndexStorageContext Context { get; }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        void Read(BinaryReader reader);

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        void Write(BinaryWriter writer);
    }
}