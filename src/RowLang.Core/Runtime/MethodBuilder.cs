using System.Collections.Generic;
using RowLang.Core.Types;

namespace RowLang.Core.Runtime;

public static class MethodBuilder
{
    public static MethodBody FromLambda(
        string owner,
        string name,
        FunctionTypeSymbol signature,
        Func<InvocationContext, IReadOnlyList<Value>, Value> body,
        RowQualifier qualifier = RowQualifier.Default)
    {
        var member = RowMemberBuilder.Method(owner, name, signature, qualifier);
        return new MethodBody(member, body);
    }
}
