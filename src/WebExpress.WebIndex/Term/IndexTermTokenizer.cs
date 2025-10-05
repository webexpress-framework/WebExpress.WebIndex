using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WebExpress.WebIndex.Term
{
    /// <summary>
    /// Tokenizes text into terms by whitespace and punctuation with culture-aware number and infinity 
    /// detection. Ensures robust handling for null culture, HTML entities, surrogate pairs (emoji), 
    /// and multi-character symbols.
    /// </summary>
    public static class IndexTermTokenizer
    {
        /// <summary>
        /// Returns the default wildcard characters used to keep wildcard punctuation as part of tokens.
        /// </summary>
        public static char[] Wildcards { get; } = ['?', '*'];

        /// <summary>
        /// Tokenizes an input string into an enumeration of terms.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="culture">The culture; falls back to invariant culture if null.</param>
        /// <param name="wildcards">Optional wildcard characters that should not split tokens.</param>
        /// <returns>An enumeration of term tokens including position and value (string or double).</returns>
        public static IEnumerable<IndexTermToken> Tokenize(string input, CultureInfo culture, char[] wildcards = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break;
            }

            var currentToken = new StringBuilder();
            var position = 0u;
            var isString = false;
            var isNumber = false;
            var hasDecimal = false;
            var hasExponent = false;
            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator[0];
            var groupSeparator = culture.NumberFormat.NumberGroupSeparator[0];
            var infinitySymbol = culture.NumberFormat.PositiveInfinitySymbol[0];
            var positiveSign = culture.NumberFormat.PositiveSign[0];
            var negativeSign = culture.NumberFormat.NegativeSign[0];

            for (int i = 0; i < input.Length; i++)
            {
                var current = input[i];
                var last = i > 0 ? input[i - 1] : (char)0;
                var next = i + 1 < input.Length ? input[i + 1] : (char)0;

                if (char.IsControl(current))
                {
                    if (currentToken.Length > 0)
                    {
                        yield return new IndexTermToken
                        {
                            Position = position,
                            Value = Convert(currentToken, false, culture)
                        };
                        currentToken.Clear();
                    }
                }
                else if (!isString && (char.IsDigit(current) ||
                    (isNumber && (current == decimalSeparator && !hasDecimal ||
                    current == groupSeparator || current == ',' ||
                    current == 'e' || current == 'E')) ||
                    (current == negativeSign && (currentToken.Length == 0 || hasExponent) &&
                    (char.IsDigit(next) || next == infinitySymbol)) ||
                    (current == positiveSign && (currentToken.Length == 0 || hasExponent) &&
                    (char.IsDigit(next) || next == infinitySymbol)) ||
                    current == infinitySymbol))
                {
                    if (!isNumber)
                    {
                        if (currentToken.Length > 0)
                        {
                            yield return new IndexTermToken
                            {
                                Position = position,
                                Value = Convert(currentToken, false, culture)
                            };
                            currentToken.Clear();
                        }
                        isNumber = true;
                        hasDecimal = current == decimalSeparator;
                        hasExponent = current == 'e' || current == 'E';
                    }
                    else if (current == decimalSeparator)
                    {
                        if (!hasDecimal)
                        {
                            hasDecimal = true;
                        }
                        else
                        {
                            yield return new IndexTermToken
                            {
                                Position = position,
                                Value = Convert(currentToken, true, culture)
                            };
                            currentToken.Clear();
                            isNumber = false;
                            hasDecimal = false;
                            hasExponent = false;
                            position++;
                            continue;
                        }
                    }
                    else if (current == groupSeparator)
                    {
                        if (!char.IsDigit(last) || !char.IsDigit(next) || hasDecimal)
                        {
                            yield return new IndexTermToken
                            {
                                Position = position,
                                Value = Convert(currentToken, true, culture)
                            };
                            currentToken.Clear();
                            isNumber = false;
                            hasDecimal = false;
                            hasExponent = false;
                            position++;
                        }
                        continue;
                    }
                    else if (current == 'e' || current == 'E')
                    {
                        if (!hasExponent)
                        {
                            hasExponent = true;
                        }
                        else
                        {
                            yield return new IndexTermToken
                            {
                                Position = position,
                                Value = Convert(currentToken, true, culture)
                            };
                            currentToken.Clear();
                            isNumber = false;
                            hasDecimal = false;
                            hasExponent = false;
                            position++;
                            continue;
                        }
                    }
                    currentToken.Append(current);
                }
                else
                {
                    if (isNumber)
                    {
                        yield return new IndexTermToken
                        {
                            Position = position,
                            Value = Convert(currentToken, true, culture)
                        };
                        currentToken.Clear();
                        isNumber = false;
                        hasDecimal = false;
                        hasExponent = false;
                        position++;
                    }

                    if (char.IsWhiteSpace(current) ||
                        char.IsSymbol(current) ||
                        (
                            char.IsPunctuation(current) &&
                            (wildcards == null || !wildcards.Contains(current))
                        ))
                    {
                        if (currentToken.Length > 0)
                        {
                            yield return new IndexTermToken
                            {
                                Position = position,
                                Value = Convert(currentToken, false, culture)
                            };
                            currentToken.Clear();
                            isString = false;
                        }
                        position++;
                    }
                    else
                    {
                        currentToken.Append(current);
                        isString = true;
                    }
                }
            }

            if (currentToken.Length > 0)
            {
                yield return new IndexTermToken
                {
                    Position = position,
                    Value = Convert(currentToken, isNumber, culture)
                };
            }
        }

        /// <summary>
        /// Converts a token buffer to its appropriate data type based on parsing outcome.
        /// Returns a double if numeric parsing succeeds or the token denotes (±)infinity; 
        /// otherwise returns the string.
        /// </summary>
        /// <param name="sb">the string builder containing the token's characters.</param>
        /// <param name="isNumber">whether the token buffer is considered a number.</param>
        /// <param name="culture">the culture for numeric parsing.</param>
        /// <returns>double or string depending on parse result.</returns>
        private static object Convert(StringBuilder sb, bool isNumber, CultureInfo culture)
        {
            var res = sb.ToString();
            var trimmed = res.Trim();

            // handle unicode infinity and culture infinity symbols
            if (trimmed == "∞" || trimmed == "+∞")
            {
                return double.PositiveInfinity;
            }

            if (trimmed == "-∞")
            {
                return double.NegativeInfinity;
            }

            var nf = (culture ?? CultureInfo.InvariantCulture).NumberFormat;
            var posInf = nf.PositiveInfinitySymbol;
            var negInf = nf.NegativeInfinitySymbol;

            if
            (
                !string.IsNullOrEmpty(posInf) &&
                string.Equals(trimmed, posInf, System.StringComparison.OrdinalIgnoreCase)
            )
            {
                return double.PositiveInfinity;
            }

            if
            (
                !string.IsNullOrEmpty(negInf) &&
                string.Equals(trimmed, negInf, System.StringComparison.OrdinalIgnoreCase)
            )
            {
                return double.NegativeInfinity;
            }

            // numeric parsing path
            if (isNumber)
            {
                if (double.TryParse(res, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out double number))
                {
                    return number;
                }

                if (double.TryParse(res, NumberStyles.Any, CultureInfo.InvariantCulture, out double numberInvariant))
                {
                    return numberInvariant;
                }
            }

            return res;
        }
    }
}
