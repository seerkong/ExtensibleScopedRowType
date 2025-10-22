using System.Threading.Tasks;
using Kon.Core;
using Kon.Core.Node;
using Kon.Interpreter.Models;
using Xunit;

namespace Kon.Interpreter.Tests;

public class ChainExpressionTests
{
    [Fact]
    public void CanEvaluateChainFrameTopCallExpression()
    {
        var result = KonInterpreter.EvaluateBlockSync("(1 :+ 1 :+ 1)");
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanEvaluateChainFrameBottomCallExpression()
    {
        var result = KonInterpreter.EvaluateBlockSync("(+ :|(+ :|1 1|) 1|)");
        Assert.Equal(3, ((KnInt64)result).Value);
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

        var result = await KonInterpreter.EvaluateBlockAsync(script);
        Assert.Equal(7, ((KnInt64)result).Value);
    }
}