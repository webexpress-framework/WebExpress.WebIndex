#nullable enable
using WebExpress.WebIndex.WiUI.Localization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebExpress.WebIndex.WebAttribute;

namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>
/// Reconstructs the scalar and nested properties described by a persisted index schema.
/// </summary>
public sealed class ExplorerSchema
{
    /// <summary>Gets the original type identity used by the storage engine.</summary>
    public string Type { get; init; } = "";

    /// <summary>Gets the schema fields in their persisted order.</summary>
    public List<ExplorerField> Fields { get; init; } = [];

    /// <summary>
    /// Validates a schema before any storage files are opened.
    /// </summary>
    /// <param name="path">The selected schema file.</param>
    /// <returns>The validated schema.</returns>
    public static ExplorerSchema Read(string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        var type = root.TryGetProperty("Type", out var identity) ? identity.GetString()
            : root.TryGetProperty("Name", out identity) ? identity.GetString() : null;
        if (string.IsNullOrWhiteSpace(type) || !root.TryGetProperty("Fields", out var fields))
        {
            throw new InvalidDataException(AppText.TheSchemaMustContainTypeAndFields);
        }

        var schema = new ExplorerSchema { Type = type };
        foreach (var field in fields.EnumerateArray())
        {
            var name = field.GetProperty("Name").GetString() ?? "";
            var fieldType = field.GetProperty("Type").GetString() ?? "";
            if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")
                || schema.Fields.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException(AppText.Format("InvalidSchemaField", name));
            }

            ResolveType(fieldType);
            schema.Fields.Add(new ExplorerField
            {
                Name = name, Type = fieldType,
                Ignore = field.TryGetProperty("Ignore", out var ignored) && ignored.GetBoolean()
            });
        }

        if (schema.Fields.FirstOrDefault(x => x.Name == "Id")?.Type is not "Guid")
        {
            throw new InvalidDataException(AppText.TheSchemaMustContainAGuidFieldNamedId);
        }

        return schema;
    }

    /// <summary>
    /// Creates a collectible runtime type so unrelated index schemas cannot share stale metadata.
    /// </summary>
    /// <returns>The runtime item type.</returns>
    public Type BuildType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Explorer_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Index");
        return BuildType(module, Type, Fields, true);
    }

    /// <summary>
    /// Builds nested objects from dotted schema paths while retaining index ignore attributes.
    /// </summary>
    /// <param name="module">The collectible module.</param>
    /// <param name="name">The runtime type name.</param>
    /// <param name="fields">The fields relative to this object.</param>
    /// <param name="root">Whether the object implements the index item contract.</param>
    /// <returns>The completed runtime type.</returns>
    private static Type BuildType(ModuleBuilder module, string name, List<ExplorerField> fields, bool root)
    {
        var builder = module.DefineType(name, TypeAttributes.Public, null, root ? [typeof(IIndexItem)] : []);
        foreach (var group in fields.GroupBy(x => x.Name.Split('.')[0]))
        {
            var leaf = group.FirstOrDefault(x => !x.Name.Contains('.'));
            if (leaf != null && group.Count() != 1)
            {
                throw new InvalidDataException(AppText.Format("ConflictingPaths", group.Key));
            }

            var propertyType = leaf != null ? ResolveType(leaf.Type) : BuildType(module, $"{name}_{group.Key}",
                group.Select(x => new ExplorerField { Name = x.Name[(group.Key.Length + 1)..], Type = x.Type, Ignore = x.Ignore }).ToList(), false);
            var field = builder.DefineField($"_{group.Key}", propertyType, FieldAttributes.Private);
            var property = builder.DefineProperty(group.Key, PropertyAttributes.None, propertyType, null);
            var attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            if (root && group.Key == "Id")
            {
                attributes |= MethodAttributes.Virtual;
            }

            var getter = builder.DefineMethod($"get_{group.Key}", attributes, propertyType, System.Type.EmptyTypes);
            var get = getter.GetILGenerator();
            get.Emit(OpCodes.Ldarg_0);
            get.Emit(OpCodes.Ldfld, field);
            get.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
            var setter = builder.DefineMethod($"set_{group.Key}", attributes, null, [propertyType]);
            var set = setter.GetILGenerator();
            set.Emit(OpCodes.Ldarg_0);
            set.Emit(OpCodes.Ldarg_1);
            set.Emit(OpCodes.Stfld, field);
            set.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);
            if (leaf?.Ignore == true)
            {
                property.SetCustomAttribute(new CustomAttributeBuilder(typeof(IndexIgnoreAttribute).GetConstructor(System.Type.EmptyTypes)!, []));
            }
        }

        return builder.CreateType()!;
    }

    /// <summary>
    /// Rejects unknown types instead of silently narrowing persisted values.
    /// </summary>
    /// <param name="name">The persisted CLR or legacy display name.</param>
    /// <returns>The corresponding CLR type.</returns>
    private static Type ResolveType(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "string" or "text" => typeof(string), "bool" or "boolean" => typeof(bool),
            "byte" => typeof(byte), "sbyte" => typeof(sbyte), "char" => typeof(char),
            "short" or "int16" => typeof(short), "uint16" => typeof(ushort),
            "int" or "integer" or "int32" => typeof(int), "uint32" => typeof(uint),
            "long" or "int64" => typeof(long), "uint64" => typeof(ulong),
            "float" or "single" => typeof(float), "double" => typeof(double), "decimal" => typeof(decimal),
            "datetime" => typeof(DateTime), "datetimeoffset" => typeof(DateTimeOffset),
            "timespan" => typeof(TimeSpan), "guid" => typeof(Guid),
            _ => throw new InvalidDataException(AppText.Format("UnsupportedType", name))
        };
    }
}

/// <summary>Preserves the field metadata needed for inspection and query construction.</summary>
public sealed class ExplorerField
{
    /// <summary>Gets the property path used by WQL.</summary>
    public string Name { get; init; } = "";
    /// <summary>Gets the original storage type name.</summary>
    public string Type { get; init; } = "";
    /// <summary>Gets whether reverse indexing is disabled for this field.</summary>
    public bool Ignore { get; init; }
}
