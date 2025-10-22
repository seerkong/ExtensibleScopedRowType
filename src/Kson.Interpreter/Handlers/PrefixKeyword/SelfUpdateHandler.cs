using System;
using Kson.Core.Node;
using Kson.Interpreter.HostSupport;
using Kson.Interpreter.Runtime;

public class SelfUpdateHandler
{
    public static void SelfUpdate_PlusOne(KsonInterpreterRuntime runtime, object nodeToRun)
    {
        dynamic node = nodeToRun;
        string varName = node.Next.Core.Value;

        var declareEnv = runtime.EnvTree.LookupDeclareEnv(runtime.GetCurEnv(), varName);
        KsNode oldValue = declareEnv.Lookup(varName);
        KsNode newVal = MathFunctions.Add(oldValue, new KsInt64(1));
        declareEnv.Define(varName, newVal);

        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushValue, newVal);
        runtime.OpBatchCommit();
    }
}
