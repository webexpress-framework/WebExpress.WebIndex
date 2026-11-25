using System.Text;

namespace WebExpress.WebIndex.Wql
{
    /// <summary>
    /// The standard <see cref="IWqlToken"/>: one syntactic unit of a WQL query (a word, operator,
    /// or value) with its position and length in the raw query string.
    /// </summary>
    public class WqlToken : IWqlToken
    {
        /// <summary>
        /// Returns the starting position of the token in the raw statement.
        /// </summary>
        public int Offset { get; internal set; }

        // the tokenizer appends one character at a time; a builder keeps a long value linear
        // where concatenating strings would copy it once per character
        private StringBuilder _builder;
        private string _value;

        /// <summary>
        /// Returns the length of the token (start + length = end) in the raw statement.
        /// </summary>
        public int Length => _builder?.Length ?? _value?.Length ?? 0;

        /// <summary>
        /// Checks if the token is empty.
        /// </summary>
        /// <returns>True if no value is stored, false otherwise.</returns>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Returns the token value.
        /// </summary>
        public string Value
        {
            get
            {
                if (_builder != null)
                {
                    _value = _builder.ToString();
                    _builder = null;
                }

                return _value;
            }
            internal set
            {
                _value = value;
                _builder = null;
            }
        }

        /// <summary>
        /// Returns whether the token is the content of a quoted value, which is data and never
        /// syntax, even when it reads like a parenthesis or a keyword.
        /// </summary>
        internal bool IsQuotedContent { get; init; }

        /// <summary>
        /// Returns the last character of the token without materializing its value.
        /// </summary>
        internal char LastCharacter => _builder != null
            ? (_builder.Length > 0 ? _builder[^1] : default)
            : (string.IsNullOrEmpty(_value) ? default : _value[^1]);

        /// <summary>
        /// Adds a character at the end.
        /// </summary>
        /// <param name="c">The character to add.</param>
        internal void Append(char c)
        {
            _builder ??= new StringBuilder(_value);
            _value = null;
            _builder.Append(c);
        }

        /// <summary>
        /// Converts the token to a string.
        /// </summary>
        /// <returns>The token as a string.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}
