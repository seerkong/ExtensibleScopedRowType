using Kson.Core;
using Kson.Core.Node;
using Xunit;

namespace Kson.Core.Tests;

public class StringTests
{
    [Fact]
    public void ParsesEscapedString()
    {
        const string source = "\"String 中文\\\" \\\\ \\/ \\b \\f \\n \\r \\t \\uAAAA \\u1337 \"";
        var node = Kson.Parse(source);
        var stringNode = Assert.IsType<KsString>(node);
        var formatted = stringNode.ToString();
        var reparsed = Kson.Parse(formatted);
        Assert.Equal(formatted, reparsed.ToString());
    }
}
