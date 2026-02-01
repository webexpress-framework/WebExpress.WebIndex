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

        /// <summary>
        /// Returns or sets the description associated with the object.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Returns or sets the file system path associated with this instance.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Returns or sets the email address associated with the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Returns or sets the collection of tags associated with the item.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }
    }
}
