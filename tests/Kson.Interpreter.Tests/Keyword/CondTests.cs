using Kson.Core.Node;
using Xunit;

namespace Kson.Interpreter.Tests.Keyword;

public class CondTests
{
    [Fact]
    public void SecondBranch()
    {
        var script = """
        (cond (> :|1 2|) %[
            1
          ]
          (> :|3 2|) %[
            2
          ]
          else %[
            3
          ]
        )
        """;

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(2, ((KsInt64)result).Value);
    }

    [Fact]
    public void ElseBranch()
    {
        var script = """
        (cond (> :|1 2|) %[
            1
          ]
          (> :|2 3|) %[
            2
          ]
          else %[
            3
          ]
        )
        """;

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(3, ((KsInt64)result).Value);
    }
}