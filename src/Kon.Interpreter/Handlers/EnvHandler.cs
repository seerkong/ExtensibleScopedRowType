using Kon.Core.Node;
using Kon.Interpreter.Runtime;

namespace Kon.Interpreter.Handlers;

/// <summary>
/// Handler for environment management
/// </summary>
public static class EnvHandler
{
    /// <summary>
    /// Dive into a process environment
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDiveProcessEnv(InterpreterRuntime runtime, Instruction instruction)
    {
        var currentEnv = runtime.GetCurEnv();
        var name = instruction.Memo as string ?? "process";
        var processEnv = Env.CreateProcessEnv(currentEnv, name);

        runtime.EnvTree.AddVertex(processEnv);
        runtime.EnvTree.AddEdge(currentEnv.GetVertexId(), processEnv.GetVertexId());

        runtime.GetCurrentFiber().ChangeEnvById(processEnv.Id);
    }

    public static Env MakeSubLocalEnvUnderEnv(
        InterpreterRuntime runtime,
        Env currentEnv, string? envName = null)
    {
        var localEnv = Env.CreateLocalEnv(currentEnv, envName);

        runtime.EnvTree.AddVertex(localEnv);
        runtime.EnvTree.AddEdge(currentEnv.GetVertexId(), localEnv.GetVertexId());

        runtime.GetCurrentFiber().ChangeEnvById(localEnv.Id);
        return localEnv;
    }

    /// <summary>
    /// Dive into a local environment
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDiveLocalEnv(InterpreterRuntime runtime, Instruction instruction)
    {
        var currentEnv = runtime.GetCurEnv();
        var name = instruction.Memo as string ?? "local";
        MakeSubLocalEnvUnderEnv(runtime, currentEnv, name);
    }

    /// <summary>
    /// Rise from the current environment
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunRise(InterpreterRuntime runtime, Instruction instruction)
    {
        var currentEnv = runtime.GetCurEnv();
        var nextEnv = currentEnv.ParentEnv;
        runtime.GetCurrentFiber().ChangeEnvById(nextEnv.Id);
    }

    /// <summary>
    /// Change the environment by ID
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunChangeEnvById(InterpreterRuntime runtime, Instruction instruction)
    {
        var envId = (int)instruction.Memo;
        runtime.GetCurrentFiber().ChangeEnvById(envId);
    }

    /// <summary>
    /// Declare a global variable
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDeclareGlobalVar(InterpreterRuntime runtime, Instruction instruction)
    {
        var name = (string)instruction.Memo;
        var value = runtime.GetCurrentFiber().OperandStack.PopValue();
        runtime.GetGlobalEnv().Define(name, value);
    }

    /// <summary>
    /// Declare a local variable
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunDeclareLocalVar(InterpreterRuntime runtime, Instruction instruction)
    {
        var name = (string)instruction.Memo;
        var value = runtime.GetCurrentFiber().OperandStack.PopValue();
        runtime.GetCurEnv().Define(name, value);
    }

    /// <summary>
    /// Set a global variable
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunSetGlobalEnv(InterpreterRuntime runtime, Instruction instruction)
    {
        var name = (string)instruction.Memo;
        var value = runtime.GetCurrentFiber().OperandStack.PopValue();
        runtime.GetGlobalEnv().Define(name, value);
    }

    /// <summary>
    /// Set a local variable
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunSetLocalEnv(InterpreterRuntime runtime, Instruction instruction)
    {
        var name = (string)instruction.Memo;
        var value = runtime.GetCurrentFiber().OperandStack.PopValue();

        // Look up the environment where the variable is defined
        var declareEnv = runtime.EnvTree.LookupDeclareEnv(runtime.GetCurEnv(), name);
        declareEnv.Define(name, value);
    }


    public static void RunBindEnvByMap(InterpreterRuntime runtime, Instruction opContState)
    {
        Env curEnv = runtime.GetCurEnv();
        object lastVal = runtime.GetCurrentFiber().OperandStack.PopValue();

        // TODO lastVal should be a dictionary
        if (lastVal is Dictionary<string, KnNode> dict)
        {
            foreach (var kvp in dict)
            {
                curEnv.Define(kvp.Key, kvp.Value);
            }
        }
        else
        {
            // 处理非字典类型的情况，或者抛出异常
            throw new InvalidOperationException("Expected a dictionary for environment binding");
        }
    }
}
