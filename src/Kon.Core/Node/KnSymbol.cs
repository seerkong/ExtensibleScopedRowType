using System;
using Kon.Core.Converter;

namespace Kon.Core.Node;

public class KnSymbol : KnValueNode
{
    public string Value { get; }

    public KnSymbol(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KnSymbol other && Value.Equals(other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}
