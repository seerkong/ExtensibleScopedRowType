using System;
using Kon.Core.Converter;
using Kon.Core.Node;

namespace Kon.Core;

public static class Kon
{
    public static bool IsNull(KnNode? node) => node is null || node is KnNull;

    public static bool IsChainNode(KnNode? node) => node is KnChainNode;

    public static KnNode Parse(string source) => KonParser.Parse(source);
    public static List<KnNode> ParseItems(string source) => KonParser.ParseItems(source);

    public static string? GetInnerStringVal(KnNode? node)
    {
        return node switch
        {
            null => null,
            KnWord word => word.Value,
            KnString str => str.Value,
            KnStackOp stackOp => stackOp.Value,
            KnSymbol symbol => symbol.Value,
            _ => null
        };
    }

    public static string? GetAsString(KnMap map, string path)
    {
        var node = map?.Get(path);
        return node is KnString str ? str.Value : null;
    }

    public static KnMap? GetAsKnMap(KnMap map, string path)
    {
        var node = map?.Get(path);
        return node as KnMap;
    }

    public static KnArray? GetAsKnArray(KnMap map, string path)
    {
        var node = map?.Get(path);
        return node as KnArray;
    }

    public static bool IsEmpty(KnNode? node)
    {
        if (node is null || node is KnNull)
        {
            return true;
        }

        if (node is KnArray array)
        {
            return array.Size() == 0;
        }

        return false;
    }
}
