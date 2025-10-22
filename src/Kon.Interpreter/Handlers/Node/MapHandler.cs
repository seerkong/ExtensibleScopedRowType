

using Kon.Core.Node;
using Kon.Interpreter.Runtime;


/// <summary>
/// Handler for map operations
/// </summary>
public static class MapHandler
{
    public static void ExpandMap(InterpreterRuntime runtime, KnMap map)
    {
        // For maps, push each key-value pair onto the stack and then create a new map
        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushFrame);

        foreach (var key in map.Keys)
        {
            runtime.AddOp(OpCode.ValStack_PushValue, new KnString(key));
            runtime.AddOp(OpCode.Node_RunNode, map[key]);
        }

        runtime.AddOp(OpCode.Node_MakeMap, map.Count);
        runtime.AddOp(OpCode.ValStack_PopFrameAndPushTopVal, "end make map");
        runtime.OpBatchCommit();
    }

    /// <summary>
    /// Creates a map from key-value pairs on the stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunMakeMap(InterpreterRuntime runtime, Instruction instruction)
    {
        var size = instruction.Memo is int ? (int)instruction.Memo : 0;
        var map = new Kon.Core.Node.KnMap();

        for (var i = 0; i < size; i++)
        {
            var value = runtime.GetCurrentFiber().OperandStack.PopValue();
            var key = runtime.GetCurrentFiber().OperandStack.PopValue();

            if (key is Kon.Core.Node.KnString keyString)
            {
                map[keyString.Value] = value;
            }
            else
            {
                map[key?.ToString() ?? string.Empty] = value;
            }
        }

        runtime.GetCurrentFiber().OperandStack.PushValue(map);
    }
}