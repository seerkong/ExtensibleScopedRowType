using Kson.Core;
using Kson.Core.Node;
using Kson.Core.Converter;
using Kson.Interpreter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kson.Interpreter.Runtime;

/// <summary>
/// Main runtime model for the Kson interpreter
/// </summary>
public static class KsonLangRuntimeExt
{


    /// <summary>
    /// Defines a global variable
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    public static void DefineGlobal(this KsonInterpreterRuntime runtime, string name, KsNode value)
    {
        runtime.GetGlobalEnv().Define(name, value);
    }

    /// <summary>
    /// Creates a new environment scope
    /// </summary>
    /// <param name="name">Name for the new environment</param>
    /// <param name="type">Type of environment to create</param>
    /// <returns>The ID of the new environment</returns>
    public static int CreateEnvironment(this KsonInterpreterRuntime runtime, string name, Env.EnvType type = Env.EnvType.Local)
    {
        var currentEnv = runtime.GetCurEnv() ?? throw new InvalidOperationException("Current environment is unavailable.");
        var newEnv = type switch
        {
            Env.EnvType.Process => Env.CreateProcessEnv(currentEnv, name),
            Env.EnvType.Local => Env.CreateLocalEnv(currentEnv, name),
            _ => throw new ArgumentException($"Cannot create environment of type {type}")
        };

        runtime.EnvTree.AddVertex(newEnv);
        runtime.EnvTree.AddEdge(currentEnv.GetVertexId(), newEnv.GetVertexId());

        return newEnv.GetVertexId();
    }

    /// <summary>
    /// Changes the current execution environment
    /// </summary>
    /// <param name="envId">The ID of the environment to switch to</param>
    public static void ChangeEnvironment(this KsonInterpreterRuntime runtime, int envId)
    {
        var currentFiber = runtime.GetCurrentFiber();
        if (currentFiber != null)
        {
            currentFiber.ChangeEnvById(envId);
        }
    }

    /// <summary>
    /// Defines a variable in the current environment
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <param name="value">The variable value</param>
    public static void DefineVariable(this KsonInterpreterRuntime runtime, string name, KsNode value)
    {
        runtime.Define(name, value);
    }

    /// <summary>
    /// Sets the value of an existing variable
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <param name="value">The new value</param>
    public static void SetVariable(this KsonInterpreterRuntime runtime, string name, KsNode value)
    {
        runtime.SetVar(name, value);
    }

    /// <summary>
    /// Looks up a variable value
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <returns>The variable value</returns>
    public static KsNode GetVariable(this KsonInterpreterRuntime runtime, string name)
    {
        return runtime.Lookup(name);
    }
}
