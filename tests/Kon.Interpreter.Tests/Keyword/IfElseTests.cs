using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests.Keyword;

public class IfElseTests
{

  [Fact]
  public void IfWithTrueFalseBranch()
  {
    var script = """
    (if (:> 5 3) %[
        true
      ]
      else %[
        false
      ]
    )
    """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnBoolean>(result);
    Assert.Equal(true, (result as KnBoolean).Value);
  }

  [Fact]
  public void NoElseBranch()
  {
    var script = """
    (var true_branch_visited false)
    (if (:> 5 3) %[
        (set true_branch_visited true)
      ]
    )
    true_branch_visited
    """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnBoolean>(result);
    Assert.Equal(true, (result as KnBoolean).Value);
  }

  [Fact]
  public void IfCheckFailJumpToElseBranch()
  {
    var script = """
    (var false_branch_visited false)
    (if (:> 2 3) %[
        5
      ]
      else %[
        (set false_branch_visited true)
      ]
    )
    false_branch_visited
    """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnBoolean>(result);
    Assert.Equal(true, (result as KnBoolean).Value);
  }

  [Fact]
  public void IfCheckFailNoElseBranch()
  {
    var script = """
    (var false_branch_visited false)
    (if (:> 2 3) %[
        
      ]
      else %[
        (set false_branch_visited true)
      ]
    )
    false_branch_visited
    """;

    var result = KonInterpreter.EvaluateBlockSync(script);

    // Check the result
    Assert.NotNull(result);
    Assert.IsType<KnBoolean>(result);
    Assert.Equal(true, (result as KnBoolean).Value);
  }

}