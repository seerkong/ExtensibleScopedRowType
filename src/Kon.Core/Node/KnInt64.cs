using System;

namespace Kon.Core.Node;

public class KnInt64 : KnNumber
{
    public long Value { get; }

    public KnInt64(long value)
    {
        Value = value;
    }

    public override long ToInt64Val() => Value;

    public override double ToDoubleVal() => Value;

    public override string ToString() => Value.ToString();

    public override bool Equals(object? obj) => obj is KnInt64 other && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();
}
