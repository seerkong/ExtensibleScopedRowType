using Kon.Core.Node;
using Kon.Interpreter.Runtime;

namespace Kon.Interpreter.Handlers;

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
    public static void RunPushFrame(InterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PushFrame();
    }

    /// <summary>
    /// Pushes a value onto the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPushValue(InterpreterRuntime runtime, Instruction instruction)
    {
        KnNode node = (KnNode)instruction.Memo;
        runtime.GetCurrentFiber().OperandStack.PushValue(node);
    }

    /// <summary>
    /// Pops a value from the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopValue(InterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopValue();
    }

    /// <summary>
    /// Duplicates the top value on the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDuplicate(InterpreterRuntime runtime, Instruction instruction)
    {
        var value = runtime.GetCurrentFiber().OperandStack.PeekTop();
        runtime.GetCurrentFiber().OperandStack.PushValue(value);
    }

    /// <summary>
    /// Pops the current frame and pushes the top value onto the value stack
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopFrameAndPushTopVal(InterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopFrameAndPushTopVal();
    }

    /// <summary>
    /// Pops the current frame and ignores the result
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunPopFrameIgnoreResult(InterpreterRuntime runtime, Instruction instruction)
    {
        runtime.GetCurrentFiber().OperandStack.PopFrameAllValues();
    }
}