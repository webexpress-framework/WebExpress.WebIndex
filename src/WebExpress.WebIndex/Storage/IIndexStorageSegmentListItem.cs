using System;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Interface for managing index storage segment list items.
    /// </summary>
    public interface IIndexStorageSegmentListItem : IIndexStorageSegment, IComparable
    {
        /// <summary>
        /// Gets or sets the address of the following list item.
        /// </summary>
        ulong SuccessorAddr { get; set; }
    }
}