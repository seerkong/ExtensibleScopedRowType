using Kson.Core.Node;
using Kson.Interpreter.Models;

namespace Kson.Interpreter.HostSupport;

public static partial class ArrayFunctions
{
    public static void Register(Runtime.Env env)
    {
        // Register string operations
        env.Define("ArrayLength", new KsHostFunction(HostFunction.FromObjectDelegate(
            "ArrayLength", ArrayLength)
            ));
    }
}