using Kson.Core.Node;
using Kson.Interpreter.Runtime;

namespace Kson.Interpreter.Handlers;

/// <summary>
/// Handler for value stack operations
/// </summary>
public static class ValueStackHandler
{
    /// <summary>
    /// Pushes a new frame onto the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPushFrame(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PushFrame();
    }

    /// <summary>
    /// Pushes a value onto the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPushValue(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PushValue((KsNode)instruction.Memo);
    }

    /// <summary>
    /// Pops a value from the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopValue(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopValue();
    }

    /// <summary>
    /// Duplicates the top value on the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDuplicate(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        var value = runtime.GetCurrentFiber().OperandStack.PeekTop();
        runtime.GetCurrentFiber().OperandStack.PushValue(value);
    }

    /// <summary>
    /// Pops the current frame and pushes the top value onto the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopFrameAndPushTopVal(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopFrameAndPushTopVal();
    }

    /// <summary>
    /// Pops the current frame and ignores the result
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopFrameIgnoreResult(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopFrameAllValues();
    }
}