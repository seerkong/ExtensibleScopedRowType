using Kon.Core.Node;
using Kon.Interpreter.Models;
using Kon.Interpreter.Runtime;
using System;
using System.Linq;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Builtin methods for KnMap type.
/// All methods receive 'self' as the first parameter.
/// </summary>
public static class MapBuiltinMethods
{
    /// <summary>
    /// Registers all builtin methods for KnMap.
    /// </summary>
    public static void Register(BuiltinMethodRegistry registry)
    {
        registry.RegisterMethod("KnMap", "Count", new KnHostFunction(new HostFunction("Count", Count)));
        registry.RegisterMethod("KnMap", "Get", new KnHostFunction(new HostFunction("Get", Get)));
        registry.RegisterMethod("KnMap", "ContainsKey", new KnHostFunction(new HostFunction("ContainsKey", ContainsKey)));
        registry.RegisterMethod("KnMap", "Keys", new KnHostFunction(new HostFunction("Keys", Keys)));
        registry.RegisterMethod("KnMap", "Values", new KnHostFunction(new HostFunction("Values", Values)));
        registry.RegisterMethod("KnMap", "IsEmpty", new KnHostFunction(new HostFunction("IsEmpty", IsEmpty)));
        registry.RegisterMethod("KnMap", "Remove", new KnHostFunction(new HostFunction("Remove", Remove)));
        registry.RegisterMethod("KnMap", "Clear", new KnHostFunction(new HostFunction("Clear", Clear)));
    }

    /// <summary>
    /// Returns the number of key-value pairs in the map.
    /// Usage: (map~Count)
    /// </summary>
    private static KnNode Count(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Count expects a map as the first argument (self)");
        }

        return new KnInt64(map.Count);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// Usage: (map~Get "key")
    /// </summary>
    private static KnNode Get(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Get expects a map as the first argument (self) and a key");
        }

        if (args[1] is not KnString key)
        {
            throw new ArgumentException("Get expects a string key as the second argument");
        }

        var value = map.Get(key.Value);
        return value ?? KnNull.Null;
    }

    /// <summary>
    /// Checks if the map contains the specified key.
    /// Usage: (map~ContainsKey "key")
    /// </summary>
    private static KnNode ContainsKey(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnMap map)
        {
            throw new ArgumentException("ContainsKey expects a map as the first argument (self) and a key");
        }

        if (args[1] is not KnString key)
        {
            throw new ArgumentException("ContainsKey expects a string key as the second argument");
        }

        return new KnBoolean(map.ContainsKey(key.Value));
    }

    /// <summary>
    /// Returns an array of all keys in the map.
    /// Usage: (map~Keys)
    /// </summary>
    private static KnNode Keys(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Keys expects a map as the first argument (self)");
        }

        var keys = map.Keys.Select(k => new KnString(k) as KnNode).ToList();
        return new KnArray(keys);
    }

    /// <summary>
    /// Returns an array of all values in the map.
    /// Usage: (map~Values)
    /// </summary>
    private static KnNode Values(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Values expects a map as the first argument (self)");
        }

        var values = map.Values.ToList();
        return new KnArray(values);
    }

    /// <summary>
    /// Checks if the map is empty.
    /// Usage: (map~IsEmpty)
    /// </summary>
    private static KnNode IsEmpty(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnMap map)
        {
            throw new ArgumentException("IsEmpty expects a map as the first argument (self)");
        }

        return new KnBoolean(map.Count == 0);
    }

    /// <summary>
    /// Removes the specified key from the map.
    /// Returns true if the key was found and removed, false otherwise.
    /// Usage: (map~Remove "key")
    /// </summary>
    private static KnNode Remove(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Remove expects a map as the first argument (self) and a key");
        }

        if (args[1] is not KnString key)
        {
            throw new ArgumentException("Remove expects a string key as the second argument");
        }

        var removed = map.Remove(key.Value);
        return new KnBoolean(removed);
    }

    /// <summary>
    /// Removes all key-value pairs from the map.
    /// Usage: (map~Clear)
    /// </summary>
    private static KnNode Clear(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnMap map)
        {
            throw new ArgumentException("Clear expects a map as the first argument (self)");
        }

        map.Clear();
        return map;
    }
}
