using System;

namespace DialogLang
{
    /// <summary>
    /// Base class for all input requests from the script.
    /// The script execution will pause until a value is provided via the Set method.
    /// </summary>
    public abstract class Request
    {
        /// <summary>
        /// Gets the name of the variable to set.
        /// </summary>
        public string VariableName { get; }

        protected Request(string variableName)
        {
            VariableName = variableName;
        }
    }

    /// <summary>
    /// Represents a request for input of any type.
    /// Syntax: >> variableName
    /// </summary>
    public sealed class RequestAny : Request
    {
        private readonly Action<object> _setter;

        public RequestAny(string variableName, Action<object> setter) : base(variableName)
        {
            _setter = setter;
        }

        /// <summary>
        /// Sets the input value and resumes script execution.
        /// </summary>
        /// <param name="value">The value to assign to the variable.</param>
        public void Set(object value)
        {
            _setter(value);
        }
    }

    /// <summary>
    /// Represents a request for numeric input.
    /// Syntax: >> variableName as number
    /// Accepts both integer and floating-point values.
    /// Integer values (e.g., 21) are stored as int.
    /// Decimal values (e.g., 21.5) are stored as float.
    /// </summary>
    public sealed class RequestNumber : Request
    {
        private readonly Action<object> _setter;

        public RequestNumber(string variableName, Action<object> setter) : base(variableName)
        {
            _setter = setter;
        }

        /// <summary>
        /// Sets the numeric input value and resumes script execution.
        /// Accepts int, float, or double values.
        /// </summary>
        /// <param name="value">The numeric value to assign to the variable.</param>
        public void Set(double value)
        {
            // Convert to int if the value is a whole number, otherwise use float
            if (Math.Abs(value % 1) < double.Epsilon)
            {
                _setter((int)value);
            }
            else
            {
                _setter((float)value);
            }
        }

        /// <summary>
        /// Sets the numeric input value and resumes script execution.
        /// </summary>
        /// <param name="value">The numeric value to assign to the variable.</param>
        public void Set(int value)
        {
            _setter(value);
        }

        /// <summary>
        /// Sets the numeric input value and resumes script execution.
        /// </summary>
        /// <param name="value">The numeric value to assign to the variable.</param>
        public void Set(float value)
        {
            _setter(value);
        }
    }

    /// <summary>
    /// Represents a request for string input.
    /// Syntax: >> variableName as string
    /// </summary>
    public sealed class RequestString : Request
    {
        private readonly Action<string> _setter;

        public RequestString(string variableName, Action<string> setter) : base(variableName)
        {
            _setter = setter;
        }

        /// <summary>
        /// Sets the string input value and resumes script execution.
        /// </summary>
        /// <param name="value">The string value to assign to the variable.</param>
        public void Set(string value)
        {
            _setter(value);
        }
    }

    /// <summary>
    /// Represents a request for boolean input.
    /// Syntax: >> variableName as bool
    /// </summary>
    public sealed class RequestBool : Request
    {
        private readonly Action<bool> _setter;

        public RequestBool(string variableName, Action<bool> setter) : base(variableName)
        {
            _setter = setter;
        }

        /// <summary>
        /// Sets the boolean input value and resumes script execution.
        /// </summary>
        /// <param name="value">The boolean value to assign to the variable.</param>
        public void Set(bool value)
        {
            _setter(value);
        }
    }
}
