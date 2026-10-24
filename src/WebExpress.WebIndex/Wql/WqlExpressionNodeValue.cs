using System;
using System.Collections.Generic;
using System.Globalization;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// Describes the value expression of a WQL statement. A value is a typed literal: a
    /// string, a number, a boolean or null. Quoted values are always strings, so that
    /// <c>'null'</c> and <c>'true'</c> remain searchable as words.
    /// </summary>
    /// <typeparam name="TIndexItem">The type of the index item.</typeparam>
    public class WqlExpressionNodeValue<TIndexItem> : IWqlExpressionNode<TIndexItem>
        where TIndexItem : IIndexItem
    {
        /// <summary>
        /// Returns the tokens associated with this syntax tree node.
        /// </summary>
        public IEnumerable<IWqlToken> Tokens { get; internal set; }

        /// <summary>
        /// Returns the value as string.
        /// </summary>
        public string StringValue { get; internal set; }

        /// <summary>
        /// Returns the value as double.
        /// </summary>
        public double? NumberValue { get; internal set; }

        /// <summary>
        /// Returns the value as boolean.
        /// </summary>
        public bool? BoolValue { get; internal set; }

        /// <summary>
        /// Returns true if the value is the literal <c>null</c>.
        /// </summary>
        public bool IsNull => StringValue is null && NumberValue is null && BoolValue is null;

        /// <summary>
        /// Returns the number exactly as written, if it has a decimal form. A double cannot
        /// hold every integer of a long, so conversions into integral attributes read the
        /// number from here.
        /// </summary>
        internal decimal? DecimalValue { get; set; }

        /// <summary>
        /// Returns the token the value was read from, used to locate semantic errors.
        /// </summary>
        internal IWqlToken Token { get; set; }

        /// <summary>
        /// Returns the culture in which to run the wql.
        /// </summary>
        public CultureInfo Culture { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        internal WqlExpressionNodeValue()
        {
        }

        /// <summary>
        /// Returns the value.
        /// </summary>
        /// <returns>The value.</returns>
        public object GetValue()
        {
            if (NumberValue.HasValue)
            {
                return NumberValue.Value;
            }

            if (BoolValue.HasValue)
            {
                return BoolValue.Value;
            }

            return StringValue;
        }

        /// <summary>
        /// Returns the value with a number in its exact form, as the converter expects it.
        /// </summary>
        /// <returns>The value.</returns>
        internal object GetExactValue()
        {
            return DecimalValue.HasValue ? DecimalValue.Value : GetValue();
        }

        /// <summary>
        /// Converts the value to a string.
        /// </summary>
        /// <param name="value">The value.</param>
        public static explicit operator string(WqlExpressionNodeValue<TIndexItem> value)
        {
            return value.GetValue()?.ToString();
        }

        /// <summary>
        /// Converts the value expression to a string.
        /// </summary>
        /// <returns>The value expression as a string.</returns>
        public override string ToString()
        {
            return GetValue() switch
            {
                null => "null",
                bool flag => flag ? "true" : "false",
                string text => "'" + text.Replace("'", "''") + "'",
                var value => Convert.ToString(value, Culture)
            };
        }
    }
}
