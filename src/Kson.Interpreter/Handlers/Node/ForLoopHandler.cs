using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class ForLoopHandler
{
    private class ForLoopMemo
    {
        public KsNode PreConditionExpr;
        public KsNode AfterBlockExpr;
        public KsArray LoopBody;
    }

    public static void ExpandForLoop(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        KsChainNode forStatement = nodeToRun;
        KsMap initVarMap = forStatement.Conf;
        KsNode preConditionExpr = forStatement.Next?.Core;
        KsNode afterBlockExpr = forStatement.Next?.Next?.Core;
        KsArray loopBody = forStatement.Next?.Next?.Body;

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);
        runtime.AddOp(KsonOpCode.Env_DiveLocalEnv);
        if (initVarMap != null)
        {
            foreach (string initVarKey in initVarMap.Keys)
            {
                KsNode initVarExpr = initVarMap[initVarKey];
                runtime.AddOp(KsonOpCode.Node_RunNode, initVarExpr);
                runtime.AddOp(KsonOpCode.Env_SetLocalEnv, initVarKey);
            }
        }

        // runtime.AddOp(KsonOpCode.Node_RunNode, initVarMap);
        // runtime.AddOp(KsonOpCode.Env_BindEnvByMap);

        runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, 2);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, "break");

        ForLoopMemo iterMemo = new ForLoopMemo
        {
            PreConditionExpr = preConditionExpr,
            AfterBlockExpr = afterBlockExpr,
            LoopBody = loopBody,
        };

        runtime.AddOp(KsonOpCode.Ctrl_IterForLoop, iterMemo);
        runtime.AddOp(KsonOpCode.Env_Rise);
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end for");
        runtime.OpBatchCommit();
    }

    public static void RunIterForLoop(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        ForLoopMemo lastMemo = opContState.Memo as ForLoopMemo;
        KsNode preConditionExpr = lastMemo.PreConditionExpr;
        KsNode afterBlockExpr = lastMemo.AfterBlockExpr;
        KsArray loopBody = lastMemo.LoopBody;

        int curOpStackTopIdx = runtime.GetCurrentFiber().InstructionStack.GetCurTopIdx();

        runtime.OpBatchStart();

        // eval condition
        runtime.AddOp(KsonOpCode.Node_RunNode, preConditionExpr);
        // 如果条件判断失败，调到下个条件判断，或者是else分支
        runtime.AddOp(KsonOpCode.Ctrl_JumpIfFalse, curOpStackTopIdx);
        // 如果条件判断成功
        runtime.AddOp(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, "continue");
        runtime.AddOp(KsonOpCode.Node_RunBlock, loopBody);
        runtime.AddOp(KsonOpCode.ValStack_PopValue);
        runtime.AddOp(KsonOpCode.Node_RunNode, afterBlockExpr);
        runtime.AddOp(KsonOpCode.ValStack_PopValue);
        // 执行post expr后，继续执行下个循环
        runtime.AddOp(KsonOpCode.Ctrl_IterForLoop, lastMemo);
        runtime.OpBatchCommit();
    }
}