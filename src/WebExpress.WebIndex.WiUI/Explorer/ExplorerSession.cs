#nullable enable
using WebExpress.WebIndex.WiUI.Localization;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.Wql;

namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>
/// Owns one index connection and keeps read inspection isolated from storage migrations.
/// </summary>
public sealed class ExplorerSession : IDisposable
{
    private readonly IExplorerDocument _document;
    private readonly string? _snapshotDirectory;

    /// <summary>Gets the selected schema path.</summary>
    public string Path { get; }
    /// <summary>Gets the schema used by the query editor and table.</summary>
    public ExplorerSchema Schema { get; }
    /// <summary>Gets whether explicit document writes are enabled.</summary>
    public bool Writable { get; }
    /// <summary>Gets the number of stored documents.</summary>
    public uint Count => _document.Count;
    /// <summary>Gets the bounded result size used to keep the desktop responsive.</summary>
    public const int ResultLimit = 10000;

    /// <summary>
    /// Opens a schema with exclusive source access for editing or an isolated inspection copy.
    /// </summary>
    /// <param name="path">The existing schema file.</param>
    /// <param name="writable">Whether the user explicitly enabled editing.</param>
    /// <param name="culture">The token processing culture of this index.</param>
    public ExplorerSession(string path, bool writable, CultureInfo culture)
    {
        Path = System.IO.Path.GetFullPath(path);
        Writable = writable;
        Schema = ExplorerSchema.Read(Path);
        var type = Schema.BuildType();
        if (!string.Equals(type.Name, System.IO.Path.GetFileNameWithoutExtension(Path), StringComparison.Ordinal))
        {
            throw new InvalidDataException(AppText.TheSchemaTypeDoesNotMatchItsFileName);
        }

        var directory = System.IO.Path.GetDirectoryName(Path)!;
        if (!File.Exists(System.IO.Path.Combine(directory, type.Name + ".wds")))
        {
            throw new InvalidDataException(AppText.TheDocumentStoreWdsIsMissing);
        }

        try
        {
            if (!writable)
            {
                _snapshotDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebIndexExplorer", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_snapshotDirectory);
                // hold all source handles until the copy is complete to prevent mixed file generations
                var sources = new List<FileStream>();
                try
                {
                    foreach (var file in Directory.EnumerateFiles(directory).Where(x =>
                        System.IO.Path.GetFileName(x).StartsWith(type.Name + ".", StringComparison.Ordinal)
                        && (System.IO.Path.GetFileName(x) == type.Name + ".ws"
                            || System.IO.Path.GetFileName(x) == type.Name + ".wds"
                            || System.IO.Path.GetExtension(x) is ".wrt" or ".wrn" or ".rebuild")))
                    {
                        sources.Add(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
                    }
                    foreach (var source in sources)
                    {
                        using var target = File.Create(System.IO.Path.Combine(_snapshotDirectory, System.IO.Path.GetFileName(source.Name)));
                        source.CopyTo(target);
                    }
                }
                finally
                {
                    sources.ForEach(x => x.Dispose());
                }
                directory = _snapshotDirectory;
            }

            _document = (IExplorerDocument)Activator.CreateInstance(typeof(ExplorerDocument<>).MakeGenericType(type), directory, writable, culture)!;
        }
        catch (Exception error)
        {
            DeleteSnapshot();
            ExceptionDispatchInfo.Capture(error is TargetInvocationException { InnerException: { } inner } ? inner : error).Throw();
            throw;
        }
    }

    /// <summary>
    /// Validates a query without enumerating documents.
    /// </summary>
    /// <param name="query">The WQL editor text.</param>
    /// <returns>The parser diagnostic, or null for valid input.</returns>
    public WqlExpressionError? Validate(string query) => _document.Validate(query);

    /// <summary>
    /// Uses the parser lookahead at the caret to suggest valid next tokens.
    /// </summary>
    /// <param name="prefix">The editor text preceding the replacement range.</param>
    /// <returns>Contextual insertion choices.</returns>
    public IReadOnlyList<string> Suggest(string prefix)
    {
        var expected = _document.Analyze(prefix).ExpectedNextTokens.ToHashSet();
        var result = new List<string>();
        if (expected.Contains(WqlExpressionType.Attribute)) result.AddRange(Schema.Fields.Select(x => x.Name));
        if (expected.Contains(WqlExpressionType.Operator)) result.AddRange(["=", "!=", ">", ">=", "<", "<=", "~", "contains", "starts with", "ends with", "in (", "between", "is null"]);
        if (expected.Contains(WqlExpressionType.LogicalOperator)) result.AddRange(["and", "or"]);
        if (expected.Contains(WqlExpressionType.Order)) result.Add("order by");
        if (expected.Contains(WqlExpressionType.OrderDirection)) result.AddRange(["asc", "desc"]);
        if (expected.Contains(WqlExpressionType.Partitioning) || expected.Contains(WqlExpressionType.PartitioningOperator)) result.AddRange(["take", "skip"]);
        if (expected.Contains(WqlExpressionType.Parameter)) result.AddRange(["'value'", "null", "true", "false", "0"]);
        if (expected.Contains(WqlExpressionType.CloseParenthesis)) result.Add(")");
        if (expected.Contains(WqlExpressionType.Separator)) result.Add(",");
        if (expected.Contains(WqlExpressionType.Function)) result.AddRange(["len(", "lower(", "upper(", "now()"]);
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Materializes a bounded result so disposed storage is never accessed by the UI.
    /// </summary>
    /// <param name="query">The WQL expression, or blank for all documents.</param>
    /// <param name="cancellationToken">The cooperative cancellation token.</param>
    /// <returns>At most one more row than the display limit to detect truncation.</returns>
    public IReadOnlyList<object> Query(string query, CancellationToken cancellationToken) => _document.Query(query, cancellationToken);

    /// <summary>
    /// Reads reverse terms for a selected text field.
    /// </summary>
    /// <param name="field">The selected schema path.</param>
    /// <param name="cancellationToken">The cooperative cancellation token.</param>
    /// <returns>A bounded term snapshot.</returns>
    public IReadOnlyList<ExplorerTerm> Terms(string field, CancellationToken cancellationToken) => _document.Terms(field, cancellationToken);

    /// <summary>
    /// Provides a valid JSON starting point for a new document.
    /// </summary>
    /// <returns>A new document with a unique identifier.</returns>
    public string NewDocument() => _document.NewDocument();

    /// <summary>Resolves durable document links independently of the result limit and query.</summary>
    /// <param name="id">The linked document identity.</param>
    /// <returns>The current document, or null when it is missing.</returns>
    public object? Find(Guid id) => _document.Find(id);

    /// <summary>
    /// Validates a whole document before changing the storage and reverse indexes.
    /// </summary>
    /// <param name="json">The document JSON.</param>
    /// <param name="existingId">The immutable identifier when updating, or null when inserting.</param>
    public void Save(string json, Guid? existingId)
    {
        EnsureWritable();
        _document.Save(json, existingId);
    }

    /// <summary>
    /// Removes an explicitly confirmed document through the index manager.
    /// </summary>
    /// <param name="id">The selected document identifier.</param>
    public void Delete(Guid id)
    {
        EnsureWritable();
        _document.Delete(id);
    }

    /// <summary>Rejects programmatic writes through inspection sessions.</summary>
    private void EnsureWritable()
    {
        if (!Writable) throw new InvalidOperationException(AppText.EnableEditingBeforeChangingDocuments);
    }

    /// <summary>Releases storage locks and the private inspection copy.</summary>
    public void Dispose()
    {
        _document.Dispose();
        DeleteSnapshot();
    }

    /// <summary>Deletes only the temporary directory owned by this session.</summary>
    private void DeleteSnapshot()
    {
        if (_snapshotDirectory != null && Directory.Exists(_snapshotDirectory)) Directory.Delete(_snapshotDirectory, true);
    }
}

/// <summary>Describes a materialized reverse term and its postings.</summary>
public sealed class ExplorerTerm
{
    /// <summary>Gets the indexed term.</summary>
    public object Value { get; init; } = "";
    /// <summary>Gets the frequency recorded by the index.</summary>
    public uint Frequency { get; init; }
    /// <summary>Gets the distinct document identifiers.</summary>
    public Guid[] DocumentIds { get; init; } = [];
    /// <summary>Gets the number of matching documents.</summary>
    public int Documents => DocumentIds.Length;
}

/// <summary>Bridges runtime schema types to the strongly typed storage API.</summary>
internal interface IExplorerDocument : IDisposable
{
    /// <summary>Resolves a document without applying table filters.</summary>
    /// <param name="id">The document identity.</param>
    /// <returns>The stored document, or null.</returns>
    object? Find(Guid id);
    /// <summary>Gets the current document count.</summary>
    uint Count { get; }
    /// <summary>Validates editor input.</summary>
    /// <param name="query">The WQL expression.</param>
    /// <returns>The diagnostic or null.</returns>
    WqlExpressionError? Validate(string query);
    /// <summary>Analyzes a caret prefix.</summary>
    /// <param name="prefix">The text before the caret.</param>
    /// <returns>The parser lookahead.</returns>
    IWqlLookahead Analyze(string prefix);
    /// <summary>Materializes query results.</summary>
    /// <param name="query">The query text.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The bounded result.</returns>
    IReadOnlyList<object> Query(string query, CancellationToken token);
    /// <summary>Materializes reverse terms.</summary>
    /// <param name="field">The field path.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The bounded terms.</returns>
    IReadOnlyList<ExplorerTerm> Terms(string field, CancellationToken token);
    /// <summary>Creates initial document JSON.</summary>
    /// <returns>The document template.</returns>
    string NewDocument();
    /// <summary>Applies a validated document mutation.</summary>
    /// <param name="json">The full document JSON.</param>
    /// <param name="existingId">The immutable update identifier.</param>
    void Save(string json, Guid? existingId);
    /// <summary>Deletes a document.</summary>
    /// <param name="id">The document identifier.</param>
    void Delete(Guid id);
}

/// <summary>Uses the normal engine for both WQL evaluation and index consistent writes.</summary>
/// <typeparam name="T">The reconstructed index item type.</typeparam>
internal sealed class ExplorerDocument<T> : WebIndex.IndexManager, IExplorerDocument where T : class, IIndexItem, new()
{
    /// <summary>Uses direct storage lookup for stable project references.</summary>
    /// <param name="id">The document identity.</param>
    /// <returns>The stored document, or null.</returns>
    public object? Find(Guid id) => GetIndexDocument<T>().DocumentStore.GetItem(id);
    private readonly WqlParser<T> _parser = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };

    /// <summary>Gets the count without scanning every document.</summary>
    public uint Count => Count<T>();

    /// <summary>
    /// Checks schema compatibility before allowing a writable engine to open source files.
    /// </summary>
    /// <param name="directory">The source or snapshot directory.</param>
    /// <param name="writable">Whether schema migrations must be rejected.</param>
    /// <param name="culture">The token processing culture.</param>
    public ExplorerDocument(string directory, bool writable, CultureInfo culture)
    {
        Initialization(new IndexContext { IndexDirectory = directory });
        try
        {
            using var schema = new IndexStorageSchema<T>(Context);
            if (writable && schema.HasSchemaChanged())
            {
                throw new InvalidDataException(AppText.SchemaMismatchError);
            }
            Create<T>(culture, IndexType.Storage);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Returns parser diagnostics without executing the query.</summary>
    /// <param name="query">The WQL expression.</param>
    /// <returns>The diagnostic or null.</returns>
    public WqlExpressionError? Validate(string query) => _parser.Parse(query).Error;

    /// <summary>Uses the engine grammar for contextual suggestions.</summary>
    /// <param name="prefix">The caret prefix.</param>
    /// <returns>The parser lookahead.</returns>
    public IWqlLookahead Analyze(string prefix) => _parser.Analyze(prefix);

    /// <summary>Rejects malformed queries before materializing a bounded result.</summary>
    /// <param name="query">The WQL expression.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The document snapshot.</returns>
    public IReadOnlyList<object> Query(string query, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var statement = _parser.Parse(query);
        if (statement.HasErrors) throw new InvalidOperationException(AppText.Diagnostic(statement.Error.Message));
        var rows = new List<object>();
        foreach (var item in statement.Apply(GetIndexDocument<T>()))
        {
            token.ThrowIfCancellationRequested();
            rows.Add(item);
            if (rows.Count > ExplorerSession.ResultLimit) break;
        }
        return rows;
    }

    /// <summary>Reads text postings without treating numeric trees as text indexes.</summary>
    /// <param name="field">The field path.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The bounded term snapshot.</returns>
    public IReadOnlyList<ExplorerTerm> Terms(string field, CancellationToken token)
    {
        var document = GetIndexDocument<T>();
        var metadata = document.Fields.FirstOrDefault(x => x.Name == field);
        if (metadata == null) return [];
        var index = document.GetReverseIndex(metadata);
        var result = new List<ExplorerTerm>();
        if (index is IndexStorageReverseNumeric<T> numeric)
        {
            foreach (var node in numeric.Numeric.PostOrder.Where(x => x.Frequency > 0))
            {
                token.ThrowIfCancellationRequested();
                result.Add(new ExplorerTerm { Value = node.Value, Frequency = node.Frequency, DocumentIds = node.Posting.All.Distinct().ToArray() });
                if (result.Count >= ExplorerSession.ResultLimit) break;
            }
            return result;
        }
        if (index is not IndexStorageReverseTerm<T> reverse) return [];
        foreach (var (value, node) in reverse.Term.Terms)
        {
            token.ThrowIfCancellationRequested();
            result.Add(new ExplorerTerm { Value = value, Frequency = node.Frequency, DocumentIds = node.Posting.PreOrder.Select(x => x.DocumentID).Distinct().ToArray() });
            if (result.Count >= ExplorerSession.ResultLimit) break;
        }
        return result;
    }

    /// <summary>Creates a fresh identifier for document insertion.</summary>
    /// <returns>The editable JSON template.</returns>
    public string NewDocument()
    {
        var item = new T();
        typeof(T).GetProperty("Id")!.SetValue(item, Guid.NewGuid());
        return JsonSerializer.Serialize(item, _jsonOptions);
    }

    /// <summary>Validates identity and complete property coverage before a mutation.</summary>
    /// <param name="json">The editable JSON document.</param>
    /// <param name="existingId">The immutable update identifier.</param>
    public void Save(string json, Guid? existingId)
    {
        using var parsed = JsonDocument.Parse(json);
        ValidateObject(parsed.RootElement, typeof(T));
        var item = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? throw new InvalidDataException(AppText.ADocumentObjectIsRequired);
        if (item.Id == Guid.Empty) throw new InvalidDataException(AppText.IdMustNotBeEmpty);
        if (existingId.HasValue && item.Id != existingId.Value) throw new InvalidDataException(AppText.AnExistingDocumentSIdCannotBeChanged);
        var exists = All<T>().Any(x => x.Id == item.Id);
        if (existingId.HasValue && !exists) throw new InvalidDataException(AppText.TheDocumentNoLongerExistsRefreshTheIndex);
        if (!existingId.HasValue && exists) throw new InvalidDataException(AppText.ADocumentWithThisIdAlreadyExists);
        if (existingId.HasValue) Update(item); else Insert(item);
    }

    /// <summary>Rejects missing or duplicate JSON properties instead of silently overwriting values.</summary>
    /// <param name="json">The current object.</param>
    /// <param name="type">The expected runtime type.</param>
    private static void ValidateObject(JsonElement json, Type type)
    {
        if (json.ValueKind != JsonValueKind.Object) throw new InvalidDataException(AppText.AJSONObjectIsRequired);
        var properties = json.EnumerateObject().ToArray();
        if (properties.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
            throw new InvalidDataException(AppText.DuplicateJSONPropertiesAreNotAllowed);
        foreach (var property in type.GetProperties())
        {
            if (!json.TryGetProperty(property.Name, out var value)) throw new InvalidDataException(AppText.Format("RequiredProperty", property.Name));
            if (value.ValueKind != JsonValueKind.Null && property.PropertyType.IsClass && property.PropertyType != typeof(string))
                ValidateObject(value, property.PropertyType);
        }
    }

    /// <summary>Deletes through the engine so reverse postings stay synchronized.</summary>
    /// <param name="id">The selected identifier.</param>
    public void Delete(Guid id)
    {
        var item = All<T>().FirstOrDefault(x => x.Id == id) ?? throw new InvalidDataException(AppText.TheDocumentNoLongerExists);
        Delete(item);
    }
}
