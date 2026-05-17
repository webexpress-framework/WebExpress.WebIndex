using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// The position segments form a linked list containing the position information of the associated terms. The position of a 
    /// term refers to its original occurrence in the field value of a document. Each position segment has a fixed size and 
    /// is created in the variable data area of the reverse index. This structure allows for efficient searching and retrieval 
    /// of terms based on their position in the documents.
    /// </summary>
    public class IndexStorageSegmentPosition : IndexStorageSegment, IIndexStorageSegmentListItem
    {
        /// <summary>
        /// Gets the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = sizeof(ulong) + sizeof(int);

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        public uint Position { get; set; }

        /// <summary>
        /// Gets or sets the address of the following position.
        /// </summary>
        public ulong SuccessorAddr { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="addr">The address of the segment.</param>
        public IndexStorageSegmentPosition(IndexStorageContext context, ulong addr)
            : base(context, addr)
        {
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public override void Read(BinaryReader reader)
        {
            Position = reader.ReadUInt32();
            SuccessorAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(Position);
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
            if (obj is IndexStorageSegmentPosition position)
            {
                return Position.CompareTo(position.Position);
            }

            throw new System.ArgumentException();
        }
    }
}