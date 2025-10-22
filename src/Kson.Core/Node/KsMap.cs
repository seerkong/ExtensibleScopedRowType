using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kson.Core.Converter;

namespace Kson.Core.Node;

public class KsMap : KsContainerNode, IDictionary<string, KsNode>
{
    public record MapComponent(string? Key, KsNode Value, bool IsSpread);

    public static readonly KsMap Empty = new();

    private readonly List<MapComponent> _components;
    private readonly Dictionary<string, KsNode> _entries;

    public KsMap()
    {
        _components = new List<MapComponent>();
        _entries = new Dictionary<string, KsNode>();
    }

    public KsMap(IDictionary<string, KsNode> value)
        : this()
    {
        foreach (var pair in value)
        {
            Add(pair.Key, pair.Value);
        }
    }

    public KsMap(IEnumerable<KeyValuePair<string, KsNode>> members)
        : this()
    {
        foreach (var pair in members)
        {
            Add(pair.Key, pair.Value);
        }
    }

    public IReadOnlyList<MapComponent> Components => _components;

    public IDictionary<string, KsNode> GetValue() => _entries;

    public KsNode? Get(string key) => _entries.TryGetValue(key, out var value) ? value : null;

    public override string ToString() => KsonFormater.SingleLine(this);

    public override bool Equals(object? obj)
    {
        if (obj is not KsMap other)
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

    public void AddSpread(KsNode spreadNode)
    {
        if (spreadNode == null)
        {
            throw new ArgumentNullException(nameof(spreadNode));
        }

        _components.Add(new MapComponent(null, spreadNode, true));
    }

    public IEnumerator<KeyValuePair<string, KsNode>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<string, KsNode> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _components.Clear();
        _entries.Clear();
    }

    public bool Contains(KeyValuePair<string, KsNode> item) =>
        _entries.TryGetValue(item.Key, out var value) && Equals(value, item.Value);

    public void CopyTo(KeyValuePair<string, KsNode>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, KsNode>>)_entries).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<string, KsNode> item) =>
        _entries.TryGetValue(item.Key, out var value) &&
        Equals(value, item.Value) &&
        Remove(item.Key);

    public int Count => _entries.Count;

    public bool IsReadOnly => false;

    public void Add(string key, KsNode value)
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

    public bool TryGetValue(string key, out KsNode value) => _entries.TryGetValue(key, out value!);

    public KsNode this[string key]
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

    public ICollection<KsNode> Values => _entries.Values;
}
