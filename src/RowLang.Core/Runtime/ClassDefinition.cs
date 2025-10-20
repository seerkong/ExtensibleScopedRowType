using System.Collections.Immutable;
using RowLang.Core.Types;

namespace RowLang.Core.Runtime;

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
