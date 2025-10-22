using System;
using Kson.Core.Node;

namespace Kson.Interpreter.Models;

public class KsHostFunction : KsValueNode
{
    public object HostFunction { get; }

    public KsHostFunction(object hostFunction)
    {
        HostFunction = hostFunction ?? throw new ArgumentNullException(nameof(hostFunction));
    }

    public override string ToString() => $"<host-function>";

    public override bool Equals(object? obj) => obj is KsHostFunction other && HostFunction.Equals(other.HostFunction);

    public override int GetHashCode() => HostFunction.GetHashCode();

    public override bool ToBoolean() => true;
}



/// <summary>
/// Represents a function delegate that can be called from the interpreter
/// </summary>
/// <param name="args">The arguments to the function</param>
/// <returns>The result of the function call</returns>
public delegate KsNode HostFunctionDelegate(params KsNode[] args);

/// <summary>
/// Represents a host function that can be called from the interpreter
/// </summary>
public class HostFunction
{
    /// <summary>
    /// The name of the function
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The function implementation
    /// </summary>
    public HostFunctionDelegate Function { get; }

    /// <summary>
    /// Creates a new host function
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <param name="function">The function implementation</param>
    public HostFunction(string name, HostFunctionDelegate function)
    {
        Name = name;
        Function = function;
    }

    /// <summary>
    /// Invokes the function
    /// </summary>
    /// <param name="args">The arguments to the function</param>
    /// <returns>The result of the function call</returns>
    public object Invoke(params KsNode[] args)
    {
        return Function(args);
    }

    /// <summary>
    /// Creates a KsNode wrapper for this function
    /// </summary>
    /// <returns>A KsNode wrapper for this function</returns>
    public KsNode AsKsNode()
    {
        return new KsHostFunction(this);
    }

    /// <summary>
    /// Creates a HostFunction from a delegate that returns object
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <param name="function">The function implementation</param>
    /// <returns>A HostFunction instance</returns>
    public static HostFunction FromObjectDelegate(string name, Func<KsNode[], KsNode> function)
    {
        return new HostFunction(name, args => function(args));
    }
}