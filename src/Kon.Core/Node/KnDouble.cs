using System;

namespace Kon.Core.Node;

public class KnDouble : KnNumber
{
    public double Value { get; }

    public KnDouble(double value)
    {
        Value = value;
    }

    public override long ToInt64Val() => (long)Value;

    public override double ToDoubleVal() => Value;

    public override string ToString() => Value.ToString();

    public override bool Equals(object? obj) => obj is KnDouble other && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();
}
