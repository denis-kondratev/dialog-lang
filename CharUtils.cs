namespace GameDialog.Lang
{
    /// <summary>
    /// Utility methods for character and string operations.
    /// </summary>
    internal static class CharUtils
    {
        /// <summary>
        /// Checks if the character is a newline character.
        /// </summary>
        public static bool IsNewLine(this char c)
        {
            return c is '\n' or '\r' or '\u2028' or '\u2029' or '\u0085';
        }

        /// <summary>
        /// Formats a character as a readable C-style literal, escaping control characters when necessary.
        /// </summary>
        /// <param name="c">The character to format.</param>
        public static string ToPrintable(this char c)
        {
            return c switch
            {
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\b' => "\\b",
                '\f' => "\\f",
                '\0' => "\\0",
                ' ' => "' '",
                _ when char.IsControl(c) => $"\\u{(int)c:X4}",
                _ => c.ToString(),
            };
        }

        /// <summary>
        /// Formats an integer as a readable C-style literal, escaping control characters when necessary.
        /// </summary>
        /// <param name="n">The integer to format.</param>
        public static string ToPrintable(this int n)
        {
            return n is -1 ? "<EOF>" : ((char)n).ToPrintable();
        }

        /// <summary>
        /// Checks if the integer represents a newline character.
        /// </summary>
        public static bool IsNewLine(this int n)
        {
            return n is not -1 && ((char)n).IsNewLine();
        }

        /// <summary>
        /// Checks if the integer represents the end of source code.
        /// </summary>
        public static bool IsEndOfSource(this int n)
        {
            return n is -1;
        }

        /// <summary>
        /// Checks if the integer represents a whitespace character that is not a newline.
        /// </summary>
        public static bool IsWhiteSpace(this int n)
        {
            return n is not -1 && char.IsWhiteSpace((char)n) && !((char)n).IsNewLine();
        }

        /// <summary>
        /// Checks if the integer represents a valid identifier character.
        /// </summary>
        public static bool IsLetterOrDigit(this int n)
        {
            if (n is -1)
            {
                return false;
            }

            var c = (char)n;
            return c is '_' || char.IsLetterOrDigit(c);
        }

        /// <summary>
        /// Checks if the integer represents a digit character.
        /// </summary>
        public static bool IsDigit(this int n)
        {
            return n is not -1 && char.IsDigit((char)n);
        }
    }
}