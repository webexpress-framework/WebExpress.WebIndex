using System;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Describes a change of an index document after it happened. It lets a host keep several
    /// copies of an index in step - one per server instance - by replaying each change on the
    /// other copies.
    /// </summary>
    public sealed class IndexChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the item type of the changed document.
        /// </summary>
        public Type ItemType { get; }

        /// <summary>
        /// Gets the kind of change.
        /// </summary>
        public IndexChangeKind Kind { get; }

        /// <summary>
        /// Gets the inserted, updated or deleted item; null when a document was cleared.
        /// </summary>
        public IIndexItem Item { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="itemType">The item type of the changed document.</param>
        /// <param name="kind">The kind of change.</param>
        /// <param name="item">The changed item, or null for a cleared document.</param>
        public IndexChangedEventArgs(Type itemType, IndexChangeKind kind, IIndexItem item)
        {
            ItemType = itemType;
            Kind = kind;
            Item = item;
        }
    }
}
