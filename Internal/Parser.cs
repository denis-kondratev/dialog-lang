using System.Collections.Generic;

namespace DialogLang
{
    /// <summary>
    /// Parses tokens into an Abstract Syntax Tree.
    /// </summary>
    internal class Parser
    {
        private readonly Lexer _lexer;
        private Token _currentToken;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _currentToken = _lexer.GetNextToken();
        }

        /// <summary>
        /// Consumes the current token if it matches the expected type.
        /// </summary>
        private void Eat(TokenType tokenType)
        {
            if (_currentToken.Type == tokenType)
            {
                _currentToken = _lexer.GetNextToken();
            }
            else
            {
                throw new ParserException(
                    $"Expected {tokenType}, got {_currentToken.Type}", 
                    _currentToken.Line, 
                    _currentToken.Column);
            }
        }

        /// <summary>
        /// Skips newline tokens.
        /// </summary>
        private void SkipNewLines()
        {
            while (_currentToken.Type == TokenType.NewLine)
            {
                Eat(TokenType.NewLine);
            }
        }

        /// <summary>
        /// Parses a factor (number, variable, or parenthesized expression).
        /// </summary>
        private AstNode Factor()
        {
            Token token = _currentToken;

            if (token.Type == TokenType.Not)
            {
                Eat(TokenType.Not);
                return new UnaryOpNode(TokenType.Not, Factor());
            }
            else if (token.Type == TokenType.Number)
            {
                Eat(TokenType.Number);
                if (token.Value == null)
                {
                    throw new ParserException("Number token must have a value", token.Line, token.Column);
                }
                return new NumberNode(token.Value);
            }
            else if (token.Type == TokenType.String)
            {
                Eat(TokenType.String);
                if (token.Value is not string stringValue)
                {
                    throw new ParserException("String token must have a string value", token.Line, token.Column);
                }
                return new StringNode(stringValue);
            }
            else if (token.Type == TokenType.Boolean)
            {
                Eat(TokenType.Boolean);
                if (token.Value is not bool boolValue)
                {
                    throw new ParserException("Boolean token must have a boolean value", token.Line, token.Column);
                }
                return new BooleanNode(boolValue);
            }
            else if (token.Type == TokenType.LeftParen)
            {
                Eat(TokenType.LeftParen);
                AstNode node = Expression();
                Eat(TokenType.RightParen);
                return node;
            }
            else if (token.Type == TokenType.Identifier)
            {
                if (token.Value is not string name)
                {
                    throw new ParserException("Identifier token must have a string value", token.Line, token.Column);
                }
                
                Eat(TokenType.Identifier);

                if (_currentToken.Type == TokenType.LeftParen)
                {
                    return ParseFunctionCall(name);
                }

                return new VariableNode(name);
            }

            throw new ParserException($"Unexpected token: {token.Type}", token.Line, token.Column);
        }

        /// <summary>
        /// Parses a term (multiplication and division).
        /// </summary>
        private AstNode Term()
        {
            AstNode node = Factor();

            while (_currentToken.Type == TokenType.Multiply || _currentToken.Type == TokenType.Divide)
            {
                Token token = _currentToken;
                if (token.Type == TokenType.Multiply)
                {
                    Eat(TokenType.Multiply);
                }
                else if (token.Type == TokenType.Divide)
                {
                    Eat(TokenType.Divide);
                }

                node = new BinaryOpNode(node, token.Type, Factor());
            }

            return node;
        }

        /// <summary>
        /// Parses an arithmetic expression (addition and subtraction).
        /// </summary>
        private AstNode ArithmeticExpression()
        {
            AstNode node = Term();

            while (_currentToken.Type == TokenType.Plus || _currentToken.Type == TokenType.Minus)
            {
                Token token = _currentToken;
                if (token.Type == TokenType.Plus)
                {
                    Eat(TokenType.Plus);
                }
                else if (token.Type == TokenType.Minus)
                {
                    Eat(TokenType.Minus);
                }

                node = new BinaryOpNode(node, token.Type, Term());
            }

            return node;
        }

        /// <summary>
        /// Parses a comparison expression.
        /// </summary>
        private AstNode ComparisonExpression()
        {
            AstNode node = ArithmeticExpression();

            while (_currentToken.Type == TokenType.Equal ||
                   _currentToken.Type == TokenType.NotEqual ||
                   _currentToken.Type == TokenType.Greater ||
                   _currentToken.Type == TokenType.GreaterOrEqual ||
                   _currentToken.Type == TokenType.Less ||
                   _currentToken.Type == TokenType.LessOrEqual)
            {
                Token token = _currentToken;
                Eat(token.Type);
                node = new BinaryOpNode(node, token.Type, ArithmeticExpression());
            }

            return node;
        }

        /// <summary>
        /// Parses a logical AND expression.
        /// </summary>
        private AstNode AndExpression()
        {
            AstNode node = ComparisonExpression();

            while (_currentToken.Type == TokenType.And)
            {
                Eat(TokenType.And);
                node = new BinaryOpNode(node, TokenType.And, ComparisonExpression());
            }

            return node;
        }

        /// <summary>
        /// Parses a logical OR expression.
        /// </summary>
        private AstNode OrExpression()
        {
            AstNode node = AndExpression();

            while (_currentToken.Type == TokenType.Or)
            {
                Eat(TokenType.Or);
                node = new BinaryOpNode(node, TokenType.Or, AndExpression());
            }

            return node;
        }

        /// <summary>
        /// Parses an expression.
        /// </summary>
        private AstNode Expression()
        {
            return OrExpression();
        }

        /// <summary>
        /// Parses a function call.
        /// </summary>
        private AstNode ParseFunctionCall(string functionName)
        {
            Eat(TokenType.LeftParen);

            var arguments = new List<AstNode>();

            if (_currentToken.Type != TokenType.RightParen)
            {
                arguments.Add(Expression());

                while (_currentToken.Type == TokenType.Plus || _currentToken.Type == TokenType.Minus)
                {
                    Token token = _currentToken;
                    Eat(token.Type);
                    arguments.Add(new BinaryOpNode(arguments[arguments.Count - 1], token.Type, Expression()));
                    arguments.RemoveAt(arguments.Count - 2);
                }
            }

            Eat(TokenType.RightParen);

            return new FunctionCallNode(functionName, arguments);
        }

        /// <summary>
        /// Parses a block of statements enclosed by INDENT/DEDENT tokens.
        /// </summary>
        private AstNode ParseBlock()
        {
            if (_currentToken.Type == TokenType.Indent)
            {
                Eat(TokenType.Indent);
                var statements = new List<AstNode>();
                
                while (_currentToken.Type != TokenType.Dedent && _currentToken.Type != TokenType.EOF)
                {
                    statements.Add(Statement());
                    SkipNewLines();
                }
                
                if (_currentToken.Type == TokenType.Dedent)
                {
                    Eat(TokenType.Dedent);
                }
                
                return new BlockNode(statements);
            }
            else
            {
                // Single statement without indent.
                return Statement();
            }
        }

        /// <summary>
        /// Parses an if statement.
        /// </summary>
        private AstNode ParseIf()
        {
            Eat(TokenType.If);
            AstNode condition = Expression();
            Eat(TokenType.Colon);
            SkipNewLines();

            AstNode thenBody = ParseBlock();

            SkipNewLines();

            AstNode? elseBody = null;
            if (_currentToken.Type == TokenType.Else)
            {
                Eat(TokenType.Else);
                Eat(TokenType.Colon);
                SkipNewLines();

                elseBody = ParseBlock();
            }

            return new IfNode(condition, thenBody, elseBody);
        }

        /// <summary>
        /// Parses a statement.
        /// </summary>
        private AstNode Statement()
        {
            SkipNewLines();

            if (_currentToken.Type == TokenType.If)
            {
                return ParseIf();
            }

            if (_currentToken.Type == TokenType.Output)
            {
                Eat(TokenType.Output);
                AstNode expression = Expression();
                return new OutputNode(expression);
            }

            if (_currentToken.Type == TokenType.Input)
            {
                Eat(TokenType.Input);
                
                if (_currentToken.Type != TokenType.Identifier)
                {
                    throw new ParserException("Expected identifier after input operator", _currentToken.Line, _currentToken.Column);
                }
                
                if (_currentToken.Value is not string variableName)
                {
                    throw new ParserException("Identifier token must have a string value", _currentToken.Line, _currentToken.Column);
                }
                
                Eat(TokenType.Identifier);
                
                // Check for optional type specification: as <type>
                InputType inputType = InputType.Any;
                if (_currentToken.Type == TokenType.As)
                {
                    Eat(TokenType.As);
                    
                    if (_currentToken.Type != TokenType.Identifier)
                    {
                        throw new ParserException("Expected type name after 'as' keyword", _currentToken.Line, _currentToken.Column);
                    }
                    
                    if (_currentToken.Value is not string typeName)
                    {
                        throw new ParserException("Type name must be a string", _currentToken.Line, _currentToken.Column);
                    }
                    
                    inputType = typeName.ToLowerInvariant() switch
                    {
                        "number" => InputType.Number,
                        "string" => InputType.String,
                        "bool" => InputType.Bool,
                        _ => throw new ParserException($"Unknown type '{typeName}'. Expected: number, string, or bool", _currentToken.Line, _currentToken.Column)
                    };
                    
                    Eat(TokenType.Identifier);
                }
                
                return new InputNode(variableName, inputType);
            }

            if (_currentToken.Type == TokenType.Identifier)
            {
                if (_currentToken.Value is not string name)
                {
                    throw new ParserException("Identifier token must have a string value", _currentToken.Line, _currentToken.Column);
                }
                
                Eat(TokenType.Identifier);

                if (_currentToken.Type == TokenType.Assign)
                {
                    Eat(TokenType.Assign);
                    AstNode value = Expression();
                    return new AssignNode(name, value);
                }
                else if (_currentToken.Type == TokenType.LeftParen)
                {
                    return ParseFunctionCall(name);
                }
                else
                {
                    throw new ParserException($"Unexpected token after identifier: {_currentToken.Type}", _currentToken.Line, _currentToken.Column);
                }
            }

            return Expression();
        }

        /// <summary>
        /// Parses the entire program.
        /// </summary>
        public ProgramNode Parse()
        {
            var statements = new List<AstNode>();

            SkipNewLines();

            while (_currentToken.Type != TokenType.EOF)
            {
                statements.Add(Statement());
                SkipNewLines();
            }

            return new ProgramNode(statements);
        }
    }
}
