namespace DialogLang
{
    /// <summary>
    /// Represents a boolean literal in the AST.
    /// </summary>
    internal class BooleanNode : AstNode
    {
        public bool Value { get; }

        public BooleanNode(bool value)
        {
            Value = value;
        }
    }
}
