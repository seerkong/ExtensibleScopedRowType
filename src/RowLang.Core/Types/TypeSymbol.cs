using System.Collections.Immutable;
using System.Linq;

namespace RowLang.Core.Types;

public abstract class TypeSymbol
{
    protected TypeSymbol(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name;
}

public sealed class PrimitiveTypeSymbol : TypeSymbol
{
    internal PrimitiveTypeSymbol(string name) : base(name)
    {
    }
}

public sealed class AnyTypeSymbol : TypeSymbol
{
    internal AnyTypeSymbol() : base("any")
    {
    }
}

public sealed class NeverTypeSymbol : TypeSymbol
{
    internal NeverTypeSymbol() : base("never")
    {
    }
}

public sealed class FunctionTypeSymbol : TypeSymbol
{
    public FunctionTypeSymbol(
        string name,
        ImmutableArray<TypeSymbol> parameters,
        TypeSymbol returnType,
        ImmutableArray<EffectSymbol> effects)
        : base(name)
    {
        Parameters = parameters;
        ReturnType = returnType;
        Effects = effects;
    }

    public ImmutableArray<TypeSymbol> Parameters { get; }

    public TypeSymbol ReturnType { get; }

    public ImmutableArray<EffectSymbol> Effects { get; }

    public override string ToString()
    {
        var parameters = string.Join(", ", Parameters.Select(p => p.Name));
        var effects = Effects.IsDefaultOrEmpty
            ? string.Empty
            : $" throws {string.Join(" & ", Effects.Select(e => e.Name))}";
        return $"({parameters}) -> {ReturnType.Name}{effects}";
    }
}

public sealed class EffectSymbol : TypeSymbol
{
    public EffectSymbol(string name) : base(name)
    {
    }
}
