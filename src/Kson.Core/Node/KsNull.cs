using System;

namespace Kson.Core.Node;

public class KsNull : KsValueNode
{
    public static readonly KsNull Null = new();

    public override string ToString() => "null";

    public override bool Equals(object? obj) => obj is KsNull;

    public override int GetHashCode() => 0;

    public override bool ToBoolean() => false;
}
