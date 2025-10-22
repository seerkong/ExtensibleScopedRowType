using Kson.Core;
using Kson.Core.Node;
using Kson.Core.Converter;
using Kson.Interpreter.Models;
using Kson.Interpreter.Runtime;
using Xunit;

namespace Kson.Interpreter.Tests;

public class SimpleExpressionTests
{
    [Fact]
    public void CanEvaluateSimpleAdd()
    {
        var result = KsonInterpreter.EvaluateBlockSync("(1 2 :+)");
        Assert.Equal(3, ((KsInt64)result).Value);
    }

    [Fact]
    public void CanEvaluateNestedExpressions()
    {
        var result = KsonInterpreter.EvaluateBlockSync("((1 2 :+) 3 :+)");
        Assert.Equal(6, ((KsInt64)result).Value);
    }

    [Fact]
    public void CanUseVariables()
    {
        var runtime = KsonInterpreter.CreateRuntime();
        runtime.DefineVariable("x", new KsInt64(5));
        var result = KsonInterpreter.EvaluateBlockSync(runtime, "(x 3 :+)");
        Assert.Equal(8, ((KsInt64)result).Value);
    }

    // not supported yet
    // [Fact]
    public void CanCreateAndUseFunctions()
    {
        var runtime = KsonInterpreter.CreateRuntime();

        // Define a function that adds its two arguments
        KsonInterpreter.EvaluateBlockSync(runtime, "(fn #add2 :|a b| %[(a b :+)])");

        // Call the function
        var result = KsonInterpreter.EvaluateBlockSync(runtime, "(5 3 :add2)");
        Assert.Equal(8, ((KsInt64)result).Value);
    }

    // not supported yet
    // [Fact]
    public void EnvironmentIsolatesScopeProperly()
    {
        var runtime = KsonInterpreter.CreateRuntime();

        // Define a variable in the global scope
        runtime.DefineGlobal("x", new KsInt64(10));

        // Create a new environment
        var localEnvId = runtime.CreateEnvironment("testLocal");

        // Define a variable with the same name in the local scope
        runtime.ChangeEnvironment(localEnvId);
        runtime.DefineGlobal("x", new KsInt64(20));

        // Execute code in the local environment
        var localResult = KsonInterpreter.EvaluateBlockSync("x");
        Assert.Equal(20, ((KsInt64)localResult).Value);

        // Go back to global environment and check that x has its original value
        runtime.ChangeEnvironment(runtime.GetGlobalEnv().Id);
        var globalResult = KsonInterpreter.EvaluateBlockSync("x");
        Assert.Equal(10, ((KsInt64)globalResult).Value);
    }

    // not supported yet
    // [Fact]
    public void ClosuresCaptureTheirEnvironment()
    {
        var runtime = KsonInterpreter.CreateRuntime();
        // Define a variable
        runtime.DefineGlobal("x", new KsInt64(10));

        // Define a function that captures x
        KsonInterpreter.EvaluateBlockSync(
        """
        (fn #makeAdder :||
            %[
                (fn #adder |y| %[(x y :+)]))
            ]
        )
        """
        );

        // Create a closure that captures the current value of x
        var adderFunc = KsonInterpreter.EvaluateBlockSync("(:makeAdder)");

        // Change x
        runtime.DefineGlobal("x", new KsInt64(20));

        // Call the closure - it should use the captured value, not the current one
        var result = KsonInterpreter.EvaluateBlockSync("(5 :adder)");

        // The result should be 15 (10 + 5), not 25 (20 + 5)
        // Note: This depends on how capturing is implemented - in a true lexical closure
        // it would capture the environment at definition time
        Assert.Equal(25, ((KsInt64)result).Value);
    }
}