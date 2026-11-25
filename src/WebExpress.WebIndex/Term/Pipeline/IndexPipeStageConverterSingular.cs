using System.Collections.Generic;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebExpress.WebIndex.Term.Pipeline
{
    /// <summary>
    /// Conversion to singular.
    /// </summary>
    public class IndexPipeStageConverterSingular : IIndexPipeStage
    {
        /// <summary>
        /// Returns the name of the process state.
        /// </summary>
        public string Name => "Singular";

        /// <summary>
        /// Returns a list of the irregular words.
        /// </summary>
        private Dictionary<CultureInfo, Dictionary<string, string>> IrregularWordDictionary { get; } = [];

        /// <summary>
        /// Returns a list of the regular words.
        /// </summary>
        private Dictionary<CultureInfo, Dictionary<string, string>> RegularWordDictionary { get; } = [];

        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        public IndexPipeStageConverterSingular(IIndexContext context)
        {
            foreach (var file in Directory.GetFiles(context.IndexDirectory, "irregularwords.*"))
            {
                var extension = Path.GetExtension(file).Trim('.');

                FillIrregularWordDictionary(context, CultureInfo.GetCultureInfo(extension));
            }

            foreach (var file in Directory.GetFiles(context.IndexDirectory, "regularwords.*"))
            {
                var extension = Path.GetExtension(file).Trim('.');

                FillRegularWordDictionary(context, CultureInfo.GetCultureInfo(extension));
            }
        }

        /// <summary>
        /// Converts specific elements on the term enumeration.
        /// </summary>
        /// <param name="input">The terms.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The terms where the words have been converted to singular.</returns>
        public IEnumerable<IndexTermToken> Process(IEnumerable<IndexTermToken> input, CultureInfo culture)
        {
            var supportedCulture = GetSupportedCulture(culture);

            if (IrregularWordDictionary.TryGetValue(supportedCulture, out Dictionary<string, string> dict))
            {
                foreach (var token in input)
                {
                    if (token.Value is string)
                    {
                        if (dict.TryGetValue(token.Value.ToString(), out string value))
                        {
                            yield return new IndexTermToken()
                            {
                                Value = value,
                                Position = token.Position
                            };
                        }
                        else
                        {
                            var treated = false;

                            foreach (var keyValue in RegularWordDictionary[supportedCulture])
                            {
                                if (TryReplace(token.Value.ToString(), keyValue.Key, keyValue.Value, out var singular))
                                {
                                    treated = true;

                                    yield return new IndexTermToken()
                                    {
                                        Value = singular,
                                        Position = token.Position
                                    };

                                    break;
                                }
                            }

                            if (!treated)
                            {
                                yield return token;
                            }
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
        /// Fills the directory with irregular words.
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        /// <param name="culture">The culture.</param>
        private void FillIrregularWordDictionary(IIndexContext context, CultureInfo culture)
        {
            var fileContent = File.ReadAllLines
            (
                Path.Combine(context.IndexDirectory,
                $"irregularwords.{culture.TwoLetterISOLanguageName}")
            );

            if (!IrregularWordDictionary.ContainsKey(culture))
            {
                IrregularWordDictionary.Add(culture, new Dictionary<string, string>());
            }

            var dict = IrregularWordDictionary[culture];

            foreach (var line in fileContent.Select(x => x.Trim()).Where(x => !x.StartsWith('#')))
            {
                var split = line.Split('=', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                var key = split[0]?.ToLowerInvariant();
                var value = split[1]?.ToLowerInvariant()?.Split('#')[0]?.TrimEnd();

                if (!string.IsNullOrWhiteSpace(key) && !dict.ContainsKey(key))
                {
                    dict.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Fills the directory with irregular words.
        /// </summary>
        /// <param name="context">The reference to the context.</param>
        /// <param name="culture">The culture.</param>
        private void FillRegularWordDictionary(IIndexContext context, CultureInfo culture)
        {
            var fileContent = File.ReadAllLines
            (
                Path.Combine(context.IndexDirectory,
                $"regularwords.{culture.TwoLetterISOLanguageName}")
            );

            if (!RegularWordDictionary.ContainsKey(culture))
            {
                RegularWordDictionary.Add(culture, new Dictionary<string, string>());
            }

            var dict = RegularWordDictionary[culture];

            foreach (var line in fileContent.Select(x => x.Trim()).Where(x => !x.StartsWith('#')))
            {
                var split = line.Split('=', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                var key = split[0]?.ToLowerInvariant();
                var value = split[1]?.ToLowerInvariant()?.Split('#')[0]?.TrimEnd();

                if (!string.IsNullOrWhiteSpace(key) && !dict.ContainsKey(key))
                {
                    dict.Add(key, value);
                }
            }
        }

        /// <summary>
        /// The compiled rules, shared by every stage; the assets hold a fixed set of them.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Regex> _rules = new();

        /// <summary>
        /// Applies a singular rule of the language assets to a word.
        /// </summary>
        /// <remarks>
        /// The rule is the asset's, the word the indexed text's: a rule that backtracks badly on
        /// some word must not hold indexing or a query for as long as it likes, so a match that
        /// runs out of time counts as no match.
        /// </remarks>
        /// <param name="word">The word.</param>
        /// <param name="rule">The pattern of the rule.</param>
        /// <param name="replacement">The replacement of the rule.</param>
        /// <param name="singular">Receives the singular form.</param>
        /// <returns>True when the rule applies.</returns>
        private static bool TryReplace(string word, string rule, string replacement, out string singular)
        {
            singular = null;

            try
            {
                var regex = _rules.GetOrAdd(rule, x => new Regex(x, RegexOptions.None, TimeSpan.FromMilliseconds(100)));

                if (!regex.IsMatch(word))
                {
                    return false;
                }

                singular = regex.Replace(word, replacement);

                return true;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Transforms a given culture into a supported culture.
        /// </summary>
        /// <param name="culture">The culture to be used.</param>
        /// <returns>A supported culture, this may differ from the desired culture.</returns>
        private CultureInfo GetSupportedCulture(CultureInfo culture)
        {
            if (RegularWordDictionary.ContainsKey(culture))
            {
                return culture;
            }

            var generalCulture = CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName);

            if (RegularWordDictionary.ContainsKey(generalCulture))
            {
                return generalCulture;
            }

            return CultureInfo.GetCultureInfo("en");
        }
    }
}
