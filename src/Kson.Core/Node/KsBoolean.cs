using System;

namespace Kson.Core.Node;

public class KsBoolean : KsValueNode
{
    public static readonly KsBoolean True = new(true);
    public static readonly KsBoolean False = new(false);

    public bool Value { get; }

    public KsBoolean(bool value)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString().ToLowerInvariant();

    public override bool Equals(object? obj) => obj is KsBoolean other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool ToBoolean() => Value;

    public override bool IsBoolean() => true;

    public override KsBoolean AsBoolean() => this;
}
