using Kon.Core.Node;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Provides conversion functions to the interpreter
/// </summary>
public static partial class ConversionFunctions
{
    public static KnNode Equals(params KnNode[] args)
    {
        // TODO 实现两个KnNode的比较
        // 当前先使用简单粗暴的方法
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KnNull.Null;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return (int1.Value == int2.Value) ? KnBoolean.True : KnBoolean.False;
        }
        return KnBoolean.False;
    }

    /// <summary>
    /// Converts a value to a string
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The string representation of the value</returns>
    public static KnNode ToString(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnString(string.Empty);
        }

        if (args[0] is KnString str)
        {
            return str;
        }
        else if (args[0] == null)
        {
            return new KnString(args[0]?.ToString() ?? "null");
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
    public static KnNode ToInt(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnInt64(0);
        }

        if (args[0] is KnInt64 int64)
        {
            return int64;
        }

        if (args[0] is KnDouble double64)
        {
            return new KnInt64((long)double64.Value);
        }

        if (args[0] is KnString stringValue)
        {
            if (long.TryParse(stringValue.Value, out var result))
            {
                return new KnInt64(result);
            }
        }

        return new KnInt64(0);
    }

    /// <summary>
    /// Converts a value to a floating-point number
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The floating-point representation of the value</returns>
    public static KnNode ToFloat(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnDouble(0.0);
        }

        if (args[0] is KnDouble doubleNode)
        {
            return doubleNode;
        }

        if (args[0] is KnInt64 intNode)
        {
            return new KnDouble(intNode.Value);
        }

        if (args[0] is KnString stringValue)
        {
            if (double.TryParse(stringValue.Value, out var result))
            {
                return new KnDouble(result);
            }
        }
        return new KnDouble(0.0);
    }

    /// <summary>
    /// Converts a value to a boolean
    /// </summary>
    /// <param name="args">The value to convert</param>
    /// <returns>The boolean representation of the value</returns>
    public static KnNode ToBoolean(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return KnBoolean.False;
        }

        if (args[0] is KnBoolean boolNode)
        {
            return boolNode;
        }

        if (args[0] is KnInt64 intNode)
        {
            return intNode.Value != 0 ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble doubleNode)
        {
            return doubleNode.Value != 0 ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnString stringNode)
        {
            return !string.IsNullOrEmpty(stringNode.Value) ? KnBoolean.True : KnBoolean.False;
        }
        return args[0] != null ? KnBoolean.True : KnBoolean.False;
    }
}