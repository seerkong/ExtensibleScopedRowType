using Kson.Interpreter.Handlers;
using Kson.Interpreter.Handlers.Node;
using Kson.Interpreter.Runtime;
using System;
using System.Collections.Generic;

namespace Kson.Interpreter;

/// <summary>
/// Static initialization for the extension registry
/// </summary>
public static class ExtensionRegistryInitializer
{
    /// <summary>
    /// Initializes the extension registry with all default handlers
    /// </summary>
    public static void RegisterDefault(KsonInterpreterRuntime runtime)
    {
        // Register value stack handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_PushFrame, ValueStackHandler.RunPushFrame);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_PushValue, ValueStackHandler.RunPushValue);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_PopValue, ValueStackHandler.RunPopValue);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_Duplicate, ValueStackHandler.RunDuplicate);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_PopFrameAndPushTopVal, ValueStackHandler.RunPopFrameAndPushTopVal);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.ValStack_PopFrameIgnoreResult, ValueStackHandler.RunPopFrameIgnoreResult);

        // Register environment handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_DiveProcessEnv, EnvHandler.RunDiveProcessEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_DiveLocalEnv, EnvHandler.RunDiveLocalEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_Rise, EnvHandler.RunRise);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_ChangeEnvById, EnvHandler.RunChangeEnvById);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_DeclareLocalVar, EnvHandler.RunDeclareLocalVar);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_DeclareGlobalVar, EnvHandler.RunDeclareGlobalVar);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_SetLocalEnv, EnvHandler.RunSetLocalEnv);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_SetGlobalEnv, EnvHandler.RunSetGlobalEnv);
        // runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Env_BindEnvByMap, EnvHandler.RunBindEnvByMap);

        // Register control handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_ApplyToFrameTop, Handlers.PrefixKeyword.FuncHandler.RunApplyToFrameTop);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_ApplyToFrameBottom, Handlers.PrefixKeyword.FuncHandler.RunApplyToFrameBottom);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_Jump, OpStackHandler.RunJump);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_JumpIfFalse, OpStackHandler.RunJumpIfFalse);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_IterConditionPairs, ConditionHandler.RunConditionPair);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_IterForEachLoop, ForeachHandler.RunIterForeachLoop);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_IterForLoop, ForLoopHandler.RunIterForLoop);


        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_MakeContExcludeTopNInstruction, ContinuationHandler.RunMakeContExcludeTopNInstruction);

        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_CurrentFiberToIdle, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_CurrentFiberToSuspended, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_AwakenMultiFibers, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.YieldToParentAndChangeCurrentFiberState, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.YieldToFiberAndChangeCurrentFiberState, ContinuationHandler.RunMakeContExcludeTopNInstruction);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Ctrl_FinalizeFiber, ContinuationHandler.RunMakeContExcludeTopNInstruction);


        // Register node handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_RunNode, NodeHandler.RunNode);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_RunBlock, BlockHandler.RunBlock);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_IterEvalChainNode, ChainExprHandler.EvalChainNode);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_RunGetSubscript, SubscriptHandler.RunGetSubscript);

        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_MakeArray, ArrayHandler.RunMakeArray);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.Node_MakeMap, MapHandler.RunMakeMap);

        // Register operation stack handlers
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.OpStack_LandSuccess, OpStackHandler.RunLandSuccess);
        runtime.ExtensionRegistry.RegisterInstructionHandler(KsonOpCode.OpStack_LandFail, OpStackHandler.RunLandFail);

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
    public static void InitializeWithStandardLibrary(KsonInterpreterRuntime runtime)
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
    }
}