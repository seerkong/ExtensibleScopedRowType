using Kon.Core;
using Kon.Core.Node;
using Kon.Core.Converter;
using Kon.Interpreter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Main runtime model for the Kon interpreter
/// </summary>
public static class InterpreterRuntimeExt
{


    /// <summary>
    /// Defines a global variable
    /// </summary>
    /// <param name="name">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    public static void DefineGlobal(this InterpreterRuntime runtime, string name, KnNode value)
    {
        runtime.GetGlobalEnv().Define(name, value);
    }


    /// <summary>
    /// Changes the current execution environment
    /// </summary>
    /// <param name="envId">The ID of the environment to switch to</param>
    public static void ChangeEnvironment(this InterpreterRuntime runtime, int envId)
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
    public static void DefineVariable(this InterpreterRuntime runtime, string name, KnNode value)
    {
        runtime.Define(name, value);
    }

    /// <summary>
    /// Sets the value of an existing variable
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <param name="value">The new value</param>
    public static void SetVariable(this InterpreterRuntime runtime, string name, KnNode value)
    {
        runtime.SetVar(name, value);
    }

    /// <summary>
    /// Looks up a variable value
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <returns>The variable value</returns>
    public static KnNode GetVariable(this InterpreterRuntime runtime, string name)
    {
        return runtime.Lookup(name);
    }
}
