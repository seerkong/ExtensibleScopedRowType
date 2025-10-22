using Kson.Core.Node;
using Kson.Core.Util;
using Kson.Interpreter.Runtime;

public static class ChainExprHandler
{
    /// <summary>
    /// Expands a chain node for execution
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="chainNode">The chain node to run</param>
    public static void ExpandChainNodeExpr(KsonInterpreterRuntime runtime, KsChainNode chainNode)
    {
        runtime.OpBatchStart();
        // 在kson lang中expression,statement,block,function都有独立的frame
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);
        runtime.AddOp(KsonOpCode.Node_IterEvalChainNode, chainNode);
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end chain expr");
        runtime.OpBatchCommit();
    }

    /// <summary>
    /// Handles chain node evaluation
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void EvalChainNode(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        var chainNode = (KsChainNode)instruction.Memo;
        var core = chainNode.Core;
        var nextNode = chainNode.Next;

        runtime.OpBatchStart();
        if (core is KsWord prefixKeyword &&
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
        if (core is KsWord infixKeyword &&
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
            if (chainNode.CallType == KsCallType.PrefixCall)
            {
                if (chainNode.Core != null)
                {
                    runtime.AddOp(KsonOpCode.Node_RunNode, chainNode.Core);
                }
                else
                {
                    throw new Exception("PrefixCall should has a core node");
                }

                if (chainNode.CallParams != null)
                {
                    if (chainNode.CallParams != null)
                    {
                        for (var i = 0; i < chainNode.CallParams.Size(); i++)
                        {
                            runtime.AddOp(KsonOpCode.Node_RunNode, chainNode.CallParams[i]);
                        }
                    }
                    runtime.AddOp(KsonOpCode.Ctrl_ApplyToFrameBottom);
                }
            }
            else if (chainNode.CallType == KsCallType.PostfixCall)
            {
                if (chainNode.CallParams != null)
                {
                    for (var i = 0; i < chainNode.CallParams.Size(); i++)
                    {
                        runtime.AddOp(KsonOpCode.Node_RunNode, chainNode.CallParams[i]);
                    }
                }
                if (chainNode.Core != null)
                {
                    runtime.AddOp(KsonOpCode.Node_RunNode, chainNode.Core);
                }
                else
                {
                    throw new Exception("PostfixCall should has a core node");
                }
                runtime.AddOp(KsonOpCode.Ctrl_ApplyToFrameTop);
            }
            else if (chainNode.CallType == KsCallType.InstanceCall)
            {
                // Handle instance call
                if (chainNode.CallParams != null)
                {
                    runtime.AddOp(KsonOpCode.ValStack_PushFrame);
                    runtime.AddOp(KsonOpCode.Node_RunGetProperty);
                    for (var i = 0; i < chainNode.CallParams.Size(); i++)
                    {
                        runtime.AddOp(KsonOpCode.Node_RunNode, chainNode.CallParams[i]);
                    }
                    runtime.AddOp(KsonOpCode.Ctrl_ApplyToFrameBottom);
                    runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal);
                }
                else
                {
                    runtime.AddOp(KsonOpCode.Node_RunGetProperty);
                }
            }
            else if (chainNode.CallType == KsCallType.StaticSubscript)
            {
                // Handle static subscript
                // Not implemented yet
            }
            else if (chainNode.CallType == KsCallType.ContainerSubscript)
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
            runtime.AddOp(KsonOpCode.Node_RunNode, core);
        }

        // Process the next node in the chain
        if (nextNode != null)
        {
            runtime.AddOp(KsonOpCode.Node_IterEvalChainNode, nextNode);
        }

        runtime.OpBatchCommit();
    }

}