using System.Collections.Generic;
using RowTypeSystem.Core.Types;

namespace RowTypeSystem.Core.Runtime;

public static class MethodBuilder
{
    public static MethodBody FromLambda(
        string owner,
        string name,
        FunctionTypeSymbol signature,
        Func<InvocationContext, IReadOnlyList<Value>, Value> body,
        RowQualifier qualifier = RowQualifier.Default,
        AccessModifier access = AccessModifier.Public)
    {
        var member = RowMemberBuilder.Method(owner, name, signature, qualifier, access);
        return new MethodBody(member, body);
    }
}
