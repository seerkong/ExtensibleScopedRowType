using System;

namespace Kon.Core.Node;

public abstract class KnNodeBase : KnNode
{
    public virtual bool ToBoolean() => true;

    public virtual bool IsBoolean() => false;

    public virtual KnBoolean AsBoolean() => throw new InvalidOperationException("Cannot convert to KnBoolean");

    public virtual KnString AsString() => throw new InvalidOperationException("Cannot convert to KnString");
}
