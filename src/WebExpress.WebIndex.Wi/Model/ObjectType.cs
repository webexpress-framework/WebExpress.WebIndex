using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Serialization;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.Wi.Model
{
    /// <summary>
    /// Represents an object within the WebExpress.WebIndex.Studio.
    /// </summary>
    internal class ObjectType
    {
        private Dictionary<string, Type> _typeCache = [];

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
        /// Retuns a collection of fields associated with the object.
        /// </summary>
        public IEnumerable<Field> Fields { get; set; } = [];

        /// <summary>
        /// Returns a collection of stored data objects.
        /// </summary>
        [JsonIgnore]
        public List<object> All => new(WiApp.ViewModel.IndexManager.All(BuildRuntimeClass()));

        /// <summary>
        /// Returns the number of items of the index.
        /// </summary>
        [JsonIgnore]
        public uint Count => WiApp.ViewModel.IndexManager.Count(BuildRuntimeClass());

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectType"/> class.
        /// </summary>
        public ObjectType()
        {

        }

        /// <summary>
        /// Dynamically builds a class based on the object's attributes.
        /// </summary>
        /// <returns>A Type representing the dynamically built class.</returns>
        public Type BuildRuntimeClass()
        {
            if (_typeCache.TryGetValue(Name, out Type type))
            {
                return type;
            }

            var assemblyName = new AssemblyName("WebExpress.WebIndex.Wi.Model.Objects");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Module_{Name}");

            var typeBuilder = moduleBuilder.DefineType(Name, TypeAttributes.Public, null, [typeof(IIndexItem)]);

            BildProperty(typeBuilder, "Id", typeof(Guid), true, true);

            foreach (var attribute in Fields.Where(x => !x.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
            {
                BildProperty(typeBuilder, attribute.Name, attribute.Type.ToType(), attribute.Ignore, attribute.Abstract);
            }

            var runtimeClass = typeBuilder.CreateType();
            _typeCache.Add(Name, runtimeClass);

            return runtimeClass;
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
        private PropertyBuilder BildProperty(TypeBuilder typeBuilder, string name, Type type, bool indexIgnore, bool @virtual)
        {
            var fieldBuilder = typeBuilder.DefineField($"_{name.ToLower()}", type, FieldAttributes.Private);
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
