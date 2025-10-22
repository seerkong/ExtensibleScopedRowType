using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Registration methods for string functions
/// </summary>
public static partial class StringFunctions
{
    /// <summary>
    /// Registers string functions with an environment
    /// </summary>
    /// <param name="env">The environment to register with</param>
    public static void Register(Runtime.Env env)
    {
        // Register string operations
        env.Define("Concat", new KnHostFunction(HostFunction.FromObjectDelegate("Concat", Concat)));
        env.Define("StringLength", new KnHostFunction(HostFunction.FromObjectDelegate("StringLength", Length)));
        env.Define("ToUpper", new KnHostFunction(HostFunction.FromObjectDelegate("ToUpper", ToUpper)));
        env.Define("ToLower", new KnHostFunction(HostFunction.FromObjectDelegate("ToLower", ToLower)));
        env.Define("Trim", new KnHostFunction(HostFunction.FromObjectDelegate("Trim", Trim)));
    }
}