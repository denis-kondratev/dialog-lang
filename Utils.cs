using System.Collections.Generic;

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
    }
}