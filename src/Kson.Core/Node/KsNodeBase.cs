using System;

namespace Kson.Core.Node;

public abstract class KsNodeBase : KsNode
{
    public virtual bool ToBoolean() => true;

    public virtual bool IsBoolean() => false;

    public virtual KsBoolean AsBoolean() => throw new InvalidOperationException("Cannot convert to KsBoolean");

    public virtual KsString AsString() => throw new InvalidOperationException("Cannot convert to KsString");
}
