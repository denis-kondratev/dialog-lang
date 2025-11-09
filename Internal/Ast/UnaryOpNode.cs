namespace DialogLang
{
    /// <summary>
    /// Represents a unary operation.
    /// </summary>
    internal class UnaryOpNode : AstNode
    {
        public TokenType Operator { get; }
        public AstNode Operand { get; }

        public UnaryOpNode(TokenType op, AstNode operand)
        {
            Operator = op;
            Operand = operand;
        }
    }
}
