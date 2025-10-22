using System;
using System.Collections.Generic;
using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class TryHandler
{
    public const string Try_Keyword = "try";
    public const string Handle_Keyword = "handle";
    public const string Perform_Keyword = "perform";
    public const string InnerKey__EffectHandlerMap = "__EffectHandlerMap";
    public const string InnerKey__ContinuationAfterTry = "__ContinuationAfterTry";

    public static void ExpandTry(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        KsArray tryBlock = nodeToRun.Body;

        KsMap effectHandlerMap = new KsMap();

        KsChainNode iter = nodeToRun.Next;
        while (iter != null)
        {
            KsNode coreNode = iter.Core;
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
                KsNode effectHandlerNameNode = iter.Next.Core;
                effectHandlerMap[effectNameStr] = effectHandlerNameNode;

                iter = iter.Next.Next;
            }
            else
            {
                break;
            }
        }

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.Env_DiveLocalEnv);
        runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, 4);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, InnerKey__ContinuationAfterTry);
        runtime.AddOp(KsonOpCode.ValStack_PushValue, effectHandlerMap);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, InnerKey__EffectHandlerMap);
        runtime.AddOp(KsonOpCode.Node_RunBlock, tryBlock);
        runtime.AddOp(KsonOpCode.Env_Rise);
        runtime.OpBatchCommit();
    }

    public static void ExpandPerform(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        KsChainNode performNode = nodeToRun;
        KsArray effectArgs = performNode.CallParams;
        string effectNameStr = performNode.Name.Value;

        // 假设 Lookup 返回一个 Dictionary<string, object>
        KsMap effectHandlerMap = runtime.Lookup(InnerKey__EffectHandlerMap) as KsMap;
        KsNode effectHandlerName = effectHandlerMap?[effectNameStr];
        string effectHandlerNameStr = (effectHandlerName as KsWord).Value;
        KsNode effectHandler = runtime.LookupThrowErrIfNotFound(effectHandlerNameStr);

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);
        runtime.AddOp(KsonOpCode.Env_DiveLocalEnv);

        int argCount = effectArgs == null ? 0 : effectArgs.GetItems().Count;
        runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, argCount + 2);

        if (effectArgs != null)
        {
            List<KsNode> args = effectArgs.GetItems();
            for (int i = 0; i < args.Count; i++)
            {
                runtime.AddOp(KsonOpCode.Node_RunNode, effectArgs[i]);
            }
        }

        runtime.AddOp(KsonOpCode.ValStack_PushValue, effectHandler);
        runtime.AddOp(KsonOpCode.Ctrl_ApplyToFrameTop);
        runtime.AddOp(KsonOpCode.Env_Rise);
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal);
        runtime.OpBatchCommit();
    }
}