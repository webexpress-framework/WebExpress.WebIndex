#nullable enable
namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>Retains one projection of an immutable result snapshot across page changes.</summary>
public sealed class ExplorerTableProjection
{
    private IReadOnlyList<object>? _source;
    private IReadOnlyList<object> _rows = [];
    private string[] _fields = [];
    private string? _filter, _sort;
    private bool _descending;

    /// <summary>Recomputes filtering and sorting only when their inputs change.</summary>
    /// <param name="source">The immutable loaded result snapshot.</param>
    /// <param name="fields">The visible column paths.</param>
    /// <param name="filter">The current loaded row filter.</param>
    /// <param name="sort">The optional sort column.</param>
    /// <param name="descending">Whether the sort direction is descending.</param>
    /// <returns>The complete projection to paginate or export.</returns>
    public IReadOnlyList<object> Select(IReadOnlyList<object> source, string[] fields, string filter, string? sort, bool descending)
    {
        if (ReferenceEquals(source, _source) && _fields.SequenceEqual(fields) && _filter == filter
            && _sort == sort && _descending == descending) return _rows;
        var rows = ExplorerTable.Select(source, fields, filter, sort, descending);
        _source = source;
        _fields = [.. fields];
        _filter = filter;
        _sort = sort;
        _descending = descending;
        _rows = rows;
        return rows;
    }
}
