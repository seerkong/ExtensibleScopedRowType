using System;
using Kon.Core.Node;

namespace Kon.Interpreter.Models;

public class KnHostFunction : KnValueNode
{
    public object HostFunction { get; }

    public KnHostFunction(object hostFunction)
    {
        HostFunction = hostFunction ?? throw new ArgumentNullException(nameof(hostFunction));
    }

    public override string ToString() => $"<host-function>";

    public override bool Equals(object? obj) => obj is KnHostFunction other && HostFunction.Equals(other.HostFunction);

    public override int GetHashCode() => HostFunction.GetHashCode();

    public override bool ToBoolean() => true;
}



/// <summary>
/// Represents a function delegate that can be called from the interpreter
/// </summary>
/// <param name="args">The arguments to the function</param>
/// <returns>The result of the function call</returns>
public delegate KnNode HostFunctionDelegate(params KnNode[] args);

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
    public object Invoke(params KnNode[] args)
    {
        return Function(args);
    }

    /// <summary>
    /// Creates a KnNode wrapper for this function
    /// </summary>
    /// <returns>A KnNode wrapper for this function</returns>
    public KnNode AsKnNode()
    {
        return new KnHostFunction(this);
    }

    /// <summary>
    /// Creates a HostFunction from a delegate that returns object
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <param name="function">The function implementation</param>
    /// <returns>A HostFunction instance</returns>
    public static HostFunction FromObjectDelegate(string name, Func<KnNode[], KnNode> function)
    {
        return new HostFunction(name, args => function(args));
    }
}