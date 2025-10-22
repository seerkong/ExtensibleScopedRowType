using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class FiberScheduleHandler
{
    public static void RunCurrentFiberToIdle(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        runtime.FiberMgr.CurrentFiberToIdle();
    }

    public static void RunCurrentFiberToSuspended(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        runtime.FiberMgr.SuspendCurrentFiber();
    }

    public static void RunAwakenMultiFibers(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        List<int> fiberIds = opContState.Memo as List<int>;
        runtime.FiberMgr.BatchUpdateFiberState(fiberIds, FiberState.Runnable);
    }

    // memo 需要有 ChangeFiberToState 字段
    public static void RunYieldToParentAndChangeCurrentFiberState(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        YieldToFiberMemo memo = opContState.Memo as YieldToFiberMemo;
        FiberState currentToState = memo.ChangeFiberToState;

        Fiber currentFiber = runtime.GetCurrentFiber();
        int parentFiberId = currentFiber.GetParentFiberId();
        // 挂起当前流程，继续执行父fiber流程
        runtime.FiberMgr.SwitchFiber(parentFiberId, currentToState);
    }

    public class YieldToFiberMemo
    {
        public FiberState ChangeFiberToState;
        public int? YieldToFiberId;
    }

    // memo 需要有 YieldToFiberId , ChangeCurrentFiberToState 字段
    // 会将当前 operand stack中的value, push到目标 fiber的 operand stack中
    public static void RunYieldToFiberAndChangeCurrentFiberState(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        YieldToFiberMemo memo = opContState.Memo as YieldToFiberMemo;
        FiberState toState = memo.ChangeFiberToState;
        int toFiberId = (int)memo.YieldToFiberId;
        // 原fiber 的传参
        List<KsNode> stackValues = runtime.GetCurrentFiber().OperandStack.PopFrameAllValues();

        // 挂起当前流程，继续执行父fiber流程
        runtime.FiberMgr.SwitchFiber(toFiberId, toState);
        // 传参给新fiber
        runtime.GetCurrentFiber().OperandStack.PushItems(stackValues);
    }

    public static void RunFinalizeFiber(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        Fiber currentFiber = runtime.GetCurrentFiber();
        // 挂起当前流程，由调度算法决定下一个指令执行哪一个fiber
        runtime.FiberMgr.SwitchFiberState(currentFiber, FiberState.Dead, null);
    }
}