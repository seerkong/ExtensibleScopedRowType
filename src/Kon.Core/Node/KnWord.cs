using System;
using System.Collections.Generic;
using System.Linq;
using Kon.Core.Converter;
using Kon.Core.Node.Inner;

namespace Kon.Core.Node;

public class KnWord : KnValueNode, SupportPrefixesPostfixes
{
    public KnArray? AnnotationPrefixes { get; set; }
    public KnMap? WithEffectPrefix { get; set; }
    public KnArray? TypePrefixes { get; set; }
    public KnArray? UnboundTypes { get; set; }
    public KnArray? Postfixes { get; set; }
    public List<string> Namespace { get; private set; } = new();
    public string Value { get; }

    public KnWord(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public KnWord(IReadOnlyList<string> pathItems)
    {
        if (pathItems == null || pathItems.Count == 0)
        {
            throw new ArgumentException("KnWord pathItems size should >= 1", nameof(pathItems));
        }

        if (pathItems.Count > 1)
        {
            Namespace = pathItems.Take(pathItems.Count - 1).ToList();
        }

        Value = pathItems[^1];
    }

    public List<string> GetFullNameList()
    {
        var result = new List<string>();
        if (Namespace is { Count: > 0 })
        {
            result.AddRange(Namespace);
        }

        result.Add(Value);
        return result;
    }

    public string GetFullNameStr() => string.Join(".", GetFullNameList());

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KnWord other && Value.Equals(other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}
