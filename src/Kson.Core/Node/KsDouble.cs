using System;

namespace Kson.Core.Node;

public class KsDouble : KsNumber
{
    public double Value { get; }

    public KsDouble(double value)
    {
        Value = value;
    }

    public override long ToInt64Val() => (long)Value;

    public override double ToDoubleVal() => Value;

    public override string ToString() => Value.ToString();

    public override bool Equals(object? obj) => obj is KsDouble other && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();
}
