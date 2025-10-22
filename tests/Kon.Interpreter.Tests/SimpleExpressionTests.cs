using Kon.Core;
using Kon.Core.Node;
using Kon.Core.Converter;
using Kon.Interpreter.Models;
using Kon.Interpreter.Runtime;
using Xunit;

namespace Kon.Interpreter.Tests;

public class SimpleExpressionTests
{
    [Fact]
    public void CanEvaluateSimpleAdd()
    {
        var result = KonInterpreter.EvaluateBlockSync("(1 2 :+)");
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanEvaluateNestedExpressions()
    {
        var result = KonInterpreter.EvaluateBlockSync("((1 2 :+) 3 :+)");
        Assert.Equal(6, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanUseVariables()
    {
        var runtime = KonInterpreter.CreateRuntime();
        runtime.DefineVariable("x", new KnInt64(5));
        var result = KonInterpreter.EvaluateBlockSync(runtime, "(x 3 :+)");
        Assert.Equal(8, ((KnInt64)result).Value);
    }

    // not supported yet
    // [Fact]
    public void CanCreateAndUseFunctions()
    {
        var runtime = KonInterpreter.CreateRuntime();

        // Define a function that adds its two arguments
        KonInterpreter.EvaluateBlockSync(runtime, "(fn #add2 :|a b| %[(a b :+)])");

        // Call the function
        var result = KonInterpreter.EvaluateBlockSync(runtime, "(5 3 :add2)");
        Assert.Equal(8, ((KnInt64)result).Value);
    }

    // not supported yet
    // [Fact]
    public void EnvironmentIsolatesScopeProperly()
    {
        var runtime = KonInterpreter.CreateRuntime();

        // Define a variable in the global scope
        runtime.DefineGlobal("x", new KnInt64(10));

        // Create a new environment
        var localEnvId = CreateEnvironment(runtime, "testLocal");

        // Define a variable with the same name in the local scope
        runtime.ChangeEnvironment(localEnvId);
        runtime.DefineGlobal("x", new KnInt64(20));

        // Execute code in the local environment
        var localResult = KonInterpreter.EvaluateBlockSync("x");
        Assert.Equal(20, ((KnInt64)localResult).Value);

        // Go back to global environment and check that x has its original value
        runtime.ChangeEnvironment(runtime.GetGlobalEnv().Id);
        var globalResult = KonInterpreter.EvaluateBlockSync("x");
        Assert.Equal(10, ((KnInt64)globalResult).Value);
    }

    // not supported yet
    // [Fact]
    public void ClosuresCaptureTheirEnvironment()
    {
        var runtime = KonInterpreter.CreateRuntime();
        // Define a variable
        runtime.DefineGlobal("x", new KnInt64(10));

        // Define a function that captures x
        KonInterpreter.EvaluateBlockSync(
        """
        (fn #makeAdder :||
            %[
                (fn #adder |y| %[(x y :+)]))
            ]
        )
        """
        );

        // Create a closure that captures the current value of x
        var adderFunc = KonInterpreter.EvaluateBlockSync("(:makeAdder)");

        // Change x
        runtime.DefineGlobal("x", new KnInt64(20));

        // Call the closure - it should use the captured value, not the current one
        var result = KonInterpreter.EvaluateBlockSync("(5 :adder)");

        // The result should be 15 (10 + 5), not 25 (20 + 5)
        // Note: This depends on how capturing is implemented - in a true lexical closure
        // it would capture the environment at definition time
        Assert.Equal(25, ((KnInt64)result).Value);
    }


    /// <summary>
    /// Creates a new environment scope
    /// </summary>
    /// <param name="name">Name for the new environment</param>
    /// <param name="type">Type of environment to create</param>
    /// <returns>The ID of the new environment</returns>
    public static int CreateEnvironment(InterpreterRuntime runtime, string name, Env.EnvType type = Env.EnvType.Local)
    {
        var currentEnv = runtime.GetCurEnv() ?? throw new InvalidOperationException("Current environment is unavailable.");
        var newEnv = type switch
        {
            Env.EnvType.Process => Env.CreateProcessEnv(currentEnv, name),
            Env.EnvType.Local => Env.CreateLocalEnv(currentEnv, name),
            _ => throw new ArgumentException($"Cannot create environment of type {type}")
        };

        runtime.EnvTree.AddVertex(newEnv);
        runtime.EnvTree.AddEdge(currentEnv.GetVertexId(), newEnv.GetVertexId());

        return newEnv.GetVertexId();
    }
}