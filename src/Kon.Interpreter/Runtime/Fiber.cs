using System;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Represents a fiber (lightweight thread) in the interpreter
/// </summary>
public class Fiber
{
    private static int _nextId = 1;

    /// <summary>
    /// Unique identifier for the fiber
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The ID of the parent fiber
    /// </summary>
    private int _parentFiberId = 0;

    /// <summary>
    /// The current state of the fiber
    /// </summary>
    private FiberState _state;

    /// <summary>
    /// The ID of the current environment
    /// </summary>
    public int CurrentEnvId { get; set; } = 0;

    /// <summary>
    /// The operand stack for this fiber
    /// </summary>
    public OperandStack OperandStack { get; set; }

    /// <summary>
    /// The instruction stack for this fiber
    /// </summary>
    public InstructionStack InstructionStack { get; set; }

    /// <summary>
    /// Creates a new fiber
    /// </summary>
    public Fiber()
    {
        Id = _nextId;
        _nextId++;
    }

    /// <summary>
    /// Creates a copy of this fiber
    /// </summary>
    /// <returns>A copy of the fiber</returns>
    public Fiber Copy()
    {
        var result = new Fiber
        {
            _state = _state,
            CurrentEnvId = CurrentEnvId,
            OperandStack = OperandStack.Copy() as OperandStack,
            InstructionStack = InstructionStack.Copy() as InstructionStack
        };

        return result;
    }

    /// <summary>
    /// Creates a root fiber
    /// </summary>
    /// <returns>A new root fiber</returns>
    public static Fiber CreateRootFiber()
    {
        var fiber = new Fiber
        {
            _state = FiberState.Running,
            InstructionStack = new InstructionStack(),
            OperandStack = new OperandStack()
        };

        return fiber;
    }

    /// <summary>
    /// Creates a sub-fiber from a parent fiber
    /// </summary>
    /// <param name="parentFiber">The parent fiber</param>
    /// <param name="initState">The initial state of the sub-fiber</param>
    /// <returns>A new sub-fiber</returns>
    public static Fiber CreateSubFiber(Fiber parentFiber, FiberState initState)
    {
        var fiber = new Fiber
        {
            _parentFiberId = parentFiber.Id,
            _state = initState,
            CurrentEnvId = parentFiber.CurrentEnvId,
            OperandStack = new OperandStack(),
            InstructionStack = new InstructionStack()
        };

        return fiber;
    }

    /// <summary>
    /// Initializes the instructions for this fiber
    /// </summary>
    /// <param name="initOps">The initial operations</param>
    public void InitInstructions(Instruction[] initOps)
    {
        InstructionStack = new InstructionStack();

        for (var i = initOps.Length - 1; i >= 0; i--)
        {
            InstructionStack.PushValue(initOps[i]);
        }
    }

    /// <summary>
    /// Checks if this fiber is the root fiber
    /// </summary>
    /// <returns>True if this is the root fiber; otherwise, false</returns>
    public bool IsRootFiber() => _parentFiberId <= 0;

    /// <summary>
    /// Gets the ID of the parent fiber
    /// </summary>
    /// <returns>The ID of the parent fiber</returns>
    public int GetParentFiberId() => _parentFiberId;

    /// <summary>
    /// Gets the current state of the fiber
    /// </summary>
    /// <returns>The current state</returns>
    public FiberState GetState() => _state;

    /// <summary>
    /// Sets the state of the fiber
    /// </summary>
    /// <param name="state">The new state</param>
    public void SetState(FiberState state) => _state = state;

    /// <summary>
    /// Changes the current environment by ID
    /// </summary>
    /// <param name="envId">The ID of the new environment</param>
    public void ChangeEnvById(int envId) => CurrentEnvId = envId;

    /// <summary>
    /// Adds an operation directly to the instruction stack
    /// </summary>
    /// <param name="opCode">The operation code</param>
    /// <param name="memo">Additional data</param>
    public void AddOpDirectly(string opCode, object? memo = null)
    {
        InstructionStack.PushValue(new Instruction(opCode, CurrentEnvId, memo));
    }
}