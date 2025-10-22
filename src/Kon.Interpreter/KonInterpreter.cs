using Kon.Core.Node;
using Kon.Interpreter.Runtime;

namespace Kon.Interpreter;

/// <summary>
/// Main entry point for the Kon interpreter
/// </summary>
public static class KonInterpreter
{

    /// <summary>
    /// Creates a new runtime instance
    /// </summary>
    /// <returns>A new runtime instance</returns>
    public static InterpreterRuntime CreateRuntime()
    {
        InterpreterRuntime runtime = new InterpreterRuntime();
        ExtensionRegistryInitializer.RegisterDefault(runtime);
        return runtime;
    }

    // public static Kon.Core.Node.KnNode EvaluateExprSync(string script)
    // {
    //     KonLangRuntime runtime = CreateRuntime();
    //     return EvaluateExprSync(runtime, script);
    // }

    // public static Kon.Core.Node.KnNode EvaluateExprSync(KonLangRuntime runtime, string script)
    // {
    //     var node = Kon.Core.Kon.Parse(script);
    //     runtime.AddOpDirectly(KonOpCode.OpStack_LandSuccess);
    //     runtime.AddOpDirectly(KonOpCode.Node_RunNode, node);
    //     return runtime.StartLoopSync();
    // }

    // public static async Task<Kon.Core.Node.KnNode> EvaluateExprAsync(string script)
    // {
    //     var node = Kon.Core.Kon.Parse(script);
    //     KonLangRuntime runtime = CreateRuntime();
    //     runtime.AddOpDirectly(KonOpCode.OpStack_LandSuccess);
    //     runtime.AddOpDirectly(KonOpCode.Node_RunNode, node);
    //     return runtime.StartLoopSync();
    // }

    public static Kon.Core.Node.KnNode EvaluateBlockSync(string script)
    {
        InterpreterRuntime runtime = CreateRuntime();
        return EvaluateBlockSync(runtime, script);
    }

    public static Kon.Core.Node.KnNode EvaluateBlockSync(InterpreterRuntime runtime, string script)
    {
        var nodes = Kon.Core.Kon.ParseItems(script);
        runtime.AddOpDirectly(OpCode.OpStack_LandSuccess);
        runtime.AddOpDirectly(OpCode.Node_RunBlock, new KnArray(nodes));
        return runtime.StartLoopSync();
    }

    public static async Task<Kon.Core.Node.KnNode> EvaluateBlockAsync(string script)
    {
        InterpreterRuntime runtime = CreateRuntime();
        return await EvaluateBlockAsync(runtime, script);
    }

    public static async Task<Kon.Core.Node.KnNode> EvaluateBlockAsync(InterpreterRuntime runtime, string script)
    {
        var nodes = Kon.Core.Kon.ParseItems(script);
        runtime.AddOpDirectly(OpCode.OpStack_LandSuccess);
        runtime.AddOpDirectly(OpCode.Node_RunBlock, new KnArray(nodes));
        return runtime.StartLoopSync();
    }
}