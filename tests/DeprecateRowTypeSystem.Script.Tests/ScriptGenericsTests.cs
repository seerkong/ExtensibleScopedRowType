using System.Linq;
using RowTypeSystem.Core.Runtime;
using RowTypeSystem.Core.Scripting;
using Xunit;

namespace RowTypeSystem.Core.Tests;

public class ScriptGenericsTests
{
  [Fact]
  public void GenericRowType_CanBeInstantiatedAndUsedInScript()
  {
    const string source = """
        (module
          (row-type Tail
            (method tail_member
              (return int)))

          (row-type GenericRow ^(type-params [P ..Rest])
            (method head
              (return P))
            (spread ..Rest))

          (class TailImpl ~ Tail
            (method tail_member
              (return int)
              (body (const int 11))))

          (class Demo ~ GenericRow<int,Tail> ^(extends TailImpl)
            (method head
              (return int)
              (body (const int 42))))

          (run Demo head)
          (run Demo tail_member))
        """;

    var module = RowLangScript.Compile(source);
    var context = module.CreateExecutionContext();
    var results = module.ExecuteRuns(context).ToList();

    Assert.Equal(2, results.Count);

    Assert.Equal("Demo", results[0].Directive.ClassName);
    Assert.Equal("head", results[0].Directive.MemberName);
    Assert.Equal(42, ((IntValue)results[0].Result).Value);

    Assert.Equal("Demo", results[1].Directive.ClassName);
    Assert.Equal("tail_member", results[1].Directive.MemberName);
    Assert.Equal(11, ((IntValue)results[1].Result).Value);
  }
}
