using System.Collections.Generic;

namespace DialogLang
{
    /// <summary>
    /// Represents a block of statements in the AST.
    /// </summary>
    internal class BlockNode : AstNode
    {
        public List<AstNode> Statements { get; }

        public BlockNode(List<AstNode> statements)
        {
            Statements = statements;
        }
    }
}
