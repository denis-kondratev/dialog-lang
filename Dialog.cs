using System;
using System.Collections.Generic;
using System.IO;

namespace DialogLang
{
    /// <summary>
    /// Executes the Abstract Syntax Tree.
    /// Supports multiple output values using the &lt;&lt; operator and input requests using the &gt;&gt; operator.
    /// Example:
    /// <code>
    /// var interpreter = new Interpreter(logger);
    /// foreach (var result in interpreter.Run("a = 10\n&lt;&lt; a\nb = 20\n&lt;&lt; b\n&lt;&lt; a + b"))
    /// {
    ///     Console.WriteLine(result); // Outputs: 10, then 20, then 30.
    /// }
    /// 
    /// // Example with input.
    /// foreach (var result in interpreter.Run("&gt;&gt; a\nLog(a)"))
    /// {
    ///     if (result is IInputRequest input)
    ///     {
    ///         input.Set(42);
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class Interpreter
    {
        private readonly Dictionary<string, object> _variables;
        private readonly ILogger _logger;

        public Interpreter(ILogger logger)
        {
            _variables = new Dictionary<string, object>();
            _logger = logger;
        }

        /// <summary>
        /// Executes the script from text.
        /// </summary>
        public IEnumerable<object?> Run(string scriptText)
        {
            ProgramNode program;
            try
            {
                var lexer = new Lexer(scriptText);
                var parser = new Parser(lexer);
                program = parser.Parse();
            }
            catch (InterpreterException ex)
            {
                _logger.LogError($"[DialogLang] {ex.Message}");
                yield break;
            }

            foreach (var value in ExecuteProgram(program))
            {
                yield return value;
            }
        }

        /// <summary>
        /// Executes the script from a TextReader (file or stream).
        /// </summary>
        public IEnumerable<object?> Run(TextReader reader)
        {
            ProgramNode program;
            try
            {
                var lexer = new Lexer(reader);
                var parser = new Parser(lexer);
                program = parser.Parse();
            }
            catch (InterpreterException ex)
            {
                _logger.LogError($"[DialogLang] {ex.Message}");
                yield break;
            }

            foreach (var value in ExecuteProgram(program))
            {
                yield return value;
            }
        }

        /// <summary>
        /// Executes a program and yields output values.
        /// </summary>
        private IEnumerable<object?> ExecuteProgram(ProgramNode program)
        {
            foreach (var statement in program.Statements)
            {
                var result = Visit(statement);
                
                // Check if this is an output statement.
                if (statement is OutputNode)
                {
                    yield return result;
                }
                // Check if this is an input statement.
                else if (statement is InputNode inputNode)
                {
                    var inputRequest = new InputRequest(inputNode.VariableName, _variables);
                    yield return inputRequest;
                }
            }
        }

        /// <summary>
        /// Visits an AST node and executes it.
        /// </summary>
        private object? Visit(AstNode node)
        {
            return node switch
            {
                NumberNode n => VisitNumber(n),
                StringNode s => VisitString(s),
                BooleanNode b => VisitBoolean(b),
                BinaryOpNode b => VisitBinaryOp(b),
                UnaryOpNode u => VisitUnaryOp(u),
                VariableNode v => VisitVariable(v),
                AssignNode a => VisitAssign(a),
                IfNode i => VisitIf(i),
                FunctionCallNode f => VisitFunctionCall(f),
                BlockNode bl => VisitBlock(bl),
                OutputNode r => VisitOutput(r),
                InputNode inp => VisitInput(inp),
                _ => throw new Exception($"Unknown node type: {node.GetType()}")
            };
        }

        /// <summary>
        /// Visits a number node.
        /// </summary>
        private object VisitNumber(NumberNode node)
        {
            return node.Value;
        }

        /// <summary>
        /// Visits a string node.
        /// </summary>
        private string VisitString(StringNode node)
        {
            return node.Value;
        }

        /// <summary>
        /// Visits a boolean node.
        /// </summary>
        private bool VisitBoolean(BooleanNode node)
        {
            return node.Value;
        }

        /// <summary>
        /// Visits a block node.
        /// </summary>
        private object? VisitBlock(BlockNode node)
        {
            object? result = null;
            foreach (var statement in node.Statements)
            {
                result = Visit(statement);
            }
            return result;
        }

        /// <summary>
        /// Visits a binary operation node.
        /// </summary>
        private object VisitBinaryOp(BinaryOpNode node)
        {
            object? left = Visit(node.Left);
            object? right = Visit(node.Right);

            if (left == null || right == null)
            {
                throw new Exception("Binary operation operands cannot be null");
            }

            return node.Operator switch
            {
                TokenType.Plus => Add(left, right),
                TokenType.Minus => Subtract(left, right),
                TokenType.Multiply => Multiply(left, right),
                TokenType.Divide => Divide(left, right),
                TokenType.Equal => AreEqual(left, right),
                TokenType.NotEqual => !AreEqual(left, right),
                TokenType.Greater => IsGreater(left, right),
                TokenType.GreaterOrEqual => IsGreaterOrEqual(left, right),
                TokenType.Less => IsLess(left, right),
                TokenType.LessOrEqual => IsLessOrEqual(left, right),
                TokenType.And => LogicalAnd(left, right),
                TokenType.Or => LogicalOr(left, right),
                _ => throw new Exception($"Unknown operator: {node.Operator}")
            };
        }

        /// <summary>
        /// Visits a unary operation node.
        /// </summary>
        private object VisitUnaryOp(UnaryOpNode node)
        {
            object? operand = Visit(node.Operand);

            if (operand == null)
            {
                throw new Exception("Unary operation operand cannot be null");
            }

            return node.Operator switch
            {
                TokenType.Not => LogicalNot(operand),
                _ => throw new Exception($"Unknown unary operator: {node.Operator}")
            };
        }

        /// <summary>
        /// Visits a variable node.
        /// </summary>
        private object VisitVariable(VariableNode node)
        {
            if (_variables.TryGetValue(node.Name, out object value))
            {
                return value;
            }
            throw new Exception($"Undefined variable: {node.Name}");
        }

        /// <summary>
        /// Visits an assignment node.
        /// </summary>
        private object VisitAssign(AssignNode node)
        {
            object? value = Visit(node.Value);
            
            if (value == null)
            {
                throw new Exception("Cannot assign null value");
            }
            
            _variables[node.VariableName] = value;
            return value;
        }

        /// <summary>
        /// Visits an if statement node.
        /// </summary>
        private object? VisitIf(IfNode node)
        {
            object? condition = Visit(node.Condition);
            
            bool isTrue = condition is bool b ? b : Convert.ToBoolean(condition);

            if (isTrue)
            {
                return Visit(node.ThenBody);
            }
            else if (node.ElseBody != null)
            {
                return Visit(node.ElseBody);
            }

            return null;
        }

        /// <summary>
        /// Visits a function call node.
        /// </summary>
        private object? VisitFunctionCall(FunctionCallNode node)
        {
            if (node.FunctionName == "Log")
            {
                if (node.Arguments.Count > 0)
                {
                    object? arg = Visit(node.Arguments[0]);
                    _logger.LogInfo(FormatValue(arg));
                }
                return null;
            }

            throw new Exception($"Unknown function: {node.FunctionName}");
        }

        /// <summary>
        /// Visits an output statement node.
        /// </summary>
        private object? VisitOutput(OutputNode node)
        {
            return Visit(node.Expression);
        }

        /// <summary>
        /// Visits an input statement node.
        /// </summary>
        private object? VisitInput(InputNode node)
        {
            // Input handling is done in ExecuteProgram.
            // This method is called during traversal but doesn't need to do anything.
            return null;
        }

        /// <summary>
        /// Formats a value for display.
        /// </summary>
        private string FormatValue(object? value)
        {
            return value switch
            {
                null => "null",
                string s => s,
                float f => f.ToString("0.##"),
                int i => i.ToString(),
                bool b => b ? "true" : "false",
                _ => value.ToString() ?? "null"
            };
        }

        /// <summary>
        /// Performs addition operation.
        /// </summary>
        private object Add(object left, object right)
        {
            if (left is string || right is string)
                return left.ToString() + right.ToString();
            if (left is int li && right is int ri)
                return li + ri;
            if (left is float lf || right is float)
                return Convert.ToSingle(left) + Convert.ToSingle(right);
            throw new Exception($"Cannot add {left} and {right}");
        }

        /// <summary>
        /// Performs subtraction operation.
        /// </summary>
        private object Subtract(object left, object right)
        {
            if (left is int li && right is int ri)
                return li - ri;
            if (left is float || right is float)
                return Convert.ToSingle(left) - Convert.ToSingle(right);
            throw new Exception($"Cannot subtract {right} from {left}");
        }

        /// <summary>
        /// Performs multiplication operation.
        /// </summary>
        private object Multiply(object left, object right)
        {
            if (left is int li && right is int ri)
                return li * ri;
            if (left is float || right is float)
                return Convert.ToSingle(left) * Convert.ToSingle(right);
            throw new Exception($"Cannot multiply {left} and {right}");
        }

        /// <summary>
        /// Performs division operation.
        /// </summary>
        private object Divide(object left, object right)
        {
            if (left is int li && right is int ri)
                return li / ri;
            if (left is float || right is float)
                return Convert.ToSingle(left) / Convert.ToSingle(right);
            throw new Exception($"Cannot divide {left} by {right}");
        }

        /// <summary>
        /// Checks if two values are equal.
        /// </summary>
        private bool AreEqual(object left, object right)
        {
            if (left is int li && right is int ri)
                return li == ri;
            if (left is float || right is float)
                return Math.Abs(Convert.ToSingle(left) - Convert.ToSingle(right)) < 0.0001f;
            return left.Equals(right);
        }

        /// <summary>
        /// Checks if left is greater than right.
        /// </summary>
        private bool IsGreater(object left, object right)
        {
            if (left is int li && right is int ri)
                return li > ri;
            if (left is float || right is float)
                return Convert.ToSingle(left) > Convert.ToSingle(right);
            throw new Exception($"Cannot compare {left} and {right}");
        }

        /// <summary>
        /// Checks if left is greater than or equal to right.
        /// </summary>
        private bool IsGreaterOrEqual(object left, object right)
        {
            return IsGreater(left, right) || AreEqual(left, right);
        }

        /// <summary>
        /// Checks if left is less than right.
        /// </summary>
        private bool IsLess(object left, object right)
        {
            return !IsGreaterOrEqual(left, right);
        }

        /// <summary>
        /// Checks if left is less than or equal to right.
        /// </summary>
        private bool IsLessOrEqual(object left, object right)
        {
            return !IsGreater(left, right);
        }

        /// <summary>
        /// Performs logical AND operation.
        /// </summary>
        private bool LogicalAnd(object left, object right)
        {
            bool leftBool = left is bool lb ? lb : Convert.ToBoolean(left);
            bool rightBool = right is bool rb ? rb : Convert.ToBoolean(right);
            return leftBool && rightBool;
        }

        /// <summary>
        /// Performs logical OR operation.
        /// </summary>
        private bool LogicalOr(object left, object right)
        {
            bool leftBool = left is bool lb ? lb : Convert.ToBoolean(left);
            bool rightBool = right is bool rb ? rb : Convert.ToBoolean(right);
            return leftBool || rightBool;
        }

        /// <summary>
        /// Performs logical NOT operation.
        /// </summary>
        private bool LogicalNot(object operand)
        {
            bool operandBool = operand is bool b ? b : Convert.ToBoolean(operand);
            return !operandBool;
        }

        /// <summary>
        /// Internal implementation of input request.
        /// </summary>
        private class InputRequest : IInputRequest
        {
            private readonly Dictionary<string, object> _variables;
            
            public string VariableName { get; }

            public InputRequest(string variableName, Dictionary<string, object> variables)
            {
                VariableName = variableName;
                _variables = variables;
            }

            public void Set(object value)
            {
                _variables[VariableName] = value;
            }
        }
    }
}
