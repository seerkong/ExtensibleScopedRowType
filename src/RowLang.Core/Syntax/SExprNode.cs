using System.Collections.Generic;
using System.Collections.Immutable;

namespace RowLang.Core.Syntax;

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

    public override string ToString() => QualifiedName;
}

public sealed record SExprList(ImmutableArray<SExprNode> Elements) : SExprNode
{
    public override string ToString() => $"({string.Join(' ', Elements)})";
}
