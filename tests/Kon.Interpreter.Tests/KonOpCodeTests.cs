using Kon.Core;
using Kon.Core.Node;
using Kon.Interpreter.Runtime;
using Xunit;

namespace Kon.Interpreter.Tests;

public class KonOpCodeTests
{
    [Fact]
    public void OpCodeEnumHasBasicOperations()
    {
        // Check that basic operation codes exist
        Assert.NotNull(OpCode.OpStack_LandSuccess);
        Assert.NotNull(OpCode.ValStack_PushValue);
        Assert.NotNull(OpCode.Node_RunNode);
    }

    [Fact]
    public void InstructionStackCanPushAndPop()
    {
        var stack = new InstructionStack();
        var instruction = new Instruction(OpCode.ValStack_PushValue, 0, new KnInt64(42));

        stack.PushValue(instruction);
        var popped = stack.PopValue();

        Assert.Equal(instruction.OpCode, popped.OpCode);
        Assert.Equal(instruction.EnvId, popped.EnvId);
        Assert.Equal(42, ((KnInt64)popped.Memo).Value);
    }
}