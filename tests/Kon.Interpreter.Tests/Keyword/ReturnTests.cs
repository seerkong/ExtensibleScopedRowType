using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests.Keyword;

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

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(1, ((KnInt64)result).Value);
    }
}