using System;
using System.Collections.Generic;
using System.IO;

namespace BitPatch.DialogLang
{
    /// <summary>
    /// Main entry point for the Game Dialog Script language.
    /// </summary>
    public class Dialog
    {
        /// <summary>
        /// The interpreter instance used for executing scripts.
        /// </summary>
        private readonly Interpreter _interpreter;

        public Dialog()
        {
            _interpreter = new Interpreter();
        }

        /// <summary>
        /// Executes a Game Dialog Script source code from a TextReader (streaming mode).
        /// Yields runtime items from << statements.
        /// </summary>
        /// <param name="reader">The TextReader to read source code from.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> Execute(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            // Tokenize (streaming).
            var lexer = new Lexer(reader);
            var tokens = lexer.Tokenize();
    
            // Parse (streaming).
            var parser = new Parser(tokens);
            var statements = parser.Parse();

            // Execute (streaming) - yields output values from << statements.
            return _interpreter.Execute(statements);
        }

        /// <summary>
        /// Executes a Game Dialog Script source code from a string.
        /// Yields runtime items from << statements.
        /// </summary>
        /// <param name="source">The source code to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> Execute(string source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            using var reader = new StringReader(source);
            foreach (var item in Execute(reader)){
                yield return item;
            }
        }

        /// <summary>
        /// Gets all variables as a sequence of name-value pairs.
        /// </summary>
        public IEnumerable<(string name, RuntimeItem value)> Variables
        {
            get
            {
                foreach (var kvp in _interpreter.Variables)
                {
                    yield return (kvp.Key, kvp.Value);
                }
            }
        }
    }
}
