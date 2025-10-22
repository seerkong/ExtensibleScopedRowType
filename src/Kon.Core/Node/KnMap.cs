using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kon.Core.Converter;

namespace Kon.Core.Node;

public class KnMap : KnContainerNode, IDictionary<string, KnNode>
{
    public record MapComponent(string? Key, KnNode Value, bool IsSpread);

    public static readonly KnMap Empty = new();

    private readonly List<MapComponent> _components;
    private readonly Dictionary<string, KnNode> _entries;

    public KnMap()
    {
        _components = new List<MapComponent>();
        _entries = new Dictionary<string, KnNode>();
    }

    public KnMap(IDictionary<string, KnNode> value)
        : this()
    {
        foreach (var pair in value)
        {
            Add(pair.Key, pair.Value);
        }
    }

    public KnMap(IEnumerable<KeyValuePair<string, KnNode>> members)
        : this()
    {
        foreach (var pair in members)
        {
            Add(pair.Key, pair.Value);
        }
    }

    public IReadOnlyList<MapComponent> Components => _components;

    public IDictionary<string, KnNode> GetValue() => _entries;

    public KnNode? Get(string key) => _entries.TryGetValue(key, out var value) ? value : null;

    public override string ToString() => KonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KnMap other)
        {
            return false;
        }

        if (_components.Count != other._components.Count)
        {
            return false;
        }

        for (var i = 0; i < _components.Count; i++)
        {
            var left = _components[i];
            var right = other._components[i];

            if (left.IsSpread != right.IsSpread)
            {
                return false;
            }

            if (left.IsSpread)
            {
                if (!Equals(left.Value, right.Value))
                {
                    return false;
                }
            }
            else
            {
                if (!string.Equals(left.Key, right.Key, StringComparison.Ordinal) ||
                    !Equals(left.Value, right.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in _components)
        {
            hash.Add(component.IsSpread);
            hash.Add(component.Key);
            hash.Add(component.Value);
        }

        return hash.ToHashCode();
    }


    public ICollection<string> KeySet() => _entries.Keys;

    public int Size() => _entries.Count;

    public bool IsEmpty() => _components.Count == 0;

    public void AddSpread(KnNode spreadNode)
    {
        if (spreadNode == null)
        {
            throw new ArgumentNullException(nameof(spreadNode));
        }

        _components.Add(new MapComponent(null, spreadNode, true));
    }

    public IEnumerator<KeyValuePair<string, KnNode>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<string, KnNode> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _components.Clear();
        _entries.Clear();
    }

    public bool Contains(KeyValuePair<string, KnNode> item) =>
        _entries.TryGetValue(item.Key, out var value) && Equals(value, item.Value);

    public void CopyTo(KeyValuePair<string, KnNode>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, KnNode>>)_entries).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<string, KnNode> item) =>
        _entries.TryGetValue(item.Key, out var value) &&
        Equals(value, item.Value) &&
        Remove(item.Key);

    public int Count => _entries.Count;

    public bool IsReadOnly => false;

    public void Add(string key, KnNode value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _entries.Add(key, value);
        _components.Add(new MapComponent(key, value, false));
    }

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public bool Remove(string key)
    {
        if (!_entries.Remove(key))
        {
            return false;
        }

        var index = _components.FindIndex(component => !component.IsSpread && component.Key == key);
        if (index >= 0)
        {
            _components.RemoveAt(index);
        }

        return true;
    }

    public bool TryGetValue(string key, out KnNode value) => _entries.TryGetValue(key, out value!);

    public KnNode this[string key]
    {
        get => _entries[key];
        set
        {
            if (_entries.ContainsKey(key))
            {
                _entries[key] = value;
                var index = _components.FindIndex(component => !component.IsSpread && component.Key == key);
                if (index >= 0)
                {
                    _components[index] = new MapComponent(key, value, false);
                }
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public ICollection<string> Keys => _entries.Keys;

    public ICollection<KnNode> Values => _entries.Values;
}
