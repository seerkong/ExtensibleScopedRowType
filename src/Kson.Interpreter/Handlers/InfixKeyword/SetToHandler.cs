using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class SetToHandler
{
    public static void ExpandSetTo(KsonInterpreterRuntime runtime, KsChainNode nodeToRun)
    {
        string varName = NodeValueHelper.GetInnerString(nodeToRun.Name);

        runtime.OpBatchStart();
        // 必须duplicate栈顶值，以便实现对应其他语言的statement在kson lang中也有计算结果
        runtime.AddOp(KsonOpCode.ValStack_Duplicate);
        runtime.AddOp(KsonOpCode.Env_SetLocalEnv, varName);
        runtime.OpBatchCommit();
    }
}