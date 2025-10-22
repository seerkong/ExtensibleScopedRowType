using System;
using System.Collections.Immutable;
using System.Linq;
using RowLang.Core;
using RowLang.Core.Types;
using Xunit;

namespace RowLang.Tests;

public class GenericRowTypeTests
{
    [Fact]
    public void DefineGenericRowType_WithTypeParameters_ShouldSucceed()
    {
        var typeSystem = new TypeSystem();
        
        // Define type parameters P and Q (where Q is a row parameter)
        var typeParams = new[]
        {
            new TypeParameter("P"),
            new TypeParameter("Q") { IsRowParameter = true }
        };
        
        // Define members: a: () -> P, b: () -> str, ..Q
        var members = new[]
        {
            new RowMember("a", new FunctionTypeSymbol("() -> P", 
                ImmutableArray<TypeSymbol>.Empty, 
                typeParams[0], 
                ImmutableArray<EffectSymbol>.Empty), 
                RowQualifier.Default, "T1", false),
            new RowMember("b", new FunctionTypeSymbol("() -> str", 
                ImmutableArray<TypeSymbol>.Empty, 
                typeSystem.Registry.String, 
                ImmutableArray<EffectSymbol>.Empty), 
                RowQualifier.Default, "T1", false),
            new RowMember("..Q", typeParams[1], RowQualifier.Default, "T1", false)
        };
        
        var genericType = typeSystem.DefineGenericRowType("T1", typeParams, members, true);
        
        Assert.Equal("T1", genericType.Name);
        Assert.Equal(2, genericType.TypeParameters.Length);
        Assert.True(genericType.IsOpen);
    }
    
    [Fact]
    public void InstantiateGenericRowType_WithConcreteTypes_ShouldCreateCorrectRowType()
    {
        var typeSystem = new TypeSystem();
        
        // Define a concrete row type T2 for spreading
        var t2Members = new[]
        {
            new RowMember("c", new FunctionTypeSymbol("() -> bool", 
                ImmutableArray<TypeSymbol>.Empty, 
                typeSystem.Registry.Bool, 
                ImmutableArray<EffectSymbol>.Empty), 
                RowQualifier.Default, "T2", false)
        };
        var t2 = typeSystem.DefineRowType("T2", t2Members, false);
        
        // Define generic type T1<P, Q>
        var typeParams = new[]
        {
            new TypeParameter("P"),
            new TypeParameter("Q") { IsRowParameter = true }
        };
        
        var members = new[]
        {
            new RowMember("a", new FunctionTypeSymbol("() -> P", 
                ImmutableArray<TypeSymbol>.Empty, 
                typeParams[0], 
                ImmutableArray<EffectSymbol>.Empty), 
                RowQualifier.Default, "T1", false),
            new RowMember("..Q", typeParams[1], RowQualifier.Default, "T1", false)
        };
        
        var genericType = typeSystem.DefineGenericRowType("T1", typeParams, members, true);
        
        // Instantiate T1<int, T2>
        var instantiated = typeSystem.InstantiateGenericRowType(genericType, typeSystem.Registry.Int, t2);
        
        Assert.Equal("T1<int,T2>", instantiated.Name);
        Assert.Equal(2, instantiated.Members.Length); // a and c (..Q spreads T2's members)
        
        var aMember = instantiated.Members.First(m => m.Name == "a");
        Assert.Equal("int", ((FunctionTypeSymbol)aMember.Type).ReturnType.Name);
        
        var cMember = instantiated.Members.First(m => m.Name == "c");
        Assert.Equal("bool", ((FunctionTypeSymbol)cMember.Type).ReturnType.Name);
    }
    
    [Fact]
    public void SpreadParameter_WithNonRowType_ShouldThrowException()
    {
        var typeSystem = new TypeSystem();
        
        var typeParams = new[]
        {
            new TypeParameter("Q") { IsRowParameter = true }
        };
        
        var members = new[]
        {
            new RowMember("..Q", typeParams[0], RowQualifier.Default, "T1", false)
        };
        
        var genericType = typeSystem.DefineGenericRowType("T1", typeParams, members, true);
        
        // Try to instantiate with a non-row type (int)
        Assert.Throws<ArgumentException>(() => 
            typeSystem.InstantiateGenericRowType(genericType, typeSystem.Registry.Int));
    }
}
