namespace DialogLang
{
    /// <summary>
    /// Represents an if statement.
    /// </summary>
    internal class IfNode : AstNode
    {
        public AstNode Condition { get; }
        public AstNode ThenBody { get; }
        public AstNode? ElseBody { get; }

        public IfNode(AstNode condition, AstNode thenBody, AstNode? elseBody = null)
        {
            Condition = condition;
            ThenBody = thenBody;
            ElseBody = elseBody;
        }
    }
}
