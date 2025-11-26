namespace BitPatch.DialogLang
{
    /// <summary>
    /// Lexer states for handling different contexts in the source code.
    /// </summary>
    internal enum LexerState
    {
        /// <summary>
        /// Default state, normal code parsing.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Reading an interpolated string.
        /// </summary>
        ReadingString = 2,

        /// <summary>
        /// Reading an expression inside a string.
        /// </summary>
        ReadingInlineExpression = 3
    }
}