using System;
using System.IO;
using System.Linq;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// The class represents a segment of an index storage that is divided into chunks. Each 
    /// chunk contains a portion of the data and a reference to the next chunk, creating 
    /// an ordered list of chunks. 
    /// This ensures fixed-size records for reliable addressing.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the chunk segment.
    /// </remarks>
    /// <param name="context">The storage context.</param>
    /// <param name="addr">The absolute address of the segment.</param>
    public class IndexStorageSegmentChunk(IndexStorageContext context, ulong addr) : IndexStorageSegment(context, addr), IIndexStorageSegmentChunk, IComparable
    {
        /// <summary>
        /// Gets the payload size of a single chunk in bytes.
        /// </summary>
        public const uint ChunkSize = 256;

        /// <summary>
        /// Gets the total on-disk size of a chunk segment.
        /// </summary>
        public const uint SegmentSize = sizeof(uint) + ChunkSize + sizeof(ulong);

        /// <summary>
        /// Gets the number of bytes of the payload currently stored (not including padding).
        /// </summary>
        public uint Length => (uint)Math.Min(DataChunk?.Length ?? 0, ChunkSize);

        /// <summary>
        /// Gets or sets the raw payload bytes of this chunk (maximum ChunkSize considered).
        /// </summary>
        public byte[] DataChunk { get; set; } = [];

        /// <summary>
        /// Gets or sets the address of the next chunk in the chain or 0 when none exists.
        /// </summary>
        public ulong NextChunkAddr { get; set; }

        /// <summary>
        /// Reads the chunk record from the stream and consumes exactly SegmentSize bytes.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        public override void Read(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var storedLen = reader.ReadUInt32();
            var actualLen = (int)Math.Min(storedLen, ChunkSize);

            // read exactly ChunkSize bytes from the stream, keep only actualLen in DataChunk
            var padded = reader.ReadBytes((int)ChunkSize);
            if (padded.Length < ChunkSize)
            {
                // unexpected end of stream
                throw new EndOfStreamException("Unexpected end of stream while reading chunk payload.");
            }

            if (actualLen == 0)
            {
                DataChunk = [];
            }
            else
            {
                DataChunk = [.. padded.Take(actualLen)];
            }

            NextChunkAddr = reader.ReadUInt64();
        }

        /// <summary>
        /// Writes the chunk record to the stream as fixed-size: [len][ChunkSize payload][next].
        /// </summary>
        /// <param name="writer">The binary writer to write to.</param>
        public override void Write(BinaryWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            // compute effective length and write it
            var effectiveLen = (int)Length;
            writer.Write((uint)effectiveLen);

            // write payload up to effectiveLen, then pad to ChunkSize with zeros
            if (effectiveLen > 0 && DataChunk is not null && DataChunk.Length > 0)
            {
                writer.Write(DataChunk, 0, effectiveLen);
            }

            var padSize = (int)ChunkSize - effectiveLen;
            if (padSize > 0)
            {
                // write zero padding for the remainder of the chunk payload
                writer.Write(new byte[padSize]);
            }

            // write link to next chunk
            writer.Write(NextChunkAddr);
        }

        /// <summary>
        /// Compares this instance with another object implementing a lexicographic byte comparison on payload.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>
        /// A negative number if this instance precedes obj; zero if equal; a positive number if it follows.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when obj is neither IndexStorageSegmentChunk nor IndexStorageSegmentItem.</exception>
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            ReadOnlySpan<byte> a = DataChunk ?? [];
            ReadOnlySpan<byte> b;

            if (obj is IndexStorageSegmentChunk chunk)
            {
                b = chunk.DataChunk ?? [];
            }
            else if (obj is IndexStorageSegmentItem item)
            {
                b = item.DataChunk ?? [];
            }
            else
            {
                throw new ArgumentException("Object is not a comparable storage segment.", nameof(obj));
            }

            // compare by length first, then by content
            var lenCompare = a.Length.CompareTo(b.Length);
            if (lenCompare != 0)
            {
                return lenCompare;
            }

            // lexicographic comparison
            for (int i = 0; i < a.Length; i++)
            {
                int diff = a[i].CompareTo(b[i]);
                if (diff != 0)
                {
                    return diff;
                }
            }

            return 0;
        }
    }
}