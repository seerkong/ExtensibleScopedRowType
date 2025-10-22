using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsUndefined : KsValueNode
{
    public static readonly KsUndefined Instance = new();

    private KsUndefined()
    {
    }

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KsUndefined;

    public override int GetHashCode() => 0;
}
