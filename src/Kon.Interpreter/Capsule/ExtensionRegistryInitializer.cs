using Kon.Interpreter.Handlers;
using Kon.Interpreter.Handlers.Call;
using Kon.Interpreter.Handlers.Node;
using Kon.Interpreter.Runtime;
using System;
using System.Collections.Generic;

namespace Kon.Interpreter;

/// <summary>
/// Static initialization for the extension registry
/// </summary>
public static class ExtensionRegistryInitializer
{
    /// <summary>
    /// Initializes the extension registry with all default handlers
    /// </summary>
    public static void RegisterDefault(InterpreterRuntime runtime)
    {
        // Register value stack handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_PushFrame, ValueStackHandler.RunPushFrame);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_PushValue, ValueStackHandler.RunPushValue);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_PopValue, ValueStackHandler.RunPopValue);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_Duplicate, ValueStackHandler.RunDuplicate);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_PopFrameAndPushTopVal, ValueStackHandler.RunPopFrameAndPushTopVal);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.ValStack_PopFrameIgnoreResult, ValueStackHandler.RunPopFrameIgnoreResult);

        // Register environment handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_DiveProcessEnv, EnvHandler.RunDiveProcessEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_DiveLocalEnv, EnvHandler.RunDiveLocalEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_Rise, EnvHandler.RunRise);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_ChangeEnvById, EnvHandler.RunChangeEnvById);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_DeclareLocalVar, EnvHandler.RunDeclareLocalVar);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_DeclareGlobalVar, EnvHandler.RunDeclareGlobalVar);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_SetLocalEnv, EnvHandler.RunSetLocalEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Env_SetGlobalEnv, EnvHandler.RunSetGlobalEnv);
        // runtime.ExtensionRegistry.RegisterInstructionHandler(KonOpCode.Env_BindEnvByMap, EnvHandler.RunBindEnvByMap);

        // Register control handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_ApplyToFrameTop, Handlers.PrefixKeyword.FuncHandler.RunApplyToFrameTop);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_ApplyToFrameBottom, Handlers.PrefixKeyword.FuncHandler.RunApplyToFrameBottom);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_Jump, OpStackHandler.RunJump);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_JumpIfFalse, OpStackHandler.RunJumpIfFalse);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_IterConditionPairs, ConditionHandler.RunConditionPair);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_IterForEachLoop, ForeachHandler.RunIterForeachLoop);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_IterForLoop, ForLoopHandler.RunIterForLoop);


        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_MakeContExcludeTopNInstruction, ContinuationHandler.RunMakeContExcludeTopNInstruction);

        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_CurrentFiberToIdle, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_CurrentFiberToSuspended, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_AwakenMultiFibers, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.YieldToParentAndChangeCurrentFiberState, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.YieldToFiberAndChangeCurrentFiberState, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Ctrl_FinalizeFiber, ContinuationHandler.RunMakeContExcludeTopNInstruction);


        // Register node handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_RunNode, NodeHandler.RunNode);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_RunBlock, BlockHandler.RunBlock);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_IterEvalChainNode, ChainExprHandler.EvalChainNode);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_RunGetSubscript, SubscriptHandler.RunGetSubscript);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_RunGetProperty, GetPropertyHandler.RunGetProperty);

        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_MakeArray, ArrayHandler.RunMakeArray);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.Node_MakeMap, MapHandler.RunMakeMap);

        // Register operation stack handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.OpStack_LandSuccess, OpStackHandler.RunLandSuccess);
        runtime.ExtensionRegistry.RegisterInstructionHandler(OpCode.OpStack_LandFail, OpStackHandler.RunLandFail);

        // Register prefix keywords
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("fn", Handlers.PrefixKeyword.FuncHandler.ExpandDeclareFunc);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("var", Handlers.PrefixKeyword.VarHandler.ExpandDeclareVar);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("set", Handlers.PrefixKeyword.VarHandler.ExpandSetVar);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("if", IfElseHandler.ExpandIfElse);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("cond", ConditionHandler.ExpandCondition);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("foreach", ForeachHandler.ExpandForeach);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("for", ForLoopHandler.ExpandForLoop);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("++", SelfUpdateHandler.SelfUpdate_PlusOne);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("try", TryHandler.ExpandTry);
        runtime.ExtensionRegistry.RegisterPrefixKeywordExpander("perform", TryHandler.ExpandPerform);

        runtime.ExtensionRegistry.RegisterInfixKeywordExpander("set_to", SetToHandler.ExpandSetTo);


        InitializeWithStandardLibrary(runtime);
    }

    /// <summary>
    /// Initializes the interpreter with standard library functions
    /// </summary>
    public static void InitializeWithStandardLibrary(InterpreterRuntime runtime)
    {
        var rootEnv = runtime.GetRootEnv();

        // Register math functions
        HostSupport.MathFunctions.Register(rootEnv);

        // Register string functions
        HostSupport.StringFunctions.Register(rootEnv);

        // Register conversion functions
        HostSupport.ConversionFunctions.Register(rootEnv);

        // Register IO functions
        HostSupport.IOFunctions.Register(rootEnv);

        HostSupport.ArrayFunctions.Register(rootEnv);

        // Register builtin methods for KnNode types
        RegisterBuiltinMethods(runtime);
    }

    /// <summary>
    /// Registers builtin methods for KnArray, KnMap, and other builtin types
    /// </summary>
    private static void RegisterBuiltinMethods(InterpreterRuntime runtime)
    {
        var registry = runtime.BuiltinMethodRegistry;

        // Register array builtin methods
        HostSupport.ArrayBuiltinMethods.Register(registry);

        // Register map builtin methods
        HostSupport.MapBuiltinMethods.Register(registry);
    }
}