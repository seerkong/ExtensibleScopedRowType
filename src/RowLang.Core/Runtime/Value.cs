using System.Collections.Generic;
using System.Collections.Immutable;
using RowLang.Core.Types;

namespace RowLang.Core.Runtime;

public abstract record Value(TypeSymbol Type);

public sealed record IntValue(int Value) : Value(RuntimeTypeRegistry.Int);

public sealed record StringValue(string Value) : Value(RuntimeTypeRegistry.Str);

public sealed record BoolValue(bool Value) : Value(RuntimeTypeRegistry.Bool);

public sealed record ListValue(ImmutableArray<Value> Elements) : Value(RuntimeTypeRegistry.Any);

public sealed record MapValue(IReadOnlyDictionary<string, Value> Properties) : Value(RuntimeTypeRegistry.Any);

public sealed record AnyValue(object? Value) : Value(RuntimeTypeRegistry.Any);

public sealed record FunctionValue(FunctionTypeSymbol Signature, Func<InvocationContext, IReadOnlyList<Value>, Value> Body) : Value(Signature);

public sealed record ObjectValue(ClassTypeSymbol Class, IReadOnlyDictionary<string, IReadOnlyList<RowImplementation>> Rows) : Value(Class.Rows);

public sealed record RowImplementation(RowMember Member, FunctionValue Function);

public sealed class InvocationContext
{
    private readonly ExecutionContext _executionContext;
    private readonly ObjectValue? _self;

    public InvocationContext(ExecutionContext executionContext, ObjectValue? self)
    {
        _executionContext = executionContext;
        _self = self;
    }

    public ExecutionContext Context => _executionContext;
    
    public ExecutionContext Execution => _executionContext;

    public ObjectValue? Self => _self;
}

internal static class RuntimeTypeRegistry
{
    public static PrimitiveTypeSymbol Int { get; private set; } = default!;
    public static PrimitiveTypeSymbol Str { get; private set; } = default!;
    public static PrimitiveTypeSymbol Bool { get; private set; } = default!;
    public static TypeSymbol Any { get; private set; } = default!;

    public static void Initialize(TypeRegistry registry)
    {
        Int = registry.Int;
        Str = registry.String;
        Bool = registry.Bool;
        Any = registry.Any;
    }
}
