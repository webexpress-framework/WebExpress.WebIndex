namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// The root of the value tree of a numeric reverse index. Every node is a distinct value of the
    /// field with the number of documents that hold it and the tree of their ids; numbers carry no
    /// positions, see <see cref="IndexStorageSegmentNumericNode"/>.
    /// </summary>
    /// <param name="context">The reference to the context of the index.</param>
    public class IndexStorageSegmentNumeric(IndexStorageContext context)
        : IndexStorageSegmentNumericNode(context, context.IndexFile.Alloc(SegmentSize))
    {
        /// <summary>
        /// Initialization method for the header segment.
        /// </summary>
        /// <param name="initializationFromFile">If true, initializes from file. Otherwise, initializes and writes to file.</param>
        public virtual void Initialization(bool initializationFromFile)
        {
            if (initializationFromFile)
            {
                Context.IndexFile.Read(this);
            }
            else
            {
                Context.IndexFile.Write(this);
            }
        }
    }
}