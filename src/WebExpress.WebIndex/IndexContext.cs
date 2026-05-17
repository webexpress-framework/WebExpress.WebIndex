using System;
using System.IO;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Represents the context for the index, providing access to the index directory.
    /// </summary>
    public class IndexContext : IIndexContext
    {
        /// <summary>
        /// Gets or sets the data directory where the index data is located.
        /// </summary>
        public string IndexDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "index");
    }
}
