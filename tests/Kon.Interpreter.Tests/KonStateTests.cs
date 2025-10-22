using System.Threading.Tasks;
using Kon.Core;
using Kon.Core.Node;
using Kon.Interpreter.Runtime;
using Xunit;

namespace Kon.Interpreter.Tests;

public class KonStateTests
{
    [Fact]
    public void KonStateCanCreateWithRootEnv()
    {
        var state = new InterpreterRuntime();
        Assert.NotNull(state.GetRootEnv());
        Assert.NotNull(state.GetGlobalEnv());
    }

    [Fact]
    public void KonStateCanDefineAndLookupValues()
    {
        var state = new InterpreterRuntime();
        var varName = "testVar";
        var value = new KnInt64(42);

        // Define a variable in the global environment
        state.Define(varName, value);

        // Look up the value
        var result = state.Lookup(varName);
        Assert.Equal(42, ((KnInt64)result).Value);
    }

    [Fact]
    public void KonStateCanPushAndPopInstructions()
    {
        var state = new InterpreterRuntime();
        var testValue = new KnInt64(123);

        // Add an operation to push a value
        state.AddOpDirectly(OpCode.ValStack_PushValue, testValue);

        // Execute one instruction
        var fiber = state.GetCurrentFiber();
        var instruction = fiber.InstructionStack.PopValue();

        Assert.Equal(OpCode.ValStack_PushValue, instruction.OpCode);
        Assert.Equal(testValue, instruction.Memo);
    }
}