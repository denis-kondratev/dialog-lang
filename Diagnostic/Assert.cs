using System;
using System.Diagnostics;

namespace GameDialog.Lang.Diagnostic
{
    /// <summary>
    /// Provides assertion methods that only execute in DEBUG builds.
    /// </summary>
    internal static class Assert
    {
        /// <summary>
        /// Asserts that the condition is true.
        /// </summary>
        [Conditional("DEBUG")]
        public static void IsTrue(bool condition, string? message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message ?? "Assertion failed: expected true.");
            }
        }

        /// <summary>
        /// Asserts that the value is not null.
        /// </summary>
        [Conditional("DEBUG")]
        public static void NotNull(object? value, string? message = null)
        {
            if (value is null)
            {
                throw new InvalidOperationException(message ?? "Assertion failed: value was null.");
            }
        }

        /// <summary>
        /// Unconditionally fails. Use for unreachable code paths.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Fail(string? message = null)
        {
            throw new InvalidOperationException(message ?? "Assertion failed.");
        }
    }
}