using System;
using System.Globalization;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Converts the literals and function results of a WQL statement into the type of the
    /// attribute they are compared with. Both execution paths - the reverse index and the
    /// expression tree of a query - rely on this single conversion, so that a value means the
    /// same in either path. A conversion that would lose information, such as 5.5 into an
    /// integer, is refused instead of rounded, since a rounded value would answer a different
    /// question than the one written.
    /// </summary>
    public static class WqlValueConverter
    {
        /// <summary>
        /// Converts a value into the specified type.
        /// </summary>
        /// <param name="value">The value to convert. May be null.</param>
        /// <param name="targetType">The type to convert into.</param>
        /// <param name="culture">The culture used to parse text; the invariant culture is tried as well.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The value cannot be converted without loss.</exception>
        public static object Convert(object value, Type targetType, CultureInfo culture)
        {
            if (TryConvert(value, targetType, culture, out var result))
            {
                return result;
            }

            throw new InvalidCastException($"The value '{value}' cannot be converted to '{targetType?.Name}'.");
        }

        /// <summary>
        /// Attempts to convert a value into the specified type.
        /// </summary>
        /// <param name="value">The value to convert. A null value converts into every type that can hold null.</param>
        /// <param name="targetType">The type to convert into.</param>
        /// <param name="culture">The culture used to parse text; the invariant culture is tried as well.</param>
        /// <param name="result">The converted value, or null if the conversion failed.</param>
        /// <returns>True if the value could be converted without loss, otherwise false.</returns>
        public static bool TryConvert(object value, Type targetType, CultureInfo culture, out object result)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            culture ??= CultureInfo.InvariantCulture;
            result = null;

            var underlying = Nullable.GetUnderlyingType(targetType);
            var type = underlying ?? targetType;

            if (value is null)
            {
                return underlying is not null || !targetType.IsValueType;
            }

            if (type == typeof(object) || type.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }

            if (type == typeof(string))
            {
                result = ToText(value);
                return true;
            }

            if (type.IsEnum)
            {
                return TryConvertEnum(value, type, culture, out result);
            }

            if (IsNumeric(type))
            {
                return TryConvertNumber(value, type, culture, out result);
            }

            if (type == typeof(bool))
            {
                return TryConvertBoolean(value, out result);
            }

            if (type == typeof(DateTime))
            {
                return TryConvertDateTime(value, culture, out result);
            }

            if (type == typeof(DateTimeOffset))
            {
                return TryConvertDateTimeOffset(value, culture, out result);
            }

            if (type == typeof(DateOnly))
            {
                return TryConvertDateOnly(value, culture, out result);
            }

            if (type == typeof(TimeOnly))
            {
                return TryConvertTimeOnly(value, culture, out result);
            }

            if (type == typeof(TimeSpan))
            {
                return TryConvertTimeSpan(value, culture, out result);
            }

            if (type == typeof(Guid))
            {
                if (value is string text && Guid.TryParse(text.Trim(), out var guid))
                {
                    result = guid;
                    return true;
                }

                return false;
            }

            if (type == typeof(char))
            {
                if (value is string text && text.Length == 1)
                {
                    result = text[0];
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a type is one of the built-in numeric types, including their
        /// nullable form.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is numeric, otherwise false.</returns>
        public static bool IsNumeric(Type type)
        {
            if (type is null)
            {
                return false;
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(float) || type == typeof(double) ||
                type == typeof(decimal);
        }

        /// <summary>
        /// Returns the text form of a value. Numbers, dates and the like are written in the
        /// invariant culture, so that the text of a value does not depend on the machine the
        /// statement runs on.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The text form, or null for a null value.</returns>
        public static string ToText(object value)
        {
            return value switch
            {
                null => null,
                string text => text,
                bool flag => flag ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Converts a numeric value or a numeric text into a numeric type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The numeric target type.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertNumber(object value, Type type, CultureInfo culture, out object result)
        {
            result = null;

            if (!TryReadNumber(value, culture, out var exact, out var approximate))
            {
                return false;
            }

            if (type == typeof(double))
            {
                result = exact.HasValue ? (double)exact.Value : approximate;
                return true;
            }

            if (type == typeof(float))
            {
                var single = exact.HasValue ? (float)exact.Value : (float)approximate;

                // a finite value beyond the range of a float would silently become infinite
                if (float.IsInfinity(single) && !double.IsInfinity(approximate))
                {
                    return false;
                }

                result = single;
                return true;
            }

            if (!exact.HasValue)
            {
                return false;
            }

            var number = exact.Value;

            if (type == typeof(decimal))
            {
                result = number;
                return true;
            }

            if (decimal.Truncate(number) != number)
            {
                return false;
            }

            try
            {
                result = System.Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads a number from a numeric value or a numeric text. Where possible the number is
        /// read exactly as a decimal; a double that has no decimal form, such as an infinity,
        /// is only available approximately.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="exact">The exact value, if it has a decimal form.</param>
        /// <param name="approximate">The value as a double.</param>
        /// <returns>True if the value is a number, otherwise false.</returns>
        private static bool TryReadNumber(object value, CultureInfo culture, out decimal? exact, out double approximate)
        {
            exact = null;
            approximate = 0;

            switch (value)
            {
                case double d:
                    approximate = d;
                    exact = ToDecimal(d);
                    return true;
                case float f:
                    approximate = f;
                    exact = ToDecimal(f);
                    return true;
                case decimal m:
                    exact = m;
                    approximate = (double)m;
                    return true;
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    exact = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    approximate = (double)exact.Value;
                    return true;
                case Enum:
                    exact = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    approximate = (double)exact.Value;
                    return true;
                case string text:
                    {
                        var trimmed = text.Trim();
                        const NumberStyles styles = NumberStyles.Number | NumberStyles.AllowExponent;

                        if (decimal.TryParse(trimmed, styles, culture, out var parsed) ||
                            decimal.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out parsed))
                        {
                            exact = parsed;
                            approximate = (double)parsed;
                            return true;
                        }

                        if (double.TryParse(trimmed, NumberStyles.Float, culture, out var d) ||
                            double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                        {
                            approximate = d;
                            exact = ToDecimal(d);
                            return true;
                        }

                        return false;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Converts a double into a decimal, if the double has a decimal form.
        /// </summary>
        /// <param name="value">The double.</param>
        /// <returns>The decimal, or null for infinities, NaN and values beyond the decimal range.</returns>
        private static decimal? ToDecimal(double value)
        {
            // the bounds are excluded, since the double nearest to them already lies beyond the range
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                value >= (double)decimal.MaxValue || value <= (double)decimal.MinValue)
            {
                return null;
            }

            return (decimal)value;
        }

        /// <summary>
        /// Converts a value into an enumeration, either by the name of a member or by its
        /// numeric value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The enumeration type.</param>
        /// <param name="culture">The culture used to parse numeric text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertEnum(object value, Type type, CultureInfo culture, out object result)
        {
            result = null;

            if (value is Enum)
            {
                // a member of another enumeration does not denote a member of this one
                return false;
            }

            if (value is string text && !TryReadNumber(text, culture, out _, out _))
            {
                if (Enum.TryParse(type, text.Trim(), true, out var member))
                {
                    result = member;
                    return true;
                }

                return false;
            }

            if (TryConvertNumber(value, Enum.GetUnderlyingType(type), culture, out var number))
            {
                result = Enum.ToObject(type, number);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a value into a boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertBoolean(object value, out object result)
        {
            result = null;

            if (value is string text && bool.TryParse(text.Trim(), out var flag))
            {
                result = flag;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a value into a date and time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertDateTime(object value, CultureInfo culture, out object result)
        {
            result = value switch
            {
                DateTimeOffset offset => offset.DateTime,
                DateOnly date => date.ToDateTime(TimeOnly.MinValue),
                string text when DateTime.TryParse(text.Trim(), culture, DateTimeStyles.None, out var parsed) => parsed,
                string text when DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
                _ => null
            };

            return result is not null;
        }

        /// <summary>
        /// Converts a value into a date and time with an offset.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertDateTimeOffset(object value, CultureInfo culture, out object result)
        {
            result = value switch
            {
                DateTime dateTime => new DateTimeOffset(dateTime),
                DateOnly date => new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue)),
                string text when DateTimeOffset.TryParse(text.Trim(), culture, DateTimeStyles.None, out var parsed) => parsed,
                string text when DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
                _ => null
            };

            return result is not null;
        }

        /// <summary>
        /// Converts a value into a date. A point in time converts only at midnight, since the
        /// time of day would otherwise be dropped.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertDateOnly(object value, CultureInfo culture, out object result)
        {
            result = value switch
            {
                DateTime dateTime when dateTime.TimeOfDay == TimeSpan.Zero => DateOnly.FromDateTime(dateTime),
                DateTimeOffset offset when offset.TimeOfDay == TimeSpan.Zero => DateOnly.FromDateTime(offset.DateTime),
                string text when DateOnly.TryParse(text.Trim(), culture, DateTimeStyles.None, out var parsed) => parsed,
                string text when DateOnly.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
                _ => null
            };

            return result is not null;
        }

        /// <summary>
        /// Converts a value into a time of day.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertTimeOnly(object value, CultureInfo culture, out object result)
        {
            result = value switch
            {
                TimeSpan span when span >= TimeSpan.Zero && span < TimeSpan.FromDays(1) => TimeOnly.FromTimeSpan(span),
                string text when TimeOnly.TryParse(text.Trim(), culture, DateTimeStyles.None, out var parsed) => parsed,
                string text when TimeOnly.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
                _ => null
            };

            return result is not null;
        }

        /// <summary>
        /// Converts a value into a time span.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture used to parse text.</param>
        /// <param name="result">The converted value.</param>
        /// <returns>True on success, otherwise false.</returns>
        private static bool TryConvertTimeSpan(object value, CultureInfo culture, out object result)
        {
            result = value switch
            {
                TimeOnly time => time.ToTimeSpan(),
                string text when TimeSpan.TryParse(text.Trim(), culture, out var parsed) => parsed,
                string text when TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };

            return result is not null;
        }
    }
}
