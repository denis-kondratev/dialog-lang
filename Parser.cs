using System.Collections.Generic;
using System;
using System.Globalization;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Parser that builds an Abstract Syntax Tree from tokens.
    /// </summary>
    internal class Parser
    {
        private readonly IEnumerator<Token> _tokens;
        private Token _current;
        private Token _next;

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = tokens.GetEnumerator();
            _current = Token.Empty();
            _next = Token.Empty();

            // Initialize first two tokens.
            MoveNext();
            MoveNext();
        }

        /// <summary>
        /// Parses tokens and yields statements one by one (streaming).
        /// </summary>
        public IEnumerable<Ast.Statement> Parse()
        {
            while (!_current.IsEndOfFile())
            {
                yield return ParseStatement();
            }
        }

        /// <summary>
        /// Parses a single statement.
        /// </summary>
        private Ast.Statement ParseStatement()
        {
            return _current.Type switch
            {
                TokenType.Identifier => ParseStatementFromIdentifier(),
                TokenType.Output => ParseOutput(),
                TokenType.Indent => ParseBlock(),
                TokenType.While => ParseWhile(),
                TokenType.If => ParseIf(),
                _ => throw new InvalidSyntaxException(_current.Location)
            };
        }

        private Ast.Statement ParseStatementFromIdentifier()
        {
            return _next.Type switch
            {
                TokenType.Assign => ParseAssignment(),
                _ => throw new InvalidSyntaxException(_current.Location)
            };
        }

        /// <summary>
        /// Parses an assignment statement: identifier = expression.
        /// </summary>
        private Ast.Assign ParseAssignment()
        {
            var identifier = ParseIdentifier();
            var startLocation = identifier.Location;

            Consume(TokenType.Assign); // consume '='
            var expression = ParseExpression();
            Consume(TokenType.Newline); // expect end of statement

            return new Ast.Assign(identifier, expression, startLocation | expression.Location);
        }

        /// <summary>
        /// Parses an output statement: << expression.
        /// </summary>
        private Ast.Output ParseOutput()
        {
            var startLocation = _current.Location;

            Consume(TokenType.Output); // consume '<<'
            var expression = ParseExpression();
            Consume(TokenType.Newline); // expect end of statement

            return new Ast.Output(expression, startLocation | expression.Location);
        }

        /// <summary>
        /// Parses a block of indented statements.
        /// </summary>
        private Ast.Block ParseBlock()
        {
            var startLocation = _current.Location;

            Consume(TokenType.Indent); // expect start of block

            var statements = new List<Ast.Statement>();

            while (_current.Type is not TokenType.Dedent)
            {
                statements.Add(ParseStatement());
            }

            Consume(TokenType.Dedent); // expect end of block

            return new Ast.Block(statements, startLocation);
        }

        /// <summary>
        /// Parses a while loop.
        /// </summary>
        private Ast.While ParseWhile()
        {
            var startLocation = _current.Location;

            Consume(TokenType.While); // consume 'while'
            var condition = ParseExpression();

            Consume(TokenType.Newline); // expect newline after condition

            // Expect an indented block (body cannot be empty)
            if (_current.Type is not TokenType.Indent)
            {
                throw new InvalidSyntaxException("While loop has no body", condition.Location.After());
            }

            var body = ParseBlock();

            return new Ast.While(condition.AssertBoolean(), body, startLocation | condition.Location);
        }

        /// <summary>
        /// Parses an if-else statement with optional else if branches.
        /// </summary>
        /// <returns>The parsed if statement.</returns>
        /// <exception cref="InvalidSyntaxException">Thrown when then/else blocks are missing.</exception>
        private Ast.If ParseIf()
        {
            var startLocation = _current.Location;

            // Parse the first 'if' branch
            Consume(TokenType.If); // consume 'if'
            var ifBranch = ParseConditionalBlock();

            // Parse optional 'else if' branches
            var elseIfBranches = new List<Ast.ConditionalBlock>();
            while (_current.Type is TokenType.Else && _next.Type is TokenType.If)
            {
                Consume(TokenType.Else); // consume 'else'
                Consume(TokenType.If);   // consume 'if'
                elseIfBranches.Add(ParseConditionalBlock());
            }

            // Parse optional 'else' block
            Ast.Block? elseBlock = null;
            if (_current.Type is TokenType.Else)
            {
                var elseLocation = _current.Location;
                Consume(TokenType.Else); // consume 'else'
                Consume(TokenType.Newline); // expect newline after 'else'

                // Expect an indented block (else block cannot be empty).
                if (_current.Type is not TokenType.Indent)
                {
                    throw new InvalidSyntaxException("Else clause has no body", elseLocation.After());
                }

                elseBlock = ParseBlock();
            }

            return new Ast.If(ifBranch, elseIfBranches, elseBlock, startLocation | ifBranch.Location);
        }

        /// <summary>
        /// Parses a conditional block (condition + then block).
        /// </summary>
        /// <returns>The parsed conditional block.</returns>
        /// <exception cref="InvalidSyntaxException">Thrown when the then block is missing.</exception>
        private Ast.ConditionalBlock ParseConditionalBlock()
        {
            var startLocation = _current.Location;
            var condition = ParseExpression();
            Consume(TokenType.Newline); // expect newline after condition

            // Expect an indented block (then block cannot be empty).
            if (_current.Type is not TokenType.Indent)
            {
                throw new InvalidSyntaxException("If statement has no body", condition.Location.After());
            }

            var thenBlock = ParseBlock();
            return new Ast.ConditionalBlock(condition.AssertBoolean(), thenBlock, startLocation | condition.Location);
        }

        private Ast.Identifier ParseIdentifier()
        {
            var token = _current;

            Consume(TokenType.Identifier); // consume identifier
            return new Ast.Identifier(token.Value, token.Location);
        }

        /// <summary>
        /// Parses an expression with operator precedence
        /// Precedence (low to high): or, xor, and, comparison (==, !=, <, >, <=, >=), not, primary
        /// </summary>
        private Ast.Expression ParseExpression()
        {
            return ParseOrExpression();
        }

        /// <summary>
        /// Parses 'or' expression (lowest precedence)
        /// </summary>
        private Ast.Expression ParseOrExpression()
        {
            var left = ParseXorExpression();

            while (_current.Type == TokenType.Or)
            {
                MoveNext(); // consume 'or'
                var right = ParseXorExpression();
                left = new Ast.OrOp(left.AssertBoolean(), right.AssertBoolean(), left.Location | right.Location);
            }

            return left;
        }

        /// <summary>
        /// Parses 'xor' expression
        /// </summary>
        private Ast.Expression ParseXorExpression()
        {
            var left = ParseAndExpression();

            while (_current.Type == TokenType.Xor)
            {
                MoveNext(); // consume 'xor'
                var right = ParseAndExpression();
                left = new Ast.XorOp(left.AssertBoolean(), right.AssertBoolean(), left.Location | right.Location);
            }

            return left;
        }

        /// <summary>
        /// Parses 'and' expression
        /// </summary>
        private Ast.Expression ParseAndExpression()
        {
            var left = ParseComparisonExpression();

            while (_current.Type == TokenType.And)
            {
                MoveNext(); // consume 'and'
                var right = ParseComparisonExpression();
                left = new Ast.AndOp(left.AssertBoolean(), right.AssertBoolean(), left.Location | right.Location);
            }

            return left;
        }

        /// <summary>
        /// Parses comparison expression (==, !=, <, >, <=, >=)
        /// </summary>
        private Ast.Expression ParseComparisonExpression()
        {
            var left = ParseNotExpression();

            while (_current.Type is TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterOrEqual
                                 or TokenType.LessOrEqual or TokenType.Equal or TokenType.NotEqual)
            {
                var opType = _current.Type;
                MoveNext(); // consume comparison operator
                var right = ParseNotExpression();
                var position = left.Location | right.Location;

                left = opType switch
                {
                    TokenType.GreaterThan => new Ast.GreaterThanOp(left, right, position),
                    TokenType.LessThan => new Ast.LessThanOp(left, right, position),
                    TokenType.GreaterOrEqual => new Ast.GreaterOrEqualOp(left, right, position),
                    TokenType.LessOrEqual => new Ast.LessOrEqualOp(left, right, position),
                    TokenType.Equal => new Ast.EqualOp(left, right, position),
                    TokenType.NotEqual => new Ast.NotEqualOp(left, right, position),
                    _ => throw new NotSupportedException($"Unexpected comparison operator: {opType}")
                };
            }

            return left;
        }

        /// <summary>
        /// Parses additive expression (+ and -)
        /// </summary>
        private Ast.Expression ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();

            while (_current.Type is TokenType.Plus or TokenType.Minus)
            {
                var opType = _current.Type;
                MoveNext(); // consume operator
                var right = ParseMultiplicativeExpression();
                var location = left.Location | right.Location;

                left = opType switch
                {
                    TokenType.Plus => new Ast.AddOp(left, right, location),
                    TokenType.Minus => new Ast.SubOp(left, right, location),
                    _ => throw new NotSupportedException($"Unexpected operator: {opType}")
                };
            }

            return left;
        }

        /// <summary>
        /// Parses multiplicative expression (*, /, %)
        /// </summary>
        private Ast.Expression ParseMultiplicativeExpression()
        {
            var left = ParseUnaryExpression();

            while (_current.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo)
            {
                var opType = _current.Type;
                MoveNext(); // consume operator
                var right = ParseUnaryExpression();
                var location = left.Location | right.Location;

                left = opType switch
                {
                    TokenType.Multiply => new Ast.MulOp(left, right, location),
                    TokenType.Divide => new Ast.DivOp(left, right, location),
                    TokenType.Modulo => new Ast.ModOp(left, right, location),
                    _ => throw new NotSupportedException($"Unexpected operator: {opType}")
                };
            }

            return left;
        }

        /// <summary>
        /// Parses unary expression (- and primary)
        /// </summary>
        private Ast.Expression ParseUnaryExpression()
        {
            if (_current.Type is TokenType.Minus)
            {
                var startLocation = _current.Location;
                MoveNext(); // consume '-'
                var operand = ParseUnaryExpression(); // right-associative
                return new Ast.NegateOp(operand, startLocation | operand.Location);
            }

            return ParsePrimaryExpression();
        }

        /// <summary>
        /// Parses 'not' expression (unary operator)
        /// </summary>
        private Ast.Expression ParseNotExpression()
        {
            if (_current.Type is TokenType.Not)
            {
                var startLocation = _current.Location;
                MoveNext(); // consume 'not'
                var operand = ParseNotExpression(); // right-associative
                return new Ast.NotOp(operand.AssertBoolean(), startLocation | operand.Location);
            }

            return ParseAdditiveExpression();
        }

        /// <summary>
        /// Parses primary expression (literals, variables, parenthesized expressions)
        /// </summary>
        private Ast.Expression ParsePrimaryExpression()
        {
            var token = _current;
            MoveNext();

            return token.Type switch
            {
                TokenType.Integer => new Ast.Integer(int.Parse(token.Value, CultureInfo.InvariantCulture), token.Location),
                TokenType.Float => new Ast.Float(float.Parse(token.Value, CultureInfo.InvariantCulture), token.Location),
                TokenType.String => new Ast.String(token.Value, token.Location),
                TokenType.True => new Ast.Boolean(true, token.Location),
                TokenType.False => new Ast.Boolean(false, token.Location),
                TokenType.Identifier => new Ast.Variable(token.Value, token.Location),
                TokenType.LeftParen => ParseParenthesizedExpression(),
                _ => throw new InvalidSyntaxException(token.Location)
            };
        }

        /// <summary>
        /// Parses a parenthesized expression: (expression)
        /// </summary>
        private Ast.Expression ParseParenthesizedExpression()
        {
            var expression = ParseExpression();

            if (_current.Type is not TokenType.RightParen)
            {
                throw new InvalidSyntaxException(_current.Location);
            }

            MoveNext(); // consume ')'
            return expression;
        }

        /// <summary>
        /// Moves to the next token
        /// </summary>
        private void MoveNext()
        {
            _current = _next;
            _next = _tokens.MoveNext() ? _tokens.Current : Token.EndOfFile(_current.Location);
        }

        /// <summary>
        /// Consumes a token of the expected type and moves to the next token
        /// </summary>
        /// <param name="expectedType">The expected token type</param>
        /// <exception cref="InvalidSyntaxException">Thrown when current token doesn't match expected type</exception>
        private void Consume(TokenType expectedType)
        {
            if (_current.Type != expectedType)
            {
                throw new InvalidSyntaxException(_current.Location);
            }

            MoveNext();
        }
    }
}
