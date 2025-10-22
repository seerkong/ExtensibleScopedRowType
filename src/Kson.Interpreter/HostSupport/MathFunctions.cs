using Kson.Core.Node;

namespace Kson.Interpreter.HostSupport;

/// <summary>
/// Provides math functions to the interpreter
/// </summary>
public static partial class MathFunctions
{
    /// <summary>
    /// Adds two numbers
    /// </summary>
    /// <param name="args">The numbers to add</param>
    /// <returns>The sum of the numbers</returns>
    public static KsNode Add(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KsNull.Null;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return new KsInt64(int1.Value + int2.Value);
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return new KsDouble(double1.Value + double2.Value);
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return new KsDouble(d1.Value + i2.Value);
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return new KsDouble(i1.Value + d2.Value);
        }

        // Default fallback
        return KsNull.Null;
    }

    /// <summary>
    /// Subtracts one number from another
    /// </summary>
    /// <param name="args">The numbers to subtract</param>
    /// <returns>The difference of the numbers</returns>
    public static KsNode Subtract(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            if (args.Length == 1)
            {
                // Unary minus
                if (args[0] is KsInt64 intVal)
                {
                    return new KsInt64(-intVal.Value);
                }

                if (args[0] is KsDouble doubleVal)
                {
                    return new KsDouble(-doubleVal.Value);
                }
            }

            return KsNull.Null;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return new KsInt64(int1.Value - int2.Value);
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return new KsDouble(double1.Value - double2.Value);
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return new KsDouble(d1.Value - i2.Value);
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return new KsDouble(i1.Value - d2.Value);
        }
        // Default fallback
        return KsNull.Null;
    }

    /// <summary>
    /// Multiplies two numbers
    /// </summary>
    /// <param name="args">The numbers to multiply</param>
    /// <returns>The product of the numbers</returns>
    public static KsNode Multiply(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KsNull.Null;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return new KsInt64(int1.Value * int2.Value);
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return new KsDouble(double1.Value * double2.Value);
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return new KsDouble(d1.Value * i2.Value);
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return new KsDouble(i1.Value * d2.Value);
        }
        // Default fallback
        return KsNull.Null;
    }

    /// <summary>
    /// Divides one number by another
    /// </summary>
    /// <param name="args">The numbers to divide</param>
    /// <returns>The quotient of the numbers</returns>
    public static KsNode Divide(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KsNull.Null;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            if (int2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KsInt64(int1.Value / int2.Value);
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            if (double2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KsDouble(double1.Value / double2.Value);
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            if (i2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KsDouble(d1.Value / i2.Value);
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            if (d2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KsDouble(i1.Value / d2.Value);
        }

        // Default fallback
        return KsNull.Null;
    }

    public static KsNode BiggerThan(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return KsBoolean.False;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return (int1.Value > int2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return (double1.Value > double2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return (d1.Value > i2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return (i1.Value > d2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        // Default fallback
        return KsBoolean.False;
    }

    public static KsNode BiggerThanOrEqual(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return KsBoolean.False;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return (int1.Value >= int2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return (double1.Value >= double2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return (d1.Value >= i2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return (i1.Value >= d2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        // Default fallback
        return KsBoolean.False;
    }

    public static KsNode LowerThan(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return KsBoolean.False;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return (int1.Value < int2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return (double1.Value < double2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return (d1.Value < i2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return (i1.Value < d2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        // Default fallback
        return KsBoolean.False;
    }

    public static KsNode LowerThanOrEqual(params KsNode[] args)
    {
        if (args.Length < 2)
        {
            return KsBoolean.False;
        }

        // Handle KsNode types
        if (args[0] is KsInt64 int1 && args[1] is KsInt64 int2)
        {
            return (int1.Value <= int2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble double1 && args[1] is KsDouble double2)
        {
            return (double1.Value <= double2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsDouble d1 && args[1] is KsInt64 i2)
        {
            return (d1.Value <= i2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        if (args[0] is KsInt64 i1 && args[1] is KsDouble d2)
        {
            return (i1.Value <= d2.Value) ? KsBoolean.True : KsBoolean.False;
        }

        // Default fallback
        return KsBoolean.False;
    }
}