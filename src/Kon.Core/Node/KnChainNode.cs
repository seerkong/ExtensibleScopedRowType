using System;
using System.Collections.Generic;
using Kon.Core.Converter;
using Kon.Core.Node.Inner;
using Kon.Core.Util;

namespace Kon.Core.Node;

public class KnChainNode : KnContainerNode, SupportPrefixesPostfixes
{
    public static readonly KnChainNode Nil = new();

    public KnArray? AnnotationPrefixes { get; set; }
    public KnMap? WithEffectPrefix { get; set; }
    public KnArray? TypePrefixes { get; set; }
    public KnArray? Postfixes { get; set; }
    public KnArray? UnboundTypes { get; set; }
    public KnCallType? CallType { get; set; }
    public KnNode? Core { get; set; }
    public KnWord? Name { get; set; }
    public KnInOutTable? InOutTable { get; set; }
    public KnArray? GenericParams { get; set; }
    public Dictionary<string, KnNode>? Attr { get; set; }
    public KnMap? Conf { get; set; }
    public Dictionary<string, KnMap>? NamedConf { get; set; }
    public KnArray? Body { get; set; }
    public Dictionary<string, KnArray>? Sections { get; set; }
    public Dictionary<string, KnChainNode>? Slots { get; set; }
    public KnChainNode? Next { get; set; }

    public KnChainNode()
    {
    }

    public KnChainNode(KnNode? core)
        : this(core, null)
    {
    }

    public KnChainNode(KnNode? core, KnChainNode? next)
        : this(core, next, null, null, null, null)
    {
    }

    public KnChainNode(
        KnNode? core,
        KnChainNode? next,
        Dictionary<string, KnNode>? attr,
        KnMap? conf,
        KnInOutTable? inoutType,
        KnArray? body)
    {
        Core = core;
        Next = next;
        Attr = attr;
        Conf = conf;
        InOutTable = inoutType;
        Body = body;
    }

    public static KnChainNode MakeByArray(params KnNode[] children) => MakeByList(new List<KnNode>(children));

    public static KnChainNode MakeByList(IList<KnNode> chainNodes)
    {
        KnChainNode? result = null;
        for (var i = chainNodes.Count - 1; i >= 0; i--)
        {
            var node = new KnChainNode(chainNodes[i], result);
            result = node;
        }

        return result ?? new KnChainNode();
    }

    public static KnChainNode ConnectChain(IList<KnChainNode> nodes)
    {
        KnChainNode? result = null;
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            var node = nodes[i];
            node.Next = result;
            result = node;
        }

        return result ?? new KnChainNode();
    }

    public bool HasNext() => Next is not null;

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KnChainNode other)
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

        if (!Equals(InOutTable, other.InOutTable))
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

    public KnNode? GetNextCore() => Next?.Core;

    public KnChainNode? GetNextNext() => Next?.Next;

    public KnNode? GetNextNextCore() => GetNextNext()?.Core;

    public KnChainNode? GetNextNextNext() => GetNextNext()?.Next;

    public bool IsCoreOrParamOnlyNode() =>
        Attr is null && Conf is null && Body is null && Sections is null;

    public bool IsCoreContainerType() => Core is KnContainerNode;

    public bool AcceptCore() => Core is null && AcceptAttr();

    public bool AcceptAttr() => Attr is null && AcceptParam();

    public bool AcceptParam() => InOutTable is null && AcceptConf();

    public bool AcceptConf() => Conf is null && AcceptBody();

    public bool AcceptBody() => Body is null;

    public KnArray? GetSectionWithTag(string tagToFind)
    {
        if (Sections is null)
        {
            return null;
        }

        return Sections.TryGetValue(tagToFind, out var value) ? value : null;
    }

    public KnChainNode? GetChainNodeHasCore(string coreToFind)
    {
        var iter = this;
        while (iter is not null)
        {
            if (iter.Core is not null &&
                string.Equals(Kon.GetInnerStringVal(iter.Core), coreToFind, StringComparison.Ordinal))
            {
                return iter;
            }

            iter = iter.Next!;
        }

        return null;
    }

    public List<KnChainNode> GetChainNodesExcludeCore(ISet<string> excludeCoreNames)
    {
        var result = new List<KnChainNode>();
        var iter = this;
        while (iter is not null)
        {
            if (iter.Core is not null && !excludeCoreNames.Contains(Kon.GetInnerStringVal(iter.Core) ?? string.Empty))
            {
                result.Add(iter);
            }

            iter = iter.Next!;
        }

        return result;
    }

    public List<KnNode> GetCoreList()
    {
        var result = new List<KnNode>();
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

    public KnNode? GetAttrValue(string name)
    {
        if (Attr is null)
        {
            return null;
        }

        return Attr.TryGetValue(name, out var value) ? value : null;
    }
}
