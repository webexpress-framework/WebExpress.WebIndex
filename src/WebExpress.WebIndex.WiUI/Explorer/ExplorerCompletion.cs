#nullable enable
namespace WebExpress.WebIndex.WiUI.Explorer;

/// <summary>Preserves caret ranges and quoted values while inserting WQL suggestions.</summary>
public static class ExplorerCompletion
{
    /// <summary>Locates the entire identifier around the caret without completing inside literals.</summary>
    /// <param name="text">The current editor text.</param>
    /// <param name="caret">The UTF-16 caret offset.</param>
    /// <param name="selection">The selected text length.</param>
    /// <returns>The replacement range, or null inside a quoted literal.</returns>
    public static (int Start, int End)? Range(string text, int caret, int selection)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        char? quote = null;
        for (var i = 0; i < caret; i++)
        {
            if (quote != null && text[i] == quote)
            {
                if (i + 1 < caret && text[i + 1] == quote) i++;
                else quote = null;
            }
            else if (quote == null && text[i] is '\'' or '"') quote = text[i];
        }
        if (quote != null) return null;
        if (selection > 0) return (caret, Math.Min(text.Length, caret + selection));
        var start = caret;
        var end = caret;
        while (start > 0 && IsIdentifier(text[start - 1])) start--;
        while (end < text.Length && IsIdentifier(text[end])) end++;
        return (start, end);
    }

    /// <summary>Inserts a completion while preserving unrelated text and punctuation spacing.</summary>
    /// <param name="text">The editor snapshot.</param>
    /// <param name="start">The replacement start.</param>
    /// <param name="end">The exclusive replacement end.</param>
    /// <param name="value">The suggestion to insert.</param>
    /// <returns>The updated text and caret offset.</returns>
    public static (string Text, int Caret) Insert(string text, int start, int end, string value)
    {
        var before = text[..start];
        var after = text[end..];
        if (value is "," or ")") before = before.TrimEnd();
        else if (before.Length > 0 && !char.IsWhiteSpace(before[^1]) && before[^1] != '(') before += " ";
        var space = value.EndsWith('(') || (after.Length > 0 && (char.IsWhiteSpace(after[0]) || after[0] is ')' or ',')) ? "" : " ";
        return (before + value + space + after, before.Length + value.Length + space.Length);
    }

    /// <summary>Recognizes dotted WQL property names.</summary>
    /// <param name="value">The candidate character.</param>
    /// <returns>True for identifier characters.</returns>
    private static bool IsIdentifier(char value) => char.IsLetterOrDigit(value) || value is '_' or '.';
}
