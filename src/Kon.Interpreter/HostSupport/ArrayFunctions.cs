using Kon.Core.Node;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Provides string functions to the interpreter
/// </summary>
public static partial class ArrayFunctions
{
    public static KnNode ArrayLength(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnInt64(0);
        }
        KnArray arr = args[0] as KnArray;
        return new KnInt64(arr.GetItems().Count);
    }
}