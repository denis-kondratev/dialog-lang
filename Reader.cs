using System;
using System.IO;

namespace GameDialog.Lang
{
    /// <summary>
    /// Low-level character reader that provides streaming access to source code with location tracking.
    /// Handles comment skipping, newline normalization, and indentation style validation.
    /// </summary>
    internal class Reader : IDisposable
    {
        /// <summary>
        /// Gets the current location in the source code (line and column).
        /// </summary>
        public Location Location => new(_source, _line, _column);

        /// <summary>
        /// Gets the source being read.
        /// </summary>
        public Source Source => _source;

        /// <summary>
        /// Gets the current line number (1-based).
        /// </summary>
        public int Line => _line;

        /// <summary>
        /// Gets the current column position (1-based).
        /// </summary>
        public int Column => _column;

        /// <summary>
        /// The source being read.
        /// </summary>
        private readonly Source _source;

        /// <summary>
        /// The underlying text reader for the source code.
        /// </summary>
        private readonly TextReader _reader;

        /// <summary>
        /// Current line number (1-based).
        /// </summary>
        private int _line = 1;

        /// <summary>
        /// Current column number (1-based).
        /// </summary>
        private int _column = 1;

        /// <summary>
        /// The next character to be read, or -1 if end of source is reached.
        /// </summary>
        private int _peek;

        /// <summary>
        /// Defines the indentation style: ' ' for spaces, '\t' for tabs, or '\0' if undefined.
        /// Once set by the first indentation, it enforces consistent style throughout the file.
        /// </summary>  
        private char _indentStyle = '\0';

        /// <summary>
        /// Tracks whether the current line contains only whitespace so far.
        /// Used to determine if a line is empty or blank.
        /// </summary>
        private bool _isLineEmpty = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Reader"/> class.
        /// </summary>
        /// <param name="source">The source code to read.</param>
        public Reader(Source source)
        {
            _source = source;
            _reader = source.CreateReader();
            _peek = _reader.Read();
        }

        /// <summary>
        /// Determines whether the current line is empty (contains only whitespace).
        /// </summary>
        /// <returns>True if the line is empty; otherwise, false.</returns>
        public bool IsLineEmpty() => _isLineEmpty;

        /// <summary>
        /// Determines whether there are more characters to read.
        /// </summary>
        /// <returns>True if more characters are available; otherwise, false.</returns>
        public bool CanRead() => _peek is not -1;

        /// <summary>
        /// Determines whether the reader is at the start of a line (column 1).
        /// </summary>
        /// <returns>True if at line start; otherwise, false.</returns>
        public bool IsAtLineStart() => _column is 1;

        /// <summary>
        /// Peeks at the next character without consuming it.
        /// </summary>
        /// <returns>The next character code, or -1 if end of source is reached.</returns>
        public int Peek() => _peek;

        /// <summary>
        /// Determines whether the reader is at the end of a line (newline, end of source, or comment start).
        /// </summary>
        /// <returns>True if at line end; otherwise, false.</returns>
        public bool IsAtLineEnd() => _peek.IsNewLine() || _peek is -1 || _peek == '#';

        /// <summary>
        /// Reads and consumes the next character from the source.
        /// Automatically skips comments (from '#' to end of line) and tracks line/column positions.
        /// </summary>
        /// <returns>The character that was read.</returns>
        /// <exception cref="InvalidOperationException">Thrown if attempting to read past end of source.</exception>
        public char Read()
        {
            if (_peek is -1)
            {
                throw new InvalidOperationException("End of source reached.");
            }

            // Handle comment: skip from '#' to end of line
            if (_peek is '#')
            {
                _peek = _reader.Read();

                // Skip comment until end of line.
                while (_peek is not -1 && !_peek.IsNewLine())
                {
                    _peek = _reader.Read();
                }

                _peek = '\n'; // Force newline after comment
            }

            // Track line and column positions
            if (_peek.IsNewLine())
            {
                _isLineEmpty = true;
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
                // Line remains empty only if we've seen only whitespace so far
                _isLineEmpty = _isLineEmpty && char.IsWhiteSpace((char)_peek);
            }

            var current = _peek;
            _peek = _reader.Read();
            return (char)current;
        }

        /// <summary>
        /// Reads and validates that the next character matches the expected character.
        /// </summary>
        /// <param name="expected">The character expected to be next.</param>
        /// <exception cref="InvalidOperationException">Thrown if the next character does not match the expected character.</exception>
        public void Skip(char expected)
        {
            if (_peek != expected)
            {
                throw new InvalidOperationException($"Expected character '{expected.ToPrintable()}' but found '{_peek.ToPrintable()}'.");
            }
    
            Read();
        }

        /// <summary>
        /// Attempts to read the next character if it is a digit (0-9).
        /// </summary>
        /// <param name="result">The digit character that was read, or default if not a digit.</param>
        /// <returns>True if a digit was read; otherwise, false.</returns>
        public bool TryReadDigit(out char result)
        {
            if (_peek is -1 || !char.IsDigit((char)_peek))
            {
                result = default;
                return false;
            }

            result = Read();
            return true;
        }

        /// <summary>
        /// Attempts to read the next character if it is a valid identifier character (letter, digit, or underscore).
        /// </summary>
        /// <param name="result">The identifier character that was read, or default if not valid.</param>
        /// <returns>True if an identifier character was read; otherwise, false.</returns>
        public bool TryReadLetterOrDigit(out char result)
        {
            if (!_peek.IsLetterOrDigit())
            {
                result = default;
                return false;
            }

            result = Read();
            return true;
        }

        /// <summary>
        /// Gets the current location in the source code.
        /// </summary>
        /// <returns>A <see cref="Location"/> object representing the current position.</returns>
        public Location GetLocation() => new(_source, _line, _column);

        /// <summary>
        /// Disposes the reader and its underlying resources.
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        /// <summary>
        /// Skips whitespace characters on the current line (spaces and tabs).
        /// </summary>
        public void SkipWhitespace()
        {
            while (_peek.IsWhiteSpace())
            {
                Read();
            }
        }

        /// <summary>
        /// Reads and validates the indentation at the start of a line.
        /// Automatically skips blank lines and comments.
        /// Enforces consistent indentation style (all spaces or all tabs) throughout the file.
        /// </summary>
        /// <param name="fixedIndent">Optional fixed indentation level to read up to.</param>
        /// <returns>The indentation level (number of spaces/tabs).</returns>
        /// <exception cref="InvalidOperationException">Thrown if not at the start of a line.</exception>
        /// <exception cref="SyntaxError">Thrown if mixed indentation styles are detected.</exception>
        public int ReadIndentLevel(int fixedIndent = int.MaxValue)
        {
            if (!IsAtLineStart())
            {
                throw new InvalidOperationException($"Cannot read indentation when not at the start of a line.");
            }

            while (CanRead() && _column <= fixedIndent)
            {
                var charValue = (char)Peek();

                // Skip blank lines and comments
                if (charValue.IsNewLine() || charValue is '#')
                {
                    Read();
                    continue;
                }

                // Stop at first non-whitespace character
                if (charValue is not ' ' and not '\t')
                {
                    break;
                }

                // Set indentation style on first indent, then enforce consistency
                if (_indentStyle is '\0')
                {
                    _indentStyle = charValue;
                }
                else if (charValue != _indentStyle)
                {
                    throw new SyntaxError($"Mixed indentation styles detected. Expected '{_indentStyle.ToPrintable()}' but found '{charValue.ToPrintable()}'.", GetLocation());
                }

                Read();
            }

            // Return indentation level (column - 1 since columns are 1-based)
            return _peek is -1 ? 0 : _column - 1;
        }

        /// <summary>
        /// Skips all occurrences of the specified character.
        /// </summary>
        /// <param name="value">The character to skip.</param>
        /// <returns>The number of characters skipped.</returns>
        public int SkipAll(char value)
        {
            var count = 0;

            while (_peek == value)
            {
                Read();
                count++;
            }

            return count;
        }
    }
}