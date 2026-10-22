using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WebExpress.WebIndex.Term;
using WebExpress.WebIndex.Utility;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Evaluates the documented WQL text contract against a text held in memory, so that a
    /// query built from a statement finds the same items as the reverse index: a phrase for
    /// <c>=</c>, words for <c>~</c>, a distance as a window around the previous word as in
    /// the <see cref="IndexTermMatcher{TPosting}"/>, wildcards and a fuzzy similarity as in
    /// <see cref="IndexFuzzy"/>. The text is split by the <see cref="IndexTermTokenizer"/> and
    /// folded to lower case without diacritics. Stemming, stop words, synonyms and the
    /// correction of misspellings belong to the analyzer of an index and are not reproduced,
    /// so a query that depends on them may differ between both paths.
    /// </summary>
    public sealed class WqlTextMatcher
    {
        private static readonly MethodInfo StringToLowerMethod = typeof(string)
            .GetMethod(nameof(string.ToLower), Type.EmptyTypes);

        private static readonly MethodInfo StringContainsMethod = typeof(string)
            .GetMethod(nameof(string.Contains), [typeof(string)]);

        private readonly IReadOnlyList<(string Term, uint Position)> _query;
        private readonly Regex[] _patterns;

        /// <summary>
        /// Returns the words searched for, folded to lower case without diacritics the way the
        /// index folds them and still holding their wildcards. A query provider that cannot
        /// run the matcher, such as a database, can build its own condition from them.
        /// </summary>
        public IReadOnlyList<string> Terms { get; }

        /// <summary>
        /// Returns the match mode.
        /// </summary>
        public WqlTextMatchMode Mode { get; }

        /// <summary>
        /// Returns the distance by which a word may stand further from the previous one.
        /// </summary>
        public uint Distance { get; }

        /// <summary>
        /// Returns the minimum similarity in percent; 0 and 100 match exactly.
        /// </summary>
        public uint Similarity { get; }

        /// <summary>
        /// Returns the culture used to split numbers within the text.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Initializes a new instance of the class. The query is analyzed once, so that a
        /// matcher built for a literal can be applied to many texts.
        /// </summary>
        /// <param name="query">The words searched for.</param>
        /// <param name="mode">The match mode.</param>
        /// <param name="distance">The distance by which a word may stand further from the previous one.</param>
        /// <param name="similarity">The minimum similarity in percent.</param>
        /// <param name="culture">The culture used to split numbers within the text.</param>
        public WqlTextMatcher(string query, WqlTextMatchMode mode, uint distance, uint similarity, CultureInfo culture)
        {
            Mode = mode;
            Distance = distance;
            Similarity = similarity;
            Culture = culture ?? CultureInfo.InvariantCulture;

            _query = Tokenize(query, Culture, true);
            _patterns = [.. _query.Select(x => IsFuzzy ? null : CreatePattern(x.Term))];
            Terms = [.. _query.Select(x => x.Term)];
        }

        /// <summary>
        /// Returns whether the similarity asks for a fuzzy comparison of words.
        /// </summary>
        private bool IsFuzzy => Similarity is > 0 and < 100;

        /// <summary>
        /// Determines whether a text satisfies the query.
        /// </summary>
        /// <param name="text">The text. A null text holds no words and never matches.</param>
        /// <returns>True if the text matches, otherwise false.</returns>
        public bool IsMatch(string text)
        {
            if (_query.Count == 0)
            {
                return false;
            }

            var words = Tokenize(text, Culture, false);

            if (words.Count == 0)
            {
                return false;
            }

            if (Mode == WqlTextMatchMode.Phrase || Distance > 0)
            {
                return IsArranged(words);
            }

            for (var i = 0; i < _query.Count; i++)
            {
                if (!words.Any(x => IsWordMatch(i, x.Term)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a text satisfies a query that is only known at evaluation time,
        /// such as the result of a function or the value of another attribute.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="query">The words searched for.</param>
        /// <param name="mode">The match mode.</param>
        /// <param name="distance">The distance by which a word may stand further from the previous one.</param>
        /// <param name="similarity">The minimum similarity in percent.</param>
        /// <param name="culture">The culture used to split numbers within the text.</param>
        /// <returns>True if the text matches, otherwise false.</returns>
        public static bool IsMatch(string text, string query, WqlTextMatchMode mode, uint distance, uint similarity, CultureInfo culture)
        {
            return new WqlTextMatcher(query, mode, distance, similarity, culture).IsMatch(text);
        }

        /// <summary>
        /// Creates a condition that a query provider such as a database can translate, for
        /// example into one LIKE per word: the text must contain every literal part of every
        /// word without regard to case. It pre-selects rather than reproducing the text
        /// contract - order, distance, word boundaries and similarity are not checked, and
        /// whether diacritics are ignored is up to the collation of the database. A fuzzy
        /// query restricts nothing beyond the null test, since a similar word need not share
        /// any part with the word searched for.
        /// </summary>
        /// <param name="text">The text searched in, of type string.</param>
        /// <returns>A boolean expression built from null tests, ToLower and Contains only.</returns>
        public Expression ToTranslatableExpression(Expression text)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (_query.Count == 0)
            {
                return Expression.Constant(false);
            }

            Expression condition = Expression.NotEqual(text, Expression.Constant(null, typeof(string)));

            if (IsFuzzy)
            {
                return condition;
            }

            var lower = Expression.Call(text, StringToLowerMethod);

            foreach (var fragment in _query.SelectMany(x => x.Term.Split(IndexTermTokenizer.Wildcards, StringSplitOptions.RemoveEmptyEntries)).Distinct())
            {
                condition = Expression.AndAlso
                (
                    condition,
                    Expression.Call(lower, StringContainsMethod, Expression.Constant(fragment))
                );
            }

            return condition;
        }

        /// <summary>
        /// Searches for an arrangement of the query words in the text, each word within the
        /// window behind its predecessor that the mode and the distance allow.
        /// </summary>
        /// <param name="words">The words of the text.</param>
        /// <returns>True if one arrangement fits, otherwise false.</returns>
        private bool IsArranged(IReadOnlyList<(string Term, uint Position)> words)
        {
            var occurrences = new uint[_query.Count][];

            for (var i = 0; i < _query.Count; i++)
            {
                var index = i;

                occurrences[i] = [.. words
                    .Where(x => IsWordMatch(index, x.Term))
                    .Select(x => x.Position)
                    .Distinct()
                    .Order()];

                if (occurrences[i].Length == 0)
                {
                    return false;
                }
            }

            // as in the index, a placement that failed once does not depend on the path that
            // led to it and is not tried again
            var failed = new HashSet<(int, uint)>();

            return occurrences[0].Any(start => Follows(start, 1));

            bool Follows(uint position, int index)
            {
                if (index >= _query.Count)
                {
                    return true;
                }

                if (failed.Contains((index, position)))
                {
                    return false;
                }

                var (lower, upper) = Window(index, position);

                foreach (var next in occurrences[index])
                {
                    if (next > upper)
                    {
                        break;
                    }

                    if (next >= lower && Follows(next, index + 1))
                    {
                        return true;
                    }
                }

                failed.Add((index, position));

                return false;
            }
        }

        /// <summary>
        /// Returns the positions at which a query word may follow its predecessor.
        /// </summary>
        /// <param name="index">The index of the query word.</param>
        /// <param name="position">The position of the predecessor in the text.</param>
        /// <returns>The lowest and the highest permitted position.</returns>
        private (uint Lower, uint Upper) Window(int index, uint position)
        {
            if (Mode == WqlTextMatchMode.Phrase)
            {
                var gap = _query[index].Position >= _query[index - 1].Position
                    ? _query[index].Position - _query[index - 1].Position
                    : 0u;

                return (Clamp(position + (ulong)gap), Clamp(position + (ulong)gap + Distance));
            }

            return (position >= Distance ? position - Distance : 0u, Clamp(position + (ulong)Distance));
        }

        /// <summary>
        /// Determines whether a word of the text matches a word of the query.
        /// </summary>
        /// <param name="index">The index of the query word.</param>
        /// <param name="term">The word of the text.</param>
        /// <returns>True on a match, otherwise false.</returns>
        private bool IsWordMatch(int index, string term)
        {
            var query = _query[index].Term;

            if (IsFuzzy)
            {
                return IndexFuzzy.CalculateLevenshteinSimilarity(query, term) >= Similarity / 100.0;
            }

            return _patterns[index] is Regex pattern
                ? pattern.IsMatch(term)
                : string.Equals(query, term, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates the pattern of a query word that contains wildcards.
        /// </summary>
        /// <param name="term">The query word.</param>
        /// <returns>The pattern, or null if the word holds no wildcard.</returns>
        private static Regex CreatePattern(string term)
        {
            if (term.IndexOfAny(IndexTermTokenizer.Wildcards) < 0)
            {
                return null;
            }

            var pattern = Regex.Escape(term)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            return new Regex("^" + pattern + "$", RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Splits a text into its words, folded the way the analyzer of an index folds them.
        /// </summary>
        /// <param name="input">The text.</param>
        /// <param name="culture">The culture used to split numbers.</param>
        /// <param name="retrieval">True to keep the wildcards of a query within its words.</param>
        /// <returns>The words with their positions.</returns>
        internal static IReadOnlyList<(string Term, uint Position)> Tokenize(string input, CultureInfo culture, bool retrieval)
        {
            if (string.IsNullOrEmpty(input))
            {
                return [];
            }

            var wildcards = retrieval ? IndexTermTokenizer.Wildcards : null;
            var words = new List<(string, uint)>();

            foreach (var token in IndexTermTokenizer.Tokenize(input, culture, wildcards))
            {
                var term = token.Value is string text
                    ? Fold(text)
                    : WqlValueConverter.ToText(token.Value);

                if (!string.IsNullOrEmpty(term))
                {
                    words.Add((term, token.Position));
                }
            }

            return words;
        }

        /// <summary>
        /// Folds a word to lower case and removes its diacritics.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns>The folded word.</returns>
        private static string Fold(string word)
        {
            var normalized = word.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Limits a position to the range of a position.
        /// </summary>
        /// <param name="value">The position.</param>
        /// <returns>The limited position.</returns>
        private static uint Clamp(ulong value)
        {
            return value > uint.MaxValue ? uint.MaxValue : (uint)value;
        }
    }
}
