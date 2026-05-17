using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a bucket segment in the hash map.
    /// </summary>
    public class IndexStorageSegmentBucket : IndexStorageSegment
    {
        /// <summary>
        /// Gets the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = sizeof(ulong);

        /// <summary>
        /// Gets or sets the address to the first element in the bucket.
        /// </summary>
        public ulong ItemAddr { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="addr">The address of the segment.</param>
        public IndexStorageSegmentBucket(IndexStorageContext context, ulong addr)
            : base(context, addr)
        {
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        public override void Read(BinaryReader reader)
        {
            ItemAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(ItemAddr);
        }
    }
}