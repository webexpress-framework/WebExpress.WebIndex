using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents a hash map segment in the index storage.
    /// </summary>
    public class IndexStorageSegmentHashMap : IndexStorageSegment
    {
        private readonly uint _capacity;
        private readonly Lock _guard = new();
        private const int _bufferSize = 4096;

        /// <summary>
        /// Returns the amount of space required on the storage device.
        /// </summary>
        public const uint SegmentSize = sizeof(uint);

        /// <summary>
        /// The number of fields (buckets) of the hash map. This should be a 
        /// prime number so that there are fewer collisions.
        /// </summary>
        public uint BucketCount { get; private set; }

        /// <summary>
        /// A hash bucket is a range of memory in a hash table that is associated with a 
        /// specific hash value. A bucket provides a concatenated list by recording the 
        /// collisions (different keys with the same hash value).
        /// </summary>
        //private IndexStorageSegmentBucket[] Buckets { get; set; }

        /// <summary>
        /// Returns all items.
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
                        addr = item.SuccessorAddr;

                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="capacity">The initial capacity of the hash map segment.</param>
        public IndexStorageSegmentHashMap(IndexStorageContext context, uint capacity)
            : base(context, context.IndexFile.Alloc(SegmentSize))
        {
            _capacity = DeterminePrimeNumber(capacity);
            BucketCount = _capacity;
        }

        /// <summary>
        /// Initialization method for the hash map segment.
        /// </summary>
        /// <param name="initializationFromFile">If true, initializes from file. Otherwise, initializes and writes to file.</param>
        public virtual void Initialization(bool initializationFromFile)
        {
            if (initializationFromFile)
            {
                Context.IndexFile.Read(this);

                Context.IndexFile.Alloc(SegmentSize + BucketCount * IndexStorageSegmentBucket.SegmentSize);
            }
            else
            {
                var initalAddress = Context.IndexFile.Alloc(SegmentSize + (BucketCount * IndexStorageSegmentBucket.SegmentSize));
                var zeroBuffer = new byte[_bufferSize];
                var totalBytes = BucketCount * IndexStorageSegmentBucket.SegmentSize;
                var bytesWritten = 0L;

                Context.IndexFile.Write(this);
                Context.IndexFile.FileStream.Seek((long)initalAddress, SeekOrigin.Begin);

                while (bytesWritten < totalBytes)
                {
                    var bytesToWrite = Math.Min(_bufferSize, totalBytes - bytesWritten);

                    Context.IndexFile.FileStream.Write(zeroBuffer, 0, (int)bytesToWrite);
                    bytesWritten += bytesToWrite;
                }
            }
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="segment">The item segment.</param>
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
                    // check whether it exists
                    var last = default(IndexStorageSegmentItem);
                    var count = 0U;

                    foreach (var i in GetBucket(segment.Id))
                    {
                        var compare = i.CompareTo(segment);

                        if (compare > 0)
                        {
                            break;
                        }
                        else if (compare == 0)
                        {
                            return i;
                        }

                        last = i;

                        count++;
                    }

                    if (last == null)
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
        /// Returns all items in a bucket.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <returns>The items in the buckets.</returns>
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
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        /// <param name="segment">The object to remove from the list.</param>
        /// <returns>True if item was successfully removed from the list, 
        /// otherwise false. This method also returns false if item is not 
        /// found in the list.</returns>
        public bool Remove(IndexStorageSegmentItem segment)
        {
            if (segment == null)
            {
                return false;
            }

            var hash = segment.Id.GetHashCode();
            var index = (uint)hash % BucketCount;
            var bucket = GetBucket(index);

            lock (_guard)
            {
                var predecessor = GetPredecessor(segment, out _);

                if (predecessor == null)
                {
                    bucket.ItemAddr = segment.SuccessorAddr;

                    Context.IndexFile.Write(bucket);
                    Context.IndexFile.Write(segment);
                }
                else
                {
                    predecessor.SuccessorAddr = segment.SuccessorAddr;
                    Context.IndexFile.Write(predecessor);
                    segment.SuccessorAddr = 0;
                }

                Context.Allocator.Free(segment);
            }

            return true;
        }

        /// <summary>
        /// Returns the predecessor.
        /// </summary>
        /// <param name="item">The segment whose predecessor is to be determined.</param>
        /// <param name="index">The index.</param>
        /// <returns>The predecessor or null if there is no predecessor.</returns>
        private IndexStorageSegmentItem GetPredecessor(IndexStorageSegmentItem item, out uint index)
        {
            var last = default(IndexStorageSegmentItem);
            index = 0u;

            foreach (var i in GetBucket(item.Id))
            {
                var compare = i.CompareTo(item);

                if (compare > 0)
                {
                    break;
                }
                else if (compare == 0)
                {
                    return last;
                }

                last = i;
                index++;
            }

            return last;
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        public override void Read(BinaryReader reader)
        {
            BucketCount = reader.ReadUInt32();
        }

        /// <summary>
        /// Returns the bucket at the specified index.
        /// </summary>
        /// <param name="index">The index of the bucket to retrieve.</param>
        /// <returns>The bucket at the specified index.</returns>
        private IndexStorageSegmentBucket GetBucket(uint index)
        {
            var addr = Addr + SegmentSize + (index * IndexStorageSegmentBucket.SegmentSize);
            var bucket = new IndexStorageSegmentBucket(Context, addr);

            return Context.IndexFile.Read(bucket);
        }

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public override void Write(BinaryWriter writer)
        {
            writer.Write(BucketCount);
        }

        /// <summary>
        /// Calculates the next prime number.
        /// </summary>
        /// <param name="capacity"></param>
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