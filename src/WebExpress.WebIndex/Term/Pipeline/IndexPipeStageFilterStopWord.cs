using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WebExpress.WebIndex.Term.Pipeline
{
    /// <summary>
    /// Removes stop words
    /// </summary>
    public class IndexPipeStageFilterStopWord : IIndexPipeStage
    {
        /// <summary>
        /// Returns the name of the process state.
        /// </summary>
        public string Name => "StopWord";

        /// <summary>
        /// Returns a list of the stop words that are used for filtering.
        /// </summary>
        private Dictionary<CultureInfo, HashSet<string>> StopWordDictionary { get; } = [];

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        public IndexPipeStageFilterStopWord(IIndexContext context)
        {
            foreach (var file in Directory.GetFiles(context.IndexDirectory, "stopwords.*"))
            {
                var extension = Path.GetExtension(file).Trim('.');

                FillStopWordDictionary(context, CultureInfo.GetCultureInfo(extension));
            }
        }

        /// <summary>
        /// Filters specific elements on the term enumeration.
        /// </summary>
        /// <param name="input">The terms.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The filtered term enumeration.</returns>
        public IEnumerable<IndexTermToken> Process(IEnumerable<IndexTermToken> input, CultureInfo culture)
        {
            var supportedCulture = GetSupportedCulture(culture);

            if (StopWordDictionary.TryGetValue(supportedCulture, out HashSet<string> value))
            {
                foreach (var token in input)
                {
                    if (token.Value is string)
                    {
                        if (!value.Contains(token.Value))
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
            else
            {
                foreach (var token in input)
                {
                    yield return token;
                }
            }
        }

        /// <summary>
        /// Fills the directory with stop words.
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        /// <param name="culture">The culture.</param>
        private void FillStopWordDictionary(IIndexContext context, CultureInfo culture)
        {
            var fileContent = File.ReadAllLines
            (
                Path.Combine(context.IndexDirectory,
                $"stopwords.{culture.TwoLetterISOLanguageName}")
            );

            if (!StopWordDictionary.ContainsKey(culture))
            {
                StopWordDictionary.Add(culture, new HashSet<string>());
            }

            foreach (var word in fileContent.Select(x => x.Trim()).Where(x => !x.StartsWith('#')))
            {
                var w = word.Normalize(System.Text.NormalizationForm.FormKD).ToLowerInvariant();
                if (!StopWordDictionary[culture].Contains(w))
                {
                    StopWordDictionary[culture].Add(w);
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
            if (StopWordDictionary.ContainsKey(culture))
            {
                return culture;
            }

            var generalCulture = CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName);

            if (StopWordDictionary.ContainsKey(generalCulture))
            {
                return generalCulture;
            }

            return CultureInfo.GetCultureInfo("en");
        }
    }
}
