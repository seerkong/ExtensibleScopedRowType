namespace Kon.Interpreter.Runtime;

/// <summary>
/// Represents an instruction in the instruction stack
/// </summary>
public class Instruction
{
    /// <summary>
    /// The operation code for this instruction
    /// </summary>
    public string OpCode { get; }

    /// <summary>
    /// The environment ID for this instruction
    /// </summary>
    public int EnvId { get; }

    /// <summary>
    /// Additional data for this instruction
    /// </summary>
    public object? Memo { get; }
    public string Comment { get; }

    /// <summary>
    /// Creates a new instruction
    /// </summary>
    /// <param name="opCode">The operation code</param>
    /// <param name="envId">The environment ID</param>
    /// <param name="memo">Additional data</param>
    public Instruction(string opCode, int envId, object? memo = null, string comment = "")
    {
        OpCode = opCode;
        EnvId = envId;
        Memo = memo;
        Comment = comment;
    }

    /// <summary>
    /// Returns a string representation of the instruction
    /// </summary>
    public override string ToString()
    {
        return $"Instruction({OpCode}, EnvId={EnvId}, Memo={(Memo?.ToString() ?? "null")})";
    }
}