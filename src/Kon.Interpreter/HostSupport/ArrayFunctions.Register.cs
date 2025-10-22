using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.HostSupport;

public static partial class ArrayFunctions
{
    public static void Register(Runtime.Env env)
    {
        // Register string operations
        env.Define("ArrayLength", new KnHostFunction(HostFunction.FromObjectDelegate(
            "ArrayLength", ArrayLength)
            ));
    }
}