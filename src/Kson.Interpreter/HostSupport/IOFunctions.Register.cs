using Kson.Core.Node;
using Kson.Interpreter.Models;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Registration methods for IO functions
/// </summary>
public static partial class IOFunctions
{
    /// <summary>
    /// Registers IO functions with an environment
    /// </summary>
    /// <param name="env">The environment to register with</param>
    public static void Register(Runtime.Env env)
    {
        // Register IO operations
        env.Define("Write", new KsHostFunction(Models.HostFunction.FromObjectDelegate("Write", Write)));
        env.Define("WriteLine", new KsHostFunction(Models.HostFunction.FromObjectDelegate("WriteLine", WriteLine)));
        env.Define("ReadLine", new KsHostFunction(Models.HostFunction.FromObjectDelegate("ReadLine", ReadLine)));
    }
}