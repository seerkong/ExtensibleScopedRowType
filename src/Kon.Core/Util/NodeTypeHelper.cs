using System.Collections.Generic;
using Kon.Core.Node;
using Kon.Core.Node.Inner;

namespace Kon.Core.Util;

public static class NodeTypeHelper
{
    private static readonly HashSet<KnNodeType> PrimaryTypes = new()
    {
        KnNodeType.Null,
        KnNodeType.Boolean,
        KnNodeType.String,
        KnNodeType.Symbol,
        KnNodeType.StackOp,
        KnNodeType.Int64,
        KnNodeType.Double,
        KnNodeType.Word
    };

    public static KnNodeType GetNodeType(KnNode? node)
    {
        return node switch
        {
            null => KnNodeType.Null,
            KnUndefined => KnNodeType.Undefined,
            KnBoolean => KnNodeType.Boolean,
            KnInt64 => KnNodeType.Int64,
            KnDouble => KnNodeType.Double,
            KnString => KnNodeType.String,
            KnSymbol => KnNodeType.Symbol,
            KnStackOp => KnNodeType.StackOp,
            KnQuoteNode => KnNodeType.Quote,
            KnWord => KnNodeType.Word,
            KnArray => KnNodeType.Array,
            KnMap => KnNodeType.Map,
            KnChainNode => KnNodeType.ChainNode,
            _ => KnNodeType.Undefined
        };
    }

    public static bool IsPrimaryNodeType(KnNode? node) => IsPrimaryNodeType(GetNodeType(node));

    public static bool IsPrimaryNodeType(KnNodeType nodeType) => PrimaryTypes.Contains(nodeType);
}
