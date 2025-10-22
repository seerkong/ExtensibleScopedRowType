using System;

namespace Kson.Core.Node;

public class KsInt64 : KsNumber
{
    public long Value { get; }

    public KsInt64(long value)
    {
        Value = value;
    }

    public override long ToInt64Val() => Value;

    public override double ToDoubleVal() => Value;

    public override string ToString() => Value.ToString();

    public override bool Equals(object? obj) => obj is KsInt64 other && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();
}
