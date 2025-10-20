using System;
using RowLang.Core.Runtime;
using RowLang.Core.Scripting;
using Xunit;

namespace RowLang.Tests;

public class EffectSystemTests
{
    [Fact]
    public void EffectfulMethodRequiresScope()
    {
        const string script = """
        (module
          (effect async)
          (class File
            !trace
            (open)
            !profile
            (method read
              (return str)
              (effects [async])
              (body !audit
                (let (
                      (meta { tags = ["async" "file"] retries = (const int 0) })
                      (message (concat (const str payload) (const str "!")))
                     )
                  message)))))
        """;

        var module = RowLangScript.Compile(script);
        var context = module.CreateExecutionContext();
        var asyncEffect = module.TypeSystem.Registry.GetOrCreateEffect("async");
        var instance = context.Instantiate("File");

        Assert.Throws<InvalidOperationException>(() =>
        {
            context.Invoke(instance, "read");
        });

        using (context.PushEffectScope(asyncEffect))
        {
            var result = (StringValue)context.Invoke(instance, "read");
            Assert.Equal("payload!", result.Value);
        }
    }

    [Fact]
    public void NestedScopesAccumulateEffects()
    {
        const string script = """
        (module
          (effect async)
          (effect IoError)
          (class Worker
            !trace
            (open)
            (method work
              (return str)
              (effects [async IoError])
              (body
                (let (
                      (payload { message = (const str ok) retries = [1 2 3] })
                     )
                  (concat (const str ok) (const str "!")))))))
        """;

        var module = RowLangScript.Compile(script);
        var context = module.CreateExecutionContext();
        var asyncEffect = module.TypeSystem.Registry.GetOrCreateEffect("async");
        var ioEffect = module.TypeSystem.Registry.GetOrCreateEffect("IoError");
        var worker = context.Instantiate("Worker");

        Assert.Throws<InvalidOperationException>(() =>
        {
            context.Invoke(worker, "work");
        });

        using (context.PushEffectScope(asyncEffect))
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Invoke(worker, "work");
            });

            using (context.PushEffectScope(ioEffect))
            {
                var result = (StringValue)context.Invoke(worker, "work");
                Assert.Equal("ok!", result.Value);
            }
        }
    }
}
