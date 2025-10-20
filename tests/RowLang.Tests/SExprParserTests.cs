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
}
