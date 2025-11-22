using System;
using System.Collections.Generic;

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
                        yield return EvaluateExpression(output.Expression);
                        break;
                    case Ast.Assign assign:
                        ExecuteAssignment(assign);
                        break;
                    case Ast.Block block:
                        block.PushTo(blockStack);
                        break;
                    case Ast.While whileLoop:
                        if (EvaluateCondition(whileLoop.Condition).Value)
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
                        EvaluateIfStatement(ifStatement)?.PushTo(blockStack);
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
            _variables[node.Identifier.Name] = EvaluateExpression(node.Expression);
        }

        /// <summary>
        /// Evaluates the condition of an <c>if</c> statement and returns the block
        /// associated with the first condition that evaluates to true.
        /// </summary>
        /// <param name="statement">The <c>if</c> statement to evaluate.</param>
        /// <returns>
        /// The block corresponding to the first true condition, or the <c>else</c> block
        /// if no conditions are true, or <c>null</c> if no block is available.
        /// </returns>
        private Ast.Block? EvaluateIfStatement(Ast.If statement)
        {
            if (EvaluateCondition(statement.IfBranch.Condition).Value)
            {
                return statement.IfBranch.Block;
            }

            foreach (var elseIfBranch in statement.ElseIfBranches)
            {
                if (EvaluateCondition(elseIfBranch.Condition).Value)
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
        private RuntimeItem EvaluateExpression(Ast.Expression expression)
        {
            return expression switch
            {
                Ast.Integer integer => new Integer(integer.Value),
                Ast.Float floatNode => new Float(floatNode.Value),
                Ast.String str => new String(str.Value),
                Ast.Boolean boolean => new Boolean(boolean.Value),
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
        /// <exception cref="ScriptException">Thrown when the operands are not Boolean values.</exception>
        private Boolean EvaluateAndOp(Ast.AndOp andOp)
        {
            var left = EvaluateExpression(andOp.Left);
            var right = EvaluateExpression(andOp.Right);

            var value = (left, right) switch
            {
                (Boolean l, Boolean r) => l.Value && r.Value,
                (Boolean l, not Boolean) => throw Exception(andOp.Right.Location),
                (not Boolean, Boolean r) => throw Exception(andOp.Left.Location),
                _ => throw Exception(andOp.Left.Location | andOp.Right.Location)
            };

            return new Boolean(value);

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot evaluate AND operation with types {left.GetType().Name} and {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates logical OR operation.
        /// </summary>
        /// <param name="orOp">The OR operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="ScriptException">Thrown when the operands are not Boolean values.</exception>
        private Boolean EvaluateOrOp(Ast.OrOp orOp)
        {
            var left = EvaluateExpression(orOp.Left);
            var right = EvaluateExpression(orOp.Right);

            var value = (left, right) switch
            {
                (Boolean l, Boolean r) => l.Value || r.Value,
                (Boolean l, not Boolean) => throw Exception(orOp.Right.Location),
                (not Boolean, Boolean r) => throw Exception(orOp.Left.Location),
                _ => throw Exception(orOp.Left.Location | orOp.Right.Location)
            };

            return new Boolean(value);

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot evaluate OR operation with types {left.GetType().Name} and {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates logical XOR operation.
        /// </summary>
        /// <param name="xorOp">The XOR operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="ScriptException">Thrown when the operands are not Boolean values.</exception>
        private Boolean EvaluateXorOp(Ast.XorOp xorOp)
        {
            var left = EvaluateExpression(xorOp.Left);
            var right = EvaluateExpression(xorOp.Right);

            var value = (left, right) switch
            {
                (Boolean l, Boolean r) => l.Value ^ r.Value,
                (Boolean l, not Boolean) => throw Exception(xorOp.Right.Location),
                (not Boolean, Boolean r) => throw Exception(xorOp.Left.Location),
                _ => throw Exception(xorOp.Left.Location | xorOp.Right.Location)
            };

            return new Boolean(value);

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot evaluate XOR operation with types {left.GetType().Name} and {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates logical NOT operation.
        /// </summary>
        /// <param name="notOp">The NOT operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        /// <exception cref="ScriptException">Thrown when the operand is not a Boolean value.</exception>
        private Boolean EvaluateNotOp(Ast.NotOp notOp)
        {
            var operand = EvaluateExpression(notOp.Operand);

            if (operand is not Boolean boolOperand)
            {
                throw new ScriptException($"Cannot evaluate NOT operation on type {operand.GetType().Name}", notOp.Operand.Location);
            }

            return new Boolean(!boolOperand.Value);
        }

        /// <summary>
        /// Evaluates greater than comparison (>).
        /// </summary>
        /// <param name="op">The greater than operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateGreaterThanOp(Ast.GreaterThanOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);
            return new Boolean(diff > float.Epsilon);
        }

        /// <summary>
        /// Evaluates less than comparison (<).
        /// </summary>
        /// <param name="op">The less than operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateLessThanOp(Ast.LessThanOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);
            return new Boolean(diff < -float.Epsilon);
        }

        /// <summary>
        /// Evaluates greater than or equal comparison (>=).
        /// </summary>
        /// <param name="op">The greater than or equal operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateGreaterOrEqualOp(Ast.GreaterOrEqualOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);
            return new Boolean(diff > -float.Epsilon);
        }

        /// <summary>
        /// Evaluates less than or equal comparison (<=).
        /// </summary>
        /// <param name="op">The less than or equal operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateLessOrEqualOp(Ast.LessOrEqualOp op)
        {
            var diff = CompareNumeric(op.Left, op.Right);
            return new Boolean(diff < float.Epsilon);
        }

        /// <summary>
        /// Evaluates equality comparison (==).
        /// </summary>
        /// <param name="op">The equality operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateEqualOp(Ast.EqualOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);
            return new Boolean(AreEqual(left, right));
        }

        /// <summary>
        /// Evaluates inequality comparison (!=).
        /// </summary>
        /// <param name="op">The inequality operation node.</param>
        /// <returns>The evaluated Boolean result.</returns>
        private Boolean EvaluateNotEqualOp(Ast.NotEqualOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);
            return new Boolean(!AreEqual(left, right));
        }

        /// <summary>
        /// Evaluates addition operation (+).
        /// </summary>
        /// <param name="op">The addition operation node.</param>
        /// <returns>The evaluated runtime item.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be added.</exception>
        private RuntimeItem EvaluateAddOp(Ast.AddOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);

            // String concatenation with type conversion.
            if (left is String || right is String)
            {
                return new String(left.ToString() + right.ToString());
            }

            // Numeric addition.
            return (left, right) switch
            {
                (Integer l, Integer r) => new Integer(l.Value + r.Value),
                (Number l, Number r) => new Float(l.FloatValue + r.FloatValue),
                (Number, not Number) => throw Exception(op.Right.Location),
                (not Number, Number) => throw Exception(op.Left.Location),
                _ => throw Exception(op.Left.Location | op.Right.Location)
            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot add {left.GetType().Name} and {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates subtraction operation (-).
        /// </summary>
        /// <param name="op">The subtraction operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be subtracted.</exception>
        private Number EvaluateSubOp(Ast.SubOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);

            return (left, right) switch
            {
                (Integer l, Integer r) => new Integer(l.Value - r.Value),
                (Number l, Number r) => new Float(l.FloatValue - r.FloatValue),
                (Number, not Number) => throw Exception(op.Right.Location),
                (not Number, Number) => throw Exception(op.Left.Location),
                _ => throw Exception(op.Left.Location | op.Right.Location)
            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot subtract {right.GetType().Name} from {left.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates multiplication operation (*).
        /// </summary>
        /// <param name="op">The multiplication operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be multiplied.</exception>
        private Number EvaluateMulOp(Ast.MulOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);

            return (left, right) switch
            {
                (Integer l, Integer r) => new Integer(l.Value * r.Value),
                (Number l, Number r) => new Float(l.FloatValue * r.FloatValue),
                (Number, not Number) => throw Exception(op.Right.Location),
                (not Number, Number) => throw Exception(op.Left.Location),
                _ => throw Exception(op.Left.Location | op.Right.Location)
            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot multiply {left.GetType().Name} by {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates division operation (/).
        /// </summary>
        /// <param name="op">The division operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be divided.</exception>
        private Number EvaluateDivOp(Ast.DivOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);

            // Division always returns float to handle cases like 5 / 2 = 2.5.
            return (left, right) switch
            {
                (_, Number n) when n.IsNil => throw new ScriptException("Division by zero", op.Right.Location),
                (Number l, Number r) when !r.IsNil => new Float(l.FloatValue / r.FloatValue),
                (Number, not Number) => throw Exception(op.Right.Location),
                (not Number, Number) => throw Exception(op.Left.Location),
                _ => throw Exception(op.Left.Location | op.Right.Location)
            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot divide {left.GetType().Name} by {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates modulo operation (%).
        /// </summary>
        /// <param name="op">The modulo operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be used for modulo operation.</exception>
        private Number EvaluateModOp(Ast.ModOp op)
        {
            var left = EvaluateExpression(op.Left);
            var right = EvaluateExpression(op.Right);

            return (left, right) switch
            {
                (_, Number n) when n.IsNil => throw new ScriptException("Division by zero", op.Right.Location),
                (Integer l, Integer r) when r.Value != 0 => new Integer(l.Value % r.Value),
                (Number l, Number r) when !r.IsNil => new Float(l.FloatValue % r.FloatValue),
                (Number, not Number) => throw Exception(op.Right.Location),
                (not Number, Number) => throw Exception(op.Left.Location),
                _ => throw Exception(op.Left.Location | op.Right.Location)
            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot calculate modulo of {left.GetType().Name} by {right.GetType().Name}", location);
            }
        }

        /// <summary>
        /// Evaluates unary negation operation (-).
        /// </summary>
        /// <param name="op">The unary negation operation node.</param>
        /// <returns>The evaluated numeric value.</returns>
        /// <exception cref="ScriptException">Thrown when the operand is not a numeric value.</exception>
        private Number EvaluateNegateOp(Ast.NegateOp op)
        {
            var operand = EvaluateExpression(op.Operand);

            return operand switch
            {
                Integer i => new Integer(-i.Value),
                Float f => new Float(-f.Value),
                _ => throw new ScriptException($"Cannot negate {operand.GetType().Name}", op.Operand.Location)
            };
        }

        /// <summary>
        /// Compares two numeric values.
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>The difference between left and right values.</returns>
        /// <exception cref="ScriptException">Thrown when the values cannot be compared.</exception>
        private float CompareNumeric(Ast.Expression left, Ast.Expression right)
        {
            var leftValue = EvaluateExpression(left);
            var rightValue = EvaluateExpression(right);

            return (leftValue, rightValue) switch
            {
                (Integer l, Integer r) => l.Value - r.Value,
                (Number l, Number r) => l.FloatValue - r.FloatValue,
                (not Number, Number) => throw Exception(left.Location),
                (Number, not Number) => throw Exception(right.Location),
                _ => throw Exception(left.Location | right.Location)

            };

            ScriptException Exception(Location location)
            {
                return new ScriptException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}", location);
            }
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
                (Integer l, Integer r) => l.Value == r.Value,
                (Float l, Float r) => Math.Abs(l.Value - r.Value) < float.Epsilon,
                (Integer l, Float r) => Math.Abs(l.Value - r.Value) < float.Epsilon,
                (Float l, Integer r) => Math.Abs(l.Value - r.Value) < float.Epsilon,
                (String l, String r) => l.Value == r.Value,
                (Boolean l, Boolean r) => l.Value == r.Value,
                _ => false
            };
        }

        /// <summary>
        /// Evaluates a variable and returns its value.
        /// </summary>
        /// <param name="variable">The variable node.</param>
        /// <returns>The runtime item of the variable.</returns>
        /// <exception cref="ScriptException">Thrown when the variable is not defined.</exception>
        private RuntimeItem EvaluateVariable(Ast.Variable variable)
        {
            if (_variables.TryGetValue(variable.Name, out var value))
            {
                return value;
            }

            throw new ScriptException($"Variable '{variable.Name}' is not defined", variable.Location);
        }

        /// <summary>
        /// Evaluates a condition expression and returns its boolean value.
        /// </summary>
        /// <param name="expression">The condition expression.</param>
        /// <returns>The evaluated Boolean value.</returns>
        /// <exception cref="ScriptException">Thrown when the expression does not evaluate to a Boolean value.</exception>
        private Boolean EvaluateCondition(Ast.Expression expression)
        {
            var value = EvaluateExpression(expression);

            if (value is Boolean boolValue)
            {
                return boolValue;
            }

            throw new ScriptException($"Expected boolean expression, got {value.GetType().Name}", expression.Location);
        }
    }
}
