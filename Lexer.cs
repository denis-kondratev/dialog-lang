using System;
using System.Collections.Generic;
using System.Text;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Lexical analyzer that converts source code into tokens.
    /// </summary>
    internal class Lexer : IDisposable
    {
        /// <summary>
        /// The underlying character reader for the source code.
        /// </summary>
        private readonly Reader _reader;

        /// <summary>
        /// The buffer to accumulate characters for the current token.
        /// </summary>
        private readonly StringBuilder _stringBuilder = new();

        /// <summary>
        /// Stack to keep track of indentation levels.
        /// </summary>
        private readonly Stack<int> _indents = new();

        /// <summary>
        /// The current lexer state.
        /// </summary>
        private LexerState _state = LexerState.Default;

        /// <summary>
        /// Buffer to hold tokens before yielding them.
        /// </summary>
        private readonly Queue<Token> _buffer = new();

        /// <summary>
        /// The current indentation mode.
        /// </summary>
        private IndentMode _indentMode = IndentMode.Default;

        /// <summary>
        /// The number of quotes that started a multi-line string.
        /// </summary>
        private int _multistringQuotes = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexer"/> class.
        /// </summary>
        public Lexer(Source source)
        {
            _reader = new Reader(source);
            _indents.Push(0);
        }

        /// <summary>
        /// Disposes the lexer and its underlying resources.
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        /// <summary>
        /// Tokenizes the source code one token at a time (streaming).
        /// </summary>
        public IEnumerable<Token> Tokenize()
        {
            while (_reader.CanRead())
            {
                if (_reader.IsAtLineStart())
                {
                    ReadIndentation(_buffer);

                    while (_buffer.Count > 0)
                    {
                        yield return _buffer.Dequeue();
                    }

                    _state = LexerState.Default;
                }

                switch (_state)
                {
                    case LexerState.Default:
                    case LexerState.ReadingInlineExpression:
                        _reader.SkipWhitespace();
                        if (_reader.CanRead())
                        {
                            yield return ReadToken();
                        }
                        break;
                    case LexerState.ReadingString:
                        yield return ReadString();
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected lexer state: {_state}");
                }
            }

            if (!_reader.IsLineEmpty())
            {
                yield return new Token(TokenType.Newline, string.Empty, _reader.GetLocation());
            }

            // Generate Dedent tokens for all remaining indentation levels.
            while (_indents.Count > 1)
            {
                _indents.Pop();
                yield return Token.Dedent(_reader.GetLocation());
            }

            yield return new Token(TokenType.EndOfSource, string.Empty, _reader.GetLocation());
        }

        /// <summary>
        /// Reads the indentation level and returns Indent/Dedent tokens as needed.
        /// </summary>
        /// <param name="buffer">The buffer to hold tokens.</param>
        /// <returns>A tuple containing the token type and the count of tokens to emit.</returns>
        private void ReadIndentation(Queue<Token> buffer)
        {
            AssertIndentMode(IndentMode.Default);
            var currentLevel = _indents.Peek();
            int identLevel = _indentMode is IndentMode.Fixed ? _reader.ReadIndentation(currentLevel) : _reader.ReadIndentation();

            if (_indentMode is IndentMode.Fixed && identLevel != currentLevel)
            {
                throw new SyntaxError("Inconsistent indentation", _reader.GetLocation());
            }

            if (identLevel > currentLevel)
            {
                _indents.Push(identLevel);
                buffer.Enqueue(Token.Indent(_reader.GetLocation()));

                if (_indentMode is IndentMode.NeenToFix)
                {
                    _indentMode = IndentMode.Fixed;
                }

                return;
            }
            else if (_indentMode is IndentMode.NeenToFix)
            {
                throw new SyntaxError("Expecting indentation", _reader.GetLocation());
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
                throw new SyntaxError("Inconsistent indentation", _reader.GetLocation());
            }

            if (count > 0 && _reader.CanRead())
            {
                for (int i = 0; i < count; i++)
                {
                    buffer.Enqueue(Token.Dedent(_reader.GetLocation()));
                }
            }
        }

        /// <summary>
        /// Reads the next token from the source code.
        /// </summary>
        private Token ReadToken()
        {
            AssertStates(LexerState.Default, LexerState.ReadingInlineExpression, "Cannot read token");

            return (char)_reader.Peek() switch
            {
                // Newline (statement terminator)
                '#' or '\n' or '\r' or '\u2028' or '\u2029' or '\u0085' => ReadNewline(),

                // String literal
                '"' when _state is LexerState.Default => StartString(),
                '"' when _state is LexerState.ReadingInlineExpression => ReadInlineString(),

                // Integer number
                >= '0' and <= '9' => ReadNumber(),

                // Identifier or keyword (variable name, true, false)
                >= 'a' and <= 'z' => ReadIdentifier(),
                >= 'A' and <= 'Z' => ReadIdentifier(),
                '_' => ReadIdentifier(),

                // Operators
                '=' => ReadFromEqualsSign(),
                '<' => ReadFromLessThanSign(),
                '>' => ReadFromGreaterThanSign(),
                '!' => ReadFromExclamationMark(),
                '+' => ReadSingleToken(TokenType.Plus, '+'),
                '-' => ReadSingleToken(TokenType.Minus, '-'),
                '*' => ReadSingleToken(TokenType.Multiply, '*'),
                '/' => ReadSingleToken(TokenType.Divide, '/'),
                '%' => ReadSingleToken(TokenType.Modulo, '%'),

                // Delimiters
                '(' => ReadSingleToken(TokenType.LeftParen, '('),
                ')' => ReadSingleToken(TokenType.RightParen, ')'),

                // Expression braces
                '{' when _state is LexerState.ReadingInlineExpression => throw new SyntaxError("Nested '{' in expression is not allowed", _reader.GetLocation()),
                '}' when _state is LexerState.ReadingInlineExpression => FinishExpression(),

                // Unknown character
                _ => throw new SyntaxError("Unexpected symbol", _reader.GetLocation()),
            };
        }

        /// <summary>
        /// Finishes reading an inline expression and returns the corresponding token.
        /// </summary>
        private Token FinishExpression()
        {
            AssertState(LexerState.ReadingInlineExpression, "Cannot finish expression");
            _state = LexerState.ReadingString;

            return ReadSingleToken(TokenType.InlineExpressionEnd, '}');
        }

        /// <summary>
        /// Reads operators starting with '&lt;': '&lt;&lt;', '&lt;=', or '&lt;'.
        /// </summary>
        private Token ReadFromLessThanSign()
        {
            var startLocation = _reader.GetLocation();

            _reader.Skip('<'); // Consume first '<'
            
            switch (_reader.Peek())
            {
                case '<':
                    _reader.Read(); // Consume second '<'
                    return new Token(TokenType.Output, "<<", startLocation | _reader);
                case '=':
                    _reader.Read(); // Consume '='
                    return new Token(TokenType.LessOrEqual, "<=", startLocation | _reader);
                default:
                    return new Token(TokenType.LessThan, "<", startLocation);
            }
        }

        /// <summary>
        /// Reads operators starting with '&gt;': '&gt;=', or '&gt;'.
        /// </summary>
        private Token ReadFromGreaterThanSign()
        {
            var startLocation = _reader.GetLocation();

            _reader.Skip('>'); // Consume first '>'

            if (_reader.Peek() is '=')
            {
                _reader.Read(); // Consume '='
                return new Token(TokenType.GreaterOrEqual, ">=", startLocation | _reader);
            }

            return new Token(TokenType.GreaterThan, ">", startLocation);
        }

        /// <summary>
        /// Reads operators starting with '=': '==', or '='.
        /// </summary>
        private Token ReadFromEqualsSign()
        {
            var startLocation = _reader.GetLocation();

            _reader.Skip('='); // Consume first '='

            if (_reader.Peek() is '=')
            {
                _reader.Read(); // Consume second '='
                return new Token(TokenType.Equal, "==", startLocation | _reader);
            }

            return new Token(TokenType.Assign, "=", startLocation);
        }

        /// <summary>
        /// Reads operators starting with '!': '!='.
        /// </summary>
        private Token ReadFromExclamationMark()
        {
            var startLocation = _reader.GetLocation();

            _reader.Skip('!'); // Consume '!'

            if (_reader.Peek() is '=')
            {
                _reader.Read(); // Consume '='
                return new Token(TokenType.NotEqual, "!=", startLocation | _reader);
            }

            throw new SyntaxError("Unexpected symbol '!'", startLocation);
        }

        /// <summary>
        /// Creates a single-character token and advances position.
        /// </summary>
        private Token ReadSingleToken(TokenType type, char value)
        {
            var startLocation = _reader.GetLocation();
            _reader.Skip(value); // Consume the character

            return new Token(type, value.ToPrintable(), startLocation);
        }

        /// <summary>
        /// Reads an integer number from the source.
        /// </summary>
        private Token ReadNumber()
        {
            _stringBuilder.Clear();

            var startLocation = _reader.GetLocation();
            bool isFloat = false;

            // Read integer part.
            while (_reader.TryReadDigit(out var charValue))
            {
                _stringBuilder.Append(charValue);
            }

            // Check for decimal point.
            if (_reader.Peek() is '.')
            {
                _reader.Read(); // Consume '.'
                isFloat = true;
                _stringBuilder.Append('.');

                // Read fractional part.
                while (_reader.TryReadDigit(out var charValue))
                {
                    _stringBuilder.Append(charValue);
                }
            }

            var tokenType = isFloat ? TokenType.Float : TokenType.Integer;

            return new Token(tokenType, _stringBuilder.ToString(), startLocation | _reader);
        }

        /// <summary>
        /// Reads an identifier or keyword (variable name, true, false).
        /// </summary>
        private Token ReadIdentifier()
        {
            _stringBuilder.Clear();
            var startLocation = _reader.GetLocation();

            while (_reader.TryReadLetterOrDigit(out var charValue))
            {
                _stringBuilder.Append(charValue);
            }

            var location = startLocation | _reader;
            var value = _stringBuilder.ToString();

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
        private Token StartString()
        {
            AssertState(LexerState.Default, "Cannot start string");

            var startLocation = _reader.GetLocation();
            var quotes = _reader.SkipAll('"');

            switch (quotes)
            {
                case 1:
                    // Single quote starts a one-line string.
                    _state = LexerState.ReadingString;
                    return new Token(TokenType.StringStart, "\"", startLocation);
                case 2:
                    // Double quotes represent an empty string.
                    return new Token(TokenType.InlineString, string.Empty, startLocation | _reader);
                default:
                    // Triple quotes and more start a multi-line string.
                    _state = LexerState.ReadingMultiString;
                    _indentMode = IndentMode.NeenToFix;
                    _multistringQuotes = quotes;
                    return new Token(TokenType.StringStart, new string('"', quotes), startLocation | _reader);
            }
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

            var startLocation = _reader.GetLocation();

            _reader.Skip('\"'); // Expect opening quote
            _stringBuilder.Clear();

            while (!_reader.IsAtLineEnd())
            {
                var peek = (char)_reader.Peek();
                switch (peek)
                {
                    case '\\':
                        _stringBuilder.Append(ReadEscapeCharacter());
                        break;
                    case '"':
                        _reader.Read(); // Consume closing quote
                        return new Token(TokenType.InlineString, _stringBuilder.ToString(), startLocation | _reader);
                    default:
                        _stringBuilder.Append(_reader.Read());
                        break;
                }
            }

            throw new SyntaxError("The string is not closed", _reader.GetLocation());
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

            var startLocation = _reader.GetLocation();
            _stringBuilder.Clear();

            while (!_reader.IsAtLineEnd())
            {
                var peek = (char)_reader.Peek();

                switch (peek)
                {
                    case '\\':
                        _reader.Read(); // Consume '\'
                        _stringBuilder.Append(ReadEscapeCharacter());
                        break;
                    case '{' when _stringBuilder.Length > 0:
                    case '"' when _stringBuilder.Length > 0:
                        // Return the accumulated string before handling special character.
                        return new Token(TokenType.InlineString, _stringBuilder.ToString(), startLocation | _reader);
                    case '{' when _stringBuilder.Length is 0:
                        _state = LexerState.ReadingInlineExpression; // Switch to expression reading state
                        return ReadSingleToken(TokenType.InlineExpressionStart, '{');
                    case '"' when _stringBuilder.Length is 0:
                        _state = LexerState.Default; // Finish string readings
                        return ReadSingleToken(TokenType.StringEnd, '"');
                    default:
                        _stringBuilder.Append(_reader.Read());
                        break;
                }
            }

            throw new SyntaxError("The string is not closed", _reader.GetLocation());
        }

        /// <summary>
        /// Reads a newline token and updates the lexer state.
        /// </summary>
        private char ReadEscapeCharacter()
        {
            var startLocation = _reader.GetLocation();
            _reader.Skip('\\'); // Consume '\'
            
            if (!_reader.IsAtLineEnd())
            {
                throw new SyntaxError("The string is not closed", _reader.GetLocation());
            }

            var charValue = _reader.Read();
            
            var escapeChar = charValue switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '\\' => '\\',
                '{' => '{',
                '}' => '}',
                _ => throw new SyntaxError($"Invalid escape sequence: '\\{charValue.ToPrintable()}'", startLocation | _reader)
            };

            return escapeChar;
        }

        /// <summary>
        /// Reads a newline token.
        /// </summary>
        private Token ReadNewline()
        {
            if (!_reader.IsAtLineEnd())
            {
                throw new InvalidOperationException($"Cannot read newline token, current character is '{_reader.Peek().ToPrintable()}'");
            }

            var startLocation = _reader.GetLocation();
            _reader.Read();

            return new Token(TokenType.Newline, string.Empty, startLocation);
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

        void AssertIndentMode(IndentMode expected)
        {
            if (_indentMode != expected)
            {
                throw new InvalidOperationException($"Expected indent mode '{expected}', but current mode is '{_indentMode}'");
            }
        }
    }
}
