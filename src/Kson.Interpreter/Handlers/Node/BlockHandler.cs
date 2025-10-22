using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public static class BlockHandler
{
    public static void RunBlock(KsonInterpreterRuntime runtime, Instruction instruction)
    {
        var body = (KsArray)instruction.Memo;
        runtime.OpBatchStart();
        _RunBlock(runtime, body);
        runtime.OpBatchCommit();
    }

    /// <summary>
    /// Runs a block of code
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="body">The body of the block</param>
    public static void _RunBlock(KsonInterpreterRuntime runtime, KsArray body)
    {
        for (var i = 0; i < body.Size(); i++)
        {
            runtime.AddOp(KsonOpCode.Node_RunNode, body[i]);

            // Pop the value from the stack for all but the last statement
            if (i < body.Size() - 1)
            {
                runtime.AddOp(KsonOpCode.ValStack_PopValue, null, "body index:" + i);
            }
        }
    }
}