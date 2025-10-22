using Kon.Core.Node;
using Kon.Interpreter.Models;

namespace Kon.Interpreter.HostSupport;

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
        var add = new KnHostFunction(Models.HostFunction.FromObjectDelegate("Add", Add));
        var substract = new KnHostFunction(Models.HostFunction.FromObjectDelegate("Subtract", Subtract));
        var multiply = new KnHostFunction(Models.HostFunction.FromObjectDelegate("Multiply", Multiply));
        var divide = new KnHostFunction(Models.HostFunction.FromObjectDelegate("Divide", Divide));
        var biggerThan = new KnHostFunction(Models.HostFunction.FromObjectDelegate("BiggerThan", BiggerThan));
        var biggerThanOrEqual = new KnHostFunction(Models.HostFunction.FromObjectDelegate("BiggerThanOrEqual", BiggerThanOrEqual));
        var lowerThan = new KnHostFunction(Models.HostFunction.FromObjectDelegate("LowerThan", LowerThan));
        var lowerThanOrEqual = new KnHostFunction(Models.HostFunction.FromObjectDelegate("LowerThanOrEqual", LowerThanOrEqual));

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