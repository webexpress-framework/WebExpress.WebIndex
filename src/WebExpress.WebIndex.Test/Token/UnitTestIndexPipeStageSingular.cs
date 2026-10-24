using System.Globalization;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Term.Pipeline;
using WebExpress.WebIndex.Test.Fixture;
using Xunit;
namespace WebExpress.WebIndex.Test.Token
{
    /// <summary>
    /// Verifies language exceptions before suffix rules can change indexed terms.
    /// </summary>
    public class UnitTestIndexPipeStageSingular : IClassFixture<UnitTestIndexFixtureToken>
    {
        /// <summary>
        /// Returns the log.
        /// </summary>
        public ITestOutputHelper Output { get; private set; }

        /// <summary>
        /// Returns the test context.
        /// </summary>
        protected UnitTestIndexFixtureToken Fixture { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="fixture">The test context.</param>
        /// <param name="output">The log.</param>
        public UnitTestIndexPipeStageSingular(UnitTestIndexFixtureToken fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        /// <summary>
        /// Tests the singular method. This function is part of the lemmatization process and the transformation of the token into the singular.
        /// </summary>
        /// <param name="pluralWord">The source form used to verify language-specific exceptions.</param>
        /// <param name="singularWord">The canonical form expected by the index.</param>
        /// <param name="cultureString">The language or regional culture selecting the conversion rules.</param>
        [Theory]
        // regular nouns (ies, ses  xes, s)
        [InlineData("babies", "baby", "en")]
        [InlineData("countries", "country", "en")]
        [InlineData("families", "family", "en")]
        [InlineData("parties", "party", "en")]
        [InlineData("pennies", "penny", "en")]
        [InlineData("studies", "study", "en")]
        [InlineData("stories", "story", "en")]
        [InlineData("autos", "auto", "de")]
        [InlineData("frauen", "frau", "de")]
        [InlineData("kinder", "kind", "de")]
        [InlineData("tische", "tisch", "de")]
        // irregular nouns
        [InlineData("axes", "axis", "en")]
        [InlineData("indices", "index", "en")]
        [InlineData("selves", "self", "en")]
        [InlineData("vortexes", "vortex", "en")]
        [InlineData("atlanten", "atlas", "de")]
        [InlineData("bücher", "buch", "de")]
        [InlineData("männer", "mann", "de")]
        [InlineData("stühle", "stuhl", "de")]
        // dictionary coverage and regional culture fallback
        [InlineData("bäche", "bach", "de")]
        [InlineData("bäder", "bad", "de")]
        [InlineData("beschlüsse", "beschluss", "de")]
        [InlineData("bräuche", "brauch", "de")]
        [InlineData("brüste", "brust", "de")]
        [InlineData("drähte", "draht", "de")]
        [InlineData("einkünfte", "einkunft", "de")]
        [InlineData("flöhe", "floh", "de")]
        [InlineData("kenntnisse", "kenntnis", "de")]
        [InlineData("kräfte", "kraft", "de")]
        [InlineData("schränke", "schrank", "de")]
        [InlineData("schlösser", "schloss", "de")]
        [InlineData("sprüche", "spruch", "de")]
        [InlineData("vorräte", "vorrat", "de")]
        [InlineData("abflüsse", "abfluss", "de-DE")]
        [InlineData("anhänge", "anhang", "de-AT")]
        [InlineData("ausschüsse", "ausschuss", "de-CH")]
        [InlineData("algorithmen", "algorithmus", "de")]
        [InlineData("aquarien", "aquarium", "de")]
        [InlineData("indizes", "index", "de")]
        [InlineData("matrizen", "matrix", "de")]
        [InlineData("maxima", "maximum", "de")]
        [InlineData("schemata", "schema", "de")]
        [InlineData("spektren", "spektrum", "de")]
        [InlineData("tempi", "tempo", "de")]
        [InlineData("herzen", "herz", "de")]
        [InlineData("muskeln", "muskel", "de")]
        [InlineData("stacheln", "stachel", "de")]
        [InlineData("segel", "segel", "de")]
        [InlineData("computer", "computer", "de")]
        [InlineData("gebäude", "gebäude", "de")]
        [InlineData("kader", "kader", "de")]
        [InlineData("abaci", "abacus", "en")]
        [InlineData("bison", "bison", "en")]
        [InlineData("businessmen", "businessman", "en")]
        [InlineData("crayfish", "crayfish", "en")]
        [InlineData("deer", "deer", "en")]
        [InlineData("ephemerides", "ephemeris", "en")]
        [InlineData("fish", "fish", "en")]
        [InlineData("ganglia", "ganglion", "en")]
        [InlineData("inuit", "inuk", "en")]
        [InlineData("menservants", "manservant", "en")]
        [InlineData("metamorphoses", "metamorphosis", "en")]
        [InlineData("prognoses", "prognosis", "en")]
        [InlineData("testes", "testis", "en")]
        [InlineData("trapezia", "trapezium", "en")]
        [InlineData("womenservants", "womanservant", "en")]
        [InlineData("buses", "bus", "en")]
        [InlineData("boxes", "box", "en")]
        [InlineData("churches", "church", "en")]
        [InlineData("foxes", "fox", "en")]
        [InlineData("houses", "house", "en")]
        [InlineData("movies", "movie", "en")]
        [InlineData("statuses", "status", "en")]
        [InlineData("zombies", "zombie", "en")]
        [InlineData("crossroads", "crossroads", "en")]
        [InlineData("headquarters", "headquarters", "en")]
        [InlineData("swine", "swine", "en")]
        [InlineData("businesswomen", "businesswoman", "en-US")]
        [InlineData("salesmen", "salesman", "en-GB")]
        [InlineData("sheep", "sheep", "en-AU")]
        public void PluralToSingular(string pluralWord, string singularWord, string cultureString)
        {
            // arrange
            var culture = CultureInfo.GetCultureInfo(cultureString);
            var pipeStage = new IndexPipeStageConverterSingular(Fixture.Context);

            // act
            var res = pipeStage.Process(IndexTermTokenizer.Tokenize(pluralWord, culture), culture)
                .FirstOrDefault();

            // validation
            Assert.Equal(singularWord, res?.Value);
        }

        /// <summary>
        /// Prevents malformed or duplicate entries from being ignored by the dictionary loader.
        /// </summary>
        /// <param name="language">The embedded language dictionary to validate.</param>
        [Theory]
        [InlineData("de")]
        [InlineData("en")]
        public void IrregularWordsHaveUniqueWellFormedEntries(string language)
        {
            // arrange
            var path = Path.Combine(Fixture.Context.IndexDirectory, $"irregularwords.{language}");
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // act
            var entries = File.ReadAllLines(path).Where(line => !line.StartsWith('#')).ToArray();

            // validation
            Assert.NotEmpty(entries);
            foreach (var entry in entries)
            {
                var parts = entry.Split('=');
                Assert.Equal(2, parts.Length);
                foreach (var part in parts)
                {
                    Assert.False(string.IsNullOrWhiteSpace(part));
                    Assert.Equal(part.Trim(), part);
                    Assert.Equal(part.ToLowerInvariant(), part);
                }
                Assert.True(keys.Add(parts[0]), $"Duplicate plural in {language}: {parts[0]}");
            }
        }
    }
}
