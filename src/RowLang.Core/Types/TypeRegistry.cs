using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace RowLang.Core.Types;

public sealed class TypeRegistry
{
    private readonly ConcurrentDictionary<string, TypeSymbol> _types = new();
    private readonly ConcurrentDictionary<string, EffectSymbol> _effects = new();

    public TypeRegistry()
    {
        Register(new PrimitiveTypeSymbol("int"));
        Register(new PrimitiveTypeSymbol("str"));
        Register(new PrimitiveTypeSymbol("bool"));
        Register(new AnyTypeSymbol());
        Register(new NeverTypeSymbol());
    }

    public TypeSymbol Any => _types["any"];

    public TypeSymbol Never => _types["never"];

    public PrimitiveTypeSymbol Int => (PrimitiveTypeSymbol)_types["int"];

    public PrimitiveTypeSymbol String => (PrimitiveTypeSymbol)_types["str"];

    public PrimitiveTypeSymbol Bool => (PrimitiveTypeSymbol)_types["bool"];

    public EffectSymbol GetOrCreateEffect(string name)
    {
        return _effects.GetOrAdd(name, static key => new EffectSymbol(key));
    }

    public FunctionTypeSymbol CreateFunctionType(
        string name,
        IEnumerable<TypeSymbol> parameters,
        TypeSymbol returnType,
        IEnumerable<EffectSymbol>? effects = null)
    {
        var effectList = effects is null
            ? ImmutableArray<EffectSymbol>.Empty
            : effects.ToImmutableArray();
        return new FunctionTypeSymbol(name, parameters.ToImmutableArray(), returnType, effectList);
    }

    public void Register(TypeSymbol symbol)
    {
        _types[symbol.Name] = symbol;
    }

    public TypeSymbol Require(string name)
    {
        if (_types.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        throw new KeyNotFoundException($"Type '{name}' is not registered.");
    }
}
