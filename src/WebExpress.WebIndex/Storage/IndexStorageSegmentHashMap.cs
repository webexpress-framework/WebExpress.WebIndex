using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a hash map segment stored on disk. Buckets hold singly linked lists of items.
    /// </summary>
    public class IndexStorageSegmentHashMap : IndexStorageSegment
    {
        private readonly uint _capacity;
        private readonly Lock _guard = new();
        private const int _bufferSize = 4096;

        /// <summary>
        /// Returns the on-disk size of the hash map header.
        /// </summary>
        public const uint SegmentSize = sizeof(uint);

        /// <summary>
        /// Returns the number of buckets. should be prime to reduce collisions.
        /// </summary>
        public uint BucketCount { get; private set; }

        /// <summary>
        /// Returns all items across all buckets (enumerates on-disk linked lists).
        /// </summary>
        public IEnumerable<IndexStorageSegmentItem> All
        {
            get
            {
                for (var i = 0u; i < BucketCount; i++)
                {
                    var bucket = GetBucket(i);
                    var addr = bucket.ItemAddr;

                    while (addr != 0)
                    {
                        var item = Context.IndexFile.Read<IndexStorageSegmentItem>(addr, Context);
                        yield return item;

                        addr = item.SuccessorAddr;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the hash map segment with the specified capacity 
        /// (rounded up to next prime).
        /// </summary>
        /// <param name="context">The index storage context.</param>
        /// <param name="capacity">The desired initial capacity.</param>
        public IndexStorageSegmentHashMap(IndexStorageContext context, uint capacity)
            : base(context, context.IndexFile.Alloc(SegmentSize))
        {
            _capacity = DeterminePrimeNumber(capacity);
            BucketCount = _capacity;
        }

        /// <summary>
        /// Initializes the hash map segment either from file or by writing a new structure 
        /// with zeroed buckets.
        /// </summary>
        /// <param name="initializationFromFile">
        /// If true, reads existing metadata; otherwise creates new buckets.
        /// </param>
        public virtual void Initialization(bool initializationFromFile)
        {
            if (initializationFromFile)
            {
                // read existing header only; do not allocate again on reopen
                Context.IndexFile.Read(this);
            }
            else
            {
                // write header
                Context.IndexFile.Write(this);

                // allocate bucket area directly after header so that GetBucket() addresses match (Addr + SegmentSize)
                var bucketsSize = BucketCount * IndexStorageSegmentBucket.SegmentSize;
                var bucketStart = Context.IndexFile.Alloc(bucketsSize);

                // this invariant should hold because constructor allocated header first
                // otherwise subsequent bucket address computations won't match layout
                if (bucketStart != Addr + SegmentSize)
                {
                    // zeroing remains correct from allocated start; GetBucket depends on Addr+SegmentSize
                    // but the mismatch indicates allocation order violation, so throw to avoid silent corruption
                    throw new IOException("hash map bucket layout mismatch: expected contiguous allocation after header.");
                }

                // zero-initialize bucket region
                var zeroBuffer = new byte[_bufferSize];
                var totalBytes = (long)bucketsSize;
                var bytesWritten = 0L;

                Context.IndexFile.FileStream.Seek((long)bucketStart, SeekOrigin.Begin);

                while (bytesWritten < totalBytes)
                {
                    var remaining = totalBytes - bytesWritten;
                    var toWrite = (int)Math.Min(_bufferSize, remaining);

                    Context.IndexFile.FileStream.Write(zeroBuffer, 0, toWrite);
                    bytesWritten += toWrite;
                }
            }
        }

        /// <summary>
        /// Adds an item to the hash map in sorted order within its bucket; returns 
        /// existing item if duplicate.
        /// </summary>
        /// <param name="segment">The item segment to insert.</param>
        /// <returns>The inserted item or the existing duplicate.</returns>
        public IndexStorageSegmentItem Add(IndexStorageSegmentItem segment)
        {
            var hash = segment.Id.GetHashCode();
            var index = (uint)hash % BucketCount;
            var bucket = GetBucket(index);

            lock (_guard)
            {
                if (bucket.ItemAddr == 0)
                {
                    bucket.ItemAddr = segment.Addr;

                    Context.IndexFile.Write(bucket);
                    Context.IndexFile.Write(segment);
                }
                else
                {
                    // check whether it exists (sorted by CompareTo)
                    var last = default(IndexStorageSegmentItem);
                    var count = 0u;

                    foreach (var i in GetBucket(segment.Id))
                    {
                        var compare = i.CompareTo(segment);

                        if (compare > 0)
                        {
                            break;
                        }
                        else if (compare == 0)
                        {
                            // duplicate; return existing
                            return i;
                        }

                        last = i;
                        count++;
                    }

                    if (last is null)
                    {
                        // insert at the beginning
                        var tempAddr = bucket.ItemAddr;
                        bucket.ItemAddr = segment.Addr;
                        segment.SuccessorAddr = tempAddr;

                        Context.IndexFile.Write(bucket);
                        Context.IndexFile.Write(segment);
                    }
                    else
                    {
                        // insert in the correct place
                        var tempAddr = last.SuccessorAddr;
                        last.SuccessorAddr = segment.Addr;
                        segment.SuccessorAddr = tempAddr;

                        Context.IndexFile.Write(last);
                        Context.IndexFile.Write(segment);
                    }
                }
            }

            return segment;
        }

        /// <summary>
        /// Enumerates all items within the bucket that corresponds to the given id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>Items in the corresponding bucket.</returns>
        public IEnumerable<IndexStorageSegmentItem> GetBucket(Guid id)
        {
            var hash = id.GetHashCode();
            var index = (uint)hash % BucketCount;
            var bucket = GetBucket(index);

            if (bucket.ItemAddr == 0)
            {
                yield break;
            }

            var addr = bucket.ItemAddr;

            while (addr != 0)
            {
                var item = Context.IndexFile.Read<IndexStorageSegmentItem>(addr, Context);
                yield return item;

                addr = item.SuccessorAddr;
            }
        }

        /// <summary>
        /// Removes the specified item from its bucket chain, if present.
        /// </summary>
        /// <param name="segment">The item to remove.</param>
        /// <returns>True if the item was found and removed; otherwise false.</returns>
        public bool Remove(IndexStorageSegmentItem segment)
        {
            if (segment is null)
            {
                return false;
            }

            var hash = segment.Id.GetHashCode();
            var index = (uint)hash % BucketCount;
            var bucket = GetBucket(index);

            lock (_guard)
            {
                // search the chain by on-disk address to ensure we only remove if present
                var prev = default(IndexStorageSegmentItem);
                var addr = bucket.ItemAddr;

                while (addr != 0)
                {
                    var current = Context.IndexFile.Read<IndexStorageSegmentItem>(addr, Context);

                    if (current.Addr == segment.Addr)
                    {
                        // unlink current
                        if (prev is null)
                        {
                            bucket.ItemAddr = current.SuccessorAddr;
                            Context.IndexFile.Write(bucket);
                        }
                        else
                        {
                            prev.SuccessorAddr = current.SuccessorAddr;
                            Context.IndexFile.Write(prev);
                        }

                        // free removed item
                        Context.Allocator.Free(current);

                        return true;
                    }

                    prev = current;
                    addr = current.SuccessorAddr;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads the hash map header from the storage medium.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        public override void Read(BinaryReader reader)
        {
            BucketCount = reader.ReadUInt32();
        }

        /// <summary>
        /// Returns the bucket structure at the specified index.
        /// </summary>
        /// <param name="index">The bucket index.</param>
        /// <returns>The bucket segment.</returns>
        private IndexStorageSegmentBucket GetBucket(uint index)
        {
            var addr = Addr + SegmentSize + (index * IndexStorageSegmentBucket.SegmentSize);
            var bucket = new IndexStorageSegmentBucket(Context, addr);

            return Context.IndexFile.Read(bucket);
        }

        /// <summary>
        /// Writes the hash map header to the storage medium.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(BucketCount);
        }

        /// <summary>
        /// Calculates the next prime number greater than or equal to capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity.</param>
        /// <returns>The next prime number.</returns>
        private static uint DeterminePrimeNumber(uint capacity)
        {
            for (uint i = capacity; i <= uint.MaxValue; i++)
            {
                if (i < 2)
                {
                    return 2;
                }

                var isPrimeNumber = true;

                for (int j = 2; j <= Math.Sqrt(i); j++)
                {
                    if (i % j == 0)
                    {
                        isPrimeNumber = false;
                        break; // break early once a divisor is found
                    }
                }

                if (isPrimeNumber)
                {
                    return i;
                }
            }

            return 65537;
        }
    }
}