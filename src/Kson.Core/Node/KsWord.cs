using System;
using System.Collections.Generic;
using System.Linq;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsWord : KsValueNode, SupportPrefixesPostfixes
{
    public KsArray? AnnotationPrefixes { get; set; }
    public KsMap? WithEffectPrefix { get; set; }
    public KsArray? TypePrefixes { get; set; }
    public KsArray? UnboundTypes { get; set; }
    public KsArray? Postfixes { get; set; }
    public List<string> Namespace { get; private set; } = new();
    public string Value { get; }

    public KsWord(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public KsWord(IReadOnlyList<string> pathItems)
    {
        if (pathItems == null || pathItems.Count == 0)
        {
            throw new ArgumentException("KsWord pathItems size should >= 1", nameof(pathItems));
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

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj) => obj is KsWord other && Value.Equals(other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}
