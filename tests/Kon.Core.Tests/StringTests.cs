using Kon.Core;
using Kon.Core.Node;
using Xunit;

namespace Kon.Core.Tests;

public class StringTests
{
    [Fact]
    public void ParsesEscapedString()
    {
        const string source = "\"String 中文\\\" \\\\ \\/ \\b \\f \\n \\r \\t \\uAAAA \\u1337 \"";
        var node = Kon.Parse(source);
        var stringNode = Assert.IsType<KnString>(node);
        var formatted = stringNode.ToString();
        var reparsed = Kon.Parse(formatted);
        Assert.Equal(formatted, reparsed.ToString());
    }
}
