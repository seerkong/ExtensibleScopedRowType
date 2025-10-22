using Kon.Interpreter.Runtime;

namespace Kon.Interpreter.Handlers;

/// <summary>
/// Handler for operation stack operations
/// </summary>
public static class OpStackHandler
{
    /// <summary>
    /// Handles a successful landing
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunLandSuccess(InterpreterRuntime runtime, Instruction instruction)
    {
        // Nothing to do, this is a marker for the end of execution
    }

    /// <summary>
    /// Handles a failed landing
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunLandFail(InterpreterRuntime runtime, Instruction instruction)
    {
        // Nothing to do, this is a marker for the end of execution with a failure
    }

    /// <summary>
    /// Performs a jump
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunJump(InterpreterRuntime runtime, Instruction instruction)
    {
        var jumpToIdx = (int)instruction.Memo;
        runtime.GetCurrentFiber().InstructionStack.JumpTo(jumpToIdx);
    }

    /// <summary>
    /// Performs a conditional jump
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunJumpIfFalse(InterpreterRuntime runtime, Instruction instruction)
    {
        var condition = runtime.GetCurrentFiber().OperandStack.PopValue();

        // If the condition is false, jump
        if (!condition.ToBoolean())
        {
            var jumpToIdx = (int)instruction.Memo;
            runtime.GetCurrentFiber().InstructionStack.JumpTo(jumpToIdx);
        }
    }
}