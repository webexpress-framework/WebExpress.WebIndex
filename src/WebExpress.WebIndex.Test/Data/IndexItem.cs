namespace WebExpress.WebIndex.Test.Data
{
    /// <summary>
    /// Represents an mock item.
    /// </summary>
    public class IndexItem : IIndexItem
    {
        private readonly Guid _id = Guid.NewGuid();

        /// <summary>
        /// Returns the unique identifier associated with this instance.
        /// </summary>
        public Guid Id => _id;

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the integer value associated with this instance.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the description associated with the object.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the file system path associated with this instance.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the collection of tags associated with the item.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }
    }
}
