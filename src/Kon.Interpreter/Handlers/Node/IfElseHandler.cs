using Kon.Core.Node;
using Kon.Interpreter.Runtime;
using static ConditionHandler;

public static class IfElseHandler
{
    public static void ExpandIfElse(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        List<ConditionExprBlockPair> exprAndBlockPairs = new List<ConditionExprBlockPair>();
        KnNode conditionExpr = nodeToRun.Next?.Core;
        KnArray ifTrueBranch = nodeToRun.Next?.Body;
        KnArray ifFalseBranch = new KnArray();

        if (nodeToRun.Next?.Next != null)
        {
            ifFalseBranch = nodeToRun.Next?.Next?.Body;
        }

        exprAndBlockPairs.Add(new ConditionExprBlockPair
        {
            Expr = conditionExpr, // condition expr
            Block = ifTrueBranch    // condition true block
        });

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);

        IterConditionMemo memo = new IterConditionMemo
        {
            ExprAndBlockPairs = exprAndBlockPairs,
            PairIdx = 0,
            FallbackBlock = ifFalseBranch
        };

        runtime.AddOp(OpCode.Ctrl_IterConditionPairs, memo);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end-if");
        runtime.OpBatchCommit();
    }
}