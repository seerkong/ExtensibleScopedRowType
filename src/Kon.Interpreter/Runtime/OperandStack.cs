using Kon.Core.Node;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Specialized stack machine for operand values
/// </summary>
public class OperandStack : StackMachine<KnNode>
{
    /// <summary>
    /// Creates a new operand stack
    /// </summary>
    public OperandStack() : base(true)
    {
    }

    public void Restore(StackMachine<KnNode> other)
    {
        FrameBottomIdxStack = new List<int>(other.FrameBottomIdxStack);
        Items = new List<KnNode>(other.Items);
        StackTop = other.StackTop;
    }
}