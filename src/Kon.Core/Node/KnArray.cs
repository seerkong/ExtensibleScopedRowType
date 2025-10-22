using System;
using System.Collections.Generic;
using System.Linq;
using Kon.Core.Converter;

namespace Kon.Core.Node;

public class KnArray : KnContainerNode
{
    protected List<KnNode> _items;

    public KnArray()
    {
        _items = new List<KnNode>();
    }

    public KnArray(IEnumerable<KnNode> value)
    {
        _items = value?.ToList() ?? new List<KnNode>();
    }

    public KnArray(params KnNode[] children)
        : this((IEnumerable<KnNode>)children)
    {
    }

    public List<KnNode> Items
    {
        get => _items;
        set => _items = value ?? new List<KnNode>();
    }

    public List<KnNode> GetItems() => _items;

    public void SetItems(List<KnNode> items) => Items = items;

    public KnNode Get(int index) => _items[index];

    public KnNode this[int index] => _items[index];

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KnArray other || _items.Count != other._items.Count)
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

    public void Unshift(KnNode value)
    {
        _items.Insert(0, value);
    }

    public KnNode Shift()
    {
        var value = _items[0];
        _items.RemoveAt(0);
        return value;
    }

    public void Push(KnNode value)
    {
        _items.Add(value);
    }

    public KnNode Pop()
    {
        var idx = _items.Count - 1;
        var value = _items[idx];
        _items.RemoveAt(idx);
        return value;
    }

    public KnNode Top() => _items[^1];
}
