using Kson.Core.Node;
using Xunit;

namespace Kson.Interpreter.Tests;

public class BasicInterpreterTests
{
    [Fact]
    public void CanEvaluateSimpleAddition()
    {
        // Evaluate a simple addition expression
        var result = KsonInterpreter.EvaluateBlockSync("(1 2 :+)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(3, ((KsInt64)result).Value);
    }

    [Fact]
    public async Task CanEvaluateChainedExpressions()
    {
        // Evaluate a chained expression
        var result = await KsonInterpreter.EvaluateBlockAsync("(1 :+ 2 :* 3)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(9, ((KsInt64)result).Value);
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

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(7, ((KsInt64)result).Value);
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

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(8, ((KsInt64)result).Value);
    }

    [Fact]
    public void CanUseStringFunctions()
    {
        // Use string functions
        var result = KsonInterpreter.EvaluateBlockSync("(\"Hello\" \"World\" :Concat)");

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsString>(result);
        Assert.Equal("HelloWorld", ((KsString)result).Value);
    }
}