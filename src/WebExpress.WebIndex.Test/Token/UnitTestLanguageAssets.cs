using System.Globalization;
using System.Text;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;

namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// Verifies that shipped language assets remain usable by the indexing pipeline.
    /// </summary>
    public class UnitTestLanguageAssets : IClassFixture<UnitTestIndexFixtureToken>
    {
        private readonly UnitTestIndexFixtureToken _fixture;

        /// <summary>
        /// Initializes isolated language resources to avoid changing other index fixtures.
        /// </summary>
        /// <param name="fixture">The fixture that extracts the embedded language assets.</param>
        public UnitTestLanguageAssets(UnitTestIndexFixtureToken fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Prevents silent overrides, malformed records, and unstable canonical synonym forms.
        /// </summary>
        /// <param name="asset">The resource family whose format is being checked.</param>
        /// <param name="language">The language selecting the resource and its sort order.</param>
        [Theory]
        [InlineData("stopwords", "de")]
        [InlineData("stopwords", "en")]
        [InlineData("synonyms", "de")]
        [InlineData("synonyms", "en")]
        [InlineData("misspelledwords", "de")]
        [InlineData("misspelledwords", "en")]
        public void EntriesHaveUniqueKeysAndStableTargets(string asset, string language)
        {
            // arrange
            var path = Path.Combine(_fixture.Context.IndexDirectory, $"{asset}.{language}");
            var mappings = new Dictionary<string, string>(StringComparer.Ordinal);
            var comparer = StringComparer.Create(CultureInfo.GetCultureInfo(language), false);

            // act
            var entries = File.ReadAllLines(path).Where(line => !line.StartsWith('#')).ToArray();

            // validation
            Assert.NotEmpty(entries);
            string previous = null;
            foreach (var entry in entries)
            {
                var parts = entry.Split('=');
                Assert.Equal(asset == "stopwords" ? 1 : 2, parts.Length);
                foreach (var part in parts)
                {
                    Assert.False(string.IsNullOrWhiteSpace(part));
                    Assert.Equal(part.Trim(), part);
                    Assert.Equal(part.ToLowerInvariant(), part);
                }
                var key = parts[0].Normalize(NormalizationForm.FormKD);
                var value = parts[^1].Normalize(NormalizationForm.FormKD);
                Assert.True(mappings.TryAdd(key, value), $"Duplicate key in {asset}.{language}: {key}");
                if (previous != null)
                {
                    Assert.True(comparer.Compare(previous, parts[0]) <= 0, $"Unsorted entry: {entry}");
                }
                previous = parts[0];
            }
            if (asset == "synonyms")
            {
                foreach (var mapping in mappings)
                {
                    Assert.False(mappings.ContainsKey(mapping.Value), $"Nonterminal synonym: {mapping.Key}={mapping.Value}");
                }
            }
            else if (asset == "misspelledwords")
            {
                foreach (var mapping in mappings)
                {
                    Assert.NotEqual(mapping.Key, mapping.Value);
                }
            }
        }

        /// <summary>
        /// Ensures new function words are filtered without removing meaningful neighboring tokens.
        /// </summary>
        /// <param name="word">The function word that should not be indexed.</param>
        /// <param name="cultureName">The language or regional culture selecting the stop list.</param>
        [Theory]
        [InlineData("bei", "de")]
        [InlineData("beziehungsweise", "de")]
        [InlineData("deswegen", "de")]
        [InlineData("einigermaßen", "de")]
        [InlineData("insofern", "de")]
        [InlineData("innerhalb", "de")]
        [InlineData("kein", "de")]
        [InlineData("keine", "de")]
        [InlineData("obwohl", "de")]
        [InlineData("sobald", "de")]
        [InlineData("solange", "de")]
        [InlineData("sowohl", "de")]
        [InlineData("unser", "de")]
        [InlineData("unsere", "de")]
        [InlineData("welche", "de")]
        [InlineData("welchem", "de")]
        [InlineData("wem", "de")]
        [InlineData("wen", "de")]
        [InlineData("wenn", "de-DE")]
        [InlineData("wessen", "de-AT")]
        [InlineData("weshalb", "de-CH")]
        [InlineData("wieso", "de")]
        [InlineData("under", "en")]
        [InlineData("whereafter", "en")]
        [InlineData("whereof", "en")]
        [InlineData("wherewith", "en")]
        [InlineData("whereupon", "en")]
        [InlineData("whenever", "en-US")]
        [InlineData("wherein", "en-GB")]
        [InlineData("wherefrom", "en")]
        [InlineData("couldn't", "en")]
        [InlineData("shouldn't", "en")]
        [InlineData("must've", "en")]
        [InlineData("she'll", "en")]
        [InlineData("it'll", "en")]
        public void StopWordsKeepContentTokens(string word, string cultureName)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var stage = new IndexPipeStageFilterStopWord(_fixture.Context);
            var tokens = new[]
            {
                new IndexTermToken { Value = word.Normalize(NormalizationForm.FormKD), Position = 3 },
                new IndexTermToken { Value = "galaxy", Position = 11 }
            };

            // act
            var result = stage.Process(tokens, culture).ToArray();

            // validation
            Assert.Same(tokens[1], Assert.Single(result));
        }

        /// <summary>
        /// Ensures synonym variants converge to the same form on repeated conversion.
        /// </summary>
        /// <param name="word">The synonym or spelling variant presented to the converter.</param>
        /// <param name="expected">The terminal form used to join equivalent indexed terms.</param>
        /// <param name="cultureName">The language or regional culture selecting the synonym list.</param>
        [Theory]
        [InlineData("telefonapparat", "telefon", "de")]
        [InlineData("fernsprecher", "telefon", "de")]
        [InlineData("notebook", "laptop", "de")]
        [InlineData("lichtbild", "foto", "de")]
        [InlineData("photographie", "fotografie", "de")]
        [InlineData("anschrift", "adresse", "de-DE")]
        [InlineData("bücherei", "bibliothek", "de")]
        [InlineData("portemonnaie", "geldbörse", "de")]
        [InlineData("streichholz", "zündholz", "de")]
        [InlineData("karfiol", "blumenkohl", "de-AT")]
        [InlineData("marille", "aprikose", "de-AT")]
        [InlineData("velo", "fahrrad", "de-CH")]
        [InlineData("spital", "krankenhaus", "de-CH")]
        [InlineData("colour", "color", "en")]
        [InlineData("favourite", "favorite", "en-GB")]
        [InlineData("centre", "center", "en")]
        [InlineData("behaviour", "behavior", "en")]
        [InlineData("aeroplane", "airplane", "en")]
        [InlineData("cellphone", "telephone", "en-US")]
        [InlineData("motorcar", "car", "en")]
        [InlineData("postcode", "zipcode", "en")]
        [InlineData("railway", "railroad", "en")]
        [InlineData("jewellery", "jewelry", "en")]
        [InlineData("encyclopaedia", "encyclopedia", "en")]
        [InlineData("buy", "purchase", "en")]
        [InlineData("purchase", "purchase", "en")]
        [InlineData("glad", "joyful", "en")]
        [InlineData("happy", "joyful", "en")]
        [InlineData("begin", "start", "en")]
        [InlineData("launch", "start", "en")]
        [InlineData("nearby", "near", "en")]
        [InlineData("cruel", "cruel", "en")]
        [InlineData("support", "support", "en")]
        [InlineData("type", "type", "en")]
        [InlineData("trust", "trust", "en")]
        [InlineData("bad", "bad", "en")]
        [InlineData("obey", "follow", "en")]
        [InlineData("victory", "win", "en")]
        public void SynonymsHaveStableCanonicalForms(string word, string expected, string cultureName)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var stage = new IndexPipeStageConverterSynonym(_fixture.Context);
            var tokens = new[] { new IndexTermToken { Value = word.Normalize(NormalizationForm.FormKD), Position = 7 } };

            // act
            var result = stage.Process(tokens, culture).ToArray();
            var repeated = stage.Process(result, culture).ToArray();

            // validation
            Assert.Equal(expected.Normalize(NormalizationForm.FormKD), Assert.Single(result).Value);
            Assert.Equal(result[0].Value, Assert.Single(repeated).Value);
            Assert.Equal(7u, result[0].Position);
        }

        /// <summary>
        /// Ensures spelling corrections preserve positions and leave valid variants unchanged.
        /// </summary>
        /// <param name="word">The misspelling or valid control word presented to the converter.</param>
        /// <param name="expected">The spelling that must reach subsequent pipeline stages.</param>
        /// <param name="cultureName">The language or regional culture selecting the correction list.</param>
        [Theory]
        [InlineData("authentifzierung", "authentifizierung", "de")]
        [InlineData("berechtiung", "berechtigung", "de")]
        [InlineData("benutztername", "benutzername", "de")]
        [InlineData("dateiendlung", "dateiendung", "de")]
        [InlineData("datenbak", "datenbank", "de")]
        [InlineData("datenbnak", "datenbank", "de")]
        [InlineData("dokumetation", "dokumentation", "de")]
        [InlineData("einstellugen", "einstellungen", "de")]
        [InlineData("konfiguartion", "konfiguration", "de-DE")]
        [InlineData("passwrot", "passwort", "de-AT")]
        [InlineData("verschlüselung", "verschlüsselung", "de")]
        [InlineData("verzeichniss", "verzeichnis", "de")]
        [InlineData("verfügabr", "verfügbar", "de")]
        [InlineData("zustimung", "zustimmung", "de")]
        [InlineData("authentciaton", "authentication", "en")]
        [InlineData("authentciation", "authentication", "en")]
        [InlineData("authorizaton", "authorization", "en")]
        [InlineData("configuartion", "configuration", "en")]
        [InlineData("concurency", "concurrency", "en")]
        [InlineData("databse", "database", "en")]
        [InlineData("dependecy", "dependency", "en")]
        [InlineData("encrpytion", "encryption", "en")]
        [InlineData("permisions", "permissions", "en")]
        [InlineData("repositroy", "repository", "en-US")]
        [InlineData("serializaton", "serialization", "en")]
        [InlineData("synchonization", "synchronization", "en")]
        [InlineData("vulnerabilty", "vulnerability", "en")]
        [InlineData("colour", "colour", "en-GB")]
        [InlineData("adaption", "adaption", "en")]
        [InlineData("gaol", "gaol", "en")]
        [InlineData("spagetti", "spaghetti", "de")]
        [InlineData("begrüssung", "begrüssung", "de-CH")]
        public void MisspellingsPreserveValidWords(string word, string expected, string cultureName)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var stage = new IndexPipeStageConverterMisspelled(_fixture.Context);
            var tokens = new[] { new IndexTermToken { Value = word, Position = 5 } };

            // act
            var result = stage.Process(tokens, culture).ToArray();

            // validation
            Assert.Equal(expected, Assert.Single(result).Value);
            Assert.Equal(5u, result[0].Position);
        }

        /// <summary>
        /// Verifies representative additions through tokenization and the complete default pipeline.
        /// </summary>
        /// <param name="input">The text combining a function word with a newly supported term.</param>
        /// <param name="expected">The single searchable term that should remain.</param>
        /// <param name="cultureName">The language selecting the complete pipeline resources.</param>
        [Theory]
        [InlineData("bei telefonapparat", "telefon", "de-DE")]
        [InlineData("wenn notebook", "laptop", "de-AT")]
        [InlineData("obwohl authentifzierung", "authentifizierung", "de")]
        [InlineData("under cellphone", "telephone", "en-US")]
        [InlineData("whereof colour", "color", "en-GB")]
        [InlineData("whereafter authentciaton", "authentication", "en")]
        [InlineData("didn’t cellphone", "telephone", "en-US")]
        [InlineData("also cellphone", "telephone", "en")]
        [InlineData("trotz notebook", "laptop", "de")]
        public void AnalyzerUsesLanguageAdditions(string input, string expected, string cultureName)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureName);

            // act
            var result = _fixture.TokenAnalyzer.Analyze(input, culture).ToArray();

            // validation
            Assert.Equal(expected, Assert.Single(result).Value);
        }
    }
}
