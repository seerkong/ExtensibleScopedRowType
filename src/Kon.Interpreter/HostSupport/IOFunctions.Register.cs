using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.HostSupport;

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
        env.Define("Write", new KnHostFunction(Models.HostFunction.FromObjectDelegate("Write", Write)));
        env.Define("WriteLine", new KnHostFunction(Models.HostFunction.FromObjectDelegate("WriteLine", WriteLine)));
        env.Define("ReadLine", new KnHostFunction(Models.HostFunction.FromObjectDelegate("ReadLine", ReadLine)));
    }
}