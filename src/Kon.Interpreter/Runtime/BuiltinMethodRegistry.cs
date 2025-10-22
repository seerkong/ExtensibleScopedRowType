using Kon.Core.Node;
using System;
using System.Collections.Generic;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Registry for builtin methods on KnNode types.
/// Allows registering C# native method implementations for builtin types like KnArray, KnMap, etc.
/// </summary>
public class BuiltinMethodRegistry
{
    // typeName -> (methodName -> method implementation)
    private readonly Dictionary<string, Dictionary<string, KnNode>> _methodsByType = new();

    /// <summary>
    /// Registers a builtin method for a specific type.
    /// </summary>
    /// <param name="typeName">The KnNode type name (e.g., "KnArray", "KnMap")</param>
    /// <param name="methodName">The method name</param>
    /// <param name="implementation">The method implementation (usually a KnHostFunction)</param>
    public void RegisterMethod(string typeName, string methodName, KnNode implementation)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
        }

        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        if (implementation == null)
        {
            throw new ArgumentNullException(nameof(implementation));
        }

        if (!_methodsByType.ContainsKey(typeName))
        {
            _methodsByType[typeName] = new Dictionary<string, KnNode>();
        }

        _methodsByType[typeName][methodName] = implementation;
    }

    /// <summary>
    /// Gets a builtin method for a specific target object.
    /// </summary>
    /// <param name="target">The target object</param>
    /// <param name="methodName">The method name</param>
    /// <returns>The method implementation, or null if not found</returns>
    public KnNode? GetMethod(KnNode target, string methodName)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        var typeName = target.GetType().Name;

        if (_methodsByType.TryGetValue(typeName, out var methods))
        {
            return methods.TryGetValue(methodName, out var method) ? method : null;
        }

        return null;
    }

    /// <summary>
    /// Checks if a builtin method exists for a specific target object.
    /// </summary>
    /// <param name="target">The target object</param>
    /// <param name="methodName">The method name</param>
    /// <returns>True if the method exists, false otherwise</returns>
    public bool HasMethod(KnNode target, string methodName)
    {
        return GetMethod(target, methodName) != null;
    }

    /// <summary>
    /// Gets all method names registered for a specific type.
    /// </summary>
    /// <param name="typeName">The type name</param>
    /// <returns>Collection of method names, or empty collection if type not found</returns>
    public IEnumerable<string> GetMethodNames(string typeName)
    {
        if (_methodsByType.TryGetValue(typeName, out var methods))
        {
            return methods.Keys;
        }

        return Array.Empty<string>();
    }
}
