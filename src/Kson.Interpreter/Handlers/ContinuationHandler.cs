using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class ContinuationHandler
{
    public static void RunMakeContExcludeTopNInstruction(KsonInterpreterRuntime runtime, Instruction opContState)
    {
        int excludeN = (opContState.Memo == null) ? 0 : (int)opContState.Memo;
        KsContinuation cont = ContinuationHandler.MakeContinuation(runtime);
        // List<Instruction> instructionsBackup = cont.InstructionStackBackup.Items; // new List<Instruction>(cont.InstructionStackBackup.FrameStackView);
        // if (excludeN > 0)
        // {
        //     for (int i = 0; i < excludeN; i++)
        //     {
        //         instructionsBackup.RemoveAt(instructionsBackup.Count - 1);
        //     }
        // }
        int actualInstructionSize = runtime.InstructionStack.Count;
        List<Instruction> instructionsBackup = cont.InstructionStackBackup.Items
        .GetRange(0, actualInstructionSize - excludeN);
        cont.InstructionStackBackup.Items = instructionsBackup;
        cont.InstructionStackBackup.StackTop = instructionsBackup.Count - 1;
        runtime.GetCurrentFiber().OperandStack.PushValue(cont);
    }

    public static KsContinuation MakeContinuation(KsonInterpreterRuntime runtime)
    {
        Fiber fiber = runtime.GetCurrentFiber();
        OperandStack operandStack = fiber.OperandStack;
        StackMachine<KsNode> operandStackBackup = operandStack.Copy();
        InstructionStack instructionStack = fiber.InstructionStack;
        StackMachine<Instruction> instructionStackBackup = instructionStack.Copy();

        KsContinuation result = new KsContinuation();
        result.CurrentEnvId = fiber.CurrentEnvId;
        result.OperandStackBackup = operandStackBackup;
        result.InstructionStackBackup = instructionStackBackup;
        return result;
    }

    public static void RestoreContinuationAppendOperandStack(KsonInterpreterRuntime runtime, KsContinuation cont, List<KsNode> operands)
    {
        Fiber fiber = runtime.GetCurrentFiber();
        fiber.CurrentEnvId = cont.CurrentEnvId;
        fiber.InstructionStack.Restore(cont.InstructionStackBackup);
        fiber.OperandStack.Restore(cont.OperandStackBackup);
        for (int i = 0; i < operands.Count; i++)
        {
            fiber.OperandStack.PushValue(operands[i]);
        }
    }
}