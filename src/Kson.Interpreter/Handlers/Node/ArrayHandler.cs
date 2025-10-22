using Kson.Core.Node;
using Kson.Interpreter.Runtime;


/// <summary>
/// Handler for array operations
/// </summary>
public static class ArrayHandler
{
    public static void ExpandArray(KsonInterpreterRuntime runtime, KsArray array)
    {
        // For arrays, push each element onto the stack and then create a new array
        runtime.OpBatchStart();
        runtime.AddOp(KsonOpCode.ValStack_PushFrame);

        for (var i = 0; i < array.Size(); i++)
        {
            runtime.AddOp(KsonOpCode.Node_RunNode, array[i]);
        }

        runtime.AddOp(KsonOpCode.Node_MakeArray, array.Size());
        runtime.AddOp(KsonOpCode.ValStack_PopFrameAndPushTopVal, null, "end make array");
        runtime.OpBatchCommit();
    }
    /// <summary>
    /// Creates an array from values on the stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunMakeArray(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        var size = instruction.Memo is int ? (int)instruction.Memo : 0;
        var items = new List<Kson.Core.Node.KsNode>();

        for (var i = 0; i < size; i++)
        {
            items.Insert(0, runtime.GetCurrentFiber().OperandStack.PopValue());
        }

        runtime.GetCurrentFiber().OperandStack.PushValue(new Kson.Core.Node.KsArray(items));
    }
}