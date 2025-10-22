using Kson.Core.Node;
using Kson.Interpreter.Models;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Registration methods for conversion functions
/// </summary>
public static partial class ConversionFunctions
{
    /// <summary>
    /// Registers conversion functions with an environment
    /// </summary>
    /// <param name="env">The environment to register with</param>
    public static void Register(Runtime.Env env)
    {
        KsHostFunction equals = new KsHostFunction(Models.HostFunction.FromObjectDelegate("Equals", ConversionFunctions.Equals));
        env.Define("Equals", equals);
        env.Define("==", equals);

        // Register conversion operations
        env.Define("ToString", new KsHostFunction(Models.HostFunction.FromObjectDelegate("ToString", ToString)));
        env.Define("ToInt", new KsHostFunction(Models.HostFunction.FromObjectDelegate("ToInt", ToInt)));
        env.Define("ToFloat", new KsHostFunction(Models.HostFunction.FromObjectDelegate("ToFloat", ToFloat)));
        env.Define("ToBoolean", new KsHostFunction(Models.HostFunction.FromObjectDelegate("ToBoolean", ToBoolean)));
    }
}