using Kson.Core;
using Kson.Core.Node;
using Kson.Interpreter.Runtime;
using Xunit;

namespace Kson.Interpreter.Tests;

public class KsonOpCodeTests
{
    [Fact]
    public void OpCodeEnumHasBasicOperations()
    {
        // Check that basic operation codes exist
        Assert.NotNull(KsonOpCode.OpStack_LandSuccess);
        Assert.NotNull(KsonOpCode.ValStack_PushValue);
        Assert.NotNull(KsonOpCode.Node_RunNode);
    }

    [Fact]
    public void InstructionStackCanPushAndPop()
    {
        var stack = new InstructionStack();
        var instruction = new Instruction(KsonOpCode.ValStack_PushValue, 0, new KsInt64(42));

        stack.PushValue(instruction);
        var popped = stack.PopValue();

        Assert.Equal(instruction.OpCode, popped.OpCode);
        Assert.Equal(instruction.EnvId, popped.EnvId);
        Assert.Equal(42, ((KsInt64)popped.Memo).Value);
    }
}