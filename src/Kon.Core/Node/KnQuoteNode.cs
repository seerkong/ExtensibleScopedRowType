using System;
using Kon.Core.Converter;
using Kon.Core.Node.Inner;

namespace Kon.Core.Node;

public class KnQuoteNode : KnValueNode
{
    public QuoteType Kind { get; }
    public KnNode Value { get; }

    public KnQuoteNode(QuoteType kind, KnNode value)
    {
        Kind = kind;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj) =>
        obj is KnQuoteNode other &&
        Kind == other.Kind &&
        Equals(Value, other.Value);

    public override int GetHashCode() => HashCode.Combine(Kind, Value);
}
