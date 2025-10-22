using Kon.Core.Node;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Provides IO functions to the interpreter
/// </summary>
public static partial class IOFunctions
{

    /// <summary>
    /// Prints a value
    /// </summary>
    /// <param name="args">The value to print</param>
    /// <returns>The value that was printed</returns>
    public static KnNode Write(params KnNode[] args)
    {
        var text = args.Length > 0 ? args[0]?.ToString() ?? "null" : string.Empty;
        Console.Write(text);
        return args.Length > 0 ? args[0] : KnNull.Null;
    }

    /// <summary>
    /// Prints a value followed by a newline
    /// </summary>
    /// <param name="args">The value to print</param>
    /// <returns>The value that was printed</returns>
    public static KnNode WriteLine(params KnNode[] args)
    {
        var text = args.Length > 0 ? args[0]?.ToString() ?? "null" : string.Empty;
        Console.WriteLine(text);
        return args.Length > 0 ? args[0] : KnNull.Null;
    }

    /// <summary>
    /// Reads a line of text from the input
    /// </summary>
    /// <param name="args">Unused</param>
    /// <returns>The line of text</returns>
    public static KnNode ReadLine(params KnNode[] args)
    {
        var line = Console.ReadLine();
        return new KnString(line ?? string.Empty);
    }
}