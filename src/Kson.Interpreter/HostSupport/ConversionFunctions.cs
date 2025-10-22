using Kson.Core.Node;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Provides conversion functions to the interpreter
/// </summary>
public static partial class ConversionFunctions
{
    public static KsNode Equals(params KsNode[] args)
    {
        // TODO 实现两个KsNode的比较
        // 当前先使用简单粗暴的方法
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KsNull.Null;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return (int1.Value == int2.Value) ? KsBoolean.True : KsBoolean.False;
        }
        return KsBoolean.False;
    }

    /// <summary>
    /// Converts a value to a string
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The string representation of the value</returns>
    public static KsNode ToString(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsString(string.Empty);
        }

        if (args[0] is KsString str)
        {
            return str;
        }
        else if (args[0] == null)
        {
            return new KsString(args[0]?.ToString() ?? "null");
        }
        else
        {
            throw new NotImplementedException();
        }

    }

    /// <summary>
    /// Converts a value to an integer
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The integer representation of the value</returns>
    public static KsNode ToInt(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsInt64(0);
        }

        if (args[0] is KsInt64 int64)
        {
            return int64;
        }

        if (args[0] is KsDouble double64)
        {
            return new KsInt64((long)double64.Value);
        }

        if (args[0] is KsString stringValue)
        {
            if (long.TryParse(stringValue.Value, out var result))
            {
                return new KsInt64(result);
            }
        }

        return new KsInt64(0);
    }

    /// <summary>
    /// Converts a value to a floating-point number
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The floating-point representation of the value</returns>
    public static KsNode ToFloat(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsDouble(0.0);
        }

        if (args[0] is KsDouble doubleNode)
        {
            return doubleNode;
        }

        if (args[0] is KsInt64 intNode)
        {
            return new KsDouble(intNode.Value);
        }

        if (args[0] is KsString stringValue)
        {
            if (double.TryParse(stringValue.Value, out var result))
            {
                return new KsDouble(result);
            }
        }
        return new KsDouble(0.0);
    }

    /// <summary>
    /// Converts a value to a boolean
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The boolean representation of the value</returns>
    public static KsNode ToBoolean(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return KsBoolean.False;
        }

        if (args[0] is KsBoolean boolNode)
        {
            return boolNode;
        }

        if (args[0] is KsInt64 intNode)
        {
            return intNode.Value != 0 ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble doubleNode)
        {
            return doubleNode.Value != 0 ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsString stringNode)
        {
            return !string.IsNullOrEmpty(stringNode.Value) ? KsBoolean.True : KsBoolean.False;
        }
        return args[0] != null ? KsBoolean.True : KsBoolean.False;
    }
}