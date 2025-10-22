using Kon.Core.Node;
using Kon.Interpreter;
using Xunit;
namespace Kon.Interpreter.Tests.Keyword;

public class SetToTests
{
    [Fact]
    public void set_to()
    {
        var script = """
        (var a 1)
        (2 set_to #a)
        a
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(2, ((KnInt64)result).Value);
    }
}