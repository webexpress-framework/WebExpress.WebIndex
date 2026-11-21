using System.Reflection;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// A read buffer item for buffering of segments.
    /// </summary>
    public class IndexStorageBufferItem
    {
        /// <summary>
        /// Gets or sets the lifetime.
        /// </summary>
        public static uint Lifetime { get; set; } = 100;

        /// <summary>
        /// Gets the lifetime counter used for deletion from the buffer.
        /// </summary>
        private uint _counter;

        /// <summary>
        /// Gets the lifetime counter used for deletion from the buffer.
        /// </summary>
        public uint Counter => _counter;

        /// <summary>
        /// Gets the segment to be cached.
        /// </summary>
        public IIndexStorageSegment Segment { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="segment">The segment to be cached.</param>
        public IndexStorageBufferItem(IIndexStorageSegment segment)
        {
            Segment = segment;

            if (segment.GetType().GetCustomAttribute<SegmentCachedAttribute>() is not null)
            {
                _counter = uint.MaxValue;
            }
            else
            {
                Refresh();
            }
        }

        /// <summary>
        /// Ages the entry by one maintenance tick. The counter stops at zero, so an idle entry
        /// stays the oldest instead of wrapping around to the freshest value.
        /// </summary>
        public void Age()
        {
            if (_counter > 0)
            {
                _counter--;
            }
        }

        /// <summary>
        /// Refresh the lifetime.
        /// </summary>
        public void Refresh()
        {
            _counter = Lifetime;
        }
    }
}
