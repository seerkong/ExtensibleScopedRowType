using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public class SubscriptHandler
{
    public static void ExpandGetSubscript(InterpreterRuntime runtime, KnNode nodeToRun)
    {
        runtime.OpBatchStart();
        runtime.AddOp(OpCode.Node_RunNode, nodeToRun);
        runtime.AddOp(OpCode.Node_RunGetSubscript);
        runtime.OpBatchCommit();
    }

    public static void RunGetSubscript(InterpreterRuntime runtime, Instruction opContState)
    {
        KnNode subscriptOperand = runtime.GetCurrentFiber().OperandStack.PopValue();
        KnNode subscriptTarget = runtime.GetCurrentFiber().OperandStack.PopValue();
        if (subscriptTarget is KnArray array && subscriptOperand is KnInt64 numIndex)
        {
            KnNode r = array.Get((int)numIndex.Value);
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.ValStack_PushValue, r);
            runtime.OpBatchCommit();
        }
        else if (subscriptTarget is KnMap map && subscriptOperand is KnString strIndex)
        {
            KnNode r = map.Get(strIndex.Value);
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.ValStack_PushValue, r);
            runtime.OpBatchCommit();
        }
        else
        {
            throw new Exception("invalid subscript operand");
        }
        // Additional logic for non-table types commented out as in original
    }
}