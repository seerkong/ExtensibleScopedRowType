using System;

namespace Kon.Core.Node;

public class KnBoolean : KnValueNode
{
    public static readonly KnBoolean True = new(true);
    public static readonly KnBoolean False = new(false);

    public bool Value { get; }

    public KnBoolean(bool value)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString().ToLowerInvariant();

    public override bool Equals(object? obj) => obj is KnBoolean other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool ToBoolean() => Value;

    public override bool IsBoolean() => true;

    public override KnBoolean AsBoolean() => this;
}
