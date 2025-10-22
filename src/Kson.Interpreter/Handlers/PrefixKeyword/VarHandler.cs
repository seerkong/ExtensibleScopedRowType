using Kson.Core.Node;
using Kson.Interpreter.Runtime;

namespace Kson.Interpreter.Handlers.PrefixKeyword;

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
    public static void ExpandDeclareVar(KsonInterpreterRuntime runtime, KsChainNode node)
    {
        // Get the variable name
        if (node.Next?.Core is KsWord nameWord)
        {
            var varName = nameWord.Value;

            // Get the variable value
            var varExpr = node.Next?.Next?.Core;

            runtime.AddOp(KsonOpCode.ValStack_PushFrame);
            runtime.AddOp(KsonOpCode.Node_RunNode, varExpr);
            // 必须duplicate栈顶值，以便实现对应其他语言的statement在kson lang中也有计算结果
            runtime.AddOp(KsonOpCode.ValStack_Duplicate);
            runtime.AddOp(KsonOpCode.Env_DeclareLocalVar, varName);
            runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end declare var");
        }
    }

    /// <summary>
    /// Expands a variable assignment
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="node">The variable assignment node</param>
    public static void ExpandSetVar(KsonInterpreterRuntime runtime, KsChainNode node)
    {
        // Get the variable name
        if (node.Next?.Core is KsWord nameWord)
        {
            var varName = nameWord.Value;

            // Get the variable value
            var varExpr = node.Next?.Next?.Core;

            runtime.AddOp(KsonOpCode.ValStack_PushFrame);
            runtime.AddOp(KsonOpCode.Node_RunNode, varExpr);
            // 必须duplicate栈顶值，以便实现对应其他语言的statement在kson lang中也有计算结果
            runtime.AddOp(KsonOpCode.ValStack_Duplicate);
            runtime.AddOp(KsonOpCode.Env_SetLocalEnv, varName);
            runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end set var");

        }
    }
}