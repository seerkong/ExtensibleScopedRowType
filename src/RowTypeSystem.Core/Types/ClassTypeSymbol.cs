using System.Collections.Immutable;

namespace RowTypeSystem.Core.Types;

public sealed class ClassTypeSymbol : TypeSymbol
{
    private readonly Lazy<ImmutableArray<ClassTypeSymbol>> _mro;
    private readonly Lazy<RowTypeSymbol> _rows;

    public ClassTypeSymbol(
        string name,
        RowTypeSymbol declaredRows,
        IReadOnlyList<BaseTypeReference> bases,
        bool isTrait,
        Func<ClassTypeSymbol, ImmutableArray<ClassTypeSymbol>> mroFactory,
        Func<ClassTypeSymbol, RowTypeSymbol> rowFactory)
        : base(name)
    {
        DeclaredRows = declaredRows;
        Bases = bases.ToImmutableArray();
        IsTrait = isTrait;
        _mro = new(() => mroFactory(this));
        _rows = new(() => rowFactory(this));
    }

    public RowTypeSymbol DeclaredRows { get; }

    public ImmutableArray<BaseTypeReference> Bases { get; }

    public bool IsTrait { get; }

    public ImmutableArray<ClassTypeSymbol> MethodResolutionOrder => _mro.Value;

    public RowTypeSymbol Rows => _rows.Value;
}

public sealed record BaseTypeReference(ClassTypeSymbol Type, InheritanceKind Inheritance, AccessModifier AccessModifier);

public enum InheritanceKind
{
    Real,
    Virtual,
}
