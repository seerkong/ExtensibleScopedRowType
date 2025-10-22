using Kson.Core.Node;

namespace Kson.Interpreter.Runtime;

/// <summary>
/// Specialized stack machine for operand values
/// </summary>
public class OperandStack : StackMachine<KsNode>
{
    /// <summary>
    /// Creates a new operand stack
    /// </summary>
    public OperandStack() : base(true)
    {
    }

    public void Restore(StackMachine<KsNode> other)
    {
        FrameBottomIdxStack = new List<int>(other.FrameBottomIdxStack);
        Items = new List<KsNode>(other.Items);
        StackTop = other.StackTop;
    }
}