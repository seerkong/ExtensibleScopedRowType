namespace RowLang.Core.Types;

public sealed record RowMember(
    string Name,
    TypeSymbol Type,
    RowQualifier Qualifier,
    string Origin,
    bool IsMethod)
{
    public bool IsVirtual => Qualifier == RowQualifier.Virtual;

    public bool IsFinal => Qualifier == RowQualifier.Final;

    public bool IsOverride => Qualifier == RowQualifier.Override;

    public bool IsInherit => Qualifier == RowQualifier.Inherit;

    public bool ShouldForward => Qualifier is RowQualifier.Default or RowQualifier.Inherit or RowQualifier.Override;

    public AccessModifier Access { get; init; } = AccessModifier.Public;
}
