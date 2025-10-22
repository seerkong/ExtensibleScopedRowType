using Kon.Core.Converter;

namespace Kon.Core.Node;

public class KnUndefined : KnValueNode
{
    public static readonly KnUndefined Instance = new();

    private KnUndefined()
    {
    }

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KnUndefined;

    public override int GetHashCode() => 0;
}
