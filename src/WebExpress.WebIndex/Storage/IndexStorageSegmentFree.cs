using System;
using System.IO;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Free memory slots are stored in a linked list, which represents the free segments in the file. These can be reused for storing new data. Unused 
    /// segments are replaced with the `Free`-Segment, and neighboring free segments are merged. This process creates room for larger segments but may 
    /// lead to the formation of dead memory spaces too small for reuse. 
    /// </summary>
    [SegmentCached]
    public class IndexStorageSegmentFree : IndexStorageSegment, IIndexStorageSegmentListItem
    {
        /// <summary>
        /// Gets the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = sizeof(uint) + sizeof(ulong);

        /// <summary>
        /// Gets or sets the address of the following free segment or 0 if the last.
        /// </summary>
        public ulong SuccessorAddr { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        public IndexStorageSegmentFree(IndexStorageContext context)
            : this(context, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="addr">The address of the free segment.</param>
        public IndexStorageSegmentFree(IndexStorageContext context, ulong addr)
            : base(context, addr)
        {
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public override void Read(BinaryReader reader)
        {
            SuccessorAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(SuccessorAddr);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns
        ///  an integer that indicates whether the current instance precedes, follows, or
        ///  occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of x and y.
        ///     Less than zero -> x is less than y.
        ///     Zero -> x equals y.
        ///     Greater than zero -> x is greater than y.
        /// </returns>
        /// <exception cref="System.ArgumentException">Obj is not the same type as this instance.</exception>
        public int CompareTo(object obj)
        {
            if (obj is IndexStorageSegmentFree free)
            {
                return Addr.CompareTo(free.Addr);
            }

            throw new ArgumentException();
        }
    }
}