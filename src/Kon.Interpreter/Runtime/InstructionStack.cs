namespace Kon.Interpreter.Runtime;

/// <summary>
/// Specialized stack machine for instructions
/// </summary>
public class InstructionStack : StackMachine<Instruction>
{
    /// <summary>
    /// Creates a new instruction stack
    /// </summary>
    public InstructionStack() : base(true)
    {
    }

    // public void Restore(StackMachine<Instruction> other)
    // {
    //     FrameBottomIdxStack = new List<int>(other.FrameBottomIdxStack);
    //     Items = new List<Instruction>(other.Items);
    //     StackTop = other.StackTop;
    // }
}