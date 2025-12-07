using System.Collections.Generic;
using System.Text;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Utility methods for the interpreter.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Pushes the statements of a block onto the stack in reverse order.
        /// </summary>
        /// <param name="block">The block whose statements to push.</param>
        /// <param name="stack">The stack to push statements onto.</param>
        public static void PushTo(this Ast.Block block, Stack<Ast.Statement> stack)
        {
            var statements = block.Statements;

            for (int i = statements.Count - 1; i >= 0; i--)
            {
                stack.Push(statements[i]);
            }
        }

        /// <summary>
        /// Pushes a statement onto the stack.
        /// </summary>
        /// <param name="statement">The statement to push.</param>
        /// <param name="stack">The stack to push the statement onto.</param>
        public static void PushTo(this Ast.Statement statement, Stack<Ast.Statement> stack)
        {
            stack.Push(statement);
        }

        /// <summary>
        /// Gets the Loop instance for the given location, creating a new one if necessary.
        /// </summary>
        /// <param name="loops">The stack of Loop instances.</param>
        /// <param name="location">The location of the loop in the source code.</param>
        /// <returns>The Loop instance for the given location.</returns>
        public static Loop Get(this Stack<Loop> loops, Location location)
        {
            if (loops.Count is 0 || loops.Peek().Line != location.Line)
            {
                var loop = new Loop(location);
                loops.Push(loop);
                return loop;
            }

            return loops.Peek();
        }

        /// <summary>
        /// Clears the Loop instance for the given location if it exists.
        /// </summary>
        /// <param name="loops">The stack of Loop instances.</param>
        /// <param name="location">The location of the loop in the source code.</param>
        public static void Clear(this Stack<Loop> loops, Location location)
        {
            if (loops.Count > 0 && loops.Peek().Line == location.Line)
            {
                loops.Pop();
            }
        }

        /// <summary>
        /// Converts the contents of the StringBuilder to a Token and clears the builder.
        /// Returns null if the builder is empty.
        /// </summary>
        /// <param name="builder">The StringBuilder to convert.</param>
        /// <param name="location">The location of the token in the source code.</param>
        /// <returns>The created Token, or null if the builder is empty.</returns>
        public static Token? ToToken(this StringBuilder builder, Location location)
        {
            if (builder.Length is 0)
            {
                return null;
            }

            var token = new Token(TokenType.InlineString, builder.ToString(), location);
            builder.Clear();
            return token;
        }

        /// <summary>
        /// Enqueues the token to the given queue.
        /// </summary>
        /// <param name="token">The token to enqueue.</param>
        /// <param name="queue">The queue to enqueue the token to.</param>
        public static void EnqueueTo(this Token token, Queue<Token> queue)
        {
            queue.Enqueue(token);
        }

        /// <summary>
        /// Converts a native object to a RuntimeValue.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The converted RuntimeValue.</returns>
        public static RuntimeValue ObjectToRuntimeValue(object value)
        {
            return value switch
            {
                int intValue => new RuntimeInteger(intValue),
                float floatValue => new RuntimeFloat(floatValue),
                string stringValue => new RuntimeString(stringValue),
                bool boolValue => new RuntimeBoolean(boolValue),
                _ => throw new System.NotSupportedException($"Type {value.GetType().Name} is not supported for conversion to RuntimeValue."),
            };
        }
    }
}