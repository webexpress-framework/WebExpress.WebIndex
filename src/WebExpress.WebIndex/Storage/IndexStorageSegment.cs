using System.IO;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Abstract class representing a segment of index storage.
    /// </summary>
    public abstract class IndexStorageSegment : IIndexStorageSegment
    {
        /// <summary>
        /// Gets the address of the segment.
        /// </summary>
        public virtual ulong Addr { get; private set; }

        /// <summary>
        /// Gets the context of the index.
        /// </summary>
        public IndexStorageContext Context { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context of the index.</param>
        /// <param name="addr">The address of the segment.</param>
        public IndexStorageSegment(IndexStorageContext context, ulong addr)
        {
            Context = context;
            Addr = addr;
        }

        /// <summary>
        /// Reads the record from the storage medium.
        /// </summary>
        /// <param name="reader">The reader for i/o operations.</param>
        public abstract void Read(BinaryReader reader);

        /// <summary>
        /// Writes the record to the storage medium.
        /// </summary>
        /// <param name="writer">The writer for i/o operations.</param>
        public abstract void Write(BinaryWriter writer);

        /// <summary>
        /// Convert the object into a string representation. 
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Addr}: {GetType().Name}";
        }
    }
}
