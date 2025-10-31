using System;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Represents an item in the index.
    /// </summary>
    public interface IIndexItem
    {
        /// <summary>
        /// The id of the item.
        /// </summary>
        [IndexIgnore]
        [JsonPropertyName("id")]
        Guid Id { get; }
    }
}
