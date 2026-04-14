namespace WebExpress.WebIndex.Term
{
    /// <summary>
    /// Represents a term token.
    /// </summary>
    public class IndexTermToken
    {
        /// <summary>
        /// Gets the position of the token in the input value.
        /// </summary>
        public uint Position { get; internal set; }

        /// <summary>
        /// Gets the token value.
        /// </summary>
        public object Value { get; internal set; }

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            // Combine the hash codes of Name and Position
            return Value.GetHashCode() ^ Position.GetHashCode();
        }

        /// <summary>
        /// Comparison with another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is IndexTermToken token)
            {
                return Value == token.Value && Position == token.Position;
            }

            return false;
        }

        /// <summary>
        /// Convert the object into a string representation. 
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Value}:{Position}";
        }

        /// <summary>
        /// Determines whether two specified instances are equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>true if left and right are equal; otherwise, false.</returns>
        public static bool operator ==(IndexTermToken left, IndexTermToken right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances are not equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>true if left and right are not equal; otherwise, false.</returns>
        public static bool operator !=(IndexTermToken left, IndexTermToken right)
        {
            return !left.Equals(right);
        }
    }
}
