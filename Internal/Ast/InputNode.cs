namespace DialogLang
{
    /// <summary>
    /// Represents the expected type for input.
    /// </summary>
    internal enum InputType
    {
        Any,
        Number,
        String,
        Bool
    }

    /// <summary>
    /// Represents an input statement.
    /// </summary>
    internal class InputNode : AstNode
    {
        public string VariableName { get; }
        public InputType ExpectedType { get; }

        public InputNode(string variableName, InputType expectedType = InputType.Any)
        {
            VariableName = variableName;
            ExpectedType = expectedType;
        }
    }
}
