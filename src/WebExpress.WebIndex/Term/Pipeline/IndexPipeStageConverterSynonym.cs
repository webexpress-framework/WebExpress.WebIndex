using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WebExpress.WebIndex.Term.Pipeline
{
    /// <summary>
    /// Conversion of synonyms.
    /// </summary>
    public class IndexPipeStageConverterSynonym : IIndexPipeStage
    {
        /// <summary>
        /// Returns the name of the process state.
        /// </summary>
        public string Name => "Synonym";

        /// <summary>
        /// Returns a list of the synonyms that are used for filtering.
        /// </summary>
        internal Dictionary<CultureInfo, Dictionary<string, string>> SynonymDictionary { get; } = [];

        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        public IndexPipeStageConverterSynonym(IIndexContext context)
        {
            foreach (var file in Directory.GetFiles(context.IndexDirectory, "synonyms.*"))
            {
                var extension = Path.GetExtension(file).Trim('.');

                // a stray file - "synonyms.bak" - names no culture and must not stop the index
                try
                {
                    FillSynonymDictionary(file, CultureInfo.GetCultureInfo(extension));
                }
                catch (CultureNotFoundException)
                {
                }
            }
        }

        /// <summary>
        /// Converts specific elements on the term enumeration.
        /// </summary>
        /// <param name="input">The terms.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The terms at which the synonyms have been converted.</returns>
        public IEnumerable<IndexTermToken> Process(IEnumerable<IndexTermToken> input, CultureInfo culture)
        {
            var supportedCulture = GetSupportedCulture(culture);

            if (!SynonymDictionary.TryGetValue(supportedCulture, out var synonyms))
            {
                // without the end here the tokens were handed on, the input read a second time,
                // and the lookup below threw for the culture that has no synonyms
                foreach (var token in input)
                {
                    yield return token;
                }

                yield break;
            }

            foreach (var token in input)
            {
                if (token.Value is string)
                {
                    if (synonyms.TryGetValue(token.Value.ToString(), out string value))
                    {
                        yield return new IndexTermToken()
                        {
                            Value = value,
                            Position = token.Position
                        };
                    }
                    else
                    {
                        yield return token;
                    }
                }
                else
                {
                    yield return token;
                }
            }
        }

        /// <summary>
        /// Fills the directory with synonyms.
        /// </summary>
        /// <param name="file">The synonym file.</param>
        /// <param name="culture">The culture.</param>
        private void FillSynonymDictionary(string file, CultureInfo culture)
        {
            // the file found is read, not one rebuilt from the culture: a three-letter extension
            // named a culture whose two-letter file did not exist
            var fileContent = File.ReadAllLines(file);

            if (!SynonymDictionary.ContainsKey(culture))
            {
                SynonymDictionary.Add(culture, new Dictionary<string, string>());
            }

            var dict = SynonymDictionary[culture];

            foreach (var line in fileContent.Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith('#')))
            {
                var l = line.Normalize(System.Text.NormalizationForm.FormKD).ToLowerInvariant();
                var split = l.Split('=', 2, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

                // a line without a pair is skipped rather than failing the whole file
                if (split.Length < 2)
                {
                    continue;
                }

                var key = split[0];
                var value = split[1].Split('#')[0].TrimEnd();

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value) && !dict.ContainsKey(key))
                {
                    dict.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Transforms a given culture into a supported culture.
        /// </summary>
        /// <param name="culture">The culture to be used.</param>
        /// <returns>A supported culture, this may differ from the desired culture.</returns>
        private CultureInfo GetSupportedCulture(CultureInfo culture)
        {
            if (SynonymDictionary.ContainsKey(culture))
            {
                return culture;
            }

            var generalCulture = CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName);

            if (SynonymDictionary.ContainsKey(generalCulture))
            {
                return generalCulture;
            }

            return CultureInfo.GetCultureInfo("en");
        }
    }
}
