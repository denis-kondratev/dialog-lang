using System;
using System.Collections.Generic;

namespace GameDialog.Lang
{
    /// <summary>
    /// Stack to manage lexer states.
    /// </summary>
    internal class StateStack
    {
        /// <summary>
        /// Gets the current lexer state at the top of the stack.
        /// </summary>
        public LexerState Peek => _peek;

        /// <summary>
        /// The internal stack of lexer states.
        /// </summary>
        private readonly Stack<LexerState> _stack = new();

        /// <summary>
        /// The current lexer state.
        /// </summary>
        private LexerState _peek = LexerState.Default;

        /// <summary>
        /// Pushes a new lexer state onto the stack.
        /// </summary>
        /// <param name="state">The new lexer state to push.</param>
        public void Push(LexerState state)
        {
            if (state is LexerState.Default)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Cannot push the default state.");
            }

            _stack.Push(_peek);
            _peek = state;
        }

        /// <summary>
        /// Pops the current lexer state from the stack.
        /// </summary>
        public void Pop()
        {
            _peek = _stack.Pop();
        }
    }
}