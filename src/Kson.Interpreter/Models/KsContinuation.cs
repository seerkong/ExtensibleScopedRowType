using Kson.Core.Node;
using Kson.Interpreter.Runtime;

public class KsContinuation : KsValueNode
{
    public int CurrentEnvId { get; set; } = 0;
    public StackMachine<KsNode> OperandStackBackup { get; set; }
    public StackMachine<Instruction> InstructionStackBackup { get; set; }
}