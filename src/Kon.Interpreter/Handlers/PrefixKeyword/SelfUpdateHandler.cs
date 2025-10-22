using System;
using Kon.Core.Node;
using Kon.Interpreter.HostSupport;
using Kon.Interpreter.Runtime;

public class SelfUpdateHandler
{
    public static void SelfUpdate_PlusOne(InterpreterRuntime runtime, object nodeToRun)
    {
        dynamic node = nodeToRun;
        string varName = node.Next.Core.Value;

        var declareEnv = runtime.EnvTree.LookupDeclareEnv(runtime.GetCurEnv(), varName);
        KnNode oldValue = declareEnv.Lookup(varName);
        KnNode newVal = MathFunctions.Add(oldValue, new KnInt64(1));
        declareEnv.Define(varName, newVal);

        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushValue, newVal);
        runtime.OpBatchCommit();
    }
}
