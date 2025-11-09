namespace DialogLang
{
    /// <summary>
    /// Represents a variable reference.
    /// </summary>
    internal class VariableNode : AstNode
    {
        public string Name { get; }

        public VariableNode(string name)
        {
            Name = name;
        }
    }
}
