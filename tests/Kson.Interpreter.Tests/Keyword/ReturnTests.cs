using Kson.Core.Node;
using Xunit;

namespace Kson.Interpreter.Tests.Keyword;

public class ReturnTests
{
    [Fact]
    public void basicReturn()
    {
        var script = """
        (fn #foo :|| %[
            (:return 1)
            2
        ])
        (:foo)
        """;

        var result = KsonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KsInt64>(result);
        Assert.Equal(1, ((KsInt64)result).Value);
    }
}