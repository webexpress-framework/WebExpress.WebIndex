using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WebExpress.WebIndex.Term.Pipeline
{
    /// <summary>
    /// Converts misspelled words to their normalized form using culture-specific dictionaries.
    /// </summary>
    public class IndexPipeStageConverterMisspelled : IIndexPipeStage
    {
        /// <summary>
        /// Returns the name of the process stage.
        /// </summary>
        public string Name => "Misspelled";

        /// <summary>
        /// Holds misspelling dictionaries per culture key (e.g., "en", "en-US").
        /// </summary>
        internal Dictionary<string, Dictionary<string, string>> MisspelledWordDictionary { get; } =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the class by loading all available misspelling dictionaries.
        /// </summary>
        /// <param name="context">The reference to the indexing context.</param>
        public IndexPipeStageConverterMisspelled(IIndexContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.IndexDirectory))
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!Directory.Exists(context.IndexDirectory))
            {
                // nothing to load if directory is absent
                return;
            }

            foreach (var file in Directory.GetFiles(context.IndexDirectory, "misspelledwords.*"))
            {
                var cultureKey = ExtractCultureKeyFromFileName(file);
                if (string.IsNullOrWhiteSpace(cultureKey))
                {
                    continue;
                }

                try
                {
                    // validate culture; normalize key to CultureInfo.Name (e.g., "en" or "en-US")
                    var culture = CultureInfo.GetCultureInfo(cultureKey);
                    var normalizedKey = culture.Name;

                    FillMisspelledWordDictionary(file, normalizedKey);
                }
                catch (CultureNotFoundException)
                {
                    // ignore files with invalid culture suffix
                }
            }
        }

        /// <summary>
        /// Converts terms by replacing misspelled tokens using the culture-specific dictionary.
        /// </summary>
        /// <param name="input">The input token sequence.</param>
        /// <param name="culture">The culture to use for the conversion.</param>
        /// <returns>Transformed token sequence with misspellings corrected where applicable.</returns>
        public IEnumerable<IndexTermToken> Process(IEnumerable<IndexTermToken> input, CultureInfo culture)
        {
            if (input == null)
            {
                yield break;
            }

            var cultureKey = GetSupportedCultureKey(culture);

            if (!MisspelledWordDictionary.TryGetValue(cultureKey, out Dictionary<string, string> dict))
            {
                foreach (var token in input)
                {
                    yield return token;
                }

                yield break;
            }

            foreach (var token in input)
            {
                if (token?.Value is string s)
                {
                    var normalized = NormalizeToken(s);

                    if (!string.IsNullOrEmpty(normalized) && dict.TryGetValue(normalized, out var replacement))
                    {
                        yield return new IndexTermToken
                        {
                            Value = replacement,
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
        /// Loads a misspelled words file into the dictionary for the given culture key.
        /// </summary>
        /// <param name="filePath">The full path to the misspelling file.</param>
        /// <param name="cultureKey">The normalized culture key (e.g., "en", "en-US").</param>
        private void FillMisspelledWordDictionary(string filePath, string cultureKey)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                return;
            }

            if (!MisspelledWordDictionary.TryGetValue(cultureKey, out Dictionary<string, string> dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                MisspelledWordDictionary[cultureKey] = dict;
            }

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine?.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith('#'))
                {
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex <= 0 || eqIndex == line.Length - 1)
                {
                    // skip lines without key=value
                    continue;
                }

                var left = line[..eqIndex];
                var right = line[(eqIndex + 1)..];

                // strip trailing inline comments in value
                var hashIdx = right.IndexOf('#');
                if (hashIdx >= 0)
                {
                    right = right[..hashIdx];
                }

                var key = NormalizeToken(left);
                var value = NormalizeToken(right);

                // skip if key/value invalid or identical
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || key == value)
                {
                    continue;
                }

                // last definition wins to allow overrides
                dict[key] = value;
            }
        }

        /// <summary>
        /// Derives a supported culture key from a requested culture. Falls back to neutral ("en") or first available.
        /// </summary>
        /// <param name="culture">The requested culture.</param>
        /// <returns>A culture key present in the dictionary, if any; otherwise a fallback.</returns>
        private string GetSupportedCultureKey(CultureInfo culture)
        {
            var requested = culture?.Name ?? "en";
            if (MisspelledWordDictionary.ContainsKey(requested))
            {
                return requested;
            }

            var neutral = culture?.TwoLetterISOLanguageName ?? "en";
            if (MisspelledWordDictionary.ContainsKey(neutral))
            {
                return neutral;
            }

            if (MisspelledWordDictionary.ContainsKey("en"))
            {
                return "en";
            }

            // fallback to any available culture to avoid empty results if dictionaries exist
            if (MisspelledWordDictionary.Count > 0)
            {
                return MisspelledWordDictionary.Keys.First();
            }

            return "en";
        }

        /// <summary>
        /// Extracts the culture part from a file name misspelledwords.{culture}[.anything].
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// <returns>The culture key or null if the pattern does not match.</returns>
        private static string ExtractCultureKeyFromFileName(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            const string prefix = "misspelledwords.";
            if (fileName == null || !fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var remainder = fileName[prefix.Length..];

            var dotIndex = remainder.IndexOf('.');
            if (dotIndex >= 0)
            {
                remainder = remainder[..dotIndex];
            }

            // normalize en_US -> en-US to satisfy CultureInfo
            remainder = remainder.Replace('_', '-').Trim();

            return remainder;
        }

        /// <summary>
        /// Normalizes tokens for dictionary lookup (compatibility decomposition, lower-case, trim).
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>Normalized token or empty string.</returns>
        private static string NormalizeToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // small inline normalization for consistent key matching
            return text
                .Normalize(NormalizationForm.FormKD)
                .ToLowerInvariant()
                .Trim();
        }
    }
}