using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kon.Core.Node;

/// <summary>
/// Represents a runtime object instance with fields and methods.
/// This is a simple object type without inheritance or access control.
/// </summary>
public class KnObject : KnNodeBase
{
    private readonly Dictionary<string, KnNode> _fields;
    private readonly Dictionary<string, KnNode> _methods;

    /// <summary>
    /// Creates a new empty KnObject instance.
    /// </summary>
    public KnObject()
    {
        _fields = new Dictionary<string, KnNode>();
        _methods = new Dictionary<string, KnNode>();
    }

    /// <summary>
    /// Creates a new KnObject with initial fields and methods.
    /// </summary>
    /// <param name="fields">Initial fields (can be null)</param>
    /// <param name="methods">Initial methods (can be null)</param>
    public KnObject(Dictionary<string, KnNode>? fields, Dictionary<string, KnNode>? methods)
    {
        _fields = fields != null ? new Dictionary<string, KnNode>(fields) : new Dictionary<string, KnNode>();
        _methods = methods != null ? new Dictionary<string, KnNode>(methods) : new Dictionary<string, KnNode>();
    }

    #region Field Management

    /// <summary>
    /// Sets a field value on the object.
    /// </summary>
    /// <param name="name">Field name</param>
    /// <param name="value">Field value</param>
    public void SetField(string name, KnNode value)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Field name cannot be null or empty", nameof(name));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Field value cannot be null");
        }

        _fields[name] = value;
    }

    /// <summary>
    /// Gets a field value from the object.
    /// </summary>
    /// <param name="name">Field name</param>
    /// <returns>The field value, or null if not found</returns>
    public KnNode? GetField(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return _fields.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if the object has a field with the given name.
    /// </summary>
    /// <param name="name">Field name</param>
    /// <returns>True if the field exists, false otherwise</returns>
    public bool HasField(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _fields.ContainsKey(name);
    }

    /// <summary>
    /// Removes a field from the object.
    /// </summary>
    /// <param name="name">Field name</param>
    /// <returns>True if the field was removed, false if it didn't exist</returns>
    public bool RemoveField(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _fields.Remove(name);
    }

    /// <summary>
    /// Gets all field names.
    /// </summary>
    public IEnumerable<string> GetFieldNames() => _fields.Keys;

    #endregion

    #region Method Management

    /// <summary>
    /// Adds a method to the object.
    /// </summary>
    /// <param name="name">Method name</param>
    /// <param name="methodBody">Method body (should be a Kon function definition)</param>
    public void AddMethod(string name, KnNode methodBody)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(name));
        }

        if (methodBody == null)
        {
            throw new ArgumentNullException(nameof(methodBody), "Method body cannot be null");
        }

        _methods[name] = methodBody;
    }

    /// <summary>
    /// Gets a method from the object.
    /// </summary>
    /// <param name="name">Method name</param>
    /// <returns>The method body, or null if not found</returns>
    public KnNode? GetMethod(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return _methods.TryGetValue(name, out var method) ? method : null;
    }

    /// <summary>
    /// Checks if the object has a method with the given name.
    /// </summary>
    /// <param name="name">Method name</param>
    /// <returns>True if the method exists, false otherwise</returns>
    public bool HasMethod(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _methods.ContainsKey(name);
    }

    /// <summary>
    /// Gets all method names.
    /// </summary>
    public IEnumerable<string> GetMethodNames() => _methods.Keys;

    #endregion

    #region KnNode Overrides

    public override string ToString()
    {
        return $"KnObject(fields: {_fields.Count}, methods: {_methods.Count})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not KnObject other)
        {
            return false;
        }

        // Two objects are equal if they reference the same instance
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        // Use reference-based hash code for object identity
        return RuntimeHelpers.GetHashCode(this);
    }

    #endregion
}
