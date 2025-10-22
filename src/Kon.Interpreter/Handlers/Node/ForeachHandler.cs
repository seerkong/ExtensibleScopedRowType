using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public static class ForeachHandler
{
    private class ForeachIterMemo
    {
        public int Index = 0;
        public KnArray LoopBody;
        public string ItemVarName;
    }

    public static void ExpandForeach(InterpreterRuntime runtime, KnChainNode nodeToRun)
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
        KnArray loopBody = nodeToRun.Next.Next.Next.Body;

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);
        runtime.AddOp(OpCode.Env_DiveLocalEnv);
        runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
        runtime.AddOp(OpCode.Env_SetLocalEnv, "break");
        runtime.AddOp(OpCode.Node_RunNode, collectionVarWord);

        ForeachIterMemo iterMemo = new ForeachIterMemo
        {
            Index = 0,
            LoopBody = loopBody,
            ItemVarName = itemVarName
        };

        runtime.AddOp(OpCode.Ctrl_IterForEachLoop, iterMemo);
        runtime.AddOp(OpCode.Env_Rise);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end foreach");
        runtime.OpBatchCommit();
    }

    public static void RunIterForeachLoop(InterpreterRuntime runtime, Instruction opContState)
    {
        ForeachIterMemo lastMemo = opContState.Memo as ForeachIterMemo;
        int index = lastMemo.Index;
        KnArray loopBody = lastMemo.LoopBody;
        string itemVarName = lastMemo.ItemVarName;
        KnNode collectionNode = runtime.GetCurrentFiber().OperandStack.PeekTop();
        // TODO 支持foreach 其他容器节点
        KnArray collection = collectionNode as KnArray;
        runtime.OpBatchStart();
        if (index <= (collection.GetItems().Count - 1))
        {
            Env currentEnv = runtime.GetCurEnv();
            currentEnv.Define(itemVarName, collection[index]);
            runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
            runtime.AddOp(OpCode.Env_SetLocalEnv, "continue");
            runtime.AddOp(OpCode.Node_RunBlock, loopBody);
            runtime.AddOp(OpCode.ValStack_PopValue);

            ForeachIterMemo newMemo = new ForeachIterMemo
            {
                Index = index + 1,
                LoopBody = loopBody,
                ItemVarName = itemVarName
            };

            runtime.AddOp(OpCode.Ctrl_IterForEachLoop, newMemo);
        }
        runtime.OpBatchCommit();
    }
}