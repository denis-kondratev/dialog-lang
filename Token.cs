namespace GameDialog.Lang
{
    /// <summary>
    /// Represents a single token in the source code.
    /// </summary>
    internal record Token(TokenType Type, string Value, Location Location)
    {
        /// <summary>
        /// Determines whether the token represents the end of the source.
        /// </summary>
        public bool IsEndOfSource() => Type is TokenType.EndOfSource;

        /// <summary>
        /// Creates an empty token representing the end of the source.
        /// </summary>
        public static Token Empty()
        {
            return new Token(TokenType.EndOfSource, string.Empty, new Location(Source.Empty(), 0, 0));
        }

        /// <summary>
        /// Creates an indent token.
        /// </summary>
        /// <param name="location">Location of the indent token.</param>
        public static Token Indent(Location location)
        {
            return new Token(TokenType.Indent, string.Empty, location);
        }

        /// <summary>
        /// Creates a dedent token.
        /// </summary>
        /// <param name="location">Location of the dedent token.</param>
        public static Token Dedent(Location location)
        {
            return new Token(TokenType.Dedent, string.Empty, location);
        }

        /// <summary>
        /// Returns a string representation of the token.
        /// </summary>
        public override string ToString()
        {
            return $"{Type}({Value}) at {Location}";
        }
    }
}
