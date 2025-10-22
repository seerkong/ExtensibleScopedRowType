using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests.Keyword;

public class TryHandlePerformTests
{
    [Fact]
    public void NoHandler()
    {
        // Define a function and call it
        var script = """
        (var result)
        (try %[
            (set result 6)
            6
          ]
        )
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(6, ((KnInt64)result).Value);
    }

    [Fact]
    public void SingleHandler()
    {
        // Define a function and call it
        var script = """
        (fn #AddHandler :|resume val1 val2| %[
            (:WriteLine "before resume add val1 val2")
            (:resume (:+ val1 val2))
            (:WriteLine "should not go here")
        ])
        (var :result)
        (try %[
            (set result (perform #add :|1 2|))
          ]
          handle #add AddHandler
        )
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public void MultiHandler()
    {
        // Define a function and call it
        var script = """
        (fn #AddHandler :|resume val1 val2| %[
            (:WriteLine "before resume add val1 val2")
            (:resume (:+ val1 val2))
            (:WriteLine "should not go here")
        ])
        (fn #MultiHandler :|resume val1 val2| %[
            (:WriteLine "before resume multi val1 val2")
            (:resume (:* val1 val2))
            (:WriteLine "should not go here")
        ])
        (var :result)
        (try %[
            (:WriteLine "run perform start")
            (var temp (perform #add :|1 2|))
            (:WriteLine "after perform add")
            (set result (perform #multi :|temp 2|))
            (:WriteLine "after perform multi")
            result
          ]
          handle #add AddHandler
          handle #multi MultiHandler
        )
        """;

        var result = KonInterpreter.EvaluateBlockSync(script);

        // Check the result
        Assert.NotNull(result);
        Assert.IsType<KnInt64>(result);
        Assert.Equal(6, ((KnInt64)result).Value);
    }
}