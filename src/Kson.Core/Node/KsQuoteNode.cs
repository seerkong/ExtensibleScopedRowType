using System;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsQuoteNode : KsValueNode
{
    public QuoteType Kind { get; }
    public KsNode Value { get; }

    public KsQuoteNode(QuoteType kind, KsNode value)
    {
        Kind = kind;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj) =>
        obj is KsQuoteNode other &&
        Kind == other.Kind &&
        Equals(Value, other.Value);

    public override int GetHashCode() => HashCode.Combine(Kind, Value);
}
