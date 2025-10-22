using System.Collections.Generic;
using Kon.Core;
using Kon.Core.Converter;
using Kon.Core.Node;
using Xunit;

namespace Kon.Core.Tests;

public class NodeBasicTests
{
    private static void AssertRoundTrip(string source)
    {
        var node = KonParser.Parse(source);
        var singleLine = KonFormater.SingleLine(node);
        var reparsed1 = KonParser.Parse(singleLine);
        var prettified = KonFormater.Prettify(node);
        // Console.WriteLine(prettified);
        var reparsed2 = KonParser.Parse(prettified);
        Assert.Equal(singleLine, KonFormater.SingleLine(reparsed1));
        Assert.Equal(singleLine, KonFormater.SingleLine(reparsed2));
    }


    [Fact]
    public void HasNamespaceWord()
    {
        const string source = "a.b.c.d";
        var node = KonParser.Parse(source);
        var word = Assert.IsType<KnWord>(node);
        Assert.Equal(new List<string> { "a", "b", "c", "d" }, word.GetFullNameList());
    }

    [Fact]
    public void Array1()
    {
        const string source = "\n[ 1 \n , 2 , 3 ]  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Array2()
    {
        const string source = "\n[ 1 \n , 2 , 3, ]  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Array3()
    {
        const string source = "\n[ 1 \n  2  3 ]  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Map1()
    {
        const string source = "\n{ \"abc\": 1 \n , efg : \"Hello World\" , \"t\" : 3 }  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Map2()
    {
        const string source = "\n{ \"abc\": 1 \n , \"efg\": \"m\" , \"t\" : 3, }  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Map3()
    {
        const string source = "\n{ \"abc\": 1 \n  \"efg\": \"m\"  \"t\" : 3 }  ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void WordSymbolString()
    {
        const string source = "[word `symbol \"Hello\\nWorld Unicode: \\uAAAA Escaped \\\"quotes\\\" Tab\\tand\\rCarriage Return \"] ";
        AssertRoundTrip(source);
    }

    [Fact]
    public void ChainHasMathOperators()
    {
        const string source = "\n(1 \n -0.1 + - * / -> >= <= =)";
        AssertRoundTrip(source);
    }

    [Fact]
    public void ChainEqualsFunc()
    {
        const string source = "(:== x 2)";
        AssertRoundTrip(source);
    }

    [Fact]
    public void ChainDslHasConfig()
    {
        const string source = "(for %{i: 0})";
        AssertRoundTrip(source);
    }
}
