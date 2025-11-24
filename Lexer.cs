using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Lexical analyzer that converts source code into tokens.
    /// </summary>
    internal class Lexer : IDisposable
    {
        /// <summary>
        /// The source code input.
        /// </summary>
        private readonly Source _source;

        /// <summary>
        /// The TextReader for reading characters from the source.
        /// </summary>
        private readonly TextReader _reader;

        /// <summary>
        /// The buffer to accumulate characters for the current token.
        /// </summary>
        private readonly StringBuilder _buffer = new();

        /// <summary>
        /// The current character being processed. If -1, end of file is reached.
        /// </summary>
        private int _current;

        /// <summary>
        /// The current line number (1-based).
        /// </summary>
        private int _line;

        /// <summary>
        /// The current column number (1-based).
        /// </summary>
        private int _column;

        /// <summary>
        /// Stack to keep track of indentation levels.
        /// </summary>
        private readonly Stack<int> _indents = new();

        /// <summary>
        /// Defines the indentation style: ' ' for spaces, '\t' for tabs, or '\0' if undefined.
        /// </summary>  
        private char _indentStyle;

        /// <summary>
        /// The current lexer state.
        /// </summary>
        private LexerState _state;

        /// <summary>
        /// Gets the current location in the source code.
        /// </summary>
        private Location CurrentLocation => new(_source, _line, _column);

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexer"/> class.
        /// </summary>
        public Lexer(Source source)
        {
            _source = source;
            _reader = _source.CreateReader();
            _line = 1;
            _column = 1;
            _current = _reader.Read();
            _indents.Clear();
            _indents.Push(0);
            _indentStyle = '\0';
            _state = LexerState.AtLineStart;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        /// <summary>
        /// Tokenizes the source code one token at a time (streaming).
        /// </summary>
        public IEnumerable<Token> Tokenize()
        {
            while (!_current.IsEndOfSource())
            {
                if (_state is LexerState.AtLineStart)
                {
                    var (tokenType, count) = ReadIndentation();

                    if (_current.IsEndOfSource())
                    {
                        break;
                    }

                    var identToken = tokenType switch
                    {
                        TokenType.Indent => Token.Indent(CurrentLocation),
                        TokenType.Dedent => Token.Dedent(CurrentLocation),
                        _ => throw new InvalidOperationException("Unexpected token type from ReadIndentation")
                    };

                    for (int i = 0; i < count; i++)
                    {
                        yield return identToken;
                    }

                    _state = LexerState.Default;
                }

                switch (_state)
                {
                    case LexerState.Default:
                    case LexerState.ReadingInlineExpression:
                        SkipEmptyChars();
                        yield return ReadCharacter((char)_current);
                        break;
                    case LexerState.ReadingString:
                        yield return ReadString();
                        break;
                    case LexerState.AtLineStart:
                        throw new Exception("Cannot reach here");
                    default:
                        throw new NotSupportedException("Unknown lexer state: " + _state);
                }
            }

            if (_state is not LexerState.AtLineStart)
            {
                yield return new Token(TokenType.Newline, string.Empty, CurrentLocation);
            }

            // Generate Dedent tokens for all remaining indentation levels.
            while (_indents.Count > 1)
            {
                _indents.Pop();
                yield return Token.Dedent(CurrentLocation);
            }

            yield return new Token(TokenType.EndOfSource, string.Empty, CurrentLocation);
        }

        /// <summary>
        /// Reads the indentation level and returns Indent/Dedent tokens as needed.
        /// </summary>
        private (TokenType tokenType, int count) ReadIndentation()
        {
            int identLevel = ReadIndentationLevel();
            var currentLevel = _indents.Peek();

            if (identLevel > currentLevel)
            {
                _indents.Push(identLevel);
                return (TokenType.Indent, 1);
            }

            var count = 0;

            while (identLevel < currentLevel)
            {
                _indents.Pop();
                currentLevel = _indents.Peek();
                count++;
            }

            if (identLevel != currentLevel)
            {
                throw new SyntaxError("Inconsistent indentation", CurrentLocation);
            }

            return (TokenType.Dedent, count);
        }

        /// <summary>
        /// Reads the indentation level (number of spaces/tabs) at the start of a line.
        /// </summary>
        private int ReadIndentationLevel()
        {
            if (_state is not LexerState.AtLineStart)
            {
                throw new InvalidOperationException($"{nameof(ReadIndentationLevel)} called when not at the start of a line");
            }

            int identLevel = 0;

            while (!_current.IsEndOfSource())
            {
                var charValue = (char)_current;

                if (charValue is '#')
                {
                    SkipComment();
                    continue;
                }

                if (charValue.IsNewLine())
                {
                    identLevel = 0;
                }
                else if (charValue is ' ' or '\t')
                {
                    if (_indentStyle is '\0')
                    {
                        _indentStyle = charValue;
                    }
                    else if (charValue != _indentStyle)
                    {
                        throw new SyntaxError("Inconsistent use of tabs and spaces for indentation", CurrentLocation);
                    }

                    identLevel++;
                }
                else
                {
                    break;
                }

                MoveNext();
            }

            return identLevel;
        }

        /// <summary>
        /// Reads a token starting from the given character.
        /// </summary>
        private Token ReadCharacter(char value)
        {
            AssertStates(LexerState.Default, LexerState.ReadingInlineExpression, "Cannot read character");

            return value switch
            {
                // Newline (statement terminator)
                '\n' or '\r' or '\u2028' or '\u2029' or '\u0085' => ReadNewline(),

                // String literal
                '"' when _state is LexerState.Default => ReadStringStart(),
                '"' when _state is LexerState.ReadingInlineExpression => ReadInlineString(),

                // Integer number
                >= '0' and <= '9' => ReadNumber(),

                // Identifier or keyword (variable name, true, false)
                >= 'a' and <= 'z' => ReadIdentifierOrKeyword(),
                >= 'A' and <= 'Z' => ReadIdentifierOrKeyword(),
                '_' => ReadIdentifierOrKeyword(),

                // Operators
                '=' => ReadFromEqualsSign(),
                '<' => ReadFromLessThanSign(),
                '>' => ReadFromGreaterThanSign(),
                '!' => ReadFromExclamationMark(),
                '+' => ReadSingleToken(TokenType.Plus, "+"),
                '-' => ReadSingleToken(TokenType.Minus, "-"),
                '*' => ReadSingleToken(TokenType.Multiply, "*"),
                '/' => ReadSingleToken(TokenType.Divide, "/"),
                '%' => ReadSingleToken(TokenType.Modulo, "%"),

                // Delimiters
                '(' => ReadSingleToken(TokenType.LeftParen, "("),
                ')' => ReadSingleToken(TokenType.RightParen, ")"),

                // Expression braces
                '{' when _state is LexerState.ReadingInlineExpression => throw new SyntaxError("Nested '{' in expression is not allowed", CurrentLocation),
                '}' when _state is LexerState.ReadingInlineExpression => FinishExpression(),

                // Unknown character
                _ => throw new SyntaxError("Unexpected symbol", CurrentLocation),
            };
        }

        private Token FinishExpression()
        {
            if (_state is not LexerState.ReadingInlineExpression)
            {
                throw new InvalidOperationException($"Cannot finish expression, state is '{_state}'");
            }

            _state = LexerState.ReadingString;
            return ReadSingleToken(TokenType.InlineExpressionEnd, "}");
        }

        /// <summary>
        /// Reads operators starting with '&lt;': '&lt;&lt;', '&lt;=', or '&lt;'.
        /// </summary>
        private Token ReadFromLessThanSign()
        {
            var startLocation = CurrentLocation;

            Read('<'); // Consume first '<'

            if (_current is '<')
            {
                Read('<'); // Consume second '<'
                return new Token(TokenType.Output, "<<", startLocation | _column);
            }

            if (_current is '=')
            {
                Read('='); // Consume '='
                return new Token(TokenType.LessOrEqual, "<=", startLocation | _column);
            }

            return new Token(TokenType.LessThan, "<", startLocation);
        }

        /// <summary>
        /// Reads operators starting with '&gt;': '&gt;=', or '&gt;'.
        /// </summary>
        private Token ReadFromGreaterThanSign()
        {
            var startLocation = CurrentLocation;

            Read('>'); // Consume first '>'

            if (_current is '=')
            {
                MoveNext(); // Consume '='
                return new Token(TokenType.GreaterOrEqual, ">=", startLocation | _column);
            }

            return new Token(TokenType.GreaterThan, ">", startLocation);
        }

        /// <summary>
        /// Reads operators starting with '=': '==', or '='.
        /// </summary>
        private Token ReadFromEqualsSign()
        {
            var startLocation = CurrentLocation;

            Read('='); // Consume first '='

            if (_current is '=')
            {
                MoveNext(); // Consume second '='
                return new Token(TokenType.Equal, "==", startLocation | _column);
            }

            return new Token(TokenType.Assign, "=", startLocation);
        }

        /// <summary>
        /// Reads operators starting with '!': '!='.
        /// </summary>
        private Token ReadFromExclamationMark()
        {
            var startLocation = CurrentLocation;

            Read('!'); // Consume '!'

            if (_current is '=')
            {
                MoveNext(); // Consume '='
                return new Token(TokenType.NotEqual, "!=", startLocation | _column);
            }

            throw new SyntaxError("Unexpected symbol '!'", startLocation);
        }

        /// <summary>
        /// Creates a single-character token and advances position.
        /// </summary>
        private Token ReadSingleToken(TokenType type, string value)
        {
            var startLocation = CurrentLocation;
            MoveNext();
            return new Token(type, value, startLocation);
        }

        /// <summary>
        /// Reads an integer number from the source.
        /// </summary>
        private Token ReadNumber()
        {
            _buffer.Clear();

            var startLocation = CurrentLocation;
            bool isFloat = false;

            // Read integer part
            ReadDigits();

            // Check for decimal point
            if (_current is '.')
            {
                isFloat = true;
                _buffer.Append('.');
                MoveNext();

                // Read fractional part
                ReadDigits();
            }

            var tokenType = isFloat ? TokenType.Float : TokenType.Integer;
            return new Token(tokenType, _buffer.ToString(), startLocation | _column);

            void ReadDigits()
            {
                while (_current.IsDigit())
                {
                    _buffer.Append((char)_current);
                    MoveNext();
                }
            }
        }

        /// <summary>
        /// Reads an identifier or keyword (variable name, true, false).
        /// </summary>
        private Token ReadIdentifierOrKeyword()
        {
            _buffer.Clear();
            var startLocation = CurrentLocation;

            while (_current.IsIdentifierChar())
            {
                _buffer.Append((char)_current);
                MoveNext();
            }

            var location = startLocation | _column;
            var value = _buffer.ToString();

            // Check for keywords
            return value switch
            {
                "true" => new Token(TokenType.True, "true", location),
                "false" => new Token(TokenType.False, "false", location),
                "and" => new Token(TokenType.And, "and", location),
                "or" => new Token(TokenType.Or, "or", location),
                "not" => new Token(TokenType.Not, "not", location),
                "xor" => new Token(TokenType.Xor, "xor", location),
                "while" => new Token(TokenType.While, "while", location),
                "break" => new Token(TokenType.Break, "break", location),
                "continue" => new Token(TokenType.Continue, "continue", location),
                "if" => new Token(TokenType.If, "if", location),
                "else" => new Token(TokenType.Else, "else", location),
                _ => new Token(TokenType.Identifier, value, location)
            };
        }

        /// <summary>
        /// Starts reading from the '"' character.
        /// </summary>
        private Token ReadStringStart()
        {
            AssertState(LexerState.Default, "Cannot start string");
            _state = LexerState.ReadingString;
            return ReadSingleToken(TokenType.StringStart, "\"");
        }

        /// <summary>
        /// Reads a simple string inside string expression. It includes opening and closing quotes.
        /// Simple strings ignore inline expressions.
        /// </summary>
        /// <returns>A token representing the string value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the lexer state is not ReadingInlineExpression.</exception>
        /// <exception cref="SyntaxError">Thrown if the string is not properly closed.</exception>
        private Token ReadInlineString()
        {
            AssertState(LexerState.ReadingInlineExpression, "Cannot read string");

            var startLocation = CurrentLocation;

            Read('"'); // Expect opening quote

            _buffer.Clear();

            while (!_current.IsEndOfSource() && !_current.IsNewLine())
            {
                var charValue = (char)_current;
                switch (charValue)
                {
                    case '\\':
                        _buffer.Append(ReadEscapeCharacter());
                        break;
                    case '"':
                        Read('"'); // Consume closing quotes
                        return new Token(TokenType.InlineString, _buffer.ToString(), startLocation | _column);
                    default:
                        _buffer.Append(charValue);
                        MoveNext();
                        break;
                }
            }

            throw new SyntaxError("The string is not closed", CurrentLocation);
        }

        /// <summary>
        /// Reads a string literal inside quotes, and excludes them. The string ends when a quote or the start of an
        /// expression inside the string is encountered.
        /// </summary>
        /// <returns>A token representing the string value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the lexer state is not ReadingString.</exception>
        /// <exception cref="SyntaxError">Thrown if the string is not properly closed.</exception>
        private Token ReadString()
        {
            AssertState(LexerState.ReadingString, "Cannot read string");

            var startLocation = CurrentLocation;
            _buffer.Clear();

            while (!_current.IsEndOfSource() && !_current.IsNewLine())
            {
                var charValue = (char)_current;
                switch (charValue)
                {
                    case '\\':
                        _buffer.Append(ReadEscapeCharacter());
                        break;
                    case '{' when _buffer.Length > 0:
                    case '"' when _buffer.Length > 0:
                        // Return the accumulated string before handling special character.
                        return new Token(TokenType.InlineString, _buffer.ToString(), startLocation | _column);
                    case '{' when _buffer.Length is 0:
                        _state = LexerState.ReadingInlineExpression; // Switch to expression reading state
                        return ReadSingleToken(TokenType.InlineExpressionStart, "{");
                    case '"' when _buffer.Length is 0:
                        _state = LexerState.Default; // Finish string readings
                        return ReadSingleToken(TokenType.StringEnd, "\"");
                    default:
                        _buffer.Append(charValue);
                        MoveNext();
                        break;
                }
            }

            throw new SyntaxError("The string is not closed", CurrentLocation);
        }

        /// <summary>
        /// Reads a newline token and updates the lexer state.
        /// </summary>
        private char ReadEscapeCharacter()
        {
            var startLocation = CurrentLocation;
            Read('\\'); // Skip the backslash

            if (_current is -1 || _current.IsNewLine())
            {
                throw new SyntaxError("The string is not closed", startLocation | _column);
            }

            var value = (char)_current;
            var escapeChar = value switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '\\' => '\\',
                '{' => '{',
                '}' => '}',
                _ => throw new SyntaxError($"Invalid escape sequence: \\{value}", startLocation | _column)
            };

            MoveNext();
            return escapeChar;
        }

        /// <summary>
        /// Reads a newline token.
        /// </summary>
        private Token ReadNewline()
        {
            if (!_current.IsNewLine())
            {
                throw new InvalidOperationException("End of file reached while trying to read newline");
            }

            return ReadSingleToken(TokenType.Newline, string.Empty);
        }

        /// <summary>
        /// Skips comment characters until the end of the line.
        /// </summary>
        private void SkipComment()
        {
            if (_current is not '#')
            {
                return;
            }

            MoveNext();

            while (!_current.IsNewLine())
            {
                MoveNext();
            }
        }

        /// <summary>
        /// Advances to the next character in the source.
        /// </summary>
        private void MoveNext()
        {
            if (_current.IsNewLine())
            {
                _line++;
                _column = 1;
                _state = LexerState.AtLineStart;
            }
            else
            {
                _column++;
            }

            _current = _reader.Read();
        }

        /// <summary>
        /// Reads the next character with a validation check.
        /// </summary>
        /// <param name="value">The expected character.</param>
        /// <exception cref="InvalidOperationException">Thrown if the current character does not match the expected character.</exception>
        private void Read(char value)
        {
            if (_current != value)
            {
                throw new InvalidOperationException($"Expected character '{value}' but found '{(char)_current}'");
            }

            MoveNext();
        }

        /// <summary>
        /// Skips whitespace characters except newlines and comments.
        /// </summary>
        private void SkipEmptyChars()
        {
            while (_current.IsWhiteSpace())
            {
                MoveNext();
            }

            SkipComment();
        }

        /// <summary>
        /// Asserts that the lexer is in the expected state.
        /// </summary>
        /// <param name="expected">The expected lexer state.</param>
        /// <param name="message">The message to include in the exception if the state does not match.</param>
        /// <exception cref="InvalidOperationException">Thrown if the lexer state does not match the expected state.</exception>
        private void AssertState(LexerState expected, string message)
        {
            if (_state != expected)
            {
                throw new InvalidOperationException($"{message}, state is '{_state}'");
            }
        }

        /// <summary>
        /// Asserts that the lexer is in one of the expected states.
        /// </summary>
        /// <param name="expected1">The first expected lexer state.</param>
        /// <param name="expected2">The second expected lexer state.</param>
        /// <param name="message">The message to include in the exception if the state does not match.</param>
        /// <exception cref="InvalidOperationException">Thrown if the lexer state does not match either of the expected states.</exception>s]
        private void AssertStates(LexerState expected1, LexerState expected2, string message)
        {
            if (_state != expected1 && _state != expected2)
            {
                throw new InvalidOperationException($"{message}, state is '{_state}'");
            }
        }
    }
}
