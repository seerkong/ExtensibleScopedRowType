using Kson.Core.Node;
using Kson.Interpreter.Models;

namespace Kson.Interpreter.HostSupport;

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
        env.Define("Concat", new KsHostFunction(HostFunction.FromObjectDelegate("Concat", Concat)));
        env.Define("StringLength", new KsHostFunction(HostFunction.FromObjectDelegate("StringLength", Length)));
        env.Define("ToUpper", new KsHostFunction(HostFunction.FromObjectDelegate("ToUpper", ToUpper)));
        env.Define("ToLower", new KsHostFunction(HostFunction.FromObjectDelegate("ToLower", ToLower)));
        env.Define("Trim", new KsHostFunction(HostFunction.FromObjectDelegate("Trim", Trim)));
    }
}