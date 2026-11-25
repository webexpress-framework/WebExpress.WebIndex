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

                // a stray file - "stopwords.bak" - names no culture and must not stop the index
                try
                {
                    FillStopWordDictionary(file, CultureInfo.GetCultureInfo(extension));
                }
                catch (CultureNotFoundException)
                {
                }
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
                    // typed as object, the value bound to the linear Enumerable.Contains instead of
                    // the set's lookup and scanned the whole list for every token
                    if (token.Value is string text)
                    {
                        if (!value.Contains(text))
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
        /// <param name="file">The stop word file.</param>
        /// <param name="culture">The culture.</param>
        private void FillStopWordDictionary(string file, CultureInfo culture)
        {
            // the file found is read, not one rebuilt from the culture's two-letter name
            var fileContent = File.ReadAllLines(file);

            if (!StopWordDictionary.ContainsKey(culture))
            {
                StopWordDictionary.Add(culture, new HashSet<string>());
            }

            foreach (var word in fileContent.Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith('#')))
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
