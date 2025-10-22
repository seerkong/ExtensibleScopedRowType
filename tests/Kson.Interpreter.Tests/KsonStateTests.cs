using System.Threading.Tasks;
using Kson.Core;
using Kson.Core.Node;
using Kson.Interpreter.Runtime;
using Xunit;

namespace Kson.Interpreter.Tests;

public class KsonStateTests
{
    [Fact]
    public void KsonStateCanCreateWithRootEnv()
    {
        var state = new KsonInterpreterRuntime();
        Assert.NotNull(state.GetRootEnv());
        Assert.NotNull(state.GetGlobalEnv());
    }

    [Fact]
    public void KsonStateCanDefineAndLookupValues()
    {
        var state = new KsonInterpreterRuntime();
        var varName = "testVar";
        var value = new KsInt64(42);

        // Define a variable in the global environment
        state.Define(varName, value);

        // Look up the value
        var result = state.Lookup(varName);
        Assert.Equal(42, ((KsInt64)result).Value);
    }

    [Fact]
    public void KsonStateCanPushAndPopInstructions()
    {
        var state = new KsonInterpreterRuntime();
        var testValue = new KsInt64(123);

        // Add an operation to push a value
        state.AddOpDirectly(KsonOpCode.ValStack_PushValue, testValue);

        // Execute one instruction
        var fiber = state.GetCurrentFiber();
        var instruction = fiber.InstructionStack.PopValue();

        Assert.Equal(KsonOpCode.ValStack_PushValue, instruction.OpCode);
        Assert.Equal(testValue, instruction.Memo);
    }
}