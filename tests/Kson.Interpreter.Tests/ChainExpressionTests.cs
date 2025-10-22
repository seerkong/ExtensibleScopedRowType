using System.Threading.Tasks;
using Kson.Core;
using Kson.Core.Node;
using Kson.Interpreter.Models;
using Xunit;

namespace Kson.Interpreter.Tests;

public class ChainExpressionTests
{
    [Fact]
    public void CanEvaluateChainFrameTopCallExpression()
    {
        var result = KsonInterpreter.EvaluateBlockSync("(1 :+ 1 :+ 1)");
        Assert.Equal(3, ((KsInt64)result).Value);
    }

    [Fact]
    public void CanEvaluateChainFrameBottomCallExpression()
    {
        var result = KsonInterpreter.EvaluateBlockSync("(+ :|(+ :|1 1|) 1|)");
        Assert.Equal(3, ((KsInt64)result).Value);
    }

    [Fact]
    public async Task CanEvaluateFunctionDefinition()
    {
        var script = """
        (fn #addTwo :|x| %[
            (x :+ 2)
        ])
        (5 :addTwo)
        """;

        var result = await KsonInterpreter.EvaluateBlockAsync(script);
        Assert.Equal(7, ((KsInt64)result).Value);
    }
}