using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Executes a Game Dialog Script source code from a string.
        /// Yields runtime items from << statements.
        /// </summary>
        /// <param name="source">The source code to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> RunInline(string source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (var item in Run(Source.Inline(source)))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Executes a Game Dialog Script file.
        /// Yields runtime items from << statements.
        /// </summary>
        /// <param name="filePath">The path to the script file to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> RunFile(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            foreach (var item in Run(Source.FromFile(filePath)))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Executes a Game Dialog Script source code from a TextReader (streaming mode).
        /// Yields runtime items from << statements.
        /// </summary>
        /// <param name="source">The source code to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        private IEnumerable<RuntimeItem> Run(Source source)
        {
            // Tokenize (streaming).
            using var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();

            // Parse (streaming).
            var parser = new Parser(tokens);
            var statements = parser.Parse().ToList();

            // Execute (streaming) - yields output values from << statements.
            return _interpreter.Execute(statements);
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
