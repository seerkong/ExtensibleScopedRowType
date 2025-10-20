using System;
using RowLang.Core;
using RowLang.Core.Runtime;
using RowLang.Core.Types;
using Xunit;

namespace RowLang.Tests;

public class SubtypingTests
{
    [Fact]
    public void RowTypeMergingEstablishesWidthSubtyping()
    {
        var typeSystem = new TypeSystem();
        var registry = typeSystem.Registry;

        var t1a = registry.CreateFunctionType("T1::a", Array.Empty<TypeSymbol>(), registry.Int);
        var t1b = registry.CreateFunctionType("T1::b", Array.Empty<TypeSymbol>(), registry.String);
        var t2b = registry.CreateFunctionType("T2::b", Array.Empty<TypeSymbol>(), registry.Int);
        var t2c = registry.CreateFunctionType("T2::c", Array.Empty<TypeSymbol>(), registry.Bool);

        var t1 = typeSystem.DefineRowType(
            "T1",
            new[]
            {
                RowMemberBuilder.Method("T1", "a", t1a),
                RowMemberBuilder.Method("T1", "b", t1b),
            },
            isOpen: true);

        var t2 = typeSystem.DefineRowType(
            "T2",
            new[]
            {
                RowMemberBuilder.Method("T2", "b", t2b),
                RowMemberBuilder.Method("T2", "c", t2c),
            },
            isOpen: true);

        var merged = typeSystem.MergeRows("T3", t1, t2);

        Assert.True(typeSystem.IsSubtype(merged, t1));
        Assert.True(typeSystem.IsSubtype(merged, t2));
        Assert.False(typeSystem.IsSubtype(t1, merged));
    }

    [Fact]
    public void ClassSubtypingFollowsRowStructure()
    {
        var typeSystem = new TypeSystem();
        var registry = typeSystem.Registry;

        var toStringSignature = registry.CreateFunctionType(
            "to_string",
            new[] { registry.Any },
            registry.String);

        typeSystem.DefineClass(
            "ToString",
            new[] { RowMemberBuilder.Method("ToString", "to_string", toStringSignature, RowQualifier.Virtual) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: Array.Empty<MethodBody>(),
            isTrait: true);

        typeSystem.DefineClass(
            "A",
            new[] { RowMemberBuilder.Method("A", "to_string", toStringSignature, RowQualifier.Override) },
            isOpen: true,
            bases: new[] { ("ToString", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "A",
                    "to_string",
                    toStringSignature,
                    static (ctx, args) => new StringValue("A"),
                    RowQualifier.Override)
            });

        typeSystem.DefineClass(
            "B",
            new[] { RowMemberBuilder.Method("B", "to_string", toStringSignature, RowQualifier.Override) },
            isOpen: true,
            bases: new[] { ("A", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "B",
                    "to_string",
                    toStringSignature,
                    static (ctx, args) => new StringValue("B"),
                    RowQualifier.Override)
            });

        var toStringType = typeSystem.RequireClassSymbol("ToString");
        var aType = typeSystem.RequireClassSymbol("A");
        var bType = typeSystem.RequireClassSymbol("B");

        Assert.True(typeSystem.IsSubtype(aType, toStringType));
        Assert.True(typeSystem.IsSubtype(bType, toStringType));
        Assert.True(typeSystem.IsSubtype(bType, aType));
    }

    [Fact]
    public void EffectMismatchBreaksSubtypingCompatibility()
    {
        var typeSystem = new TypeSystem();
        var registry = typeSystem.Registry;
        var asyncEffect = registry.GetOrCreateEffect("async");

        var baseline = registry.CreateFunctionType(
            "Worker::work",
            Array.Empty<TypeSymbol>(),
            registry.String);

        var effectful = registry.CreateFunctionType(
            "AsyncWorker::work",
            Array.Empty<TypeSymbol>(),
            registry.String,
            new[] { asyncEffect });

        var worker = typeSystem.DefineRowType(
            "Worker",
            new[] { RowMemberBuilder.Method("Worker", "work", baseline) },
            isOpen: true);

        var asyncWorker = typeSystem.DefineRowType(
            "AsyncWorker",
            new[] { RowMemberBuilder.Method("AsyncWorker", "work", effectful) },
            isOpen: true);

        Assert.False(typeSystem.IsSubtype(asyncWorker, worker));
    }
}
