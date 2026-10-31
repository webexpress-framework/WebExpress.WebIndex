using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using WebExpress.WebIndex.WiUI.Localization;
using WebExpress.WebIndex.Wql;
using Xunit;

namespace WebExpress.WebIndex.WiUI.Test;

/// <summary>Protects language fallback, resource completeness and culture-independent query behavior.</summary>
public sealed class UnitTestLocalization
{
    /// <summary>Verifies display culture fallback without changing index language settings.</summary>
    /// <param name="culture">The requested display culture.</param>
    /// <param name="expected">The expected cancel caption.</param>
    [Theory]
    [InlineData("de-DE", "Abbrechen")]
    [InlineData("de-AT", "Abbrechen")]
    [InlineData("en-US", "Cancel")]
    [InlineData("fr-FR", "Cancel")]
    public void DisplayCulture_ResolvesGermanAndEnglishFallback(string culture, string expected)
    {
        // arrange
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            // act
            var caption = AppText.Cancel;
            // validation
            Assert.Equal(expected, caption);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    /// <summary>Ensures every neutral resource has a German value with the same placeholder contract.</summary>
    [Fact]
    public void Resources_HaveMatchingKeysAndPlaceholders()
    {
        // arrange
        var resources = new ResourceManager("WebExpress.WebIndex.WiUI.Localization.Strings", typeof(AppText).Assembly);
        var english = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var german = resources.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false)!;
        // act
        var englishKeys = english.Cast<DictionaryEntry>().Select(x => (string)x.Key).Order().ToArray();
        var germanKeys = german.Cast<DictionaryEntry>().Select(x => (string)x.Key).Order().ToArray();
        // validation
        Assert.Equal(englishKeys, germanKeys);
        Assert.Equal(englishKeys.Length, englishKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var property in typeof(AppText).GetProperties()) Assert.IsType<string>(property.GetValue(null));
        foreach (var key in englishKeys)
        {
            var original = english.GetString(key)!;
            var translation = german.GetString(key)!;
            Assert.False(string.IsNullOrWhiteSpace(translation), key);
            Assert.Equal(Regex.Matches(original, @"\{\d+(?::[^}]+)?\}").Select(x => x.Value).Order(),
                Regex.Matches(translation, @"\{\d+(?::[^}]+)?\}").Select(x => x.Value).Order());
            Assert.DoesNotContain('\uFFFD', translation);
        }
    }

    /// <summary>Checks singular forms and localized number formatting in complete messages.</summary>
    [Fact]
    public void Counts_UseGermanPluralAndNumberFormatting()
    {
        // arrange
        var display = CultureInfo.CurrentUICulture;
        var formatting = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            // act
            var singular = AppText.Count("Documents", 1);
            var plural = AppText.Count("Documents", 1234);
            var result = AppText.Format("QueryResult", plural, 12);
            // validation
            Assert.Equal("1 Dokument", singular);
            Assert.Equal("1.234 Dokumente", plural);
            Assert.Equal("1.234 Dokumente in 12 ms.", result);
            Assert.Equal("0 Begriffe", AppText.Count("Terms", 0));
            Assert.Equal("1 Feld", AppText.Count("Schema", 1));
        }
        finally { CultureInfo.CurrentUICulture = display; CultureInfo.CurrentCulture = formatting; }
    }

    /// <summary>Localizes parser diagnostics while leaving WQL syntax and model paths unchanged.</summary>
    [Fact]
    public void Wql_LocalizesDiagnosticsWithoutTranslatingSyntax()
    {
        // arrange
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            var parser = new WqlParser<ExplorerTestItem>();
            // act
            var invalid = parser.Parse("UnknownField = 3");
            var valid = parser.Parse("Price > 2.25 order by Name desc take 5");
            var help = AppText.Format("WqlHelpBody", "Name");
            // validation
            Assert.Equal("Unbekanntes Feld.", AppText.Diagnostic(invalid.Error.Message));
            Assert.False(valid.HasErrors);
            Assert.Contains("order by Name asc", help);
            Assert.Contains("Name contains 'server'", help);
            Assert.Equal("external detail", AppText.Diagnostic("external detail"));
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
}
