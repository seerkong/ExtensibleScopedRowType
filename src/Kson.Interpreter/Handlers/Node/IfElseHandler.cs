using Kson.Core.Node;
using Kson.Interpreter.Runtime;
using static ConditionHandler;

public static class IfElseHandler
{
    public static void ExpandIfElse(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        List<ConditionExprBlockPair> exprAndBlockPairs = new List<ConditionExprBlockPair>();
        KsNode conditionExpr = nodeToRun.Next?.Core;
        KsArray ifTrueBranch = nodeToRun.Next?.Body;
        KsArray ifFalseBranch = new KsArray();

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
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);

        IterConditionMemo memo = new IterConditionMemo
        {
            ExprAndBlockPairs = exprAndBlockPairs,
            PairIdx = 0,
            FallbackBlock = ifFalseBranch
        };

        runtime.AddOp(KsonOpCode.Ctrl_IterConditionPairs, memo);
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end-if");
        runtime.OpBatchCommit();
    }
}