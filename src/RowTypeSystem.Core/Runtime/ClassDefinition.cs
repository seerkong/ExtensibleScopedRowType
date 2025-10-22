using System.Collections.Immutable;
using RowTypeSystem.Core.Types;

namespace RowTypeSystem.Core.Runtime;

public sealed class ClassDefinition
{
    public ClassDefinition(ClassTypeSymbol type, ImmutableArray<MethodBody> methods)
    {
        Type = type;
        Methods = methods;
    }

    public ClassTypeSymbol Type { get; }

    public ImmutableArray<MethodBody> Methods { get; }
}

public sealed record MethodBody(RowMember Member, Func<InvocationContext, IReadOnlyList<Value>, Value> Implementation);
