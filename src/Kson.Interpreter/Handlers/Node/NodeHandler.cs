using Kson.Core.Node;
using Kson.Core.Util;
using Kson.Interpreter.Runtime;

namespace Kson.Interpreter.Handlers.Node;

/// <summary>
/// Handler for node operations
/// </summary>
public static class NodeHandler
{
    /// <summary>
    /// Expands a node for execution
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="nodeToRun">The node to run</param>
    public static void ExpandNode(KsonInterpreterRuntime runtime, KsNode nodeToRun)
    {
        if (nodeToRun is KsInt64 or KsDouble or KsString or KsBoolean or KsNull)
        {
            // For primitive values, just push them onto the stack
            runtime.AddOpDirectly(KsonOpCode.ValStack_PushValue, nodeToRun);
        }
        else if (nodeToRun is KsWord word)
        {
            // For words, look up their value in the environment
            var value = runtime.LookupThrowErrIfNotFound(word.Value);
            runtime.AddOpDirectly(KsonOpCode.ValStack_PushValue, value ?? KsNull.Null);
        }
        else if (nodeToRun is KsArray array)
        {
            ArrayHandler.ExpandArray(runtime, array);
        }
        else if (nodeToRun is KsMap map)
        {
            MapHandler.ExpandMap(runtime, map);
        }
        else if (nodeToRun is KsChainNode chainNode)
        {
            // For chain nodes, handle the chain of expressions
            ChainExprHandler.ExpandChainNodeExpr(runtime, chainNode);
        }
        else
        {
            // For other node types, just push them onto the stack
            runtime.AddOpDirectly(KsonOpCode.ValStack_PushValue, nodeToRun);
        }
    }

    /// <summary>
    /// Handles node execution
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunNode(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        var nodeToRun = (KsNode)instruction.Memo;

        runtime.OpBatchStart();
        ExpandNode(runtime, nodeToRun);
        runtime.OpBatchCommit();
    }

}