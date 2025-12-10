using System;
using System.Collections.Generic;

namespace GameDialog.Lang
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

        /// <summary>
        /// Initializes a new instance of the Dialog class.
        /// </summary>
        public Dialog()
        {
            _interpreter = new Interpreter();
        }

        /// <summary>
        /// Executes a Game Dialog Script source code from a string.
        /// Yields runtime items from &lt;&lt; statements.
        /// </summary>
        /// <param name="inlineSource">The source code to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> RunInline(string inlineSource)
        {
            var source = Source.Inline(inlineSource ?? throw new ArgumentNullException(nameof(inlineSource)));
            return Run(source);
        }

        /// <summary>
        /// Executes a Game Dialog Script file.
        /// Yields runtime items from &lt;&lt; statements.
        /// </summary>
        /// <param name="filePath">The path to the script file to execute.</param>
        /// <returns>Enumerable of runtime items.</returns>
        public IEnumerable<RuntimeItem> RunFile(string filePath)
        {
            var source = Source.FromFile(filePath ?? throw new ArgumentNullException(nameof(filePath)));
            return Run(source);
        }

        /// <summary>
        /// Executes a Game Dialog Script source code from a TextReader (streaming mode).
        /// Yields runtime items from &lt;&lt; statements.
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
            var statements = parser.Parse();

            // Execute (streaming) - yields output values from << statements.
            foreach (var item in _interpreter.Execute(statements))
            {
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
