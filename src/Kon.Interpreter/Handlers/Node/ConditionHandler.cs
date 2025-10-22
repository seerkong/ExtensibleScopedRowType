using Kon.Core.Node;
using Kon.Interpreter.Runtime;



public static class ConditionHandler
{
    public class ConditionExprBlockPair
    {
        public KnNode Expr;
        public KnArray Block;
    }
    public class IterConditionMemo
    {
        public List<ConditionExprBlockPair> ExprAndBlockPairs = new();
        public KnArray FallbackBlock;
        public int PairIdx = 0;
    }

    public static void ExpandCondition(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        KnChainNode iter = nodeToRun.Next;
        List<ConditionExprBlockPair> exprAndBlockPairs = new();
        KnArray fallbackBlock = null;

        while (iter != null)
        {
            KnNode clauseCore = iter.Core;
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
        runtime.AddOp(OpCode.ValStack_PushFrame);
        runtime.AddOp(OpCode.Ctrl_IterConditionPairs, new IterConditionMemo
        {
            ExprAndBlockPairs = exprAndBlockPairs,
            PairIdx = 0,
            FallbackBlock = fallbackBlock
        });

        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end condition");
        runtime.OpBatchCommit();
    }

    public static void RunConditionPair(InterpreterRuntime runtime, Instruction opContState)
    {
        IterConditionMemo lastContMemo = opContState.Memo as IterConditionMemo;
        List<ConditionExprBlockPair> exprAndBlockPairs = lastContMemo.ExprAndBlockPairs;
        int pairIdx = lastContMemo.PairIdx;
        KnArray fallbackBlock = lastContMemo.FallbackBlock;
        KnNode condition = exprAndBlockPairs[pairIdx].Expr;
        KnArray ifTrueClause = exprAndBlockPairs[pairIdx].Block;

        int curOpStackTopIdx = runtime.GetCurrentFiber().InstructionStack.GetCurTopIdx();

        runtime.OpBatchStart();
        // eval condition
        runtime.AddOp(OpCode.Node_RunNode, condition);
        // 如果条件判断失败，调到下个条件判断，或者是else分支
        runtime.AddOp(OpCode.Ctrl_JumpIfFalse, curOpStackTopIdx + 1);
        // 如果条件判断成功，运行对应的判断成功的分支后，跳转到curOpStackTopIdx,即ValStack_PopFrameAndPushTopVal
        runtime.AddOp(OpCode.Node_RunBlock, ifTrueClause);
        runtime.AddOp(OpCode.Ctrl_Jump, curOpStackTopIdx);

        // 如果条件判断失败，会执行下面的指令之一
        if (pairIdx < (exprAndBlockPairs.Count - 1))
        {
            runtime.AddOp(OpCode.Ctrl_IterConditionPairs, new IterConditionMemo
            {
                ExprAndBlockPairs = exprAndBlockPairs,
                PairIdx = pairIdx + 1,
                FallbackBlock = fallbackBlock
            });
        }
        else
        {
            runtime.AddOp(OpCode.Node_RunBlock, fallbackBlock);
        }
        runtime.OpBatchCommit();
    }
}