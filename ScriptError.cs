using System;

namespace GameDialog.Lang
{
    /// <summary>
    /// Exception thrown when an error occurs during script execution.
    /// </summary>
    public abstract class ScriptError : Exception
    {
        /// <summary>
        /// Gets the line number where the error occurred.
        /// </summary>
        public int Line => Location.Line;

        /// <summary>
        /// Gets the start column where the error occurred.
        /// </summary>
        public int Initial => Location.Initial;

        /// <summary>
        /// Gets the end column where the error occurred.
        /// </summary>
        public int Final => Location.Final;
        
        /// <summary>
        /// Gets the source code input where the error occurred.
        /// </summary>
        public Source SourceCode => Location.Source;

        /// <summary>
        /// Gets the location in the source code where the error occurred.
        /// </summary>
        internal Location Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptError"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="location">The location in the source code where the error occurred.</param>
        internal ScriptError(string message, Location location) : base(message)
        {
            Location = location;
        }
    }

    /// <summary>
    /// Exception thrown when a syntax error is encountered in the script.
    /// </summary>
    public class SyntaxError : ScriptError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxError"/> class.
        /// </summary>
        /// <param name="location">The location in the source code where the syntax error occurred.</param>
        internal SyntaxError(Location location) : base("Invalid syntax", location)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxError"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="location">The location in the source code where the syntax error occurred.</param>
        internal SyntaxError(string message, Location location)
            : base(message, location)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a runtime error occurs during script execution.
    /// </summary>
    public class RuntimeError : ScriptError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeError"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="location">The location in the source code where the runtime error occurred.</param>
        internal RuntimeError(string message, Location location) : base(message, location)
        {
        }
    }
}
