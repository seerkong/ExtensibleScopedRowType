using System.Threading.Tasks;
using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// The interpreter engine that executes Kon code
/// </summary>
public static class KonInterpreterEngine
{

    /// <summary>
    /// Starts the execution loop synchronously
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <returns>The result of execution</returns>
    public static KnNode StartLoopSync(this InterpreterRuntime runtime)
    {
        var instruction = runtime.GetCurrentFiber().InstructionStack.PopValue();
        var currentFiber = runtime.GetCurrentFiber();

        while (instruction.OpCode != OpCode.OpStack_LandSuccess &&
               instruction.OpCode != OpCode.OpStack_LandFail)
        {
            var handler = runtime.ExtensionRegistry.GetInstructionHandler(instruction.OpCode);
            try
            {
                handler(runtime, instruction);
            }
            catch (Exception e)
            {
                throw e;
            }

            var log = new InstructionExecLog(currentFiber.Id, instruction);
            runtime.InstructionHistory.Add(log);

            var nextRunFiber = runtime.FiberMgr.GetNextActiveFiber();
            if (nextRunFiber == null)
            {
                runtime.FiberMgr.WaitAndConsumeResumeTokenSync();
                nextRunFiber = runtime.FiberMgr.GetNextActiveFiber();
            }

            if (nextRunFiber == null)
            {
                break; // No more fibers to run
            }

            currentFiber = nextRunFiber;
            instruction = currentFiber.InstructionStack.PopValue();
        }
        var r = currentFiber.OperandStack.PeekBottomOfAllFrames();
        return r;
    }

    /// <summary>
    /// Starts the execution loop asynchronously
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <returns>The result of execution</returns>
    public static async Task<KnNode> StartLoopAsync(this InterpreterRuntime runtime)
    {
        var instruction = runtime.GetCurrentFiber().InstructionStack.PopValue();
        var currentFiber = runtime.GetCurrentFiber();

        while (instruction.OpCode != OpCode.OpStack_LandSuccess &&
               instruction.OpCode != OpCode.OpStack_LandFail)
        {
            var handler = runtime.ExtensionRegistry.GetInstructionHandler(instruction.OpCode);
            handler(runtime, instruction);

            var log = new InstructionExecLog(currentFiber.Id, instruction);
            runtime.InstructionHistory.Add(log);

            var nextRunFiber = runtime.FiberMgr.GetNextActiveFiber();
            if (nextRunFiber == null)
            {
                await Task.Yield(); // Simulate asynchronous wait
                runtime.FiberMgr.WaitAndConsumeResumeTokenSync();
                nextRunFiber = runtime.FiberMgr.GetNextActiveFiber();
            }

            currentFiber = nextRunFiber;
            instruction = currentFiber.InstructionStack.PopValue();
        }

        return currentFiber.OperandStack.PeekBottomOfAllFrames();
    }
}