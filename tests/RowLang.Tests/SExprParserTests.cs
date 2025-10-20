using RowLang.Core.Syntax;
using Xunit;

namespace RowLang.Tests;

public class SExprParserTests
{
    [Fact]
    public void ParsesNamespacedIdentifier()
    {
        var parser = new SExprParser("a::b::c");
        var nodes = parser.Parse();

        var identifier = Assert.IsType<SExprIdentifier>(Assert.Single(nodes));
        Assert.Equal(new[] { "a", "b", "c" }, identifier.Parts);
        Assert.Equal("c", identifier.Name);
    }

    [Fact]
    public void ParsesAnnotatedList()
    {
        var parser = new SExprParser("!(a b) !prefix (core x) ^suffix ^(tail)");
        var nodes = parser.Parse();

        var list = Assert.IsType<SExprList>(Assert.Single(nodes));
        Assert.Equal(2, list.PrefixAnnotations.Count);
        Assert.Equal(2, list.PostfixAnnotations.Count);

        var firstPrefix = Assert.IsType<SExprList>(list.PrefixAnnotations[0]);
        Assert.Equal(2, firstPrefix.Elements.Length);

        var head = Assert.IsType<SExprIdentifier>(list.Elements[0]);
        Assert.Equal("core", head.QualifiedName);
    }

    [Fact]
    public void ParsesArrayAndObject()
    {
        const string source = "[1 2 \"3\"] {k1: 1 k2: 2 ABC::k3: [1] \"key4\": {}}";
        var parser = new SExprParser(source);
        var nodes = parser.Parse();

        Assert.Equal(2, nodes.Length);

        var array = Assert.IsType<SExprArray>(nodes[0]);
        Assert.Collection(
            array.Elements,
            first => Assert.Equal("1", Assert.IsType<SExprIdentifier>(first).QualifiedName),
            second => Assert.Equal("2", Assert.IsType<SExprIdentifier>(second).QualifiedName),
            third => Assert.Equal("3", Assert.IsType<SExprString>(third).Value));

        var obj = Assert.IsType<SExprObject>(nodes[1]);
        Assert.Equal(4, obj.Properties.Length);

        Assert.Equal("k1", Assert.IsType<SExprIdentifier>(obj.Properties[0].Key).QualifiedName);
        Assert.Equal("1", Assert.IsType<SExprIdentifier>(obj.Properties[0].Value).QualifiedName);

        Assert.Equal("k2", Assert.IsType<SExprIdentifier>(obj.Properties[1].Key).QualifiedName);
        Assert.Equal("2", Assert.IsType<SExprIdentifier>(obj.Properties[1].Value).QualifiedName);

        Assert.Equal("ABC::k3", Assert.IsType<SExprIdentifier>(obj.Properties[2].Key).QualifiedName);
        var nestedArray = Assert.IsType<SExprArray>(obj.Properties[2].Value);
        Assert.Equal("1", Assert.IsType<SExprIdentifier>(Assert.Single(nestedArray.Elements)).QualifiedName);

        var stringKey = Assert.IsType<SExprString>(obj.Properties[3].Key);
        Assert.Equal("key4", stringKey.Value);
        Assert.IsType<SExprObject>(obj.Properties[3].Value);
    }
}
