using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests;

public class BasicInterpreterTests
{
    [Fact]
    public void CanEvaluateSimpleAddition()
    {
        // Evaluate a simple addition expression
        var result = KonInterpreter.EvaluateBlockSync("(1 2 :+)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task CanEvaluateChainedExpressions()
    {
        // Evaluate a chained expression
        var result = await KonInterpreter.EvaluateBlockAsync("(1 :+ 2 :* 3)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(9, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanDefineFunctions()
    {
        // Define a function and call it
        var script = """
        (fn #addTwo :|x| %[
            (x :+ 2)
        ])
        (5 :addTwo)
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(7, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanUseVariables()
    {
        // Define variables and use them
        var script = """
        (var x 5)
        (var y 3)
        (x :+ y)
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(8, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanUseStringFunctions()
    {
        // Use string functions
        var result = KonInterpreter.EvaluateBlockSync("(\"Hello\" \"World\" :Concat)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnString>(result);
        Assert.Equal("HelloWorld", ((KnString)result).Value);
    }
}