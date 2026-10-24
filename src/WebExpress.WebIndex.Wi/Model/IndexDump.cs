using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// The content of an export file: the object type an index holds and every item of it.
    /// </summary>
    /// <remarks>
    /// An export carries its schema so that an import can rebuild the index without one
    /// being open - which is why import is offered before an index is opened, and export
    /// inside one. The type name is written under the key the index library uses in its
    /// own schema file.
    /// </remarks>
    internal class IndexDump
    {
        /// <summary>
        /// Returns or sets the name of the object type.
        /// </summary>
        [JsonPropertyName("Type")]
        public string Name { get; set; }

        /// <summary>
        /// Returns or sets the fields of the object type.
        /// </summary>
        public IEnumerable<Field> Fields { get; set; } = [];

        /// <summary>
        /// Returns or sets the items, each as its property values keyed by property name.
        /// </summary>
        public List<Dictionary<string, JsonElement>> Items { get; set; } = [];
    }
}
