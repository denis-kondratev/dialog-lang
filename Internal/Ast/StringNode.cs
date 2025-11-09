namespace DialogLang
{
    /// <summary>
    /// Represents a string literal.
    /// </summary>
    internal class StringNode : AstNode
    {
        public string Value { get; }

        public StringNode(string value)
        {
            Value = value;
        }
    }
}
