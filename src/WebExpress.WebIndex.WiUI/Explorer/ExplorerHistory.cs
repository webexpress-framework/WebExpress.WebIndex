#nullable enable
using System.Text.Json;

namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>Persists successful queries and favorites independently for each index.</summary>
public sealed class ExplorerHistory
{
    /// <summary>Gets or sets the bounded recent query list.</summary>
    public List<ExplorerQuery> Queries { get; set; } = [];

    /// <summary>
    /// Reads history without hiding a damaged settings file from the caller.
    /// </summary>
    /// <param name="path">The application settings path.</param>
    /// <returns>The existing history or an empty history.</returns>
    public static ExplorerHistory Load(string path) => File.Exists(path)
        ? JsonSerializer.Deserialize<ExplorerHistory>(File.ReadAllText(path)) ?? new() : new();

    /// <summary>
    /// Records only queries that the caller has successfully executed or validated.
    /// </summary>
    /// <param name="index">The schema path used as the query scope.</param>
    /// <param name="query">The WQL text.</param>
    /// <param name="favorite">Whether to retain this query as a favorite.</param>
    public void Remember(string index, string query, bool favorite = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        var previous = Queries.FirstOrDefault(x => x.Index.Equals(index, StringComparison.OrdinalIgnoreCase) && x.Query == query);
        if (previous != null) Queries.Remove(previous);
        Queries.Insert(0, new ExplorerQuery { Index = index, Query = query, Favorite = favorite || previous?.Favorite == true });
        var recent = Queries.Where(x => !x.Favorite).Skip(50).ToArray();
        foreach (var item in recent) Queries.Remove(item);
    }

    /// <summary>
    /// Replaces settings atomically so an interrupted write keeps the previous history readable.
    /// </summary>
    /// <param name="path">The application settings file.</param>
    public void Save(string path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

/// <summary>Retains the scope and text of a reusable WQL query.</summary>
public sealed class ExplorerQuery
{
    /// <summary>Gets or sets the schema path.</summary>
    public string Index { get; set; } = "";
    /// <summary>Gets or sets the WQL text.</summary>
    public string Query { get; set; } = "";
    /// <summary>Gets or sets whether automatic history trimming must retain this query.</summary>
    public bool Favorite { get; set; }
    /// <summary>Gets the label displayed in the query picker.</summary>
    public string Label => (Favorite ? "★  " : "↶  ") + Query;
}
