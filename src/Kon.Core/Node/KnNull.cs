using System;

namespace Kon.Core.Node;

public class KnNull : KnValueNode
{
    public static readonly KnNull Null = new();

    public override string ToString() => "null";

    public override bool Equals(object? obj) => obj is KnNull;

    public override int GetHashCode() => 0;

    public override bool ToBoolean() => false;
}
