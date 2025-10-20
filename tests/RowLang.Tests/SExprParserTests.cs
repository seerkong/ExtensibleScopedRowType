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
        const string source = "[1 2 \"3\"] {k1 = 1 k2 = 2 ABC::k3 = [1] \"key4\" = {}}";
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

    [Fact]
    public void ParsesTypeAnnotatedIdentifier()
    {
        const string source = "value ~ (Map [str str])";
        var parser = new SExprParser(source);
        var nodes = parser.Parse();

        var identifier = Assert.IsType<SExprIdentifier>(Assert.Single(nodes));
        Assert.Equal("value", identifier.QualifiedName);

        var annotation = Assert.IsType<SExprList>(identifier.TypeAnnotation);
        Assert.Equal("Map", Assert.IsType<SExprIdentifier>(annotation.Elements[0]).QualifiedName);

        var tuple = Assert.IsType<SExprArray>(annotation.Elements[1]);
        Assert.Equal("str", Assert.IsType<SExprIdentifier>(tuple.Elements[0]).QualifiedName);
        Assert.Equal("str", Assert.IsType<SExprIdentifier>(tuple.Elements[1]).QualifiedName);
    }

    [Fact]
    public void PostfixAnnotationsRemainAfterTypeAnnotation()
    {
        const string source = "value ~ str ^meta";
        var parser = new SExprParser(source);
        var nodes = parser.Parse();

        var identifier = Assert.IsType<SExprIdentifier>(Assert.Single(nodes));
        Assert.Equal("value", identifier.QualifiedName);
        Assert.Equal("str", Assert.IsType<SExprIdentifier>(identifier.TypeAnnotation).QualifiedName);

        Assert.Single(identifier.PostfixAnnotations);
        Assert.Equal("meta", Assert.IsType<SExprIdentifier>(identifier.PostfixAnnotations[0]).QualifiedName);
    }
}
