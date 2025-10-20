namespace RowLang.Core.Types;

public static class RowMemberBuilder
{
    public static RowMember Method(
        string owner,
        string name,
        FunctionTypeSymbol signature,
        RowQualifier qualifier = RowQualifier.Default,
        AccessModifier access = AccessModifier.Public)
        => new(name, signature, qualifier, owner, IsMethod: true)
        {
            Access = access,
        };

    public static RowMember Field(
        string owner,
        string name,
        TypeSymbol type,
        RowQualifier qualifier = RowQualifier.Default,
        AccessModifier access = AccessModifier.Public)
        => new(name, type, qualifier, owner, IsMethod: false)
        {
            Access = access,
        };
}
