using System;
using System.Collections.Immutable;
using System.Linq;
using RowLang.Core;
using RowLang.Core.Types;
using RowLang.Core.Runtime;
using Xunit;

namespace RowLang.Tests;

public class TypeProjectionTests
{
    [Fact]
    public void TypeProjection_ValidProjection_ShouldSucceed()
    {
        var typeSystem = new TypeSystem();
        
        // Define ToString trait
        _ = typeSystem.DefineClass(
            "ToString",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str", 
                    ImmutableArray<TypeSymbol>.Empty, 
                    typeSystem.Registry.String, 
                    ImmutableArray<EffectSymbol>.Empty), 
                    RowQualifier.Default, "ToString", true)
            },
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>(),
            isTrait: true);

        // Define class A implementing ToString
        var classA = typeSystem.DefineClass(
            "A",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str", 
                    ImmutableArray<TypeSymbol>.Empty, 
                    typeSystem.Registry.String, 
                    ImmutableArray<EffectSymbol>.Empty), 
                    RowQualifier.Default, "A", true)
            },
            isOpen: false,
            bases: new[] { ("ToString", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda("A", "to_string", 
                    new FunctionTypeSymbol("() -> str", 
                        ImmutableArray<TypeSymbol>.Empty, 
                        typeSystem.Registry.String, 
                        ImmutableArray<EffectSymbol>.Empty),
                    (ctx, args) => new StringValue("A"))
            });

        var context = new RowLang.Core.Runtime.ExecutionContext(typeSystem);
        var instance = context.Instantiate("A");
        
        // Test projection to trait
        var projection = new TypeProjection(classA.Type, typeSystem.RequireClassSymbol("ToString"));
        Assert.True(projection.IsValidProjection(typeSystem));
        
        // Test runtime projection
        var result = context.InvokeWithProjection(instance, "ToString", "to_string");
        Assert.Equal("A", ((StringValue)result).Value);
    }
    
    [Fact]
    public void TypeProjection_InvalidProjection_ShouldThrow()
    {
        var typeSystem = new TypeSystem();
        
        var classA = typeSystem.DefineClass(
            "A",
            Array.Empty<RowMember>(),
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>());

        var classB = typeSystem.DefineClass(
            "B",
            Array.Empty<RowMember>(),
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>());

        var context = new RowLang.Core.Runtime.ExecutionContext(typeSystem);
        var instance = context.Instantiate("A");
        
        // Test invalid projection (A cannot be viewed as B)
        Assert.Throws<InvalidOperationException>(() => 
            context.InvokeWithProjection(instance, "B", "someMethod"));
    }
    
    [Fact]
    public void TraitProjection_ShouldPointToTopImplementation()
    {
        var typeSystem = new TypeSystem();
        
        // Define ToString trait
        _ = typeSystem.DefineClass(
            "ToString",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str", 
                    ImmutableArray<TypeSymbol>.Empty, 
                    typeSystem.Registry.String, 
                    ImmutableArray<EffectSymbol>.Empty), 
                    RowQualifier.Default, "ToString", true)
            },
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>(),
            isTrait: true);

        // Define class A
        var classA = typeSystem.DefineClass(
            "A",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str", 
                  ImmutableArray<TypeSymbol>.Empty, 
                    typeSystem.Registry.String, 
                    ImmutableArray<EffectSymbol>.Empty), 
                    RowQualifier.Default, "A", true)
            },
            isOpen: false,
            bases: new[] { ("ToString", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda("A", "to_string", 
                    new FunctionTypeSymbol("() -> str", 
                        ImmutableArray<TypeSymbol>.Empty, 
                        typeSystem.Registry.String, 
                        ImmutableArray<EffectSymbol>.Empty),
                    (ctx, args) => new StringValue("A"))
            });

        // Define class B inheriting from A
        var classB = typeSystem.DefineClass(
            "B",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str", 
                    ImmutableArray<TypeSymbol>.Empty, 
                    typeSystem.Registry.String, 
                    ImmutableArray<EffectSymbol>.Empty), 
                    RowQualifier.Override, "B", true)
            },
            isOpen: false,
            bases: new[] { ("A", InheritanceKind.Real, AccessModifier.Public) },
            methodBodies: new[]
            {
                MethodBuilder.FromLambda("B", "to_string", 
                    new FunctionTypeSymbol("() -> str", 
                        ImmutableArray<TypeSymbol>.Empty, 
                        typeSystem.Registry.String, 
                        ImmutableArray<EffectSymbol>.Empty),
                    (ctx, args) => new StringValue("B"))
            });

        var context = new RowLang.Core.Runtime.ExecutionContext(typeSystem);
        var instance = context.Instantiate("B");
        
        // ToString::to_string should point to the topmost implementation ("B")
        var traitResult = context.InvokeWithProjection(instance, "ToString", "to_string");
        Assert.Equal("B", ((StringValue)traitResult).Value);
        
        // A::to_string should call A's specific implementation
        var aResult = context.Invoke(instance, "to_string", "A");
        Assert.Equal("A", ((StringValue)aResult).Value);
    }

    [Fact]
    public void TraitProjection_WhenTraitNotImplemented_ShouldFail()
    {
        var typeSystem = new TypeSystem();

        var toStringTrait = typeSystem.DefineClass(
            "ToString",
            new[]
            {
                new RowMember("to_string", new FunctionTypeSymbol("() -> str",
                    ImmutableArray<TypeSymbol>.Empty,
                    typeSystem.Registry.String,
                    ImmutableArray<EffectSymbol>.Empty),
                    RowQualifier.Default, "ToString", true)
            },
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>(),
            isTrait: true);

        typeSystem.DefineClass(
            "A",
            Array.Empty<RowMember>(),
            isOpen: false,
            bases: Array.Empty<(string, InheritanceKind, AccessModifier)>(),
            methodBodies: Array.Empty<MethodBody>());

        var context = new RowLang.Core.Runtime.ExecutionContext(typeSystem);
        var instance = context.Instantiate("A");

        Assert.Throws<InvalidOperationException>(() =>
            context.InvokeWithProjection(instance, "ToString", "to_string"));
    }
}
