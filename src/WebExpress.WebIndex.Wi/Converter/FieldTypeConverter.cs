using System.Text.Json;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.Wi.Model;

namespace WebExpress.WebIndex.Wi.Converter
{
    /// <summary>
    /// Preserves supported schema types across JSON import and export.
    /// </summary>
    internal class FieldTypeConverter : JsonConverter<FieldType>
    {
        /// <summary>
        /// Rejects unknown schema types before import can change existing storage.
        /// </summary>
        /// <param name="reader">The reader positioned on the field type token.</param>
        /// <param name="typeToConvert">The target field type enumeration.</param>
        /// <param name="options">The active serializer settings.</param>
        /// <returns>The supported field type.</returns>
        public override FieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("A field type must be a string.");
            }

            var name = reader.GetString();
            var type = FieldTypeExtention.FromStringValue(name);
            if (type == FieldType.Object && !string.Equals(name?.Trim(), "Object", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException($"Unknown field type '{name}'.");
            }

            return type;
        }

        /// <summary>
        /// Writes the field type under the name the tool shows for it, which is also one of
        /// the names <see cref="Read"/> accepts, so a file the tool wrote reads back as it was.
        /// </summary>
        /// <param name="writer">The destination JSON writer.</param>
        /// <param name="value">The field type to persist.</param>
        /// <param name="options">The active serializer settings.</param>
        public override void Write(Utf8JsonWriter writer, FieldType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(FieldTypeExtention.ToString(value));
        }
    }
}
