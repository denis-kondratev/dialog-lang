using System.Collections.Generic;

namespace DialogLang
{
    /// <summary>
    /// Represents a function call.
    /// </summary>
    internal class FunctionCallNode : AstNode
    {
        public string FunctionName { get; }
        public List<AstNode> Arguments { get; }

        public FunctionCallNode(string functionName, List<AstNode> arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }
    }
}
