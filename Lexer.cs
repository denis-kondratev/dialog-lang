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
        /// Indicates if we are at the start of a new line (to process indentation).
        /// </summary>
        private bool _atLineStart;

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
            _atLineStart = true;
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
            while (_current.IsChar())
            {
                if (_atLineStart)
                {
                    var (tokenType, count) = ReadIndentation();

                    if (!_current.IsChar())
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

                    _atLineStart = false;
                }

                SkipEmptyChars();
                yield return ReadFromCharacter((char)_current);
            }

            if (!_atLineStart)
            {
                yield return Token.NewLine(CurrentLocation);
            }

            // Generate Dedent tokens for all remaining indentation levels.
            while (_indents.Count > 1)
            {
                _indents.Pop();
                yield return Token.Dedent(CurrentLocation);
            }

            yield return Token.EndOfFile(CurrentLocation);
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
                throw new InvalidSyntaxException("Inconsistent indentation", CurrentLocation);
            }

            return (TokenType.Dedent, count);
        }

        /// <summary>
        /// Reads the indentation level (number of spaces/tabs) at the start of a line.
        /// </summary>
        private int ReadIndentationLevel()
        {
            if (!_atLineStart)
            {
                throw new InvalidOperationException($"{nameof(ReadIndentationLevel)} called when not at the start of a line");
            }

            int identLevel = 0;

            while (_current.IsChar())
            {
                var ch = (char)_current;

                if (ch is '#')
                {
                    SkipComment();
                    continue;
                }

                if (ch.IsNewLine())
                {
                    identLevel = 0;
                }
                else if (ch is ' ' or '\t')
                {
                    if (_indentStyle is '\0')
                    {
                        _indentStyle = ch;
                    }
                    else if (ch != _indentStyle)
                    {
                        throw new InvalidSyntaxException("Inconsistent use of tabs and spaces for indentation", CurrentLocation);
                    }

                    identLevel++;
                }
                else
                {
                    break;
                }

                MoveNextChar();
            }

            return identLevel;
        }

        /// <summary>
        /// Reads a token starting from the given character.
        /// </summary>
        private Token ReadFromCharacter(char value)
        {
            return value switch
            {
                // Newline (statement terminator)
                '\n' or '\r' or '\u2028' or '\u2029' or '\u0085' => ReadNewline(),

                // String literal
                '"' => ReadString(),

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
                '+' => ReadSingleCharToken(TokenType.Plus, "+"),
                '-' => ReadSingleCharToken(TokenType.Minus, "-"),
                '*' => ReadSingleCharToken(TokenType.Multiply, "*"),
                '/' => ReadSingleCharToken(TokenType.Divide, "/"),
                '%' => ReadSingleCharToken(TokenType.Modulo, "%"),

                // Delimiters
                '(' => ReadSingleCharToken(TokenType.LeftParen, "("),
                ')' => ReadSingleCharToken(TokenType.RightParen, ")"),

                // Unknown character
                _ => throw new InvalidSyntaxException("Unexpected symbol", CurrentLocation),
            };
        }

        /// <summary>
        /// Reads operators starting with '<': '<<', '<=', or '<'.
        /// </summary>
        private Token ReadFromLessThanSign()
        {
            var line = _line;
            var initial = _column;

            MoveNextChar(); // consume first '<'

            if (_current is '<')
            {
                MoveNextChar(); // consume second '<'
                return new Token(TokenType.Output, "<<", _source, line, initial, _column);
            }

            if (_current is '=')
            {
                MoveNextChar(); // consume '='
                return new Token(TokenType.LessOrEqual, "<=", _source, line, initial, _column);
            }

            return new Token(TokenType.LessThan, "<", _source, line, initial, _column);
        }

        /// <summary>
        /// Reads operators starting with '>': '>=', or '>'.
        /// </summary>
        private Token ReadFromGreaterThanSign()
        {
            var line = _line;
            var initial = _column;

            MoveNextChar(); // consume '>'

            if (_current is '=')
            {
                MoveNextChar(); // consume '='
                return new Token(TokenType.GreaterOrEqual, ">=", _source, line, initial, _column);
            }

            return new Token(TokenType.GreaterThan, ">", _source, line, initial, _column);
        }

        /// <summary>
        /// Reads operators starting with '=': '==', or '='.
        /// </summary>
        private Token ReadFromEqualsSign()
        {
            var line = _line;
            var initial = _column;

            MoveNextChar(); // consume first '='

            if (_current is '=')
            {
                MoveNextChar(); // consume second '='
                return new Token(TokenType.Equal, "==", _source, line, initial, _column);
            }

            return new Token(TokenType.Assign, "=", _source, line, initial, _column);
        }

        /// <summary>
        /// Reads operators starting with '!': '!='.
        /// </summary>
        private Token ReadFromExclamationMark()
        {
            var line = _line;
            var initial = _column;

            MoveNextChar(); // consume '!'

            if (_current is '=')
            {
                MoveNextChar(); // consume '='
                return new Token(TokenType.NotEqual, "!=", _source, line, initial, _column);
            }

            throw new InvalidSyntaxException("Unexpected symbol '!'", _source, line, initial);
        }

        /// <summary>
        /// Creates a single-character token and advances position.
        /// </summary>
        private Token ReadSingleCharToken(TokenType type, string value)
        {
            var line = _line;
            var initial = _column;
            MoveNextChar();
            return new Token(type, value, _source, line, initial, _column);
        }

        /// <summary>
        /// Reads an integer number from the source.
        /// </summary>
        private Token ReadNumber()
        {
            _buffer.Clear();

            var line = _line;
            var initial = _column;
            bool isFloat = false;

            // Read integer part
            ReadDigits();

            // Check for decimal point
            if (_current == '.')
            {
                isFloat = true;
                _buffer.Append('.');
                MoveNextChar();

                // Read fractional part
                ReadDigits();
            }

            var tokenType = isFloat ? TokenType.Float : TokenType.Integer;
            return new Token(tokenType, _buffer.ToString(), _source, line, initial, _column);

            void ReadDigits()
            {
                while (_current.IsDigit())
                {
                    _buffer.Append((char)_current);
                    MoveNextChar();
                }
            }
        }

        /// <summary>
        /// Reads an identifier or keyword (variable name, true, false).
        /// </summary>
        private Token ReadIdentifierOrKeyword()
        {
            _buffer.Clear();
            var line = _line;
            var initial = _column;

            while (_current.IsIdentifierChar())
            {
                _buffer.Append((char)_current);
                MoveNextChar();
            }

            var location = new Location(_source, line, initial, _column);
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
        /// Reads a string literal from the source (enclosed in double quotes).
        /// </summary>
        private Token ReadString()
        {
            _buffer.Clear();
            var line = _line;
            var initial = _column;

            // Skip opening quote
            MoveNextChar();

            while (_current != -1 && (char)_current != '"')
            {
                if ((char)_current == '\\')
                {
                    // Handle escape sequences
                    MoveNextChar();

                    if (_current == -1)
                    {
                        throw new InvalidSyntaxException(CurrentLocation);
                    }

                    var escapeChar = (char)_current;
                    _buffer.Append(escapeChar switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '"' => '"',
                        _ => throw new InvalidSyntaxException($"Invalid escape sequence: \\{escapeChar}", CurrentLocation)
                    });

                    MoveNextChar();
                }
                else if (_current.IsNewLine())
                {
                    throw new InvalidSyntaxException("String is not closed with a quote", CurrentLocation);
                }
                else
                {
                    _buffer.Append((char)_current);
                    MoveNextChar();
                }
            }

            if (_current == -1)
            {
                throw new InvalidSyntaxException("Unterminated string literal", CurrentLocation);
            }

            // Skip closing quote
            MoveNextChar();

            return new Token(TokenType.String, _buffer.ToString(), _source, line, initial, _column);
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

            MoveNextChar();

            _atLineStart = true; // Next token will handle indentation
            return Token.NewLine(CurrentLocation);
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

            MoveNextChar();

            while (!_current.IsNewLine())
            {
                MoveNextChar();
            }
        }

        /// <summary>
        /// Advances to the next character.
        /// </summary>
        private void MoveNextChar()
        {
            if (_current.IsNewLine())
            {
                _line++;
                _column = 1;
                _atLineStart = true;
            }
            else
            {
                _column++;
            }

            _current = _reader.Read();
        }

        /// <summary>
        /// Skips whitespace characters except newlines and comments.
        /// </summary>
        private void SkipEmptyChars()
        {
            while (_current.IsWhiteSpace())
            {
                MoveNextChar();
            }

            SkipComment();
        }
    }
}
