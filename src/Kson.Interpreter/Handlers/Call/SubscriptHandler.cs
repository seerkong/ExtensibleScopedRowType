using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public class SubscriptHandler
{
    public static void ExpandGetSubscript(KsonInterpreterRuntime runtime, KsNode nodeToRun)
    {
        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.Node_RunNode, nodeToRun);
        runtime.AddOp(KsonOpCode.Node_RunGetSubscript);
        runtime.OpBatchCommit();
    }

    public static void RunGetSubscript(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        KsNode subscriptOperand = runtime.GetCurrentFiber().OperandStack.PopValue();
        KsNode subscriptTarget = runtime.GetCurrentFiber().OperandStack.PopValue();
        if (subscriptTarget is KsArray array && subscriptOperand is KsInt64 numIndex)
        {
            KsNode r = array.Get((int)numIndex.Value);
            runtime.AddOpDirectly(KsonOpCode.ValStack_PushValue, r);
        }
        else if (subscriptTarget is KsMap map && subscriptOperand is KsString strIndex)
        {
            KsNode r = map.Get(strIndex.Value);
            runtime.AddOpDirectly(KsonOpCode.ValStack_PushValue, r);
        }
        else
        {
            throw new Exception("invalid subscript operand");
        }
        // Additional logic for non-table types commented out as in original
    }
}