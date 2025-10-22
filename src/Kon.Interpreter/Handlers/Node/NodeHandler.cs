using Kon.Core.Node;
using Kon.Core.Util;
using Kon.Interpreter.Runtime;

namespace Kon.Interpreter.Handlers.Node;

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
    public static void ExpandNode(InterpreterRuntime runtime, KnNode nodeToRun)
    {
        if (nodeToRun is KnInt64 or KnDouble or KnString or KnBoolean or KnNull)
        {
            // For primitive values, just push them onto the stack
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.ValStack_PushValue, nodeToRun);
            runtime.OpBatchCommit();
        }
        else if (nodeToRun is KnWord word)
        {
            // For words, look up their value in the environment
            var value = runtime.LookupThrowErrIfNotFound(word.Value);
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.ValStack_PushValue, value ?? KnNull.Null);
            runtime.OpBatchCommit();
        }
        else if (nodeToRun is KnArray array)
        {
            ArrayHandler.ExpandArray(runtime, array);
        }
        else if (nodeToRun is KnMap map)
        {
            MapHandler.ExpandMap(runtime, map);
        }
        else if (nodeToRun is KnChainNode chainNode)
        {
            // For chain nodes, handle the chain of expressions
            ChainExprHandler.ExpandChainNodeExpr(runtime, chainNode);
        }
        else
        {
            // For other node types, just push them onto the stack
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.ValStack_PushValue, nodeToRun);
            runtime.OpBatchCommit();
        }
    }

    /// <summary>
    /// Handles node execution
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunNode(InterpreterRuntime runtime, Instruction instruction)
    {
        var nodeToRun = (KnNode)instruction.Memo;

        runtime.OpBatchStart();
        ExpandNode(runtime, nodeToRun);
        runtime.OpBatchCommit();
    }

}