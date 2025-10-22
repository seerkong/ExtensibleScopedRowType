using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests.Keyword;

public class ForeachTests
{
  [Fact]
  public void ForeachInValue()
  {
    var script = """
        (var b 0)
        (foreach x in [1 2 3] %[
            (:WriteLine x)
            (set b x)
          ]
        )
        b
        """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(3, ((KnInt64)result).Value);
  }

  [Fact]
  public void ForeachInVar()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (foreach x in a %[
            (:WriteLine x)
            (set b x)
          ]
        )
        b
        """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(3, ((KnInt64)result).Value);
  }

  [Fact]
  public void Break()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (foreach x in a %[
            (if (:== x 2) %[
                (:break)
              ]
            )
            (:WriteLine x)
            (set b x)
          ]
        )
        b
        """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(1, ((KnInt64)result).Value);
  }

  [Fact]
  public void Continue()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (foreach x in a %[
            (if (:== x 2) %[
                (:continue)
              ]
            )
            (:WriteLine x)
            (set b x)
          ]
        )
        b
        """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnInt64>(result);
    Assert.Equal(3, ((KnInt64)result).Value);
  }
}