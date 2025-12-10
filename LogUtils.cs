using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GameDialog.Lang
{
    /// <summary>
    /// Provides utility methods for formatting script errors with source context.
    /// </summary>
    public static class LogUtils
    {
        /// <summary>
        /// Formats a script exception with source code context, showing the error line and underlining the error location.
        /// </summary>
        /// <param name="exception">The script exception to format.</param>
        /// <param name="indent">Optional indentation string (default is 4 spaces).</param>
        /// <returns>A formatted error message with source context, or a fallback message if source cannot be read.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static string FormatError(ScriptError exception, string indent = "    ")
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (indent is null)
            {
                throw new ArgumentNullException(nameof(indent));
            }

            try
            {
                var result = new StringBuilder();
                result.AppendLine($"{exception.GetType().Name}: {exception.Message}");
                result.AppendLine($"{exception.SourceCode}, line {exception.Line}");

                if (exception.Line <= 0)
                {
                    result.AppendLine(indent + "<unknown location>");
                    return result.ToString();
                }

                string? errorLine = GetSourceLine(exception.SourceCode, exception.Line);
                if (errorLine is null)
                {
                    result.AppendLine(indent + "<line unavailable>");
                    return result.ToString();
                }

                result.AppendLine(indent + errorLine);

                // Build the underline with proper spacing and tabs
                var underlineBuilder = new StringBuilder();
                underlineBuilder.Append(indent);

                // Add spaces/tabs up to the start column
                for (int i = 1; i < exception.Initial; i++)
                {
                    if (i <= errorLine.Length && errorLine[i - 1] == '\t')
                    {
                        underlineBuilder.Append('\t');
                    }
                    else
                    {
                        underlineBuilder.Append(' ');
                    }
                }

                // Underline the error range with tildes
                int underlineLength = Math.Max(1, exception.Final - exception.Initial);
                underlineBuilder.Append('¯', underlineLength);
                result.AppendLine(underlineBuilder.ToString());

                return result.ToString();
            }
            catch (Exception)
            {
                return indent + "<unable to display error location>";
            }
        }

        /// <summary>
        /// Gets a specific line from the source code.
        /// </summary>
        /// <param name="source">The source to read from.</param>
        /// <param name="lineNumber">The line number (1-based).</param>
        /// <returns>The line content, or null if the line cannot be read.</returns>
        private static string? GetSourceLine(Source source, int lineNumber)
        {
            using var reader = source.CreateReader();
            return ReadLines(reader).Skip(lineNumber - 1).FirstOrDefault();
        }

        /// <summary>
        /// Reads all lines from a TextReader as an enumerable.
        /// </summary>
        private static IEnumerable<string> ReadLines(TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
