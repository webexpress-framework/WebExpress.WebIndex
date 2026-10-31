using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// Describes the schema used to construct runtime index items for the console.
    /// </summary>
    internal class ObjectType
    {
        private readonly Dictionary<string, Type> _typeCache = [];

        /// <summary>
        /// Returns or sets the name of the object.
        /// </summary>
        /// <remarks>
        /// The schema file the index library writes carries the name under "Type", so the
        /// name has to be read from and written to that key; without the mapping an index
        /// reopened from its schema has no name and no runtime class can be built for it.
        /// </remarks>
        [JsonPropertyName("Type")]
        public string Name { get; set; }

        /// <summary>
        /// Returns a collection of fields associated with the object.
        /// </summary>
        public IEnumerable<Field> Fields { get; set; } = [];

        /// <summary>
        /// Dynamically builds a class based on the object's attributes.
        /// </summary>
        /// <returns>A Type representing the dynamically built class.</returns>
        public Type BuildRuntimeClass()
        {
            Validate();
            var key = JsonSerializer.Serialize(this);

            if (_typeCache.TryGetValue(key, out Type type))
            {
                return type;
            }

            var assemblyName = new AssemblyName("WebExpress.WebIndex.Wi.Model.Objects");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Module_{Name}");

            var typeBuilder = moduleBuilder.DefineType(Name, TypeAttributes.Public, null, [typeof(IIndexItem)]);

            BuildProperty(typeBuilder, "Id", typeof(Guid), true, true);

            foreach (var attribute in Fields.Where(x => !x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
            {
                BuildProperty(typeBuilder, attribute.Name, attribute.Type.ToType(), attribute.Ignore, attribute.Abstract);
            }

            var runtimeClass = typeBuilder.CreateType();
            _typeCache.Add(key, runtimeClass);

            return runtimeClass;
        }

        /// <summary>
        /// Rejects ambiguous fields and names that could escape the index directory.
        /// </summary>
        /// <exception cref="FormatException">The schema cannot be represented safely.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name) || Name != Name.Trim() || Name.Contains('.')
                || Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || Name.IndexOfAny(['/', '\\', '*', '?', ':']) >= 0 || Name is "." or "..")
            {
                throw new FormatException("The object type must have a valid file name.");
            }

            if (Fields is null)
            {
                throw new FormatException("The schema must contain a field collection.");
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Fields)
            {
                if (field is null || string.IsNullOrWhiteSpace(field.Name)
                    || !(char.IsLetter(field.Name[0]) || field.Name[0] == '_')
                    || field.Name.Any(x => !char.IsLetterOrDigit(x) && x != '_')
                    || !names.Add(field.Name) || !Enum.IsDefined(field.Type)
                    || (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && field.Type != FieldType.Guid))
                {
                    throw new FormatException("Fields must have unique identifiers and valid types; Id must be a Guid.");
                }
            }
        }

        /// <summary>
        /// Builds a property for the dynamically created class.
        /// </summary>
        /// <param name="typeBuilder">The builder for the class type.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="type">The type of the property.</param>
        /// <param name="indexIgnore">Indicates whether the property should be ignored by the index.</param>
        /// <param name="virtual">Indicates whether the property should be virtual.</param>
        /// <returns>A PropertyBuilder for the created property.</returns>
        private static PropertyBuilder BuildProperty(TypeBuilder typeBuilder, string name, Type type, bool indexIgnore, bool @virtual)
        {
            var fieldBuilder = typeBuilder.DefineField($"_{name.ToLowerInvariant()}", type, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, Type.EmptyTypes);

            if (indexIgnore)
            {
                var attrCtorParams = Array.Empty<Type>();
                var attrCtorInfo = typeof(IndexIgnoreAttribute).GetConstructor(attrCtorParams);
                var attrBuilder = new CustomAttributeBuilder(attrCtorInfo, []);
                propertyBuilder.SetCustomAttribute(attrBuilder);
            }

            var getMethodBuilder = typeBuilder.DefineMethod($"get_{name}", @virtual ? MethodAttributes.Public | MethodAttributes.Virtual : MethodAttributes.Public, type, Type.EmptyTypes);
            var getIlGenerator = getMethodBuilder.GetILGenerator();
            getIlGenerator.Emit(OpCodes.Ldarg_0);
            getIlGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            getIlGenerator.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getMethodBuilder);

            var setMethodBuilder = typeBuilder.DefineMethod($"set_{name}", @virtual ? MethodAttributes.Public | MethodAttributes.Virtual : MethodAttributes.Public, null, [type]);
            var setIlGenerator = setMethodBuilder.GetILGenerator();
            setIlGenerator.Emit(OpCodes.Ldarg_0);
            setIlGenerator.Emit(OpCodes.Ldarg_1);
            setIlGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            setIlGenerator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            return propertyBuilder;
        }
    }
}
