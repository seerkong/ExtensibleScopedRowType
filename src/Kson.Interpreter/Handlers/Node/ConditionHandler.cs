using Kson.Core.Node;
using Kson.Interpreter.Runtime;



public static class ConditionHandler
{
    public class ConditionExprBlockPair
    {
        public KsNode Expr;
        public KsArray Block;
    }
    public class IterConditionMemo
    {
        public List<ConditionExprBlockPair> ExprAndBlockPairs = new();
        public KsArray FallbackBlock;
        public int PairIdx = 0;
    }

    public static void ExpandCondition(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        KsChainNode iter = nodeToRun.Next;
        List<ConditionExprBlockPair> exprAndBlockPairs = new();
        KsArray fallbackBlock = null;

        while (iter != null)
        {
            KsNode clauseCore = iter.Core;
            string clauseCoreStr = NodeValueHelper.GetInnerString(clauseCore);
            if ("else".Equals(clauseCoreStr))
            {
                fallbackBlock = iter.Body;
            }
            else
            {
                exprAndBlockPairs.Add(new ConditionExprBlockPair
                {
                    Expr = iter.Core,
                    Block = iter.Body
                });
            }
            iter = iter.Next;
        }

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);
        runtime.AddOp(KsonOpCode.Ctrl_IterConditionPairs, new IterConditionMemo
        {
            ExprAndBlockPairs = exprAndBlockPairs,
            PairIdx = 0,
            FallbackBlock = fallbackBlock
        });

        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end condition");
        runtime.OpBatchCommit();
    }

    public static void RunConditionPair(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        IterConditionMemo lastContMemo = opContState.Memo as IterConditionMemo;
        List<ConditionExprBlockPair> exprAndBlockPairs = lastContMemo.ExprAndBlockPairs;
        int pairIdx = lastContMemo.PairIdx;
        KsArray fallbackBlock = lastContMemo.FallbackBlock;
        KsNode condition = exprAndBlockPairs[pairIdx].Expr;
        KsArray ifTrueClause = exprAndBlockPairs[pairIdx].Block;

        int curOpStackTopIdx = runtime.GetCurrentFiber().InstructionStack.GetCurTopIdx();

        runtime.OpBatchStart();
        // eval condition
        runtime.AddOp(KsonOpCode.Node_RunNode, condition);
        // 如果条件判断失败，调到下个条件判断，或者是else分支
        runtime.AddOp(KsonOpCode.Ctrl_JumpIfFalse, curOpStackTopIdx + 1);
        // 如果条件判断成功，运行对应的判断成功的分支后，跳转到curOpStackTopIdx,即ValStack_PopFrameAndPushTopVal
        runtime.AddOp(KsonOpCode.Node_RunBlock, ifTrueClause);
        runtime.AddOp(KsonOpCode.Ctrl_Jump, curOpStackTopIdx);

        // 如果条件判断失败，会执行下面的指令之一
        if (pairIdx < (exprAndBlockPairs.Count - 1))
        {
            runtime.AddOp(KsonOpCode.Ctrl_IterConditionPairs, new IterConditionMemo
            {
                ExprAndBlockPairs = exprAndBlockPairs,
                PairIdx = pairIdx + 1,
                FallbackBlock = fallbackBlock
            });
        }
        else
        {
            runtime.AddOp(KsonOpCode.Node_RunBlock, fallbackBlock);
        }
        runtime.OpBatchCommit();
    }
}