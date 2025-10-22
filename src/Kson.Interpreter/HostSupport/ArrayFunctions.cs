using Kson.Core.Node;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Provides string functions to the interpreter
/// </summary>
public static partial class ArrayFunctions
{
    public static KsNode ArrayLength(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsInt64(0);
        }
        KsArray arr = args[0] as KsArray;
        return new KsInt64(arr.GetItems().Count);
    }
}