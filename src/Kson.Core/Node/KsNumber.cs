namespace Kson.Core.Node;

public abstract class KsNumber : KsValueNode
{
    public abstract long ToInt64Val();

    public abstract double ToDoubleVal();
}
