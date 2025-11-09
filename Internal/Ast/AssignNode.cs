namespace DialogLang
{
    /// <summary>
    /// Represents a variable assignment.
    /// </summary>
    internal class AssignNode : AstNode
    {
        public string VariableName { get; }
        public AstNode Value { get; }

        public AssignNode(string variableName, AstNode value)
        {
            VariableName = variableName;
            Value = value;
        }
    }
}
