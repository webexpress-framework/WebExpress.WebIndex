using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Test.Document
{
    /// <summary>
    /// Abstract class representing a unit test document for index testing.
    /// </summary>
    public abstract class UnitTestIndexTestDocument : IIndexItem
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [IndexIgnore]
        public Guid Id { get; set; }
    }
}
