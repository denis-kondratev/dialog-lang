using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DialogLang
{
    /// <summary>
    /// Tokenizes source code into a sequence of tokens.
    /// </summary>
    internal class Lexer
    {
        private readonly TextReader _reader;
        private char _currentChar;
        private char _nextChar;
        private int _line;
        private int _column;
        private readonly Stack<int> _indentStack;
        private readonly Queue<Token> _tokenQueue;
        private bool _atLineStart;

        /// <summary>
        /// Creates a lexer from a string.
        /// </summary>
        public Lexer(string text) : this(new StringReader(text))
        {
        }

        /// <summary>
        /// Creates a lexer from a TextReader.
        /// </summary>
        public Lexer(TextReader reader)
        {
            _reader = reader;
            _line = 1;
            _column = 1;
            _indentStack = new Stack<int>();
            _indentStack.Push(0);
            _tokenQueue = new Queue<Token>();
            _atLineStart = true;
            
            int first = _reader.Read();
            int second = _reader.Read();
            
            _currentChar = first == -1 ? '\0' : (char)first;
            _nextChar = second == -1 ? '\0' : (char)second;
        }

        /// <summary>
        /// Advances to the next character.
        /// </summary>
        private void Advance()
        {
            if (_currentChar == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _currentChar = _nextChar;
            
            int next = _reader.Read();
            _nextChar = next == -1 ? '\0' : (char)next;
        }

        /// <summary>
        /// Peeks at the next character without advancing.
        /// </summary>
        private char Peek()
        {
            return _nextChar;
        }

        /// <summary>
        /// Skips whitespace characters except newlines.
        /// </summary>
        private void SkipWhitespace()
        {
            while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar) && _currentChar != '\n')
            {
                Advance();
            }
        }

        /// <summary>
        /// Skips a comment until the end of the line.
        /// </summary>
        private void SkipComment()
        {
            while (_currentChar != '\0' && _currentChar != '\n')
            {
                Advance();
            }
        }

        /// <summary>
        /// Reads a number from the current position.
        /// </summary>
        private Token Number()
        {
            int startLine = _line;
            int startColumn = _column;
            var result = new StringBuilder();
            
            while (_currentChar != '\0' && (char.IsDigit(_currentChar) || _currentChar == '.'))
            {
                result.Append(_currentChar);
                Advance();
            }

            if (result.ToString().Contains("."))
            {
                return new Token(TokenType.Number, startLine, startColumn, float.Parse(result.ToString()));
            }
            
            return new Token(TokenType.Number, startLine, startColumn, int.Parse(result.ToString()));
        }

        /// <summary>
        /// Reads an identifier or keyword from the current position.
        /// </summary>
        private Token Identifier()
        {
            int startLine = _line;
            int startColumn = _column;
            var result = new StringBuilder();
            
            while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
            {
                result.Append(_currentChar);
                Advance();
            }

            string value = result.ToString();
            
            return value switch
            {
                "if" => new Token(TokenType.If, startLine, startColumn),
                "else" => new Token(TokenType.Else, startLine, startColumn),
                "and" => new Token(TokenType.And, startLine, startColumn),
                "or" => new Token(TokenType.Or, startLine, startColumn),
                "not" => new Token(TokenType.Not, startLine, startColumn),
                "true" => new Token(TokenType.Boolean, startLine, startColumn, true),
                "false" => new Token(TokenType.Boolean, startLine, startColumn, false),
                _ => new Token(TokenType.Identifier, startLine, startColumn, value)
            };
        }

        /// <summary>
        /// Reads a string literal, checking for triple-quoted strings.
        /// </summary>
        private Token StringLiteral(char quoteChar)
        {
            int startLine = _line;
            int startColumn = _column;
            
            // Check for triple-quoted string (""").
            if (quoteChar == '"' && Peek() == '"')
            {
                Advance(); // Move to second ".
                if (Peek() == '"')
                {
                    Advance(); // Move to third ".
                    return TripleQuotedString(startLine, startColumn);
                }
                // Only two quotes - this is an empty string followed by another quote.
                // Return empty string, next call will handle the second quote.
                Advance(); // Skip second quote.
                return new Token(TokenType.String, startLine, startColumn, string.Empty);
            }
            
            var result = new StringBuilder();
            Advance(); // Skip opening quote.
            
            while (_currentChar != '\0' && _currentChar != quoteChar)
            {
                if (_currentChar == '\\')
                {
                    Advance();
                    if (_currentChar != '\0')
                    {
                        // Handle escape sequences.
                        result.Append(_currentChar switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            '\\' => '\\',
                            '"' => '"',
                            '\'' => '\'',
                            _ => _currentChar
                        });
                        Advance();
                    }
                }
                else
                {
                    result.Append(_currentChar);
                    Advance();
                }
            }
            
            if (_currentChar == '\0')
            {
                throw new LexerException($"Unterminated string literal", startLine, startColumn);
            }
            
            Advance(); // Skip closing quote.
            return new Token(TokenType.String, startLine, startColumn, result.ToString());
        }

        /// <summary>
        /// Reads a triple-quoted string literal.
        /// </summary>
        private Token TripleQuotedString(int startLine, int startColumn)
        {
            var result = new StringBuilder();
            
            // Already at third opening quote.
            Advance(); // Skip third opening quote.
            
            while (_currentChar != '\0')
            {
                if (_currentChar == '"' && Peek() == '"')
                {
                    Advance(); // Move to second ".
                    if (Peek() == '"')
                    {
                        Advance(); // Move to third ".
                        Advance(); // Skip closing """.
                        return new Token(TokenType.String, startLine, startColumn, result.ToString());
                    }
                    // Only two quotes, add first and continue.
                    result.Append('"');
                }
                else
                {
                    result.Append(_currentChar);
                    Advance();
                }
            }
            
            throw new LexerException($"Unterminated triple-quoted string", startLine, startColumn);
        }

        /// <summary>
        /// Reads an operator token.
        /// </summary>
        private Token ScanOperator()
        {
            int line = _line;
            int column = _column;
            char ch = _currentChar;
            Advance();

            switch (ch)
            {
                case '+':
                    return new Token(TokenType.Plus, line, column);
                case '-':
                    return new Token(TokenType.Minus, line, column);
                case '*':
                    return new Token(TokenType.Multiply, line, column);
                case '/':
                    return new Token(TokenType.Divide, line, column);
                case ':':
                    return new Token(TokenType.Colon, line, column);
                case '(':
                    return new Token(TokenType.LeftParen, line, column);
                case ')':
                    return new Token(TokenType.RightParen, line, column);
                case '=':
                    if (_currentChar == '=')
                    {
                        Advance();
                        return new Token(TokenType.Equal, line, column);
                    }
                    return new Token(TokenType.Assign, line, column);
                case '!':
                    if (_currentChar == '=')
                    {
                        Advance();
                        return new Token(TokenType.NotEqual, line, column);
                    }
                    throw new LexerException("Invalid character '!'", line, column);
                case '<':
                    if (_currentChar == '<')
                    {
                        Advance();
                        return new Token(TokenType.Output, line, column);
                    }
                    if (_currentChar == '=')
                    {
                        Advance();
                        return new Token(TokenType.LessOrEqual, line, column);
                    }
                    return new Token(TokenType.Less, line, column);
                case '>':
                    if (_currentChar == '>')
                    {
                        Advance();
                        return new Token(TokenType.Input, line, column);
                    }
                    if (_currentChar == '=')
                    {
                        Advance();
                        return new Token(TokenType.GreaterOrEqual, line, column);
                    }
                    return new Token(TokenType.Greater, line, column);
                default:
                    throw new LexerException($"Invalid character '{ch}'", line, column);
            }
        }

        /// <summary>
        /// Gets the next token from the input.
        /// </summary>
        public Token GetNextToken()
        {
            // Return queued tokens first.
            if (_tokenQueue.Count > 0)
            {
                return _tokenQueue.Dequeue();
            }

            while (_currentChar != '\0')
            {
                // Handle indentation at the start of a line.
                if (_atLineStart && _currentChar != '\n' && _currentChar != '\0')
                {
                    _atLineStart = false;
                    int indentLevel = 0;
                    
                    while (_currentChar == ' ' || _currentChar == '\t')
                    {
                        indentLevel += _currentChar == '\t' ? 4 : 1;
                        Advance();
                    }
                    
                    // Skip empty lines and comments.
                    if (_currentChar == '\n' || _currentChar == '#')
                    {
                        if (_currentChar == '#')
                        {
                            SkipComment();
                        }
                        continue;
                    }
                    
                    int currentIndent = _indentStack.Peek();
                    
                    if (indentLevel > currentIndent)
                    {
                        _indentStack.Push(indentLevel);
                        return new Token(TokenType.Indent, _line, _column);
                    }
                    else if (indentLevel < currentIndent)
                    {
                        // Generate DEDENT tokens.
                        while (_indentStack.Count > 1 && _indentStack.Peek() > indentLevel)
                        {
                            _indentStack.Pop();
                            _tokenQueue.Enqueue(new Token(TokenType.Dedent, _line, _column));
                        }
                        
                        if (_indentStack.Peek() != indentLevel)
                        {
                            throw new LexerException("Inconsistent indentation", _line, _column);
                        }
                        
                        return _tokenQueue.Dequeue();
                    }
                }

                if (_currentChar == '#')
                {
                    SkipComment();
                    continue;
                }

                if (_currentChar == ' ' || _currentChar == '\t')
                {
                    SkipWhitespace();
                    continue;
                }

                if (_currentChar == '\n')
                {
                    int line = _line;
                    int column = _column;
                    Advance();
                    _atLineStart = true;
                    return new Token(TokenType.NewLine, line, column);
                }

                if (_currentChar == '"')
                {
                    return StringLiteral('"');
                }

                if (_currentChar == '\'')
                {
                    return StringLiteral('\'');
                }

                if (char.IsDigit(_currentChar))
                {
                    return Number();
                }

                if (char.IsLetter(_currentChar) || _currentChar == '_')
                {
                    return Identifier();
                }

                if (_currentChar == '+' || _currentChar == '-' || _currentChar == '*' || 
                    _currentChar == '/' || _currentChar == '=' || _currentChar == '!' || 
                    _currentChar == '>' || _currentChar == '<' || _currentChar == ':' || 
                    _currentChar == '(' || _currentChar == ')')
                {
                    return ScanOperator();
                }

                throw new LexerException($"Invalid character '{_currentChar}'", _line, _column);
            }
            
            // Generate remaining DEDENT tokens at EOF.
            while (_indentStack.Count > 1)
            {
                _indentStack.Pop();
                _tokenQueue.Enqueue(new Token(TokenType.Dedent, _line, _column));
            }
            
            if (_tokenQueue.Count > 0)
            {
                return _tokenQueue.Dequeue();
            }

            return new Token(TokenType.EOF, _line, _column);
        }
    }
}
