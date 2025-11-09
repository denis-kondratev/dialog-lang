namespace DialogLang
{
    /// <summary>
    /// Represents an input statement.
    /// </summary>
    internal class InputNode : AstNode
    {
        public string VariableName { get; }

        public InputNode(string variableName)
        {
            VariableName = variableName;
        }
    }
}
