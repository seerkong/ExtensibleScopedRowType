using System.Collections.Generic;
using Kson.Core.Node;

namespace Kson.Core.Util;

public static class NodeTypeHelper
{
    private static readonly HashSet<KsNodeType> PrimaryTypes = new()
    {
        KsNodeType.Null,
        KsNodeType.Boolean,
        KsNodeType.String,
        KsNodeType.Symbol,
        KsNodeType.StackOp,
        KsNodeType.Int64,
        KsNodeType.Double,
        KsNodeType.Word
    };

    public static KsNodeType GetNodeType(KsNode? node)
    {
        return node switch
        {
            null => KsNodeType.Null,
            KsUndefined => KsNodeType.Undefined,
            KsBoolean => KsNodeType.Boolean,
            KsInt64 => KsNodeType.Int64,
            KsDouble => KsNodeType.Double,
            KsString => KsNodeType.String,
            KsSymbol => KsNodeType.Symbol,
            KsStackOp => KsNodeType.StackOp,
            KsQuoteNode => KsNodeType.Quote,
            KsWord => KsNodeType.Word,
            KsArray => KsNodeType.Array,
            KsMap => KsNodeType.Map,
            KsChainNode => KsNodeType.ChainNode,
            _ => KsNodeType.Undefined
        };
    }

    public static bool IsPrimaryNodeType(KsNode? node) => IsPrimaryNodeType(GetNodeType(node));

    public static bool IsPrimaryNodeType(KsNodeType nodeType) => PrimaryTypes.Contains(nodeType);
}
