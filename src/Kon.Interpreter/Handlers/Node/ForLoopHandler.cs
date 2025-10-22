using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public static class ForLoopHandler
{
    private class ForLoopMemo
    {
        public KnNode PreConditionExpr;
        public KnNode AfterBlockExpr;
        public KnArray LoopBody;
    }

    public static void ExpandForLoop(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        KnChainNode forStatement = nodeToRun;
        KnMap initVarMap = forStatement.Conf;
        KnNode preConditionExpr = forStatement.Next?.Core;
        KnNode afterBlockExpr = forStatement.Next?.Next?.Core;
        KnArray loopBody = forStatement.Next?.Next?.Body;

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);
        runtime.AddOp(OpCode.Env_DiveLocalEnv);
        if (initVarMap != null)
        {
            foreach (string initVarKey in initVarMap.Keys)
            {
                KnNode initVarExpr = initVarMap[initVarKey];
                runtime.AddOp(OpCode.Node_RunNode, initVarExpr);
                runtime.AddOp(OpCode.Env_SetLocalEnv, initVarKey);
            }
        }

        // runtime.AddOp(KonOpCode.Node_RunNode, initVarMap);
        // runtime.AddOp(KonOpCode.Env_BindEnvByMap);

        runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 2);
        runtime.AddOp(OpCode.Env_SetLocalEnv, "break");

        ForLoopMemo iterMemo = new ForLoopMemo
        {
            PreConditionExpr = preConditionExpr,
            AfterBlockExpr = afterBlockExpr,
            LoopBody = loopBody,
        };

        runtime.AddOp(OpCode.Ctrl_IterForLoop, iterMemo);
        runtime.AddOp(OpCode.Env_Rise);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end for");
        runtime.OpBatchCommit();
    }

    public static void RunIterForLoop(InterpreterRuntime runtime, Instruction opContState)
    {
        ForLoopMemo lastMemo = opContState.Memo as ForLoopMemo;
        KnNode preConditionExpr = lastMemo.PreConditionExpr;
        KnNode afterBlockExpr = lastMemo.AfterBlockExpr;
        KnArray loopBody = lastMemo.LoopBody;

        int curOpStackTopIdx = runtime.GetCurrentFiber().InstructionStack.GetCurTopIdx();

        runtime.OpBatchStart();

        // eval condition
        runtime.AddOp(OpCode.Node_RunNode, preConditionExpr);
        // 如果条件判断失败，调到下个条件判断，或者是else分支
        runtime.AddOp(OpCode.Ctrl_JumpIfFalse, curOpStackTopIdx);
        // 如果条件判断成功
        runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 3);
        runtime.AddOp(OpCode.Env_SetLocalEnv, "continue");
        runtime.AddOp(OpCode.Node_RunBlock, loopBody);
        runtime.AddOp(OpCode.ValStack_PopValue);
        runtime.AddOp(OpCode.Node_RunNode, afterBlockExpr);
        runtime.AddOp(OpCode.ValStack_PopValue);
        // 执行post expr后，继续执行下个循环
        runtime.AddOp(OpCode.Ctrl_IterForLoop, lastMemo);
        runtime.OpBatchCommit();
    }
}