namespace DialogLang
{
    /// <summary>
    /// Represents a number literal.
    /// </summary>
    internal class NumberNode : AstNode
    {
        public object Value { get; }

        public NumberNode(object value)
        {
            Value = value;
        }
    }
}
