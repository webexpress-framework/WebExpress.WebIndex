namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// Represents the types of field that can be used.
    /// </summary>
    internal enum FieldType
    {
        /// <summary>
        /// Field type for plain text.
        /// </summary>
        Text,

        /// <summary>
        /// Field type for boolean values.
        /// </summary>
        Bool,

        /// <summary>
        /// Field type for integer values.
        /// </summary>
        Int,

        /// <summary>
        /// Field type for double precision floating point numbers.
        /// </summary>
        Double,

        /// <summary>
        /// Field type for date time values.
        /// </summary>
        DateTime,

        /// <summary>
        /// Field type for guid values.
        /// </summary>
        Guid,

        /// <summary>
        /// Field type for undefine value.
        /// </summary>
        Object
    }

    /// <summary>
    /// Provides extension methods for the FieldType enumeration.
    /// </summary>
    internal static class FieldTypeExtention
    {
        /// <summary>
        /// Converts the FieldType value to a corresponding System.Type.
        /// </summary>
        /// <param name="type">The FieldType value to convert.</param>
        /// <returns>The System.Type that corresponds to the given FieldType value.</returns>
        public static Type ToType(this FieldType type)
        {
            return type switch
            {
                FieldType.Text => typeof(string),
                FieldType.Bool => typeof(bool),
                FieldType.Int => typeof(int),
                FieldType.Double => typeof(double),
                FieldType.DateTime => typeof(DateTime),
                FieldType.Guid => typeof(Guid),
                _ => typeof(object)
            };
        }

        /// <summary>
        /// Converts the FieldType value to a corresponding string representation.
        /// </summary>
        /// <param name="type">The FieldType value to convert.</param>
        /// <returns>The string representation of the given FieldType value.</returns>
        public static string ToString(this FieldType type)
        {
            return type switch
            {
                FieldType.Text => "Text",
                FieldType.Bool => "Boolean",
                FieldType.Int => "Integer",
                FieldType.Double => "Double",
                FieldType.DateTime => "DateTime",
                FieldType.Guid => "Guid",
                _ => "Object"
            };
        }

        /// <summary>
        /// Converts a string to a corresponding FieldType value.
        /// </summary>
        /// <remarks>
        /// Two vocabularies are read: the CLR type names the index library writes into its
        /// schema file (<c>String</c>, <c>Int32</c>, ...) and the names this tool shows and
        /// writes into an export (<c>Text</c>, <c>Integer</c>, ...). A name of either kind
        /// has to come back as the same field type, or a schema read from disk would turn
        /// its numbers into untyped objects.
        /// </remarks>
        /// <param name="str">The string to convert.</param>
        /// <returns>The FieldType value that corresponds to the given string.</returns>
        public static FieldType FromStringValue(string str)
        {
            return str?.Trim().ToLowerInvariant() switch
            {
                "string" or "text" => FieldType.Text,
                "boolean" or "bool" => FieldType.Bool,
                "integer" or "int" or "int32" or "int64" or "int16" => FieldType.Int,
                "double" or "single" or "decimal" => FieldType.Double,
                "datetime" => FieldType.DateTime,
                "guid" => FieldType.Guid,
                _ => FieldType.Object
            };
        }
    }
}
