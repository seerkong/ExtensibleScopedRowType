namespace Kon.Interpreter.Runtime;

/// <summary>
/// State of a fiber in the execution environment
/// </summary>
public enum FiberState
{
    /// <summary>
    /// Ready to be executed
    /// </summary>
    Runnable,

    /// <summary>
    /// Currently executing
    /// </summary>
    Running,

    /// <summary>
    /// Waiting for instructions
    /// </summary>
    Idle,

    /// <summary>
    /// Suspended, waiting to be awakened
    /// </summary>
    Suspended,

    /// <summary>
    /// Execution completed, ready to be garbage collected
    /// </summary>
    Dead
}