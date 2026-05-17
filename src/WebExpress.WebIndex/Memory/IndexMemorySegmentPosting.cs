using System;

namespace WebExpress.WebIndex.Memory
{
    /// <summary>
    /// Saves the referenc to the items.
    /// </summary>
    public class IndexMemorySegmentPosting
    {
        /// <summary>
        /// Gets the document id.
        /// </summary>
        public Guid DocumentId { get; private set; }

        /// <summary>
        /// Gets the a list of the positions.
        /// </summary>
        public IndexMemorySegmentPosition Positions { get; private set; } = [];

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="id">The item.</param>
        /// <param name="position">The position of the term in the input value.</param>
        public IndexMemorySegmentPosting(Guid id, uint position)
        {
            DocumentId = id;

            Positions.Add(position);
        }

        /// <summary>
        /// Converts the order expression to a string.
        /// </summary>
        /// <returns>The order expression as a string.</returns>
        public override string ToString()
        {
            return $"{DocumentId} : {base.ToString()}";
        }
    }
}
