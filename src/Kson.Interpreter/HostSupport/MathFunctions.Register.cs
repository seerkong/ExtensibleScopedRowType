using Kson.Core.Node;
using Kson.Interpreter.Models;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Registration methods for math functions
/// </summary>
public static partial class MathFunctions
{
    /// <summary>
    /// Registers math functions with an environment
    /// </summary>
    /// <param name="env">The environment to register with</param>
    public static void Register(Runtime.Env env)
    {
        // Register basic math operations
        var add = new KsHostFunction(Models.HostFunction.FromObjectDelegate("Add", Add));
        var substract = new KsHostFunction(Models.HostFunction.FromObjectDelegate("Subtract", Subtract));
        var multiply = new KsHostFunction(Models.HostFunction.FromObjectDelegate("Multiply", Multiply));
        var divide = new KsHostFunction(Models.HostFunction.FromObjectDelegate("Divide", Divide));
        var biggerThan = new KsHostFunction(Models.HostFunction.FromObjectDelegate("BiggerThan", BiggerThan));
        var biggerThanOrEqual = new KsHostFunction(Models.HostFunction.FromObjectDelegate("BiggerThanOrEqual", BiggerThanOrEqual));
        var lowerThan = new KsHostFunction(Models.HostFunction.FromObjectDelegate("LowerThan", LowerThan));
        var lowerThanOrEqual = new KsHostFunction(Models.HostFunction.FromObjectDelegate("LowerThanOrEqual", LowerThanOrEqual));

        env.Define("+", add);
        env.Define("-", substract);
        env.Define("*", multiply);
        env.Define("/", divide);
        env.Define(">", biggerThan);
        env.Define(">=", biggerThanOrEqual);
        env.Define("<", lowerThan);
        env.Define("<=", lowerThanOrEqual);
    }
}