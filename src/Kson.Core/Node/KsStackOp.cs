using System;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsStackOp : KsValueNode
{
    public string Value { get; }

    public KsStackOp(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KsStackOp other && Value.Equals(other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}
