using Kon.Core.Node;
using Kon.Interpreter.Runtime;

namespace Kon.Interpreter.Handlers.PrefixKeyword;

/// <summary>
/// Handler for variable declaration and assignment
/// </summary>
public static class VarHandler
{
    /// <summary>
    /// Expands a variable declaration
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="node">The variable declaration node</param>
    public static void ExpandDeclareVar(InterpreterRuntime runtime, KnChainNode node)
    {
        // Get the variable name
        if (node.Next?.Core is KnWord nameWord)
        {
            var varName = nameWord.Value;

            // Get the variable value
            var varExpr = node.Next?.Next?.Core;

            runtime.AddOp(OpCode.ValStack_PushFrame);
            runtime.AddOp(OpCode.Node_RunNode, varExpr);
            // 必须duplicate栈顶值，以便实现对应其他语言的statement在Kon lang中也有计算结果
            runtime.AddOp(OpCode.ValStack_Duplicate);
            runtime.AddOp(OpCode.Env_DeclareLocalVar, varName);
            runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end declare var");
        }
    }

    /// <summary>
    /// Expands a variable assignment
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="node">The variable assignment node</param>
    public static void ExpandSetVar(InterpreterRuntime runtime, KnChainNode node)
    {
        // Get the variable name
        if (node.Next?.Core is KnWord nameWord)
        {
            var varName = nameWord.Value;

            // Get the variable value
            var varExpr = node.Next?.Next?.Core;

            runtime.AddOp(OpCode.ValStack_PushFrame);
            runtime.AddOp(OpCode.Node_RunNode, varExpr);
            // 必须duplicate栈顶值，以便实现对应其他语言的statement在Kon lang中也有计算结果
            runtime.AddOp(OpCode.ValStack_Duplicate);
            runtime.AddOp(OpCode.Env_SetLocalEnv, varName);
            runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end set var");

        }
    }
}