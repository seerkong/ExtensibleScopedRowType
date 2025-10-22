using Kson.Core.Node;
using Xunit;

namespace Kson.Interpreter.Tests.Keyword;

public class ForLoopTests
{
  [Fact]
  public void ForInVar()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (for %{i: 0} (i (:ArrayLength a) :<) (++ i) %[
            (var x (a::i))
            (:WriteLine x)
            (set b x)
          ]
        )
        b
        """;

    var result = KsonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KsInt64>(result);
    Assert.Equal(3, ((KsInt64)result).Value);
  }

  [Fact]
  public void Break()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (for %{i: 0} (i (:ArrayLength a) :<) (++ i) %[
            (var x (a::i))
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

    var result = KsonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KsInt64>(result);
    Assert.Equal(1, ((KsInt64)result).Value);
  }

  [Fact]
  public void Continue()
  {
    var script = """
        (var a [1 2 3])
        (var b 0)
        (for %{i: 0} (i (:ArrayLength a) :<) (++ i) %[
            (var x (a::i))
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

    var result = KsonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KsInt64>(result);
    Assert.Equal(3, ((KsInt64)result).Value);
  }
}