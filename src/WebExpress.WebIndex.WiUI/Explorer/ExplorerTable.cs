#nullable enable
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>Provides deterministic table projection and exports without depending on MAUI.</summary>
public static class ExplorerTable
{
    /// <summary>
    /// Resolves a dotted schema path without losing the original value type.
    /// </summary>
    /// <param name="item">The source document.</param>
    /// <param name="path">The schema property path.</param>
    /// <returns>The stored value, or null for an absent nested object.</returns>
    public static object? Value(object? item, string path)
    {
        foreach (var part in path.Split('.'))
        {
            item = item?.GetType().GetProperty(part)?.GetValue(item);
        }
        return item;
    }

    /// <summary>
    /// Formats cells consistently while preserving full values in the detail view.
    /// </summary>
    /// <param name="value">The cell value.</param>
    /// <returns>The culture independent display value.</returns>
    public static string Format(object? value) => value switch
    {
        null => "∅", DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        bool boolean => boolean ? "true" : "false",
        IFormattable formatted => formatted.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    /// <summary>
    /// Filters the loaded result and sorts typed values before pagination.
    /// </summary>
    /// <param name="items">The loaded result snapshot.</param>
    /// <param name="fields">The visible schema columns.</param>
    /// <param name="filter">The case insensitive cell filter.</param>
    /// <param name="sort">The optional column path.</param>
    /// <param name="descending">Whether the selected sort is descending.</param>
    /// <returns>The ordered, filtered result.</returns>
    public static IReadOnlyList<object> Select(IEnumerable<object> items, IEnumerable<string> fields, string filter, string? sort, bool descending)
    {
        if (string.IsNullOrWhiteSpace(filter) && sort == null && items is IReadOnlyList<object> snapshot) return snapshot;
        var columns = fields.ToArray();
        var result = items.Where(item => string.IsNullOrWhiteSpace(filter)
            || columns.Any(field => Format(Value(item, field)).Contains(filter, StringComparison.OrdinalIgnoreCase)));
        if (sort != null)
        {
            result = descending ? result.OrderByDescending(x => Value(x, sort), Comparer<object?>.Default)
                : result.OrderBy(x => Value(x, sort), Comparer<object?>.Default);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Exports the complete loaded result using stable column order and quoted CSV cells.
    /// </summary>
    /// <param name="items">The result to export.</param>
    /// <param name="fields">The exported column paths.</param>
    /// <returns>CSV text with spreadsheet formula protection.</returns>
    public static string Csv(IEnumerable<object> items, IEnumerable<string> fields)
    {
        var columns = fields.ToArray();
        var text = new StringBuilder();
        text.AppendLine(string.Join(",", columns.Select(EscapeCsv)));
        foreach (var item in items)
        {
            text.AppendLine(string.Join(",", columns.Select(field => EscapeCsv(Value(item, field) is { } value ? Format(value) : ""))));
        }
        return text.ToString();
    }

    /// <summary>
    /// Preserves original numeric and nested values when exporting JSON.
    /// </summary>
    /// <param name="items">The result snapshot.</param>
    /// <returns>Indented JSON.</returns>
    public static string Json(IEnumerable<object> items) => JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Quotes CSV delimiters and neutralizes cells interpreted as spreadsheet formulas.
    /// </summary>
    /// <param name="value">The unescaped cell value.</param>
    /// <returns>The quoted cell.</returns>
    private static string EscapeCsv(string value)
    {
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r'))
        {
            value = "'" + value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
