using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests.Keyword;

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

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(2, ((KnInt64)result).Value);
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

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(3, ((KnInt64)result).Value);
  }
}