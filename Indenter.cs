using System;
using System.Collections.Generic;
using BitPatch.DialogLang.Diagnostic;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Handles indentation tokens based on the current indentation level.
    /// </summary>
    internal class Indenter
    {
        /// <summary>
        /// The underlying reader for source code.
        /// </summary>
        private readonly Reader _reader;

        /// <summary>
        /// The current state of the indenter.
        /// </summary>
        private IndenterState _state = IndenterState.Default;

        /// <summary>
        /// The stack of indentation levels.
        /// </summary>
        private readonly Stack<int> _levels = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Indenter"/> class.
        /// </summary>
        public Indenter(Reader reader)
        {
            _reader = reader;
            _levels.Push(0);
        }

        /// <summary>
        /// Reads the current indentation level and generates corresponding tokens.
        /// </summary>
        public void Read(Queue<Token> output)
        {
            Assert.IsTrue(_reader.IsAtLineStart(), $"Expected start of line, column {_reader.Column}.");

            if (_state is IndenterState.Locked)
            {
                Unlock();
            }

            var startLocation = _reader.GetLocation();
            var last = _levels.Peek();
            int current = _reader.ReadIndentLevel();

            if (current > last)
            {
                _levels.Push(current);
                var indentLocation = new Location(_reader.Source, startLocation.Line, last + 1, current + 1);
                output.Enqueue(Token.Indent(indentLocation));

                return;
            }

            while (current < last)
            {
                var final = _levels.Pop();
                last = _levels.TryPeek(out var level) ? level : 0;
                output.Enqueue(Token.Dedent(new Location(_reader.Source, startLocation.Line, last + 1, final + 1)));
            }

            if (current != last)
            {
                throw new SyntaxError("Inconsistent indentation", last > 0 ? startLocation | last + 1 : startLocation);
            }
        }

        /// <summary>
        /// Locks the indenter to enforce consistent indentation.
        /// </summary>
        public void Locking()
        {
            Assert.IsTrue(_reader.IsAtLineStart(), $"Expected start of line, column {_reader.Column}.");

            var startLocation = _reader.GetLocation();
            var peek = _levels.Peek();
            int current = _state is IndenterState.Locked ? _reader.ReadIndentLevel(peek) : _reader.ReadIndentLevel();

            switch (_state)
            {
                case IndenterState.Default:
                    _levels.Push(current);
                    _state = IndenterState.Locked;
                    break;
                case IndenterState.Locked when current != peek:
                    throw new SyntaxError("Inconsistent indentation", startLocation | _reader);
                case IndenterState.Locked:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown indenter state: {_state}.");
            }
        }

        /// <summary>
        /// Empties all indentation levels, generating dedent tokens as needed.
        /// </summary>
        public void Empty(Queue<Token> output)
        {
            if (_state is IndenterState.Locked)
            {
                Unlock();
            }

            while (_levels.Count > 1)
            {
                _levels.Pop();
                output.Enqueue(Token.Dedent(_reader.GetLocation()));
            }
        }

        /// <summary>
        /// Unlocks the indenter from locked state.
        /// </summary>
        private void Unlock()
        {
            Assert.IsTrue(_state is IndenterState.Locked, "Indenter is not locked.");
            _state = IndenterState.Default;
            _levels.Pop();
        }
    }
}