using System;
using System.Collections.Generic;
using System.Text;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Interpreter that executes the AST.
    /// </summary>
    internal class Interpreter
    {
        /// <summary>
        /// Dictionary to store variable names and their runtime values.
        /// </summary>
        private readonly Dictionary<string, RuntimeItem> _variables;

        /// <summary>
        /// Maximum allowed iterations for loops to prevent infinite loops.
        /// </summary>
        private readonly int MaxLoopIterations;

        /// <summary>
        /// StringBuilder to accumulate output text.
        /// </summary>
        private readonly StringBuilder _buffer = new();

        /// <summary>
        /// Initializes a new instance of the Interpreter class.
        /// </summary>
        /// <param name="maxLoopIterations">Maximum allowed iterations for loops to prevent infinite loops.</param>
        public Interpreter(int maxLoopIterations = 100)
        {
            MaxLoopIterations = maxLoopIterations;
            _variables = new Dictionary<string, RuntimeItem>();
        }

        /// <summary>
        /// Gets all variables in the current scope.
        /// </summary>
        public IReadOnlyDictionary<string, RuntimeItem> Variables => _variables;

        /// <summary>
        /// Executes statements one by one as they arrive (streaming), yielding output values.
        /// </summary>
        /// <param name="statements">The enumerable of statements to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        /// <exception cref="NotSupportedException">Thrown when the statement type is not supported.</exception>
        public IEnumerable<RuntimeItem> Execute(IEnumerable<Ast.Statement> statements)
        {
            var blockStack = new Stack<Ast.Statement>();
            var enumerator = statements.GetEnumerator();
            var loops = new Stack<Loop>();

            while (blockStack.Count > 0 || enumerator.MoveNext())
            {
                Ast.Statement statement = blockStack.Count > 0 ? blockStack.Pop() : enumerator.Current;

                switch (statement)
                {
                    case Ast.Output output:
                        yield return Evaluate(output.Expression);
                        break;
                    case Ast.Assign assign:
                        ExecuteAssignment(assign);
                        break;
                    case Ast.Block block:
                        block.PushTo(blockStack);
                        break;
                    case Ast.While whileLoop:
                        if (Evaluate<RuntimeBoolean>(whileLoop.Condition).Value)
                        {
                            loops.Get(whileLoop.Location).Increment().Assert(MaxLoopIterations);
                            whileLoop.PushTo(blockStack); // Re-push the while loop for the next iteration
                            whileLoop.Body.PushTo(blockStack);
                        }
                        else
                        {
                            loops.Clear(whileLoop.Location);
                        }
                        break;
                    case Ast.If ifStatement:
                        ResolveIfStatement(ifStatement)?.PushTo(blockStack);
                        break;
                    case Ast.Input input:
                        var inputRequest = new RuntimeValueRequest(input.Location);
                        yield return inputRequest;
                        _variables[input.Identifier.Name] = inputRequest.GetResult();
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported statement type: {statement.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Executes an assignment statement.
        /// </summary>
        /// <param name="node">The assignment statement node.</param>
        private void ExecuteAssignment(Ast.Assign node)
        {
            _variables[node.Identifier.Name] = Evaluate(node.Expression);
        }

        /// <summary>
        /// Resolves the condition of an <c>if</c> statement and returns the block
        /// associated with the first condition that evaluates to true.
        /// </summary>
        /// <param name="statement">The <c>if</c> statement to evaluate.</param>
        /// <returns>
        /// The block corresponding to the first true condition, or the <c>else</c> block
        /// if no conditions are true, or <c>null</c> if no block is available.
        /// </returns>
        private Ast.Block? ResolveIfStatement(Ast.If statement)
        {
            if (Evaluate<RuntimeBoolean>(statement.IfBranch.Condition).Value)
            {
                return statement.IfBranch.Block;
            }

            foreach (var elseIfBranch in statement.ElseIfBranches)
            {
                if (Evaluate<RuntimeBoolean>(elseIfBranch.Condition).Value)
                {
                    return elseIfBranch.Block;
                }
            }

            return statement.ElseBlock;
        }

        /// <summary>
        /// Evaluates an expression and returns its value.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The evaluated runtime item.</returns>
        /// <exception cref="NotSupportedException">Thrown when the expression type is not supported.</exception>
        private RuntimeItem Evaluate(Ast.Expression expression)
        {
            return expression switch
            {
                Ast.Integer integer => new RuntimeInteger(integer.Value),
                Ast.Float floatNode => new RuntimeFloat(floatNode.Value),
                Ast.InlineString str => new RuntimeString(str.Value),
                Ast.String interpolated => EvaluateInterpolatedString(interpolated),
                Ast.Boolean boolean => new RuntimeBoolean(boolean.Value),
                Ast.Variable variable => EvaluateVariable(variable),
                Ast.AndOp andOp => EvaluateAndOp(andOp),
                Ast.OrOp orOp => EvaluateOrOp(orOp),
                Ast.XorOp xorOp => EvaluateXorOp(xorOp),
                Ast.NotOp notOp => EvaluateNotOp(notOp),
                Ast.GreaterThanOp greaterThan => EvaluateGreaterThanOp(greaterThan),
                Ast.LessThanOp lessThan => EvaluateLessThanOp(lessThan),
                Ast.GreaterOrEqualOp greaterOrEqual => EvaluateGreaterOrEqualOp(greaterOrEqual),
                Ast.LessOrEqualOp lessOrEqual => EvaluateLessOrEqualOp(lessOrEqual),
                Ast.EqualOp equal => EvaluateEqualOp(equal),
                Ast.NotEqualOp notEqual => EvaluateNotEqualOp(notEqual),
                Ast.AddOp addOp => EvaluateAddOp(addOp),
                Ast.SubOp subOp => EvaluateSubOp(subOp),
                Ast.MulOp mulOp => EvaluateMulOp(mulOp),
                Ast.DivOp divOp => EvaluateDivOp(divOp),
                Ast.ModOp modOp => EvaluateModOp(modOp),
                Ast.NegateOp negateOp => EvaluateNegateOp(negateOp),
                _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
            };
        }

        /// <summary>
        /// Evaluates logical AND operation.
        /// </summary>
        /// <param name="andOp">The AND operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="RuntimeError">Thrown when the operands are not Boolean values.</exception>
        private RuntimeBoolean EvaluateAndOp(Ast.AndOp andOp)
        {
            var left = Evaluate<RuntimeBoolean>(andOp.Left);
            var right = Evaluate<RuntimeBoolean>(andOp.Right);

            return new RuntimeBoolean(left.Value && right.Value);
        }

        /// <summary>
        /// Evaluates logical OR operation.
        /// </summary>
        /// <param name="orOp">The OR operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="RuntimeError">Thrown when the operands are not Boolean values.</exception>
        private RuntimeBoolean EvaluateOrOp(Ast.OrOp orOp)
        {
            var left = Evaluate<RuntimeBoolean>(orOp.Left);
            var right = Evaluate<RuntimeBoolean>(orOp.Right);

            return new RuntimeBoolean(left.Value || right.Value);
        }

        /// <summary>
        /// Evaluates logical XOR operation.
        /// </summary>
        /// <param name="xorOp">The XOR operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="RuntimeError">Thrown when the operands are not Boolean values.</exception>
        private RuntimeBoolean EvaluateXorOp(Ast.XorOp xorOp)
        {
            var left = Evaluate<RuntimeBoolean>(xorOp.Left);
            var right = Evaluate<RuntimeBoolean>(xorOp.Right);

            return new RuntimeBoolean(left.Value ^ right.Value);

        }

        /// <summary>
        /// Evaluates logical NOT operation.
        /// </summary>
        /// <param name="notOp">The NOT operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="RuntimeError">Thrown when the operand is not a Boolean value.</exception>
        private RuntimeBoolean EvaluateNotOp(Ast.NotOp notOp)
        {
            var operand = Evaluate<RuntimeBoolean>(notOp.Operand);

            return new RuntimeBoolean(!operand.Value);
        }

        /// <summary>
        /// Evaluates greater than comparison (&gt;).
        /// </summary>
        /// <param name="op">The greater than operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateGreaterThanOp(Ast.GreaterThanOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);

            return new RuntimeBoolean(diff > float.Epsilon);
        }

        /// <summary>
        /// Evaluates less than comparison (&lt;).
        /// </summary>
        /// <param name="op">The less than operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateLessThanOp(Ast.LessThanOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);

            return new RuntimeBoolean(diff < -float.Epsilon);
        }

        /// <summary>
        /// Evaluates greater than or equal comparison (&gt;=).
        /// </summary>
        /// <param name="op">The greater than or equal operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateGreaterOrEqualOp(Ast.GreaterOrEqualOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);

            return new RuntimeBoolean(diff > -float.Epsilon);
        }

        /// <summary>
        /// Evaluates less than or equal comparison (&lt;=).
        /// </summary>
        /// <param name="op">The less than or equal operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateLessOrEqualOp(Ast.LessOrEqualOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);

            return new RuntimeBoolean(diff < float.Epsilon);
        }

        /// <summary>
        /// Evaluates equality comparison (==).
        /// </summary>
        /// <param name="op">The equality operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateEqualOp(Ast.EqualOp op)
        {
            var left = Evaluate(op.Left);
            var right = Evaluate(op.Right);

            return new RuntimeBoolean(AreEqual(left, right));
        }

        /// <summary>
        /// Evaluates inequality comparison (!=).
        /// </summary>
        /// <param name="op">The inequality operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private RuntimeBoolean EvaluateNotEqualOp(Ast.NotEqualOp op)
        {
            var left = Evaluate(op.Left);
            var right = Evaluate(op.Right);

            return new RuntimeBoolean(!AreEqual(left, right));
        }

        /// <summary>
        /// Evaluates addition operation (+).
        /// </summary>
        /// <param name="op">The addition operation node.</param>
        /// <returns>The evaluated runtime item.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be added.</exception>
        private RuntimeItem EvaluateAddOp(Ast.AddOp op)
        {
            var left = Evaluate(op.Left);
            var right = Evaluate(op.Right);

            // String concatenation with type conversion.
            if (left is RuntimeString || right is RuntimeString)
            {
                return new RuntimeString(left.ToString() + right.ToString());
            }

            var leftNumber = Assert<RuntimeNumber>(left, op.Left.Location);
            var rightNumber = Assert<RuntimeNumber>(right, op.Right.Location);

            // Numeric addition.
            return (left, right) switch
            {
                (RuntimeInteger l, RuntimeInteger r) => new RuntimeInteger(l.Value + r.Value),
                _ => new RuntimeFloat(leftNumber.FloatValue + rightNumber.FloatValue),

            };
        }

        /// <summary>
        /// Evaluates subtraction operation (-).
        /// </summary>
        /// <param name="op">The subtraction operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be subtracted.</exception>
        private RuntimeNumber EvaluateSubOp(Ast.SubOp op)
        {
            var left = Evaluate<RuntimeNumber>(op.Left);
            var right = Evaluate<RuntimeNumber>(op.Right);

            return (left, right) switch
            {
                (RuntimeInteger l, RuntimeInteger r) => new RuntimeInteger(l.Value - r.Value),
                _ => new RuntimeFloat(left.FloatValue - right.FloatValue)
            };
        }

        /// <summary>
        /// Evaluates multiplication operation (*).
        /// </summary>
        /// <param name="op">The multiplication operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be multiplied.</exception>
        private RuntimeNumber EvaluateMulOp(Ast.MulOp op)
        {
            var left = Evaluate<RuntimeNumber>(op.Left);
            var right = Evaluate<RuntimeNumber>(op.Right);

            return (left, right) switch
            {
                (RuntimeInteger l, RuntimeInteger r) => new RuntimeInteger(l.Value * r.Value),
                _ => new RuntimeFloat(left.FloatValue * right.FloatValue)
            };
        }

        /// <summary>
        /// Evaluates division operation (/).
        /// </summary>
        /// <param name="op">The division operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be divided.</exception>
        private RuntimeNumber EvaluateDivOp(Ast.DivOp op)
        {
            var left = Evaluate<RuntimeNumber>(op.Left);
            var right = Evaluate<RuntimeNumber>(op.Right);

            if (right.IsNil)
            {
                throw new RuntimeError("Division by zero", op.Right.Location);
            }

            // Division always returns float to handle cases like 5 / 2 = 2.5.
            return new RuntimeFloat(left.FloatValue / right.FloatValue);
        }

        /// <summary>
        /// Evaluates modulo operation (%).
        /// </summary>
        /// <param name="op">The modulo operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be used for modulo operation.</exception>
        private RuntimeNumber EvaluateModOp(Ast.ModOp op)
        {
            var left = Evaluate<RuntimeNumber>(op.Left);
            var right = Evaluate<RuntimeNumber>(op.Right);

            if (right.IsNil)
            {
                throw new RuntimeError("Division by zero", op.Right.Location);
            }

            return (left, right) switch
            {
                (RuntimeInteger l, RuntimeInteger r) => new RuntimeInteger(l.Value % r.Value),
                _ => new RuntimeFloat(left.FloatValue % right.FloatValue)
            };
        }

        /// <summary>
        /// Evaluates unary negation operation (-).
        /// </summary>
        /// <param name="op">The unary negation operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="RuntimeError">Thrown when the operand is not a numeric value.</exception>
        private RuntimeNumber EvaluateNegateOp(Ast.NegateOp op)
        {
            var operand = Evaluate(op.Operand);

            return operand switch
            {
                RuntimeInteger i => new RuntimeInteger(-i.Value),
                RuntimeFloat f => new RuntimeFloat(-f.Value),
                _ => throw new RuntimeError($"Cannot negate {operand.GetTypeName()}", op.Operand.Location)
            };
        }

        /// <summary>
        /// Compares two numeric values.
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>The difference between left and right values.</returns>
        /// <exception cref="RuntimeError">Thrown when the values cannot be compared.</exception>
        private float CompareNumeric(Ast.Expression left, Ast.Expression right)
        {
            var leftValue = Evaluate<RuntimeNumber>(left);
            var rightValue = Evaluate<RuntimeNumber>(right);

            return leftValue.FloatValue - rightValue.FloatValue;
        }

        /// <summary>
        /// Checks if two runtime items are equal.
        /// </summary>
        /// <param name="left">The left runtime item.</param>
        /// <param name="right">The right runtime item.</param>
        /// <returns>True if the values are equal; otherwise, false.</returns>
        private bool AreEqual(RuntimeItem left, RuntimeItem right)
        {
            return (left, right) switch
            {
                (RuntimeInteger l, RuntimeInteger r) => l.Value == r.Value,
                (RuntimeNumber l, RuntimeNumber r) => Math.Abs(l.FloatValue - r.FloatValue) < float.Epsilon,
                (RuntimeString l, RuntimeString r) => l.Value == r.Value,
                (RuntimeBoolean l, RuntimeBoolean r) => l.Value == r.Value,
                _ => false
            };
        }

        /// <summary>
        /// Evaluates a variable and returns its value.
        /// </summary>
        /// <param name="variable">The variable node.</param>
        /// <returns>The runtime item of the variable.</returns>
        /// <exception cref="RuntimeError">Thrown when the variable is not defined.</exception>
        private RuntimeItem EvaluateVariable(Ast.Variable variable)
        {
            if (_variables.TryGetValue(variable.Name, out var value))
            {
                return value;
            }

            throw new RuntimeError($"Variable '{variable.Name}' is not defined", variable.Location);
        }

        /// <summary>
        /// Evaluates an interpolated string by concatenating its parts.
        /// </summary>
        /// <param name="interpolated">The interpolated string node.</param>
        /// <returns>The evaluated String value.</returns>
        private RuntimeString EvaluateInterpolatedString(Ast.String interpolated)
        {
            _buffer.Clear();

            foreach (var part in interpolated.Parts)
            {
                var value = Evaluate(part);
                _buffer.Append(value.ToString());
            }

            return new RuntimeString(_buffer.ToString());
        }

        /// <summary>
        /// Evaluates an expression and ensures it is of the expected type.
        /// </summary>
        /// <typeparam name="T">The expected type of the evaluated expression.</typeparam>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The evaluated expression cast to the expected type.</returns>
        /// <exception cref="RuntimeError">Thrown when the evaluated expression is not of the expected type.</exception>
        private T Evaluate<T>(Ast.Expression expression) where T : RuntimeItem
        {
            var result = Evaluate(expression);

            if (result is T typedResult)
            {
                return typedResult;
            }

            var displayValue = result is RuntimeString ? $"\"{result}\"" : result.ToString();
            var expectedType = RuntimeItem.GetDisplayName<T>();
            throw new RuntimeError($"Expected {expectedType} type, but got value {displayValue} of type {result.GetTypeName()}", expression.Location);
        }

        /// <summary>
        /// Asserts that a runtime item is of the expected type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="value">The runtime item to check.</param>
        /// <param name="location">The location in the source code for error reporting.</param>
        /// <returns>The runtime item cast to the expected type.</returns>
        /// <exception cref="RuntimeError">Thrown when the runtime item is not of the expected type.</exception>
        private T Assert<T>(RuntimeItem value, Location location) where T : RuntimeItem
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            var displayValue = value is RuntimeString ? $"\"{value}\"" : value.ToString();
            var expectedType = RuntimeItem.GetDisplayName<T>();
            throw new RuntimeError($"Expected {expectedType} type, but got value {displayValue} of type {value.GetTypeName()}", location);
        }
    }
}
