using Kson.Core.Node;
using Kson.Interpreter;
using Xunit;
namespace Kson.Interpreter.Tests.Keyword;

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

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(2, ((KsInt64)result).Value);
    }
}