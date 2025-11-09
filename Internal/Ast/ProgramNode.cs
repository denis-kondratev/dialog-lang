using System.Collections.Generic;

namespace DialogLang
{
    /// <summary>
    /// Represents a program (list of statements).
    /// </summary>
    internal class ProgramNode : AstNode
    {
        public List<AstNode> Statements { get; }

        public ProgramNode(List<AstNode> statements)
        {
            Statements = statements;
        }
    }
}
