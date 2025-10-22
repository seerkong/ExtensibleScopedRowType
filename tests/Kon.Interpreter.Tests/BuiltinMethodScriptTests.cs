using System.Threading.Tasks;
using Kon.Core.Node;
using Xunit;

namespace Kon.Interpreter.Tests;

/// <summary>
/// Tests for builtin methods on KnArray and KnMap using Kon script syntax.
/// Tests instance call syntax with the ~ operator.
/// </summary>
public class BuiltinMethodScriptTests
{
    #region KnArray Builtin Method Tests

    [Fact]
    public async Task Array_Count_ReturnsCorrectSize()
    {
        // var script = "([1 2 3])";
        var script = "([1 2 3] ~Count)";
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Length_ReturnsCorrectSize()
    {
        var script = "([10 20 30 40]~Length)";
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(4, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Get_RetrievesElement()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Get 0)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(1, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Get_RetrievesMiddleElement()
    {
        var script = """
        (var arr ["a" "b" "c"])
        (arr~Get 1)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnString>(result);
        Assert.Equal("b", ((KnString)result).Value);
    }

    [Fact]
    public async Task Array_Push_AddsElement()
    {
        var script = """
        (var arr [1 2])
        (arr~Push 3)
        (arr~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Pop_RemovesAndReturnsElement()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Pop)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Pop_ReducesSize()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Pop)
        (arr~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(2, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Unshift_AddsElementAtBeginning()
    {
        var script = """
        (var arr [2 3])
        (arr~Unshift 1)
        (arr~Get 0)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(1, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Shift_RemovesFirstElement()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Shift)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(1, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_Top_ReturnsLastElement()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Top)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Array_IsEmpty_ReturnsFalseForNonEmptyArray()
    {
        var script = "([1 2 3]~IsEmpty)";
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.False(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Array_IsEmpty_ReturnsTrueForEmptyArray()
    {
        var script = "([]~IsEmpty)";
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.True(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Array_ChainedMethodCalls()
    {
        var script = """
        (var arr [1 2 3])
        (arr~Push 4)
        (arr~Push 5)
        (arr~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(5, ((KnInt64)result).Value);
    }

    #endregion

    #region KnMap Builtin Method Tests

    [Fact]
    public async Task Map_Count_ReturnsCorrectSize()
    {
        var script = """
        (var m {a: 1 b: 2 c: 3})
        (m~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Map_Get_RetrievesValue()
    {
        var script = """
        (var m {name: "Alice" age: 25})
        (m~Get "name")
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnString>(result);
        Assert.Equal("Alice", ((KnString)result).Value);
    }

    [Fact]
    public async Task Map_ContainsKey_ReturnsTrueForExistingKey()
    {
        var script = """
        (var m {x: 1 y: 2})
        (m~ContainsKey "x")
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.True(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Map_ContainsKey_ReturnsFalseForNonExistingKey()
    {
        var script = """
        (var m {x: 1 y: 2})
        (m~ContainsKey "z")
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.False(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Map_Keys_ReturnsAllKeys()
    {
        var script = """
        (var m {a: 1 b: 2})
        (var keys (m~Keys))
        (keys~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(2, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Map_Values_ReturnsAllValues()
    {
        var script = """
        (var m {x: 10 y: 20 z: 30})
        (var vals (m~Values))
        (vals~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Map_IsEmpty_ReturnsTrueForEmptyMap()
    {
        var script = """
        (var m {})
        (m~IsEmpty)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.True(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Map_IsEmpty_ReturnsFalseForNonEmptyMap()
    {
        var script = """
        (var m {key: "value"})
        (m~IsEmpty)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnBoolean>(result);
        Assert.False(((KnBoolean)result).Value);
    }

    [Fact]
    public async Task Map_Remove_RemovesKey()
    {
        var script = """
        (var m {a: 1 b: 2 c: 3})
        (m~Remove "b")
        (m~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(2, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Map_Clear_RemovesAllKeys()
    {
        var script = """
        (var m {a: 1 b: 2 c: 3})
        (m~Clear)
        (m~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(0, ((KnInt64)result).Value);
    }

    #endregion

    #region Mixed Tests

    [Fact]
    public async Task CanUseArrayMethodInMapValue()
    {
        var script = """
        (var m {data: [1 2 3 4 5]})
        (var arr (m~Get "data"))
        (arr~Count)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(5, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task CanUseMapMethodInArrayElement()
    {
        var script = """
        (var arr [{name: "Alice"} {name: "Bob"}])
        (var first (arr~Get 0))
        (first~Get "name")
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(script);

        Assert.IsType<KnString>(result);
        Assert.Equal("Alice", ((KnString)result).Value);
    }

    #endregion
}
