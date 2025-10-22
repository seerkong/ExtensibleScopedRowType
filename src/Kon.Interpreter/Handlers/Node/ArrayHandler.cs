using Kon.Core.Node;
using Kon.Interpreter.Runtime;


/// <summary>
/// Handler for array operations
/// </summary>
public static class ArrayHandler
{
    public static void ExpandArray(InterpreterRuntime runtime, KnArray array)
    {
        // For arrays, push each element onto the stack and then create a new array
        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);

        for (var i = 0; i < array.Size(); i++)
        {
            runtime.AddOp(OpCode.Node_RunNode, array[i]);
        }

        runtime.AddOp(OpCode.Node_MakeArray, array.Size());
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, null, "end make array");
        runtime.OpBatchCommit();
    }
    /// <summary>
    /// Creates an array from values on the stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunMakeArray(InterpreterRuntime runtime, Instruction instruction)
    {
        var size = instruction.Memo is int ? (int)instruction.Memo : 0;
        var items = new List<Kon.Core.Node.KnNode>();

        for (var i = 0; i < size; i++)
        {
            items.Insert(0, runtime.GetCurrentFiber().OperandStack.PopValue());
        }

        runtime.GetCurrentFiber().OperandStack.PushValue(new Kon.Core.Node.KnArray(items));
    }
}