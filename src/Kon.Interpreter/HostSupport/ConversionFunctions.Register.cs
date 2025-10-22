using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.HostSupport;

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
        KnHostFunction equals = new KnHostFunction(Models.HostFunction.FromObjectDelegate("Equals", ConversionFunctions.Equals));
        env.Define("Equals", equals);
        env.Define("==", equals);

        // Register conversion operations
        env.Define("ToString", new KnHostFunction(Models.HostFunction.FromObjectDelegate("ToString", ToString)));
        env.Define("ToInt", new KnHostFunction(Models.HostFunction.FromObjectDelegate("ToInt", ToInt)));
        env.Define("ToFloat", new KnHostFunction(Models.HostFunction.FromObjectDelegate("ToFloat", ToFloat)));
        env.Define("ToBoolean", new KnHostFunction(Models.HostFunction.FromObjectDelegate("ToBoolean", ToBoolean)));
    }
}