using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public static class SetToHandler
{
    public static void ExpandSetTo(InterpreterRuntime runtime, KnChainNode nodeToRun)
    {
        string varName = NodeValueHelper.GetInnerString(nodeToRun.Name);

        runtime.OpBatchStart();
        // 必须duplicate栈顶值，以便实现对应其他语言的statement在Kon lang中也有计算结果
        runtime.AddOp(OpCode.ValStack_Duplicate);
        runtime.AddOp(OpCode.Env_SetLocalEnv, varName);
        runtime.OpBatchCommit();
    }
}