using System;
using RowLang.Core.Runtime;
using RowLang.Core.Scripting;
using RowLang.Core.Types;
using Xunit;

namespace RowLang.Tests;

public class RowLangScriptTests
{
    [Fact]
    public void CompilesClassAndEffect()
    {
        const string source = """
        (module
          (effect async)
          (class File
            (open)
            (method read
              (return str)
              (effects async)
              (body (const str payload)))))
        """;

        var module = RowLangScript.Compile(source);
        var typeSystem = module.TypeSystem;
        var fileClass = typeSystem.RequireClassSymbol("File");

        Assert.True(typeSystem.IsSubtype(fileClass, typeSystem.RequireClassSymbol("object")));

        var context = module.CreateExecutionContext();
        var effect = typeSystem.Registry.GetOrCreateEffect("async");
        var instance = context.Instantiate("File");

        Assert.Throws<InvalidOperationException>(() => context.Invoke(instance, "read"));

        using (context.PushEffectScope(effect))
        {
            var value = (StringValue)context.Invoke(instance, "read");
            Assert.Equal("payload", value.Value);
        }
    }
}
