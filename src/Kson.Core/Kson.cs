using System;
using Kson.Core.Converter;
using Kson.Core.Node;

namespace Kson.Core;

public static class Kson
{
    public static bool IsNull(KsNode? node) => node is null || node is KsNull;

    public static bool IsChainNode(KsNode? node) => node is KsChainNode;

    public static KsNode Parse(string source) => KsonParser.Parse(source);
    public static List<KsNode> ParseItems(string source) => KsonParser.ParseItems(source);

    public static string? GetInnerStringVal(KsNode? node)
    {
        return node switch
        {
            null => null,
            KsWord word => word.Value,
            KsString str => str.Value,
            KsStackOp stackOp => stackOp.Value,
            KsSymbol symbol => symbol.Value,
            _ => null
        };
    }

    public static string? GetAsString(KsMap map, string path)
    {
        var node = map?.Get(path);
        return node is KsString str ? str.Value : null;
    }

    public static KsMap? GetAsKsMap(KsMap map, string path)
    {
        var node = map?.Get(path);
        return node as KsMap;
    }

    public static KsArray? GetAsKsArray(KsMap map, string path)
    {
        var node = map?.Get(path);
        return node as KsArray;
    }

    public static bool IsEmpty(KsNode? node)
    {
        if (node is null || node is KsNull)
        {
            return true;
        }

        if (node is KsArray array)
        {
            return array.Size() == 0;
        }

        return false;
    }
}
