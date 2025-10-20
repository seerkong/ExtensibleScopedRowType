using System;
using System.Linq;
using RowLang.Core;
using RowLang.Core.Runtime;
using RowLang.Core.Scripting;
using RuntimeExecutionContext = RowLang.Core.Runtime.ExecutionContext;
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

Console.WriteLine("\nSubtype relationships:");
Console.WriteLine($"  T3 <: T1 ? {typeSystem.IsSubtype(merged, t1)}");
Console.WriteLine($"  T3 <: T2 ? {typeSystem.IsSubtype(merged, t2)}");

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

var context = new RuntimeExecutionContext(typeSystem);
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

Console.WriteLine("\nClass subtyping checks:");
var toStringClass = typeSystem.RequireClassSymbol("ToString");
var aClass = typeSystem.RequireClassSymbol("A");
var bClass = typeSystem.RequireClassSymbol("B");
Console.WriteLine($"  B <: A ? {typeSystem.IsSubtype(bClass, aClass)}");
Console.WriteLine($"  B <: ToString ? {typeSystem.IsSubtype(bClass, toStringClass)}");

var asyncEffect = registry.GetOrCreateEffect("async");
var fileReadSignature = registry.CreateFunctionType(
    "File::read",
    Array.Empty<TypeSymbol>(),
    registry.String,
    new[] { asyncEffect });

typeSystem.DefineClass(
    "File",
    new[] { RowMemberBuilder.Method("File", "read", fileReadSignature) },
    isOpen: true,
    bases: new[] { ("object", InheritanceKind.Real, AccessModifier.Public) },
    methodBodies: new[]
    {
        MethodBuilder.FromLambda(
            "File",
            "read",
            fileReadSignature,
            static (ctx, args) => new StringValue("file-data"))
    });

var fileInstance = context.Instantiate("File");

Console.WriteLine("\nEffect system demo:");
try
{
    context.Invoke(fileInstance, "read");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Calling File.read without async effect fails: {ex.Message}");
}

using (context.PushEffectScope(asyncEffect))
{
    var data = (StringValue)context.Invoke(fileInstance, "read");
    Console.WriteLine($"  Within async scope File.read -> {data.Value}");
}

Console.WriteLine("\nDemo complete.");

const string script = """
(module
  (effect async)
  (class ScriptFile
    (open)
    (method read
      (return str)
      (effects async)
      (body (const str script-data)))))
""";

Console.WriteLine("\nScript-based module demo:\n" + script + "\n");

var module = RowLangScript.Compile(script);
var scriptContext = module.CreateExecutionContext();
var scriptEffect = module.TypeSystem.Registry.GetOrCreateEffect("async");
var scriptInstance = scriptContext.Instantiate("ScriptFile");

try
{
    scriptContext.Invoke(scriptInstance, "read");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Without effect scope: {ex.Message}");
}

using (scriptContext.PushEffectScope(scriptEffect))
{
    var payload = (StringValue)scriptContext.Invoke(scriptInstance, "read");
    Console.WriteLine($"  With async scope ScriptFile.read -> {payload.Value}");
}
