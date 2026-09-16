using System.Text.Json;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.Wi.Model;

namespace WebExpress.WebIndex.Wi.Converter
{
    /// <summary>
    /// Custom converter for field typ to string.
    /// </summary>
    internal class FieldTypeConverter : JsonConverter<FieldType>
    {
        /// <summary>
        /// Converts a string to an int.
        /// </summary>
        public override FieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return FieldTypeExtention.FromStringValue(reader.GetString());
        }

        /// <summary>
        /// Writes the field type under the name the tool shows for it, which is also one of
        /// the names <see cref="Read"/> accepts, so a file the tool wrote reads back as it was.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, FieldType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(FieldTypeExtention.ToString(value));
        }
    }
}
