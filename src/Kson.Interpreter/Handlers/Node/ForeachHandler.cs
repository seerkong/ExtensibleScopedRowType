using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class ForeachHandler
{
    private class ForeachIterMemo
    {
        public int Index = 0;
        public KsArray LoopBody;
        public string ItemVarName;
    }

    public static void ExpandForeach(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        if (nodeToRun.Next == null)
        {
            throw new Exception("Invalid foreach syntax: missing item variable name");
        }
        string itemVarName = NodeValueHelper.GetInnerString(nodeToRun.Next.Core);

        if (nodeToRun.Next.Next?.Next == null)
        {
            throw new Exception("Invalid foreach syntax: missing collection variable");
        }
        object collectionVarWord = nodeToRun.Next.Next.Next.Core;
        KsArray loopBody = nodeToRun.Next.Next.Next.Body;

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);
        runtime.AddOp(KsonOpCode.Env_DiveLocalEnv);
        runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, "break");
        runtime.AddOp(KsonOpCode.Node_RunNode, collectionVarWord);

        ForeachIterMemo iterMemo = new ForeachIterMemo
        {
            Index = 0,
            LoopBody = loopBody,
            ItemVarName = itemVarName
        };

        runtime.AddOp(KsonOpCode.Ctrl_IterForEachLoop, iterMemo);
        runtime.AddOp(KsonOpCode.Env_Rise);
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end foreach");
        runtime.OpBatchCommit();
    }

    public static void RunIterForeachLoop(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        ForeachIterMemo lastMemo = opContState.Memo as ForeachIterMemo;
        int index = lastMemo.Index;
        KsArray loopBody = lastMemo.LoopBody;
        string itemVarName = lastMemo.ItemVarName;
        KsNode collectionNode = runtime.GetCurrentFiber().OperandStack.PeekTop();
        // TODO 支持foreach 其他容器节点
        KsArray collection = collectionNode as KsArray;
        runtime.OpBatchStart();
        if (index <= (collection.GetItems().Count - 1))
        {
            Env currentEnv = runtime.GetCurEnv();
            currentEnv.Define(itemVarName, collection[index]);
            runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
            runtime.AddOp(KsonOpCode.Env_SetLocalEnv, "continue");
            runtime.AddOp(KsonOpCode.Node_RunBlock, loopBody);
            runtime.AddOp(KsonOpCode.ValStack_PopValue);

            ForeachIterMemo newMemo = new ForeachIterMemo
            {
                Index = index + 1,
                LoopBody = loopBody,
                ItemVarName = itemVarName
            };

            runtime.AddOp(KsonOpCode.Ctrl_IterForEachLoop, newMemo);
        }
        runtime.OpBatchCommit();
    }
}