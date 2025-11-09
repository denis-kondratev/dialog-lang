namespace DialogLang
{
    /// <summary>
    /// Represents a request for input from the script.
    /// The script execution will pause until a value is provided via the Set method.
    /// </summary>
    public interface IInputRequest
    {
        /// <summary>
        /// Gets the name of the variable to set.
        /// </summary>
        string VariableName { get; }

        /// <summary>
        /// Sets the input value and resumes script execution.
        /// </summary>
        /// <param name="value">The value to assign to the variable.</param>
        void Set(object value);
    }
}
