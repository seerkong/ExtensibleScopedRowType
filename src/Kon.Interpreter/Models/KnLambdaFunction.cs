using System;
using Kon.Core.Node;

namespace Kon.Interpreter.Models;

/// <summary>
/// Lambda function wrapper for KnNode system
/// </summary>
public class KnLambdaFunction : KnValueNode
{
    /// <summary>
    /// The wrapped lambda function
    /// </summary>
    public LambdaFunction Function { get; }

    /// <summary>
    /// Creates a new lambda function node
    /// </summary>
    /// <param name="function">The function to wrap</param>
    public KnLambdaFunction(LambdaFunction function)
    {
        Function = function;
    }

    public override string ToString() => $"<lambda-function>";

    public override bool Equals(object? obj) => obj is KnLambdaFunction other && Function.Equals(other.Function);

    public override int GetHashCode() => Function.GetHashCode();

    public override bool ToBoolean() => true;
}


/// <summary>
/// Represents a lambda function defined in the interpreter
/// </summary>
public class LambdaFunction
{
    /// <summary>
    /// The name of the function
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The parameter names
    /// </summary>
    public string[] Parameters { get; }

    /// <summary>
    /// The body of the function
    /// </summary>
    public KnArray Body { get; }

    /// <summary>
    /// The environment in which the function was defined
    /// </summary>
    public Runtime.Env DefinitionEnv { get; }

    /// <summary>
    /// Creates a new lambda function
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <param name="parameters">The parameter names</param>
    /// <param name="body">The body of the function</param>
    /// <param name="definitionEnvironment">The environment in which the function was defined</param>
    public LambdaFunction(string name, string[] parameters, KnArray body, Runtime.Env definitionEnvironment)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
        DefinitionEnv = definitionEnvironment;
    }

    /// <summary>
    /// Gets the arity (number of parameters) of the function
    /// </summary>
    public int Arity => Parameters.Length;
}