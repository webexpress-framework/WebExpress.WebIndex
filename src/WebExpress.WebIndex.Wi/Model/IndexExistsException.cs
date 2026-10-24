using System;

namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// The exception raised when an import is refused because an index of the exported object
    /// type already exists. It is a type of its own so that only this refusal points the user to
    /// the replace option; any other invalid operation during an import has another cause that
    /// replacing the index would not remove.
    /// </summary>
    /// <param name="name">The name of the object type.</param>
    /// <param name="directory">The directory that holds the index.</param>
    public class IndexExistsException(string name, string directory)
        : InvalidOperationException($"The index '{name}' already exists in '{directory}'.")
    {
    }
}
