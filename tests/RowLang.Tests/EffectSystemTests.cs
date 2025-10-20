using System;
using RowLang.Core;
using RowLang.Core.Runtime;
using RowLang.Core.Types;
using Xunit;

namespace RowLang.Tests;

public class EffectSystemTests
{
    [Fact]
    public void EffectfulMethodRequiresScope()
    {
        var typeSystem = new TypeSystem();
        var registry = typeSystem.Registry;
        var asyncEffect = registry.GetOrCreateEffect("async");
        var signature = registry.CreateFunctionType(
            "File::read",
            Array.Empty<TypeSymbol>(),
            registry.String,
            new[] { asyncEffect });

        typeSystem.DefineClass(
            "File",
            new[] { RowMemberBuilder.Method("File", "read", signature) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "File",
                    "read",
                    signature,
                    static (ctx, args) => new StringValue("payload"))
            });

        var context = new ExecutionContext(typeSystem);
        var instance = context.Instantiate("File");

        Assert.Throws<InvalidOperationException>(() => context.Invoke(instance, "read"));

        using (context.PushEffectScope(asyncEffect))
        {
            var result = (StringValue)context.Invoke(instance, "read");
            Assert.Equal("payload", result.Value);
        }
    }

    [Fact]
    public void NestedScopesAccumulateEffects()
    {
        var typeSystem = new TypeSystem();
        var registry = typeSystem.Registry;
        var asyncEffect = registry.GetOrCreateEffect("async");
        var ioEffect = registry.GetOrCreateEffect("IoError");
        var signature = registry.CreateFunctionType(
            "Worker::work",
            Array.Empty<TypeSymbol>(),
            registry.String,
            new[] { asyncEffect, ioEffect });

        typeSystem.DefineClass(
            "Worker",
            new[] { RowMemberBuilder.Method("Worker", "work", signature) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Worker",
                    "work",
                    signature,
                    static (ctx, args) => new StringValue("ok"))
            });

        var context = new ExecutionContext(typeSystem);
        var worker = context.Instantiate("Worker");

        Assert.Throws<InvalidOperationException>(() => context.Invoke(worker, "work"));

        using (context.PushEffectScope(asyncEffect))
        {
            Assert.Throws<InvalidOperationException>(() => context.Invoke(worker, "work"));

            using (context.PushEffectScope(ioEffect))
            {
                var result = (StringValue)context.Invoke(worker, "work");
                Assert.Equal("ok", result.Value);
            }
        }
    }
}
