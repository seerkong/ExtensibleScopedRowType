namespace Kon.Core.Node;

public abstract class KnNumber : KnValueNode
{
    public abstract long ToInt64Val();

    public abstract double ToDoubleVal();
}
