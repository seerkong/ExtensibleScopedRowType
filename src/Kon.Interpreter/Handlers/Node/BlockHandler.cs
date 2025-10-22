using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public static class BlockHandler
{
    public static void RunBlock(InterpreterRuntime runtime, Instruction instruction)
    {
        var body = (KnArray)instruction.Memo;
        runtime.OpBatchStart();
        _RunBlock(runtime, body);
        runtime.OpBatchCommit();
    }

    /// <summary>
    /// Runs a block of code
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="body">The body of the block</param>
    public static void _RunBlock(InterpreterRuntime runtime, KnArray body)
    {
        for (var i = 0; i < body.Size(); i++)
        {
            runtime.AddOp(OpCode.Node_RunNode, body[i]);

            // Pop the value from the stack for all but the last statement
            if (i < body.Size() - 1)
            {
                runtime.AddOp(OpCode.ValStack_PopValue, null, "body index:" + i);
            }
        }
    }
}