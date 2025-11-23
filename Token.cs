namespace BitPatch.DialogLang
{
    /// <summary>
    /// Token types for the Game Dialog Script language.
    /// </summary>
    internal enum TokenType
    {
        // Literals
        Integer,        // 123, 456
        Float,          // 3.14, 2.5
        String,         // "Hello World"
        True,           // true
        False,          // false
        Identifier,     // variable names
        
        // Operators
        Assign,         // =
        Output,         // <<
        And,            // and
        Or,             // or
        Not,            // not
        Xor,            // xor
        Plus,           // +
        Minus,          // -
        Multiply,       // *
        Divide,         // /
        Modulo,         // %
        
        // Control flow
        While,          // while
        Break,          // break (reserved)
        Continue,       // continue (reserved)
        If,             // if
        Else,           // else
        
        // Comparison operators
        GreaterThan,    // >
        LessThan,       // <
        GreaterOrEqual, // >=
        LessOrEqual,    // <=
        Equal,          // ==
        NotEqual,       // !=
        
        // Delimiters
        LeftParen,      // (
        RightParen,     // )
        
        // Indentation
        Indent,         // Increase in indentation level
        Dedent,         // Decrease in indentation level
        
        // Special
        Newline,        // End of line (statement terminator)
        EndOfFile
    }

    /// <summary>
    /// Represents a single token in the source code.
    /// </summary>
    internal class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public Location Location { get; }

        public Token(TokenType type, string value, Source source, int line, int initial, int final)
            : this(type, value, new Location(source, line, initial, final))
        {
        }

        public Token(TokenType type, string value, Source source, int line, int position)
            : this(type, value, new Location(source, line, position))
        {
        }

        public Token(TokenType type, string value, Location location)
        {
            Type = type;
            Value = value;
            Location = location;
        }

        public bool IsEndOfFile()
        {
            return Type is TokenType.EndOfFile;
        }

        public static Token EndOfFile(Location location)
        {
            return new Token(TokenType.EndOfFile, string.Empty, location);
        }

        public static Token EndOfFile(Source source, int line, int column)
        {
            return EndOfFile(new Location(source, line, column));
        }

        public static Token Empty()
        {
            return new Token(TokenType.EndOfFile, string.Empty, new Location(Source.Empty(), 0, 0));
        }

        public static Token NewLine(Location location)
        {
            return new Token(TokenType.Newline, string.Empty, location);
        }

        public static Token Indent(Location location)
        {
            return new Token(TokenType.Indent, string.Empty, location);
        }

        public static Token Dedent(Location location)
        {
            return new Token(TokenType.Dedent, string.Empty, location);
        }

        public override string ToString()
        {
            return $"Token({Type}, '{Value}', {Location})";
        }
    }
}
