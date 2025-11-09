namespace DialogLang
{
    /// <summary>
    /// Represents an output statement.
    /// </summary>
    internal class OutputNode : AstNode
    {
        public AstNode Expression { get; }

        public OutputNode(AstNode expression)
        {
            Expression = expression;
        }
    }
}
