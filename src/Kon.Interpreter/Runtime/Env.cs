using System;
using System.Collections.Generic;
using Kon.Core.Node;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Execution environment node used by the interpreter.
/// </summary>
public class Env : ISingleEntryGraphNode<int>
{
    /// <summary>
    /// Categories of environments supported by the interpreter.
    /// </summary>
    public enum EnvType
    {
        BuiltIn,
        Global,
        Process,
        Local
    }

    private static int _nextEnvId = 0;
    private readonly Dictionary<string, KnNode> _variables = new(StringComparer.Ordinal);

    /// <summary>
    /// Unique identifier for this environment node.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Type of the environment (built-in, global, process, local).
    /// </summary>
    public EnvType Type { get; private set; }

    /// <summary>
    /// Optional descriptive name, primarily for diagnostics.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Parent environment in the lexical hierarchy, if any.
    /// </summary>
    public Env? ParentEnv { get; private set; }

    private Env(EnvType type, string? name = null)
    {
        Id = _nextEnvId++;
        Type = type;
        Name = name;
    }

    private static Env CreateRootEnv()
    {
        return new Env(EnvType.BuiltIn, "built_in");
    }

    private static Env CreateChildEnv(EnvType envType, Env parentEnv, string? name = null)
    {
        if (parentEnv == null)
        {
            throw new ArgumentNullException(nameof(parentEnv));
        }

        var env = new Env(envType, name);
        env.ParentEnv = parentEnv;
        return env;
    }

    /// <summary>
    /// Creates the built-in root environment.
    /// </summary>
    public static Env CreateBuiltInEnv()
    {
        return CreateRootEnv();
    }

    /// <summary>
    /// Creates the global environment as a child of the specified parent.
    /// </summary>
    public static Env CreateGlobalEnv(Env parentEnv, string? name = null)
    {
        return CreateChildEnv(EnvType.Global, parentEnv, name ?? "global");
    }

    /// <summary>
    /// Creates a process scope environment beneath the specified parent.
    /// </summary>
    public static Env CreateProcessEnv(Env parentEnv, string? name = null)
    {
        return CreateChildEnv(EnvType.Process, parentEnv, name ?? "process");
    }

    /// <summary>
    /// Creates a local scope environment beneath the specified parent.
    /// </summary>
    public static Env CreateLocalEnv(Env parentEnv, string? name = null)
    {
        return CreateChildEnv(EnvType.Local, parentEnv, name ?? "local");
    }

    /// <summary>
    /// Defines or updates the value of a variable in this environment.
    /// </summary>
    public void Define(string key, KnNode value)
    {
        _variables[key] = value;
    }

    /// <summary>
    /// Checks if a variable has been declared in this environment.
    /// </summary>
    public bool ContainsVar(string key)
    {
        return _variables.ContainsKey(key);
    }

    /// <summary>
    /// Retrieves a variable value declared in this environment.
    /// </summary>
    public KnNode? Lookup(string key)
    {
        return _variables.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Returns the identifier used by the graph infrastructure.
    /// </summary>
    public int GetVertexId() => Id;

    internal void ChangeType(EnvType type)
    {
        Type = type;
    }

    internal void Rename(string? name)
    {
        Name = name;
    }
}
