namespace DialogLang
{
    /// <summary>
    /// Represents token types for the script interpreter.
    /// </summary>
    internal enum TokenType
    {
        Number,
        String,
        Boolean,
        Plus,
        Minus,
        Multiply,
        Divide,
        Assign,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        And,
        Or,
        Not,
        Identifier,
        If,
        Else,
        As,
        Output,
        Input,
        Colon,
        NewLine,
        Indent,
        Dedent,
        LeftParen,
        RightParen,
        EOF
    }

    /// <summary>
    /// Represents a single token in the script.
    /// </summary>
    internal class Token
    {
        public TokenType Type { get; }
        public object? Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, int line, int column, object? value = null)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return Value != null 
                ? $"Token({Type}, {Value}) at line {Line}, column {Column}" 
                : $"Token({Type}) at line {Line}, column {Column}";
        }
    }
}
