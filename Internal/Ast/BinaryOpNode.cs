namespace DialogLang
{
    /// <summary>
    /// Represents a binary operation.
    /// </summary>
    internal class BinaryOpNode : AstNode
    {
        public AstNode Left { get; }
        public TokenType Operator { get; }
        public AstNode Right { get; }

        public BinaryOpNode(AstNode left, TokenType op, AstNode right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
