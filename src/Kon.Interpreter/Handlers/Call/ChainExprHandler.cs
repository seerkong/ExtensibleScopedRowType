using Kon.Core.Node;
using Kon.Core.Node.Inner;
using Kon.Core.Util;
using Kon.Interpreter.Runtime;

public static class ChainExprHandler
{
    /// <summary>
    /// Expands a chain node for execution
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="chainNode">The chain node to run</param>
    public static void ExpandChainNodeExpr(InterpreterRuntime runtime, KnChainNode chainNode)
    {
        runtime.OpBatchStart();
        // 在Kon lang中expression,statement,block,function都有独立的frame
        runtime.AddOp(OpCode.ValStack_PushFrame);
        runtime.AddOp(OpCode.Node_IterEvalChainNode, chainNode);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end chain expr");
        runtime.OpBatchCommit();
    }

    /// <summary>
    /// Handles chain node evaluation
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void EvalChainNode(InterpreterRuntime runtime, Instruction instruction)
    {
        var chainNode = (KnChainNode)instruction.Memo;
        var core = chainNode.Core;
        var nextNode = chainNode.Next;

        runtime.OpBatchStart();
        if (core is KnWord prefixKeyword &&
                 runtime.ExtensionRegistry.IsPrefixKeyword(prefixKeyword.GetFullNameStr()))
        {
            // Handle prefix keyword
            var keyWordStr = prefixKeyword.GetFullNameStr();
            var instructionExpander = runtime.ExtensionRegistry.GetPrefixKeywordExpander(keyWordStr);
            instructionExpander(runtime, chainNode);
            // 对于前缀宏，展开后，不需要执行下面后续逻辑
            runtime.OpBatchCommit();
            return;
        }
        if (core is KnWord infixKeyword &&
                         runtime.ExtensionRegistry.IsInfixKeyword(infixKeyword.GetFullNameStr()))
        {
            // Handle prefix keyword
            var keyWordStr = infixKeyword.GetFullNameStr();
            var instructionExpander = runtime.ExtensionRegistry.GetInfixKeywordExpander(keyWordStr);
            instructionExpander(runtime, chainNode);
            // 对于中缀宏，展开后，不需要执行下面后续逻辑
            runtime.OpBatchCommit();
            return;
        }
        else if (chainNode.CallType != null)
        {
            if (chainNode.CallType == KnCallType.PrefixCall)
            {
                if (chainNode.Core != null)
                {
                    runtime.AddOp(OpCode.Node_RunNode, chainNode.Core);
                }
                else
                {
                    throw new Exception("PrefixCall should has a core node");
                }

                if (chainNode.InOutTable != null)
                {
                    if (chainNode.InOutTable != null)
                    {
                        List<KnNode> inputItems = chainNode.InOutTable.GetInputNodes();
                        for (var i = 0; i < inputItems.Count; i++)
                        {
                            runtime.AddOp(OpCode.Node_RunNode, inputItems[i]);
                        }
                    }
                    runtime.AddOp(OpCode.Ctrl_ApplyToFrameBottom);
                }
            }
            else if (chainNode.CallType == KnCallType.PostfixCall)
            {
                if (chainNode.InOutTable != null)
                {
                    List<KnNode> inputItems = chainNode.InOutTable.GetInputNodes();
                    for (var i = 0; i < inputItems.Count; i++)
                    {
                        runtime.AddOp(OpCode.Node_RunNode, inputItems[i]);
                    }
                }
                if (chainNode.Core != null)
                {
                    runtime.AddOp(OpCode.Node_RunNode, chainNode.Core);
                }
                else
                {
                    throw new Exception("PostfixCall should has a core node");
                }
                runtime.AddOp(OpCode.Ctrl_ApplyToFrameTop);
            }
            else if (chainNode.CallType == KnCallType.InstanceCall)
            {
                var targetNode = runtime.GetCurrentFiber().OperandStack.PopValue();

                // Handle instance call
                runtime.AddOp(OpCode.ValStack_PushFrame);
                runtime.AddOp(OpCode.ValStack_PushValue, targetNode);
                runtime.AddOp(OpCode.Node_RunGetProperty, chainNode);
                if (chainNode.InOutTable != null)
                {
                    List<KnNode> inputItems = chainNode.InOutTable.GetInputNodes();
                    for (var i = 0; i < inputItems.Count; i++)
                    {
                        runtime.AddOp(OpCode.Node_RunNode, inputItems[i]);
                    }
                }
                runtime.AddOp(OpCode.Ctrl_ApplyToFrameBottom);
                runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal);
            }
            else if (chainNode.CallType == KnCallType.StaticSubscript)
            {
                // Handle static subscript
                // Not implemented yet
            }
            else if (chainNode.CallType == KnCallType.ContainerSubscript)
            {
                // Handle container subscript
                SubscriptHandler.ExpandGetSubscript(runtime, chainNode.Core);
            }
        }
        else if (core == null)
        {
            // Handle the case where the core is null
            if (chainNode.Body != null)
            {
                // Run the body as a block
                BlockHandler._RunBlock(runtime, chainNode.Body);
            }
        }

        else
        {
            // Handle general expression
            runtime.AddOp(OpCode.Node_RunNode, core);
        }

        // Process the next node in the chain
        if (nextNode != null)
        {
            runtime.AddOp(OpCode.Node_IterEvalChainNode, nextNode);
        }

        runtime.OpBatchCommit();
    }

}