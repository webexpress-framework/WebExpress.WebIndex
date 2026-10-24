using System;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// The contract a data object must implement to be stored in and retrieved from the index.
    /// The only requirement is a unique <see cref="Id"/> that identifies the item.
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
