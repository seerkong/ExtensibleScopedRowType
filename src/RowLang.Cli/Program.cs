using System;
using System.Linq;
using RowLang.Core;
using RowLang.Core.Runtime;
using RowLang.Core.Types;

var typeSystem = new TypeSystem();
var registry = typeSystem.Registry;

Console.WriteLine("RowLang CLI demo - extensible scoped row types\n");

var t1 = typeSystem.DefineRowType(
    "T1",
    new[]
    {
        RowMemberBuilder.Method("T1", "a", registry.CreateFunctionType("T1::a", Array.Empty<TypeSymbol>(), registry.Int)),
        RowMemberBuilder.Method("T1", "b", registry.CreateFunctionType("T1::b", Array.Empty<TypeSymbol>(), registry.String)),
    },
    isOpen: true);

var t2 = typeSystem.DefineRowType(
    "T2",
    new[]
    {
        RowMemberBuilder.Method("T2", "b", registry.CreateFunctionType("T2::b", Array.Empty<TypeSymbol>(), registry.Int)),
        RowMemberBuilder.Method("T2", "c", registry.CreateFunctionType("T2::c", Array.Empty<TypeSymbol>(), registry.Bool)),
    },
    isOpen: true);

var merged = typeSystem.MergeRows("T3", t1, t2);
Console.WriteLine("Merged row type T3 combines T1 and T2:");
foreach (var row in merged.Members)
{
    Console.WriteLine($"  {row.Origin}::{row.Name} : {row.Type}");
}

var toStringSignature = registry.CreateFunctionType("to_string", new[] { registry.Any }, registry.String);

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

var context = new ExecutionContext(typeSystem);
var instance = context.Instantiate("B");
var defaultCall = (StringValue)context.Invoke(instance, "to_string");
var traitCall = (StringValue)context.Invoke(instance, "to_string", "ToString");
var baseCall = (StringValue)context.Invoke(instance, "to_string", "A");

Console.WriteLine("\nToString trait dispatch:");
Console.WriteLine($"  B::to_string() -> {defaultCall.Value}");
Console.WriteLine($"  ToString::to_string(B) -> {traitCall.Value}");
Console.WriteLine($"  A::to_string(B) -> {baseCall.Value}");

Console.WriteLine("\nMethod resolution order for class B:");
Console.WriteLine(string.Join(" -> ", typeSystem.RequireClassSymbol("B").MethodResolutionOrder.Select(t => t.Name)));

Console.WriteLine("\nDemo complete.");
