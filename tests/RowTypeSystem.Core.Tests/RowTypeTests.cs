using System;
using System.Linq;
using RowTypeSystem.Core;
using RowTypeSystem.Core.Runtime;
using RuntimeExecutionContext = RowTypeSystem.Core.Runtime.ExecutionContext;
using RowTypeSystem.Core.Types;
using Xunit;

namespace RowTypeSystem.Core.Tests;

public class RowTypeTests
{
    [Fact]
    public void RowTypesKeepScopedOrigins()
    {
        var system = new TypeSystem();
        var registry = system.Registry;
        var t1 = system.DefineRowType(
            "T1",
            new[]
            {
                RowMemberBuilder.Method("T1", "a", registry.CreateFunctionType("T1::a", Array.Empty<TypeSymbol>(), registry.Int)),
                RowMemberBuilder.Method("T1", "b", registry.CreateFunctionType("T1::b", Array.Empty<TypeSymbol>(), registry.String)),
            },
            isOpen: true);

        var t2 = system.DefineRowType(
            "T2",
            new[]
            {
                RowMemberBuilder.Method("T2", "b", registry.CreateFunctionType("T2::b", Array.Empty<TypeSymbol>(), registry.Int)),
                RowMemberBuilder.Method("T2", "c", registry.CreateFunctionType("T2::c", Array.Empty<TypeSymbol>(), registry.Bool)),
            },
            isOpen: true);

        var merged = system.MergeRows("T3", t1, t2);

        var defaultB = merged.Resolve("b");
        Assert.NotNull(defaultB);
        Assert.Equal("T1", defaultB!.Origin);

        var specificB = merged.Resolve("b", "T2");
        Assert.NotNull(specificB);
        Assert.Equal("T2", specificB!.Origin);

        Assert.Equal(2, merged.EnumerateByName("b").Count());
    }

    [Fact]
    public void MethodResolutionOrderFollowsC3()
    {
        var system = new TypeSystem();
        var emptyRows = Array.Empty<RowMember>();
        var emptyBodies = Array.Empty<MethodBody>();

        system.DefineClass("A", emptyRows, true, new[] { ("object", InheritanceKind.Real, AccessModifier.Private) }, emptyBodies);
        system.DefineClass("B", emptyRows, true, new[] { ("object", InheritanceKind.Real, AccessModifier.Private) }, emptyBodies);
        system.DefineClass("C", emptyRows, true, new[] { ("object", InheritanceKind.Real, AccessModifier.Private) }, emptyBodies);
        system.DefineClass("D", emptyRows, true, new[] { ("object", InheritanceKind.Real, AccessModifier.Private) }, emptyBodies);
        system.DefineClass("E", emptyRows, true, new[] { ("object", InheritanceKind.Real, AccessModifier.Private) }, emptyBodies);

        system.DefineClass(
            "K1",
            emptyRows,
            true,
            new[] { ("C", InheritanceKind.Real, AccessModifier.Private), ("A", InheritanceKind.Real, AccessModifier.Private), ("B", InheritanceKind.Real, AccessModifier.Private) },
            emptyBodies);

        system.DefineClass(
            "K2",
            emptyRows,
            true,
            new[] { ("B", InheritanceKind.Real, AccessModifier.Private), ("D", InheritanceKind.Real, AccessModifier.Private), ("E", InheritanceKind.Real, AccessModifier.Private) },
            emptyBodies);

        system.DefineClass(
            "K3",
            emptyRows,
            true,
            new[] { ("A", InheritanceKind.Real, AccessModifier.Private), ("D", InheritanceKind.Real, AccessModifier.Private) },
            emptyBodies);

        var z = system.DefineClass(
            "Z",
            emptyRows,
            true,
            new[] { ("K1", InheritanceKind.Real, AccessModifier.Private), ("K3", InheritanceKind.Real, AccessModifier.Private), ("K2", InheritanceKind.Real, AccessModifier.Private) },
            emptyBodies);

        var order = z.Type.MethodResolutionOrder.Select(t => t.Name).ToArray();
        Assert.Equal(new[] { "Z", "K1", "C", "K3", "A", "K2", "B", "D", "E", "object" }, order);
    }

    [Fact]
    public void VirtualOverridesRequireImplementation()
    {
        var system = new TypeSystem();
        var registry = system.Registry;
        var toStringSignature = registry.CreateFunctionType("to_string", new[] { system.Registry.Any }, registry.String);

        system.DefineClass(
            "ToString",
            new[] { RowMemberBuilder.Method("ToString", "to_string", toStringSignature, RowQualifier.Virtual) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: Array.Empty<MethodBody>(),
            isTrait: true);

        system.DefineClass(
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

        var b = system.DefineClass(
            "B",
            new[]
            {
                RowMemberBuilder.Method("B", "to_string", toStringSignature, RowQualifier.Override)
            },
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

        var context = new RuntimeExecutionContext(system);
        var instance = context.Instantiate("B");

        var direct = (StringValue)context.Invoke(instance, "to_string");
        Assert.Equal("B", direct.Value);

        var viaTrait = (StringValue)context.Invoke(instance, "to_string", "ToString");
        Assert.Equal("B", viaTrait.Value);

        var viaBase = (StringValue)context.Invoke(instance, "to_string", "A");
        Assert.Equal("A", viaBase.Value);
    }

    [Fact]
    public void InheritQualifierForwardsBaseImplementation()
    {
        var system = new TypeSystem();
        var registry = system.Registry;
        var valueSignature = registry.CreateFunctionType("value", Array.Empty<TypeSymbol>(), registry.Int);

        system.DefineClass(
            "Base",
            new[] { RowMemberBuilder.Method("Base", "value", valueSignature) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Base",
                    "value",
                    valueSignature,
                    static (ctx, args) => new IntValue(42))
            });

        system.DefineClass(
            "Derived",
            new[] { RowMemberBuilder.Method("Derived", "value", valueSignature, RowQualifier.Inherit) },
            isOpen: true,
            bases: new[] { ("Base", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: Array.Empty<MethodBody>());

        var context = new RuntimeExecutionContext(system);
        var instance = context.Instantiate("Derived");

        var result = (IntValue)context.Invoke(instance, "value");
        Assert.Equal(42, result.Value);

        var viaBase = (IntValue)context.Invoke(instance, "value", "Base");
        Assert.Equal(42, viaBase.Value);
    }

    [Fact]
    public void VirtualMembersPruneEarlierRows()
    {
        var system = new TypeSystem();
        var registry = system.Registry;

        var signature = registry.CreateFunctionType("value", Array.Empty<TypeSymbol>(), registry.Int);

        system.DefineClass(
            "DefaultValue",
            new[]
            {
                RowMemberBuilder.Method("DefaultValue", "value", signature),
            },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "DefaultValue",
                    "value",
                    signature,
                    static (_, _) => new IntValue(0))
            },
            isTrait: true);

        system.DefineClass(
            "Base",
            new[]
            {
                RowMemberBuilder.Method("Base", "value", signature, RowQualifier.Virtual),
            },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Base",
                    "value",
                    signature,
                    static (_, _) => new IntValue(1),
                    RowQualifier.Virtual),
            });

        var derived = system.DefineClass(
            "Derived",
            new[]
            {
                RowMemberBuilder.Method("Derived", "value", signature, RowQualifier.Override),
            },
            isOpen: true,
            bases: new[]
            {
                ("DefaultValue", InheritanceKind.Real, AccessModifier.Public),
                ("Base", InheritanceKind.Real, AccessModifier.Public)
            },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Derived",
                    "value",
                    signature,
                    static (_, _) => new IntValue(2),
                    RowQualifier.Override),
            });

        var relevant = derived.Type.Rows.Members.Where(m => m.Name == "value").ToArray();
        Assert.Contains(relevant, m => m.Origin == "Derived");
        Assert.Contains(relevant, m => m.Origin == "Base" && m.IsVirtual);
        Assert.DoesNotContain(relevant, m => m.Origin == "DefaultValue");
    }

    [Fact]
    public void FinalMembersCannotBeOverridden()
    {
        var system = new TypeSystem();
        var registry = system.Registry;
        var valueSignature = registry.CreateFunctionType("value", Array.Empty<TypeSymbol>(), registry.Int);

        system.DefineClass(
            "Base",
            new[] { RowMemberBuilder.Method("Base", "value", valueSignature, RowQualifier.Final) },
            isOpen: true,
            bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Base",
                    "value",
                    valueSignature,
                    static (ctx, args) => new IntValue(7),
                    RowQualifier.Final)
            });

        system.DefineClass(
            "Derived",
            new[] { RowMemberBuilder.Method("Derived", "value", valueSignature, RowQualifier.Override) },
            isOpen: true,
            bases: new[] { ("Base", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda(
                    "Derived",
                    "value",
                    valueSignature,
                    static (ctx, args) => new IntValue(99),
                    RowQualifier.Override)
            });

        var derived = system.RequireClass("Derived");

        Assert.Throws<InvalidOperationException>(() => _ = derived.Type.Rows);
    }
}
