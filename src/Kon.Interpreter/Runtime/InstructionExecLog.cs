using System;
using System.Collections.Generic;
using Kon.Core.Node;

namespace Kon.Interpreter.Runtime;

/// <summary>
/// Log entry for instruction execution
/// </summary>
public class InstructionExecLog
{
  /// <summary>
  /// The ID of the fiber that executed the instruction
  /// </summary>
  public int FiberId { get; }

  /// <summary>
  /// The instruction that was executed
  /// </summary>
  public Instruction Instruction { get; }

  /// <summary>
  /// Creates a new instruction execution log
  /// </summary>
  /// <param name="fiberId">The fiber ID</param>
  /// <param name="instruction">The instruction</param>
  public InstructionExecLog(int fiberId, Instruction instruction)
  {
    FiberId = fiberId;
    Instruction = instruction;
  }
}