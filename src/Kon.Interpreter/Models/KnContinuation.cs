using Kon.Core.Node;
using Kon.Interpreter.Runtime;

public class KnContinuation : KnValueNode
{
    public int CurrentEnvId { get; set; } = 0;
    public StackMachine<KnNode> OperandStackBackup { get; set; }
    public StackMachine<Instruction> InstructionStackBackup { get; set; }
}