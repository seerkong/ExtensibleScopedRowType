namespace RowLang.Core.Types;

public static class RowMemberBuilder
{
    public static RowMember Method(
        string owner,
        string name,
        FunctionTypeSymbol signature,
        RowQualifier qualifier = RowQualifier.Default)
        => new(name, signature, qualifier, owner, IsMethod: true);

    public static RowMember Field(
        string owner,
        string name,
        TypeSymbol type,
        RowQualifier qualifier = RowQualifier.Default)
        => new(name, type, qualifier, owner, IsMethod: false);
}
