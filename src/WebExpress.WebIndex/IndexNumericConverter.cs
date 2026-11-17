using System;
using System.Globalization;
using System.Numerics;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Converts numbers into the decimal keys of the numeric reverse index. Indexing, retrieval
    /// and the WQL conversion share this one rule, so a key found in the index is the number a
    /// query wrote; a second rule anywhere would let a comparison miss its own value.
    /// </summary>
    /// <remarks>
    /// A binary floating point number is not taken by the cast, which yields its binary
    /// expansion (0.1 becomes 0.1000000000000000055511151231), nor by Convert.ToDecimal, which
    /// rounded to 15 significant digits up to .NET 10 and equals the cast since .NET 11. A
    /// whole number is taken exactly; a fraction through its shortest round-trip text. That
    /// text only tells the number apart from its binary neighbours and is not its value
    /// (10000001024f reads as 1.0000001E+10), which is why a whole number does not use it. The
    /// text of a fraction never is a whole number, since a whole number that round-trips to the
    /// value would be the value itself.
    /// </remarks>
    internal static class IndexNumericConverter
    {
        /// <summary>
        /// Converts a number, or a text holding one, into a decimal.
        /// </summary>
        /// <param name="value">The number or text.</param>
        /// <param name="culture">The culture of a text; the invariant culture is tried as well.</param>
        /// <param name="result">The decimal.</param>
        /// <returns>True for a finite number within the decimal range, otherwise false.</returns>
        public static bool TryToDecimal(object value, CultureInfo culture, out decimal result)
        {
            decimal? number = value switch
            {
                null => null,
                decimal m => m,
                double d => ToDecimal(d),
                float f => ToDecimal(f),
                sbyte or byte or short or ushort or int or uint or long or ulong => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                _ => Parse(value.ToString(), culture)
            };

            result = number ?? default;

            return number.HasValue;
        }

        /// <summary>
        /// Converts a double into a decimal.
        /// </summary>
        /// <param name="value">The double.</param>
        /// <returns>The decimal, or null for infinities, NaN and values beyond the decimal range.</returns>
        public static decimal? ToDecimal(double value)
        {
            return ToDecimal(value, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Converts a float into a decimal. A fraction takes the float's own digits, so 0.1f
        /// becomes 0.1 and not the digits of the double it widens to.
        /// </summary>
        /// <param name="value">The float.</param>
        /// <returns>The decimal, or null for infinities, NaN and values beyond the decimal range.</returns>
        public static decimal? ToDecimal(float value)
        {
            return ToDecimal(value, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Converts a binary floating point number into a decimal: a whole number exactly, a
        /// fraction through its shortest round-trip text.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <param name="digits">Its shortest round-trip text in the invariant culture.</param>
        /// <returns>The decimal, or null for infinities, NaN and values beyond the decimal range.</returns>
        private static decimal? ToDecimal(double value, string digits)
        {
            // the bounds are excluded, since the double nearest to them already lies beyond the range
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                value >= (double)decimal.MaxValue || value <= (double)decimal.MinValue)
            {
                return null;
            }

            if (Math.Truncate(value) == value)
            {
                return (decimal)new BigInteger(value);
            }

            return decimal.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
        }

        /// <summary>
        /// Parses a text in the given culture, then in the invariant culture.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The decimal, or null when the text holds no number.</returns>
        private static decimal? Parse(string text, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (decimal.TryParse(text, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out var parsed) ||
                decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
