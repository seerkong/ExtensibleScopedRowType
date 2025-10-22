using System;
using System.Collections.Generic;
using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public static class TryHandler
{
    public const string Try_Keyword = "try";
    public const string Handle_Keyword = "handle";
    public const string Perform_Keyword = "perform";
    public const string InnerKey__EffectHandlerMap = "__EffectHandlerMap";
    public const string InnerKey__ContinuationAfterTry = "__ContinuationAfterTry";

    public static void ExpandTry(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        KnArray tryBlock = nodeToRun.Body;

        KnMap effectHandlerMap = new KnMap();

        KnChainNode iter = nodeToRun.Next;
        while (iter != null)
        {
            KnNode coreNode = iter.Core;
            if (NodeValueHelper.GetInnerString(coreNode).Equals(Handle_Keyword))
            {
                if (iter.Next == null)
                {
                    throw new Exception("Expected effect name after handle");
                }
                string effectNameStr = iter.Name.Value;
                if (iter.Next == null)
                {
                    throw new Exception("Expected effect handler after effect name");
                }
                KnNode effectHandlerNameNode = iter.Next.Core;
                effectHandlerMap[effectNameStr] = effectHandlerNameNode;

                iter = iter.Next.Next;
            }
            else
            {
                break;
            }
        }

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.Env_DiveLocalEnv);
        runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 4);
        runtime.AddOp(OpCode.Env_SetLocalEnv, InnerKey__ContinuationAfterTry);
        runtime.AddOp(OpCode.ValStack_PushValue, effectHandlerMap);
        runtime.AddOp(OpCode.Env_SetLocalEnv, InnerKey__EffectHandlerMap);
        runtime.AddOp(OpCode.Node_RunBlock, tryBlock);
        runtime.AddOp(OpCode.Env_Rise);
        runtime.OpBatchCommit();
    }

    public static void ExpandPerform(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        KnChainNode performNode = nodeToRun;
        List<KnNode> argNodes = performNode.InOutTable.GetInputNodes();
        string effectNameStr = performNode.Name.Value;

        // 假设 Lookup 返回一个 Dictionary<string, object>
        KnMap effectHandlerMap = runtime.Lookup(InnerKey__EffectHandlerMap) as KnMap;
        KnNode effectHandlerName = effectHandlerMap?[effectNameStr];
        string effectHandlerNameStr = (effectHandlerName as KnWord).Value;
        KnNode effectHandler = runtime.LookupThrowErrIfNotFound(effectHandlerNameStr);

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);
        runtime.AddOp(OpCode.Env_DiveLocalEnv);

        int argCount = argNodes.Count;
        runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, argCount + 2);

        for (int i = 0; i < argNodes.Count; i++)
        {
            runtime.AddOp(OpCode.Node_RunNode, argNodes[i]);
        }

        runtime.AddOp(OpCode.ValStack_PushValue, effectHandler);
        runtime.AddOp(OpCode.Ctrl_ApplyToFrameTop);
        runtime.AddOp(OpCode.Env_Rise);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal);
        runtime.OpBatchCommit();
    }
}