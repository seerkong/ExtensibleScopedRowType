using System;
using System.Collections.Generic;
using System.Linq;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsArray : KsContainerNode
{
    protected List<KsNode> _items;

    public KsArray()
    {
        _items = new List<KsNode>();
    }

    public KsArray(IEnumerable<KsNode> value)
    {
        _items = value?.ToList() ?? new List<KsNode>();
    }

    public KsArray(params KsNode[] children)
        : this((IEnumerable<KsNode>)children)
    {
    }

    public List<KsNode> Items
    {
        get => _items;
        set => _items = value ?? new List<KsNode>();
    }

    public List<KsNode> GetItems() => _items;

    public void SetItems(List<KsNode> items) => Items = items;

    public KsNode Get(int index) => _items[index];

    public KsNode this[int index] => _items[index];

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KsArray other || _items.Count != other._items.Count)
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

    public int Size() => _items.Count;

    public void Unshift(KsNode value)
    {
        _items.Insert(0, value);
    }

    public KsNode Shift()
    {
        var value = _items[0];
        _items.RemoveAt(0);
        return value;
    }

    public void Push(KsNode value)
    {
        _items.Add(value);
    }

    public KsNode Pop()
    {
        var idx = _items.Count - 1;
        var value = _items[idx];
        _items.RemoveAt(idx);
        return value;
    }

    public KsNode Top() => _items[^1];
}
