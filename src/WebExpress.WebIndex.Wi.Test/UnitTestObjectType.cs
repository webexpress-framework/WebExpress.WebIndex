using System.Text.Json;
using WebExpress.WebIndex.WebAttribute;
using WebExpress.WebIndex.Wi.Converter;
using WebExpress.WebIndex.Wi.Model;
using Xunit;

namespace WebExpress.WebIndex.Wi.Test
{
    /// <summary>
    /// Protects runtime schema generation and lossless field type serialization.
    /// </summary>
    public class UnitTestObjectType
    {
        /// <summary>
        /// Verifies the identity contract, ignored fields and property value storage.
        /// </summary>
        [Fact]
        public void RuntimeTypeImplementsIndexContract()
        {
            // arrange
            var schema = new ObjectType { Name = "Records", Fields = [
                new Field { Name = "Value", Type = FieldType.Decimal },
                new Field { Name = "Secret", Type = FieldType.Text, Ignore = true }] };

            // act
            var type = schema.BuildRuntimeClass();
            var item = (IIndexItem)Activator.CreateInstance(type);
            type.GetProperty("Value").SetValue(item, 1234567890.123456789m);

            // validation
            Assert.Equal(typeof(Guid), type.GetProperty("Id").PropertyType);
            Assert.NotEmpty(type.GetProperty("Id").GetCustomAttributes(typeof(IndexIgnoreAttribute), false));
            Assert.NotEmpty(type.GetProperty("Secret").GetCustomAttributes(typeof(IndexIgnoreAttribute), false));
            Assert.Equal(1234567890.123456789m, type.GetProperty("Value").GetValue(item));
            Assert.Same(type, schema.BuildRuntimeClass());
        }

        /// <summary>
        /// Prevents cached types from silently retaining a previous schema.
        /// </summary>
        [Fact]
        public void SchemaChangeBuildsMatchingType()
        {
            // arrange
            var field = new Field { Name = "Value", Type = FieldType.Int };
            var schema = new ObjectType { Name = "Records", Fields = [field] };
            var original = schema.BuildRuntimeClass();

            // act
            field.Type = FieldType.Long;
            var changed = schema.BuildRuntimeClass();

            // validation
            Assert.NotSame(original, changed);
            Assert.Equal(typeof(long), changed.GetProperty("Value").PropertyType);
        }

        /// <summary>
        /// Rejects ambiguous names before reflection emits an invalid runtime type.
        /// </summary>
        /// <param name="name">The invalid or duplicate field name.</param>
        [Theory]
        [InlineData("value")]
        [InlineData("Id")]
        [InlineData("")]
        [InlineData("with space")]
        [InlineData("../file")]
        public void InvalidFieldsAreRejected(string name)
        {
            // arrange
            var schema = new ObjectType { Name = "Records", Fields = [
                new Field { Name = "Value", Type = FieldType.Text },
                new Field { Name = name, Type = FieldType.Text }] };

            // act
            var error = Record.Exception(() => schema.BuildRuntimeClass());

            // validation
            Assert.IsType<FormatException>(error);
        }

        /// <summary>
        /// Round trips every supported field type through its exported spelling.
        /// </summary>
        [Fact]
        public void AllFieldTypesRoundTrip()
        {
            // arrange
            var options = new JsonSerializerOptions { Converters = { new FieldTypeConverter() } };

            foreach (var fieldType in Enum.GetValues<FieldType>())
            {
                // act
                var json = JsonSerializer.Serialize(fieldType, options);
                var restored = JsonSerializer.Deserialize<FieldType>(json, options);

                // validation
                Assert.Equal(fieldType, restored);
                Assert.Equal(fieldType.ToType(), restored.ToType());
            }
        }

        /// <summary>
        /// Accepts CLR schema spellings without narrowing numeric widths.
        /// </summary>
        /// <param name="name">The persisted CLR type name.</param>
        /// <param name="expected">The expected runtime type.</param>
        [Theory]
        [InlineData("String", typeof(string))]
        [InlineData("Boolean", typeof(bool))]
        [InlineData("Int16", typeof(short))]
        [InlineData("Int32", typeof(int))]
        [InlineData("Int64", typeof(long))]
        [InlineData("Single", typeof(float))]
        [InlineData("Double", typeof(double))]
        [InlineData("Decimal", typeof(decimal))]
        [InlineData("DateTime", typeof(DateTime))]
        [InlineData("Guid", typeof(Guid))]
        [InlineData("Object", typeof(object))]
        public void ClrTypeNamesKeepTheirWidth(string name, Type expected)
        {
            // arrange
            var options = new JsonSerializerOptions { Converters = { new FieldTypeConverter() } };

            // act
            var fieldType = JsonSerializer.Deserialize<FieldType>(JsonSerializer.Serialize(name), options);

            // validation
            Assert.Equal(expected, fieldType.ToType());
        }

        /// <summary>
        /// Rejects malformed types instead of silently importing them as objects.
        /// </summary>
        /// <param name="json">The invalid serialized type.</param>
        [Theory]
        [InlineData("null")]
        [InlineData("42")]
        [InlineData("\"Unknown\"")]
        public void InvalidJsonTypesAreRejected(string json)
        {
            // arrange
            var options = new JsonSerializerOptions { Converters = { new FieldTypeConverter() } };

            // act
            var error = Record.Exception(() => JsonSerializer.Deserialize<FieldType>(json, options));

            // validation
            Assert.IsType<JsonException>(error);
        }
    }
}
