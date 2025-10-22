namespace Kon.Interpreter.Runtime;

/// <summary>
/// OpCodes for the Kon interpreter, similar to the kunun.ts XnlOpCode
/// </summary>
public static class OpCode
{
    // Operation stack control
    public static readonly string OpStack_LandSuccess = "OpStack_LandSuccess";
    public static readonly string OpStack_LandFail = "OpStack_LandFail";

    // Value stack operations
    public static readonly string ValStack_PushFrame = "ValStack_PushFrame";
    public static readonly string ValStack_PopFrameAndPushTopVal = "ValStack_PopFrameAndPushTopVal";
    public static readonly string ValStack_PopFrameIgnoreResult = "ValStack_PopFrameIgnoreResult";
    public static readonly string ValStack_PushValue = "ValStack_PushValue";
    public static readonly string ValStack_PopValue = "ValStack_PopValue";
    public static readonly string ValStack_Duplicate = "ValStack_Duplicate";

    // Environment operations
    public static readonly string Env_DiveProcessEnv = "Env_DiveProcessEnv";
    public static readonly string Env_DiveLocalEnv = "Env_DiveLocalEnv";
    public static readonly string Env_Rise = "Env_Rise";
    public static readonly string Env_ChangeEnvById = "Env_ChangeEnvById";
    public static readonly string Env_DeclareLocalVar = "Env_DeclareLocalVar";
    public static readonly string Env_DeclareGlobalVar = "Env_DeclareGlobalVar";
    public static readonly string Env_SetLocalEnv = "Env_SetLocalEnv";
    public static readonly string Env_SetGlobalEnv = "Env_SetGlobalEnv";
    // public static readonly string Env_BindEnvByMap = "Env_BindEnvByMap";

    // Control operations
    public static readonly string Ctrl_ApplyToFrameTop = "Ctrl_ApplyToFrameTop";
    public static readonly string Ctrl_ApplyToFrameBottom = "Ctrl_ApplyToFrameBottom";
    public static readonly string Ctrl_Jump = "Ctrl_Jump";
    public static readonly string Ctrl_JumpIfFalse = "Ctrl_JumpIfFalse";
    public static readonly string Ctrl_IterConditionPairs = "Ctrl_IterConditionPairs";
    public static readonly string Ctrl_IterForEachLoop = "Ctrl_IterForEachLoop";
    public static readonly string Ctrl_IterForLoop = "Ctrl_IterForLoop";

    public static readonly string Ctrl_MakeContExcludeTopNInstruction = "Ctrl_MakeContExcludeTopNInstruction";

    // Fiber operations
    public static readonly string Ctrl_CurrentFiberToIdle = "Ctrl_CurrentFiberToIdle";
    public static readonly string Ctrl_CurrentFiberToSuspended = "Ctrl_CurrentFiberToSuspended";
    public static readonly string Ctrl_AwakenMultiFibers = "Ctrl_AwakenMultiFibers";
    public static readonly string YieldToParentAndChangeCurrentFiberState = "YieldToParentAndChangeCurrentFiberState";
    public static readonly string YieldToFiberAndChangeCurrentFiberState = "YieldToFiberAndChangeCurrentFiberState";
    public static readonly string Ctrl_FinalizeFiber = "Ctrl_FinalizeFiber";

    // Node operations
    public static readonly string Node_RunNode = "Node_RunNode";
    public static readonly string Node_RunLastVal = "Node_RunLastVal";
    public static readonly string Node_MakeArray = "Node_MakeArray";
    public static readonly string Node_MakeMap = "Node_MakeMap";
    public static readonly string Node_IterEvalChainNode = "Node_IterEvalChainNode";
    public static readonly string Node_RunBlock = "Node_RunBlock";
    public static readonly string Node_RunGetProperty = "Node_RunGetProperty";
    public static readonly string Node_RunSetProperty = "Node_RunSetProperty";
    public static readonly string Node_RunGetSubscript = "Node_RunGetSubscript";
    public static readonly string Node_RunSetSubscript = "Node_RunSetSubscript";
}