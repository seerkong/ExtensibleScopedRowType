using System;
using System.Collections.Generic;
using Kson.Core.Converter;
using Kson.Core.Util;

namespace Kson.Core.Node;

public class KsChainNode : KsContainerNode, SupportPrefixesPostfixes
{
    public static readonly KsChainNode Nil = new();

    public KsArray? AnnotationPrefixes { get; set; }
    public KsMap? WithEffectPrefix { get; set; }
    public KsArray? TypePrefixes { get; set; }
    public KsArray? Postfixes { get; set; }
    public KsArray? UnboundTypes { get; set; }
    public KsCallType? CallType { get; set; }
    public KsNode? Core { get; set; }
    public KsWord? Name { get; set; }
    public KsArray? CallParams { get; set; }
    public KsFuncInOutData? CallResults { get; set; }
    public Dictionary<string, KsNode>? Attr { get; set; }
    public KsMap? Conf { get; set; }
    public Dictionary<string, KsMap>? NamedConf { get; set; }
    public KsArray? Body { get; set; }
    public Dictionary<string, KsArray>? Sections { get; set; }
    public Dictionary<string, KsChainNode>? Slots { get; set; }
    public KsChainNode? Next { get; set; }

    public KsChainNode()
    {
    }

    public KsChainNode(KsNode? core)
        : this(core, null)
    {
    }

    public KsChainNode(KsNode? core, KsChainNode? next)
        : this(core, next, null, null, null, null)
    {
    }

    public KsChainNode(
        KsNode? core,
        KsChainNode? next,
        Dictionary<string, KsNode>? attr,
        KsMap? conf,
        KsArray? inoutType,
        KsArray? body)
    {
        Core = core;
        Next = next;
        Attr = attr;
        Conf = conf;
        CallParams = inoutType;
        Body = body;
    }

    public static KsChainNode MakeByArray(params KsNode[] children) => MakeByList(new List<KsNode>(children));

    public static KsChainNode MakeByList(IList<KsNode> chainNodes)
    {
        KsChainNode? result = null;
        for (var i = chainNodes.Count - 1; i >= 0; i--)
        {
            var node = new KsChainNode(chainNodes[i], result);
            result = node;
        }

        return result ?? new KsChainNode();
    }

    public static KsChainNode ConnectChain(IList<KsChainNode> nodes)
    {
        KsChainNode? result = null;
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            var node = nodes[i];
            node.Next = result;
            result = node;
        }

        return result ?? new KsChainNode();
    }

    public bool HasNext() => Next is not null;

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KsChainNode other)
        {
            return false;
        }

        if (!Equals(Core, other.Core))
        {
            return false;
        }

        if (!Equals(Conf, other.Conf))
        {
            return false;
        }

        if (!Equals(CallParams, other.CallParams))
        {
            return false;
        }

        if (!Equals(Body, other.Body))
        {
            return false;
        }

        if (Next is null && other.Next is null)
        {
            return true;
        }

        if (Next is not null && other.Next is not null)
        {
            return Next.Equals(other.Next);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = 0;
        if (Core is not null)
        {
            hash += 7 * Core.GetHashCode();
        }

        return hash;
    }

    public KsNode? GetNextCore() => Next?.Core;

    public KsChainNode? GetNextNext() => Next?.Next;

    public KsNode? GetNextNextCore() => GetNextNext()?.Core;

    public KsChainNode? GetNextNextNext() => GetNextNext()?.Next;

    public bool IsCoreOrParamOnlyNode() =>
        Attr is null && Conf is null && Body is null && Sections is null;

    public bool IsCoreContainerType() => Core is KsContainerNode;

    public bool AcceptCore() => Core is null && AcceptAttr();

    public bool AcceptAttr() => Attr is null && AcceptParam();

    public bool AcceptParam() => CallParams is null && AcceptConf();

    public bool AcceptConf() => Conf is null && AcceptBody();

    public bool AcceptBody() => Body is null;

    public KsArray? GetSectionWithTag(string tagToFind)
    {
        if (Sections is null)
        {
            return null;
        }

        return Sections.TryGetValue(tagToFind, out var value) ? value : null;
    }

    public KsChainNode? GetChainNodeHasCore(string coreToFind)
    {
        var iter = this;
        while (iter is not null)
        {
            if (iter.Core is not null &&
                string.Equals(Kson.GetInnerStringVal(iter.Core), coreToFind, StringComparison.Ordinal))
            {
                return iter;
            }

            iter = iter.Next!;
        }

        return null;
    }

    public List<KsChainNode> GetChainNodesExcludeCore(ISet<string> excludeCoreNames)
    {
        var result = new List<KsChainNode>();
        var iter = this;
        while (iter is not null)
        {
            if (iter.Core is not null && !excludeCoreNames.Contains(Kson.GetInnerStringVal(iter.Core) ?? string.Empty))
            {
                result.Add(iter);
            }

            iter = iter.Next!;
        }

        return result;
    }

    public List<KsNode> GetCoreList()
    {
        var result = new List<KsNode>();
        var iter = this;
        while (iter is not null)
        {
            if (iter.Core is not null)
            {
                result.Add(iter.Core);
            }

            iter = iter.Next!;
        }

        return result;
    }

    public bool HasAttrKey(string name) => Attr is not null && Attr.ContainsKey(name);

    public KsNode? GetAttrValue(string name)
    {
        if (Attr is null)
        {
            return null;
        }

        return Attr.TryGetValue(name, out var value) ? value : null;
    }
}
