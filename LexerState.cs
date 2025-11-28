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
        Default,

        /// <summary>
        /// Reading an interpolated string.
        /// </summary>
        ReadingString,

        /// <summary>
        /// Reading a multi-line string.
        /// </summary>
        ReadingMultiString,

        /// <summary>
        /// Reading an expression inside a string.
        /// </summary>
        ReadingInlineExpression
    }
}