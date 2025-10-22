using System.Collections.Generic;
using System.Collections.Immutable;

namespace RowTypeSystem.Core.Syntax;

public abstract record SExprNode
{
    public IReadOnlyList<SExprNode> PrefixAnnotations { get; init; } = Array.Empty<SExprNode>();

    public IReadOnlyList<SExprNode> PostfixAnnotations { get; init; } = Array.Empty<SExprNode>();
}

public sealed record SExprIdentifier(ImmutableArray<string> Parts) : SExprNode
{
    public string QualifiedName => string.Join("::", Parts);

    public string Name => Parts.IsDefaultOrEmpty ? string.Empty : Parts[^1];

    public ImmutableArray<string> Namespace => Parts.Length <= 1 ? ImmutableArray<string>.Empty : Parts[..^1];

    public SExprNode? TypeAnnotation { get; init; }

    public override string ToString()
    {
        if (TypeAnnotation is null)
        {
            return QualifiedName;
        }

        return $"{QualifiedName} ~ {TypeAnnotation}";
    }
}

public sealed record SExprString(string Value) : SExprNode
{
    public override string ToString() => $"\"{Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}

public sealed record SExprList(ImmutableArray<SExprNode> Elements) : SExprNode
{
    public override string ToString() => $"({string.Join(' ', Elements)})";
}

public sealed record SExprArray(ImmutableArray<SExprNode> Elements) : SExprNode
{
    public override string ToString() => $"[{string.Join(' ', Elements)}]";
}

public sealed record SExprObjectProperty(SExprNode Key, SExprNode Value)
{
    public override string ToString() => $"{Key}: {Value}";
}

public sealed record SExprObject(ImmutableArray<SExprObjectProperty> Properties) : SExprNode
{
    public override string ToString() => $"{{{string.Join(' ', Properties)}}}";
}
