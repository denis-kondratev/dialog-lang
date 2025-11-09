using System;

namespace DialogLang
{
    /// <summary>
    /// Base exception for interpreter errors.
    /// </summary>
    public class InterpreterException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public InterpreterException(string message, int line, int column) 
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }

        public InterpreterException(string message, int line, int column, Exception innerException) 
            : base($"{message} at line {line}, column {column}", innerException)
        {
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// Exception thrown during lexical analysis.
    /// </summary>
    public class LexerException : InterpreterException
    {
        public LexerException(string message, int line, int column) 
            : base(message, line, column)
        {
        }
    }

    /// <summary>
    /// Exception thrown during parsing.
    /// </summary>
    public class ParserException : InterpreterException
    {
        public ParserException(string message, int line, int column) 
            : base(message, line, column)
        {
        }
    }

    /// <summary>
    /// Exception thrown during runtime execution.
    /// </summary>
    public class RuntimeException : InterpreterException
    {
        public RuntimeException(string message, int line, int column) 
            : base(message, line, column)
        {
        }

        public RuntimeException(string message, int line, int column, Exception innerException) 
            : base(message, line, column, innerException)
        {
        }
    }
}
