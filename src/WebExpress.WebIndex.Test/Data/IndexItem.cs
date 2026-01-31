namespace WebExpress.WebIndex.Test.Data
{
    /// <summary>
    /// Represents an mock item.
    /// </summary>
    public class IndexItem : IIndexItem
    {
        /// <summary>
        /// Returns a new unique identifier each time the property is accessed.
        /// </summary>
        public Guid Id => Guid.NewGuid();

        /// <summary>
        /// Returns or sets the name associated with the object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns or sets the integer value associated with this instance.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Returns or sets a value indicating whether the object is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
