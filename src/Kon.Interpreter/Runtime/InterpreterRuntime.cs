using System;
using System.Collections.Generic;
using Kon.Core.Node;

namespace Kon.Interpreter.Runtime;


/// <summary>
/// Represents the state of the Kon interpreter
/// </summary>
public class InterpreterRuntime
{
    /// <summary>
    /// The environment tree
    /// </summary>
    public EnvTree EnvTree { get; private set; } = new();

    /// <summary>
    /// The fiber manager
    /// </summary>
    public FiberManager FiberMgr { get; private set; } = new();

    /// <summary>
    /// Execution history
    /// </summary>
    public List<InstructionExecLog> InstructionHistory { get; } = new();

    public ExtensionRegistry ExtensionRegistry { get; private set; } = new();

    /// <summary>
    /// Registry for builtin methods on KnNode types
    /// </summary>
    public BuiltinMethodRegistry BuiltinMethodRegistry { get; private set; } = new();

    /// <summary>
    /// Creates a new Kon state
    /// </summary>
    public InterpreterRuntime()
    {
        var buildInEnv = Env.CreateBuiltInEnv();
        EnvTree.AddVertex(buildInEnv);
        EnvTree.SetEntryVertexId(buildInEnv.Id);

        var globalEnv = Env.CreateGlobalEnv(buildInEnv);
        EnvTree.AddVertex(globalEnv);
        EnvTree.AddEdge(buildInEnv.GetVertexId(), globalEnv.GetVertexId());

        ResetFiberMgr();
    }

    /// <summary>
    /// Creates a copy of this state
    /// </summary>
    /// <returns>A copy of the state</returns>
    public InterpreterRuntime Copy()
    {
        var result = new InterpreterRuntime
        {
            EnvTree = EnvTree,
            FiberMgr = FiberMgr.Copy()
        };

        return result;
    }

    /// <summary>
    /// Gets the current environment ID
    /// </summary>
    public int CurrentEnvId => FiberMgr.GetCurrentFiber()?.CurrentEnvId ?? 0;

    /// <summary>
    /// Gets the operand stack of the current fiber
    /// </summary>
    public List<KnNode> OperandStack => FiberMgr.GetCurrentFiber()?.OperandStack.FrameStackView;

    /// <summary>
    /// Gets the instruction stack of the current fiber
    /// </summary>
    public List<Instruction> InstructionStack => FiberMgr.GetCurrentFiber()?.InstructionStack.FrameStackView;

    /// <summary>
    /// Resets the fiber manager
    /// </summary>
    public void ResetFiberMgr()
    {
        FiberMgr.ResetAllState();
        var globalEnv = GetGlobalEnv();
        var mainFiber = Fiber.CreateRootFiber();
        FiberMgr.GetRunnableFibers().Add(mainFiber);
        FiberMgr.AddFiber(mainFiber);
        mainFiber.CurrentEnvId = globalEnv.Id;

        // Set the current fiber after adding it
        FiberMgr.SetCurrentFiber(mainFiber);
    }

    /// <summary>
    /// Gets the current fiber
    /// </summary>
    /// <returns>The current fiber</returns>
    public Fiber GetCurrentFiber() => FiberMgr.GetCurrentFiber();

    /// <summary>
    /// Gets the root fiber
    /// </summary>
    /// <returns>The root fiber</returns>
    public Fiber GetRootFiber() => FiberMgr.GetRootFiber();

    /// <summary>
    /// Gets the root environment
    /// </summary>
    /// <returns>The root environment</returns>
    public Env GetRootEnv() => EnvTree.GetEntryVertex();

    /// <summary>
    /// Gets the global environment
    /// </summary>
    /// <returns>The global environment</returns>
    public Env GetGlobalEnv()
    {
        var rootEnv = GetRootEnv();
        var envSetUnderRoot = EnvTree.GetNextVertexDetails(rootEnv.GetVertexId());

        foreach (var env in envSetUnderRoot)
        {
            if (env.Type == Env.EnvType.Global)
            {
                return env;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the current environment ID
    /// </summary>
    /// <returns>The current environment ID</returns>
    public int GetCurEnvId() => FiberMgr.GetCurrentFiber()?.CurrentEnvId ?? 0;

    /// <summary>
    /// Gets the current environment
    /// </summary>
    /// <returns>The current environment</returns>
    public Env GetCurEnv()
    {
        var currentEnvId = FiberMgr.GetCurrentFiber()?.CurrentEnvId ?? 0;
        return EnvTree.GetVertexDetail(currentEnvId);
    }

    /// <summary>
    /// Looks up a value by key
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <returns>The value, or null if not found</returns>
    public KnNode Lookup(string key)
    {
        var declareEnv = EnvTree.LookupDeclareEnv(GetCurEnv(), key);
        return declareEnv.Lookup(key);
    }

    public KnNode LookupThrowErrIfNotFound(string key)
    {
        var declareEnv = EnvTree.LookupDeclareEnv(GetCurEnv(), key);
        if (!declareEnv.ContainsVar(key))
        {
            throw new ApplicationException(string.Format("var {0} not found", key));
        }
        return declareEnv.Lookup(key);
    }

    /// <summary>
    /// Looks up a value from a specific environment
    /// </summary>
    /// <param name="fromEnv">The environment to look up from</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The value, or null if not found</returns>
    public KnNode LookupFromEnv(Env fromEnv, string key)
    {
        var declareEnv = EnvTree.LookupDeclareEnv(fromEnv, key);
        return declareEnv.Lookup(key);
    }

    /// <summary>
    /// Defines a value in the current environment
    /// </summary>
    /// <param name="key">The key to define</param>
    /// <param name="value">The value to define</param>
    public void Define(string key, KnNode value)
    {
        var declareEnv = GetCurEnv();
        declareEnv.Define(key, value);
    }

    /// <summary>
    /// Sets a variable value
    /// </summary>
    /// <param name="key">The key to set</param>
    /// <param name="value">The value to set</param>
    public void SetVar(string key, KnNode value)
    {
        var declareEnv = EnvTree.LookupDeclareEnv(GetCurEnv(), key);
        declareEnv.Define(key, value);
    }

    /// <summary>
    /// Sets a variable value from a specific environment
    /// </summary>
    /// <param name="fromEnv">The environment to set from</param>
    /// <param name="key">The key to set</param>
    /// <param name="value">The value to set</param>
    public void SetVarFromEnv(Env fromEnv, string key, KnNode value)
    {
        var declareEnv = EnvTree.LookupDeclareEnv(fromEnv, key);
        declareEnv.Define(key, value);
    }

    private readonly List<List<Instruction>> _opBatchStack = new();

    /// <summary>
    /// Starts an operation batch
    /// </summary>
    public void OpBatchStart()
    {
        _opBatchStack.Add(new List<Instruction>());
    }

    /// <summary>
    /// Adds an operation to the current batch
    /// </summary>
    /// <param name="opCode">The operation code</param>
    /// <param name="memo">Additional data</param>
    public void AddOp(string opCode, object memo = null, string comment = "")
    {
        var top = _opBatchStack[^1];
        top.Add(new Instruction(opCode, GetCurEnvId(), memo, comment));
    }

    /// <summary>
    /// Commits the current operation batch
    /// </summary>
    public void OpBatchCommit()
    {
        var opList = _opBatchStack[^1];
        _opBatchStack.RemoveAt(_opBatchStack.Count - 1);

        if (_opBatchStack.Count > 0 && opList.Count > 0)
        {
            // Peek at the next batch
            var top = _opBatchStack[^1];

            // Add the operations to the next batch
            top.AddRange(opList);
        }
        else
        {
            // Add the operations to the instruction stack
            FiberMgr.GetCurrentFiber().InstructionStack.ReversePushItems(opList);
        }
    }

    /// <summary>
    /// Adds an operation directly to the instruction stack
    /// </summary>
    /// <param name="opCode">The operation code</param>
    /// <param name="memo">Additional data</param>
    public void AddOpDirectly(string opCode, object memo = null)
    {
        FiberMgr.GetCurrentFiber().InstructionStack.PushValue(new Instruction(opCode, GetCurEnvId(), memo));
    }
}