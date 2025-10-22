using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kon.Core.Node;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Token used to resume a fiber with results and optional pre-resume instructions.
/// </summary>
public class ResumeFiberToken
{
    /// <summary>
    /// Identifier of the fiber that should be resumed.
    /// </summary>
    public int FiberId { get; }

    /// <summary>
    /// Values that will be pushed onto the operand stack once the fiber resumes.
    /// </summary>
    public List<KnNode> Result { get; }

    /// <summary>
    /// Instructions that will be pushed onto the instruction stack before resuming.
    /// </summary>
    public List<Instruction> BeforeResumeOps { get; }

    /// <summary>
    /// Creates a new resume token.
    /// </summary>
    /// <param name="fiberId">Target fiber identifier.</param>
    /// <param name="result">Optional result payload.</param>
    /// <param name="beforeResumeOps">Optional instructions to execute prior to resuming.</param>
    public ResumeFiberToken(
        int fiberId,
        IEnumerable<KnNode>? result = null,
        IEnumerable<Instruction>? beforeResumeOps = null)
    {
        FiberId = fiberId;
        Result = result?.ToList() ?? new List<KnNode>();
        BeforeResumeOps = beforeResumeOps?.ToList() ?? new List<Instruction>();
    }
}

/// <summary>
/// Manages the lifecycle and scheduling of fibers.
/// </summary>
public class FiberManager
{
    private readonly Dictionary<int, Fiber> _fibersById = new();

    private List<Fiber> _runnableFibers = new();
    private List<Fiber> _idleFibers = new();
    private List<Fiber> _suspendedFibers = new();

    private readonly Queue<ResumeFiberToken> _resumeEventQueue = new();

    private Fiber? _currentFiber;
    private Fiber? _rootFiber;

    private readonly List<List<Instruction>> _opBatchStack = new();

    /// <summary>
    /// Creates a shallow copy of the fiber manager and its fibers.
    /// </summary>
    /// <returns>A copy of the current manager.</returns>
    public FiberManager Copy()
    {
        var copy = new FiberManager();
        var mapping = new Dictionary<int, Fiber>();

        foreach (var fiber in GetAllFibers())
        {
            var copied = fiber.Copy();
            mapping[fiber.Id] = copied;
            copy._fibersById[copied.Id] = copied;

            if (fiber.IsRootFiber())
            {
                copy._rootFiber = copied;
            }
        }

        copy._runnableFibers = _runnableFibers.Select(f => mapping[f.Id]).ToList();
        copy._idleFibers = _idleFibers.Select(f => mapping[f.Id]).ToList();
        copy._suspendedFibers = _suspendedFibers.Select(f => mapping[f.Id]).ToList();

        if (_currentFiber != null && mapping.TryGetValue(_currentFiber.Id, out var mappedCurrent))
        {
            copy._currentFiber = mappedCurrent;
        }

        return copy;
    }

    /// <summary>
    /// Adds a fiber to the manager and places it in the list matching its current state.
    /// </summary>
    public void AddFiber(Fiber fiber)
    {
        if (fiber == null)
        {
            throw new ArgumentNullException(nameof(fiber));
        }

        _fibersById[fiber.Id] = fiber;

        if (fiber.IsRootFiber())
        {
            _rootFiber = fiber;
        }

        RemoveFiberFromAllLists(fiber.Id);

        switch (fiber.GetState())
        {
            case FiberState.Runnable:
                AddUniqueFiber(_runnableFibers, fiber);
                break;
            case FiberState.Running:
                AddUniqueFiber(_runnableFibers, fiber, true);
                _currentFiber = fiber;
                break;
            case FiberState.Idle:
                AddUniqueFiber(_idleFibers, fiber);
                break;
            case FiberState.Suspended:
                AddUniqueFiber(_suspendedFibers, fiber);
                break;
            case FiberState.Dead:
            default:
                break;
        }
    }

    /// <summary>
    /// Adds a fiber to the suspended pool.
    /// </summary>
    public void AddToSuspendedFibersLast(Fiber fiber)
    {
        if (fiber == null)
        {
            throw new ArgumentNullException(nameof(fiber));
        }

        RemoveFiberFromAllLists(fiber.Id);
        AddUniqueFiber(_suspendedFibers, fiber);
        fiber.SetState(FiberState.Suspended);
    }

    /// <summary>
    /// Moves the current fiber to the idle list and returns a resume token.
    /// </summary>
    public ResumeFiberToken CurrentFiberToIdle()
    {
        var fiber = GetRequiredCurrentFiber();
        SwitchFiberState(fiber, FiberState.Idle, null);
        return new ResumeFiberToken(fiber.Id);
    }

    /// <summary>
    /// Suspends the current fiber and returns a resume token.
    /// </summary>
    public ResumeFiberToken SuspendCurrentFiber()
    {
        var fiber = GetRequiredCurrentFiber();
        SwitchFiberState(fiber, FiberState.Suspended, null);
        return new ResumeFiberToken(fiber.Id);
    }

    /// <summary>
    /// Retrieves a mapping of fiber IDs to their instances.
    /// </summary>
    public Dictionary<int, Fiber> GetFiberByIds(IEnumerable<int> fiberIds)
    {
        var idSet = new HashSet<int>(fiberIds);
        var result = new Dictionary<int, Fiber>();

        foreach (var fiber in _runnableFibers)
        {
            if (idSet.Contains(fiber.Id))
            {
                result[fiber.Id] = fiber;
            }
        }

        foreach (var fiber in _idleFibers)
        {
            if (idSet.Contains(fiber.Id))
            {
                result[fiber.Id] = fiber;
            }
        }

        foreach (var fiber in _suspendedFibers)
        {
            if (idSet.Contains(fiber.Id))
            {
                result[fiber.Id] = fiber;
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves a fiber by its identifier.
    /// </summary>
    public Fiber? GetFiberById(int fiberId)
    {
        if (_fibersById.TryGetValue(fiberId, out var fiber))
        {
            return fiber;
        }

        var map = GetFiberByIds(new[] { fiberId });

        if (map.TryGetValue(fiberId, out fiber))
        {
            _fibersById[fiberId] = fiber;
        }

        return fiber;
    }

    /// <summary>
    /// Switches execution to the specified fiber.
    /// </summary>
    public void SwitchFiber(int toFiberId, FiberState? oldFiberToState)
    {
        var toFiber = GetFiberById(toFiberId);

        if (toFiber == null)
        {
            throw new InvalidOperationException("Attempted to switch to a fiber that does not exist.");
        }

        SwitchFiberState(toFiber, FiberState.Running, oldFiberToState);
    }

    /// <summary>
    /// Updates fiber states and scheduling lists to reflect a switch.
    /// </summary>
    public void SwitchFiberState(Fiber toFiber, FiberState targetState, FiberState? oldFiberToState)
    {
        if (toFiber == null)
        {
            throw new ArgumentNullException(nameof(toFiber));
        }

        var currentFiber = GetCurrentFiber();
        var excludeIds = new List<int> { toFiber.Id };

        if (currentFiber != null && currentFiber.Id != toFiber.Id)
        {
            excludeIds.Add(currentFiber.Id);
        }

        var (updatedRunnable, updatedIdle, updatedSuspended) = ExcludeFibers(excludeIds);

        if (currentFiber != null && oldFiberToState.HasValue && currentFiber.Id != toFiber.Id)
        {
            currentFiber.SetState(oldFiberToState.Value);

            switch (oldFiberToState.Value)
            {
                case FiberState.Runnable:
                case FiberState.Running:
                    AddUniqueFiber(updatedRunnable, currentFiber, oldFiberToState.Value == FiberState.Running);
                    break;
                case FiberState.Idle:
                    AddUniqueFiber(updatedIdle, currentFiber);
                    break;
                case FiberState.Suspended:
                    AddUniqueFiber(updatedSuspended, currentFiber);
                    break;
                case FiberState.Dead:
                default:
                    break;
            }
        }

        toFiber.SetState(targetState);

        switch (targetState)
        {
            case FiberState.Runnable:
                AddUniqueFiber(updatedRunnable, toFiber);
                break;
            case FiberState.Running:
                AddUniqueFiber(updatedRunnable, toFiber, true);
                _currentFiber = toFiber;
                break;
            case FiberState.Idle:
                AddUniqueFiber(updatedIdle, toFiber);
                _currentFiber = null;
                break;
            case FiberState.Suspended:
                AddUniqueFiber(updatedSuspended, toFiber);
                _currentFiber = null;
                break;
            case FiberState.Dead:
            default:
                break;
        }

        _runnableFibers = updatedRunnable;
        _idleFibers = updatedIdle;
        _suspendedFibers = updatedSuspended;

        _fibersById[toFiber.Id] = toFiber;

        if (toFiber.IsRootFiber())
        {
            _rootFiber = toFiber;
        }
    }

    /// <summary>
    /// Marks a fiber as dead and removes it from scheduling queues.
    /// </summary>
    public void FinalizeFiber(Fiber fiber)
    {
        if (fiber == null || fiber.IsRootFiber())
        {
            return;
        }

        fiber.SetState(FiberState.Dead);
        var (updatedRunnable, updatedIdle, updatedSuspended) = ExcludeFibers(new[] { fiber.Id });
        _runnableFibers = updatedRunnable;
        _idleFibers = updatedIdle;
        _suspendedFibers = updatedSuspended;

        if (_currentFiber?.Id == fiber.Id)
        {
            _currentFiber = null;
        }
    }

    /// <summary>
    /// Batch updates the state of multiple fibers.
    /// </summary>
    public void BatchUpdateFiberState(IEnumerable<int> fiberIds, FiberState targetState)
    {
        var idList = fiberIds?.ToList() ?? new List<int>();
        var fibers = GetFiberByIds(idList);
        var (updatedRunnable, updatedIdle, updatedSuspended) = ExcludeFibers(idList);

        foreach (var fiber in fibers.Values)
        {
            fiber.SetState(targetState);

            switch (targetState)
            {
                case FiberState.Runnable:
                case FiberState.Running:
                    AddUniqueFiber(updatedRunnable, fiber, targetState == FiberState.Running);
                    break;
                case FiberState.Idle:
                    AddUniqueFiber(updatedIdle, fiber);
                    break;
                case FiberState.Suspended:
                    AddUniqueFiber(updatedSuspended, fiber);
                    break;
                case FiberState.Dead:
                default:
                    break;
            }
        }

        _runnableFibers = updatedRunnable;
        _idleFibers = updatedIdle;
        _suspendedFibers = updatedSuspended;
    }

    /// <summary>
    /// Adds a resume token to the queue.
    /// </summary>
    public void AddResumeFiberEvent(ResumeFiberToken resumeToken)
    {
        if (resumeToken == null)
        {
            throw new ArgumentNullException(nameof(resumeToken));
        }

        _resumeEventQueue.Enqueue(resumeToken);
    }

    /// <summary>
    /// Asynchronously waits for a resume token and resumes the corresponding fiber.
    /// </summary>
    public async Task WaitAndConsumeResumeTokenAsync(
        int maxSleepSeconds = 30,
        int sleepTimeInMillis = 200,
        CancellationToken cancellationToken = default)
    {
        if (maxSleepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSleepSeconds));
        }

        if (sleepTimeInMillis <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sleepTimeInMillis));
        }

        var maxSleepCount = (1000 * maxSleepSeconds) / sleepTimeInMillis;
        var sleepCount = 1;

        while (sleepCount < maxSleepCount && _resumeEventQueue.Count == 0)
        {
            await Task.Delay(sleepTimeInMillis, cancellationToken).ConfigureAwait(false);
            sleepCount += 1;
        }

        if (_resumeEventQueue.Count == 0)
        {
            throw new InvalidOperationException("sleep exceed max time");
        }

        var resumeFiberToken = _resumeEventQueue.Dequeue();
        ProcessResumeFiberToken(resumeFiberToken);
    }

    /// <summary>
    /// Immediately consumes a resume token if available.
    /// </summary>
    public void WaitAndConsumeResumeTokenSync()
    {
        if (_resumeEventQueue.Count == 0)
        {
            return;
        }

        var resumeFiberToken = _resumeEventQueue.Dequeue();
        ProcessResumeFiberToken(resumeFiberToken);
    }

    /// <summary>
    /// Returns the runnable fibers list.
    /// </summary>
    public List<Fiber> GetRunnableFibers() => _runnableFibers;

    /// <summary>
    /// Returns all fibers regardless of their state.
    /// </summary>
    public List<Fiber> GetAllFibers()
    {
        var result = new List<Fiber>();
        var seen = new HashSet<int>();

        void AddRange(IEnumerable<Fiber> fibers)
        {
            foreach (var fiber in fibers)
            {
                if (seen.Add(fiber.Id))
                {
                    result.Add(fiber);
                }
            }
        }

        AddRange(_runnableFibers);
        AddRange(_idleFibers);
        AddRange(_suspendedFibers);

        return result;
    }

    /// <summary>
    /// Retrieves the root fiber.
    /// </summary>
    public Fiber? GetRootFiber()
    {
        if (_rootFiber != null)
        {
            return _rootFiber;
        }

        foreach (var fiber in GetAllFibers())
        {
            if (fiber.IsRootFiber())
            {
                _rootFiber = fiber;
                return fiber;
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the currently running fiber.
    /// </summary>
    public Fiber? GetCurrentFiber()
    {
        if (_currentFiber != null && _currentFiber.GetState() == FiberState.Running)
        {
            return _currentFiber;
        }

        if (_runnableFibers.Count > 0)
        {
            var candidate = _runnableFibers[0];
            if (candidate.GetState() == FiberState.Running)
            {
                _currentFiber = candidate;
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Explicitly sets the current fiber to the provided instance.
    /// </summary>
    public void SetCurrentFiber(Fiber? fiber)
    {
        _currentFiber = fiber;

        if (fiber == null)
        {
            return;
        }

        AddFiber(fiber);
        SwitchFiberState(fiber, FiberState.Running, null);
    }

    /// <summary>
    /// Retrieves the next fiber that should execute.
    /// </summary>
    public Fiber? GetNextActiveFiber()
    {
        if (_runnableFibers.Count == 0)
        {
            return null;
        }

        var currentFiber = GetCurrentFiber();

        if (currentFiber != null)
        {
            var runnableExcludeRoot = new List<Fiber>();

            foreach (var fiber in _runnableFibers)
            {
                if (!fiber.IsRootFiber())
                {
                    runnableExcludeRoot.Add(fiber);
                }
            }

            var runnableCountExcludeRoot = runnableExcludeRoot.Count;
            var idleCount = _idleFibers.Count;
            var suspendedCount = _suspendedFibers.Count;

            if (currentFiber.IsRootFiber())
            {
                var topInstruction = currentFiber.InstructionStack.PeekTop();
                var rootShouldContinue = topInstruction.OpCode != OpCode.OpStack_LandSuccess;
                var noOtherFibers = runnableCountExcludeRoot == 0 && idleCount == 0 && suspendedCount == 0;

                if (rootShouldContinue || noOtherFibers)
                {
                    return currentFiber;
                }

                currentFiber.SetState(FiberState.Runnable);

                Fiber? nextFiber = null;

                if (runnableCountExcludeRoot > 0)
                {
                    nextFiber = runnableExcludeRoot[0];
                    nextFiber.SetState(FiberState.Running);
                    _currentFiber = nextFiber;
                }
                else
                {
                    _currentFiber = null;
                }

                runnableExcludeRoot.Add(currentFiber);
                _runnableFibers = runnableExcludeRoot;

                return nextFiber;
            }

            return currentFiber;
        }
        else
        {
            var nextFiber = _runnableFibers[0];
            nextFiber.SetState(FiberState.Running);
            _currentFiber = nextFiber;
            return nextFiber;
        }
    }

    /// <summary>
    /// Resets all fiber state within the manager.
    /// </summary>
    public void ResetAllState()
    {
        _runnableFibers = new List<Fiber>();
        _idleFibers = new List<Fiber>();
        _suspendedFibers = new List<Fiber>();
        _resumeEventQueue.Clear();
        _fibersById.Clear();
        _currentFiber = null;
        _rootFiber = null;
        _opBatchStack.Clear();
    }

    /// <summary>
    /// Starts a new operation batch on the current fiber.
    /// </summary>
    public void OpBatchStart()
    {
        _opBatchStack.Add(new List<Instruction>());
    }

    /// <summary>
    /// Adds an operation to the current batch or directly to the instruction stack.
    /// </summary>
    public void AddOp(string opCode, object? memo = null)
    {
        var currentFiber = GetRequiredCurrentFiber();
        var instruction = new Instruction(opCode, currentFiber.CurrentEnvId, memo);

        if (_opBatchStack.Count == 0)
        {
            currentFiber.InstructionStack.PushValue(instruction);
        }
        else
        {
            _opBatchStack[^1].Add(instruction);
        }
    }

    /// <summary>
    /// Commits the current operation batch.
    /// </summary>
    public void OpBatchCommit()
    {
        var currentFiber = GetRequiredCurrentFiber();

        if (_opBatchStack.Count == 0)
        {
            return;
        }

        var opList = _opBatchStack[^1];
        _opBatchStack.RemoveAt(_opBatchStack.Count - 1);

        if (_opBatchStack.Count > 0 && opList.Count > 0)
        {
            _opBatchStack[^1].AddRange(opList);
        }
        else if (opList.Count > 0)
        {
            currentFiber.InstructionStack.ReversePushItems(opList);
        }
    }

    /// <summary>
    /// Adds an instruction directly to the current fiber.
    /// </summary>
    public void AddOpDirectly(string opCode, object? memo = null)
    {
        GetRequiredCurrentFiber().AddOpDirectly(opCode, memo);
    }

    /// <summary>
    /// Ensures there is a current fiber and returns it.
    /// </summary>
    private Fiber GetRequiredCurrentFiber()
    {
        var fiber = GetCurrentFiber();

        if (fiber == null)
        {
            throw new InvalidOperationException("No current fiber is available.");
        }

        return fiber;
    }

    /// <summary>
    /// Removes a fiber from all scheduling lists.
    /// </summary>
    private void RemoveFiberFromAllLists(int fiberId)
    {
        _runnableFibers = _runnableFibers.Where(f => f.Id != fiberId).ToList();
        _idleFibers = _idleFibers.Where(f => f.Id != fiberId).ToList();
        _suspendedFibers = _suspendedFibers.Where(f => f.Id != fiberId).ToList();
    }

    /// <summary>
    /// Adds a fiber to a list, replacing any previous instance.
    /// </summary>
    private static void AddUniqueFiber(List<Fiber> fibers, Fiber fiber, bool insertAtFront = false)
    {
        for (var i = fibers.Count - 1; i >= 0; i--)
        {
            if (fibers[i].Id == fiber.Id)
            {
                fibers.RemoveAt(i);
            }
        }

        if (insertAtFront)
        {
            fibers.Insert(0, fiber);
        }
        else
        {
            fibers.Add(fiber);
        }
    }

    /// <summary>
    /// Removes the specified fibers from all scheduling lists and returns the updated lists.
    /// </summary>
    private (List<Fiber> Runnable, List<Fiber> Idle, List<Fiber> Suspended) ExcludeFibers(IEnumerable<int> excludeFiberIds)
    {
        var excludeSet = new HashSet<int>(excludeFiberIds);

        var updatedRunnable = _runnableFibers.Where(f => !excludeSet.Contains(f.Id)).ToList();
        var updatedIdle = _idleFibers.Where(f => !excludeSet.Contains(f.Id)).ToList();
        var updatedSuspended = _suspendedFibers.Where(f => !excludeSet.Contains(f.Id)).ToList();

        return (updatedRunnable, updatedIdle, updatedSuspended);
    }

    /// <summary>
    /// Applies the resume token to its target fiber.
    /// </summary>
    private void ProcessResumeFiberToken(ResumeFiberToken resumeFiberToken)
    {
        var fiber = GetFiberById(resumeFiberToken.FiberId)
                    ?? throw new InvalidOperationException($"Fiber {resumeFiberToken.FiberId} not found.");

        SwitchFiber(fiber.Id, FiberState.Idle);

        if (resumeFiberToken.Result.Count > 0)
        {
            fiber.OperandStack.PushItems(resumeFiberToken.Result);
        }

        if (resumeFiberToken.BeforeResumeOps.Count > 0)
        {
            fiber.InstructionStack.ReversePushItems(resumeFiberToken.BeforeResumeOps);
        }
    }

    /// <summary>
    /// Marks a fiber as runnable.
    /// </summary>
    public bool MakeFiberRunnable(int fiberId)
    {
        var fiber = GetFiberById(fiberId);

        if (fiber == null)
        {
            return false;
        }

        RemoveFiberFromAllLists(fiberId);
        fiber.SetState(FiberState.Runnable);
        AddUniqueFiber(_runnableFibers, fiber);
        return true;
    }
}
