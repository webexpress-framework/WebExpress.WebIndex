using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// The items are stored in a segment. The size of the segment is variable and is determined by the size of the compressed 
    /// item instance. The segment are stored in the variable memory area of the IndexDocumentStore.
    /// </summary>
    public class IndexStorageSegmentItem : IndexStorageSegment, IIndexStorageSegmentListItem, IIndexStorageSegmentChunk
    {
        /// <summary>
        /// Returns the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = 16 + sizeof(uint) + IndexStorageSegmentChunk.ChunkSize + sizeof(ulong) + sizeof(ulong);

        /// <summary>
        /// Returns or sets the id of the item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Returns the number of characters in the term.
        /// </summary>
        public uint Length => (uint)DataChunk.Length;

        /// <summary>
        /// Returns or sets the item data. 
        /// </summary>
        public byte[] DataChunk { get; set; }

        /// <summary>
        /// Returns or sets the address of the next chunk element of a list or 0 if there is no element.
        /// </summary>
        public ulong NextChunkAddr { get; set; }

        /// <summary>
        /// Returns or sets the address of the next bucket element of a sorted list or 0 if there is no element.
        /// </summary>
        public ulong SuccessorAddr { get; set; }

        /// <summary>
        /// Returns the a sorted list of the chunk segments.
        /// </summary>
        public IEnumerable<IndexStorageSegmentChunk> ChunkSegments
        {
            get
            {
                if (NextChunkAddr == 0)
                {
                    yield break;
                }

                var addr = NextChunkAddr;

                while (addr != 0)
                {
                    var item = Context.IndexFile.Read<IndexStorageSegmentChunk>(addr, Context);
                    yield return item;

                    addr = item.NextChunkAddr;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="addr">The address of the segment.</param>
        public IndexStorageSegmentItem(IndexStorageContext context, ulong addr)
            : base(context, addr)
        {
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public override void Read(BinaryReader reader)
        {
            Id = new Guid(reader.ReadBytes(16));
            var length = reader.ReadUInt32();
            DataChunk = reader.ReadBytes((int)Math.Min(length, IndexStorageSegmentChunk.ChunkSize));
            NextChunkAddr = reader.ReadUInt64();
            SuccessorAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write(Length);
            writer.Write(DataChunk);
            writer.Write(NextChunkAddr);
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
            if (obj is IndexStorageSegmentItem item)
            {
                return DataChunk.SequenceEqual(item.DataChunk) ? 0 : -1;
            }

            throw new System.ArgumentException();
        }
    }
}