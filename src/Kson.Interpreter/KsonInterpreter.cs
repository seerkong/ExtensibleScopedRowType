using Kson.Core.Node;
using Kson.Interpreter.Runtime;

namespace Kson.Interpreter;

/// <summary>
/// Main entry point for the Kson interpreter
/// </summary>
public static class KsonInterpreter
{

    /// <summary>
    /// Creates a new runtime instance
    /// </summary>
    /// <returns>A new runtime instance</returns>
    public static KsonInterpreterRuntime CreateRuntime()
    {
        KsonInterpreterRuntime runtime = new KsonInterpreterRuntime();
        ExtensionRegistryInitializer.RegisterDefault(runtime);
        return runtime;
    }

    // public static Kson.Core.Node.KsNode EvaluateExprSync(string script)
    // {
    //     KsonLangRuntime runtime = CreateRuntime();
    //     return EvaluateExprSync(runtime, script);
    // }

    // public static Kson.Core.Node.KsNode EvaluateExprSync(KsonLangRuntime runtime, string script)
    // {
    //     var node = Kson.Core.Kson.Parse(script);
    //     runtime.AddOpDirectly(KsonOpCode.OpStack_LandSuccess);
    //     runtime.AddOpDirectly(KsonOpCode.Node_RunNode, node);
    //     return runtime.StartLoopSync();
    // }

    // public static async Task<Kson.Core.Node.KsNode> EvaluateExprAsync(string script)
    // {
    //     var node = Kson.Core.Kson.Parse(script);
    //     KsonLangRuntime runtime = CreateRuntime();
    //     runtime.AddOpDirectly(KsonOpCode.OpStack_LandSuccess);
    //     runtime.AddOpDirectly(KsonOpCode.Node_RunNode, node);
    //     return runtime.StartLoopSync();
    // }

    public static Kson.Core.Node.KsNode EvaluateBlockSync(string script)
    {
        KsonInterpreterRuntime runtime = CreateRuntime();
        return EvaluateBlockSync(runtime, script);
    }

    public static Kson.Core.Node.KsNode EvaluateBlockSync(KsonInterpreterRuntime runtime, string script)
    {
        var nodes = Kson.Core.Kson.ParseItems(script);
        runtime.AddOpDirectly(KsonOpCode.OpStack_LandSuccess);
        runtime.AddOpDirectly(KsonOpCode.Node_RunBlock, new KsArray(nodes));
        return runtime.StartLoopSync();
    }

    public static async Task<Kson.Core.Node.KsNode> EvaluateBlockAsync(string script)
    {
        KsonInterpreterRuntime runtime = CreateRuntime();
        return await EvaluateBlockAsync(runtime, script);
    }

    public static async Task<Kson.Core.Node.KsNode> EvaluateBlockAsync(KsonInterpreterRuntime runtime, string script)
    {
        var nodes = Kson.Core.Kson.ParseItems(script);
        runtime.AddOpDirectly(KsonOpCode.OpStack_LandSuccess);
        runtime.AddOpDirectly(KsonOpCode.Node_RunBlock, new KsArray(nodes));
        return runtime.StartLoopSync();
    }
}