using System.Collections.Generic;
using System.Linq;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsBlock : KsContainerNode
{
    private List<KsNode> _items;

    public KsBlock()
    {
        _items = new List<KsNode>();
    }

    public KsBlock(IEnumerable<KsNode> value)
    {
        _items = value?.ToList() ?? new List<KsNode>();
    }

    public KsBlock(params KsNode[] children)
        : this((IEnumerable<KsNode>)children)
    {
    }

    public List<KsNode> GetItems() => _items;

    public int Size() => _items.Count;

    public KsNode Get(int index) => _items[index];

    public void Push(KsNode value) => _items.Add(value);

    public KsNode Pop()
    {
        var idx = _items.Count - 1;
        var value = _items[idx];
        _items.RemoveAt(idx);
        return value;
    }

    public void Unshift(KsNode value) => _items.Insert(0, value);

    public KsNode Shift()
    {
        var value = _items[0];
        _items.RemoveAt(0);
        return value;
    }

    public KsNode Top() => _items[^1];

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KsBlock other || _items.Count != other._items.Count)
        {
            return false;
        }

        for (var i = 0; i < _items.Count; i++)
        {
            if (!Equals(_items[i], other._items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() => _items?.GetHashCode() ?? 0;
}
