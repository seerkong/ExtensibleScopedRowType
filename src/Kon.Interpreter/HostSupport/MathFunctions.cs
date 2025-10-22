using Kon.Core.Node;

namespace Kon.Interpreter.HostSupport;

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
    public static KnNode Add(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KnNull.Null;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return new KnInt64(int1.Value + int2.Value);
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return new KnDouble(double1.Value + double2.Value);
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return new KnDouble(d1.Value + i2.Value);
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return new KnDouble(i1.Value + d2.Value);
        }

        // Default fallback
        return KnNull.Null;
    }

    /// <summary>
    /// Subtracts one number from another
    /// </summary>
    /// <param name="args">The numbers to subtract</param>
    /// <returns>The difference of the numbers</returns>
    public static KnNode Subtract(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            if (args.Length == 1)
            {
                // Unary minus
                if (args[0] is KnInt64 intVal)
                {
                    return new KnInt64(-intVal.Value);
                }

                if (args[0] is KnDouble doubleVal)
                {
                    return new KnDouble(-doubleVal.Value);
                }
            }

            return KnNull.Null;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return new KnInt64(int1.Value - int2.Value);
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return new KnDouble(double1.Value - double2.Value);
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return new KnDouble(d1.Value - i2.Value);
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return new KnDouble(i1.Value - d2.Value);
        }
        // Default fallback
        return KnNull.Null;
    }

    /// <summary>
    /// Multiplies two numbers
    /// </summary>
    /// <param name="args">The numbers to multiply</param>
    /// <returns>The product of the numbers</returns>
    public static KnNode Multiply(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KnNull.Null;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return new KnInt64(int1.Value * int2.Value);
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return new KnDouble(double1.Value * double2.Value);
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return new KnDouble(d1.Value * i2.Value);
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return new KnDouble(i1.Value * d2.Value);
        }
        // Default fallback
        return KnNull.Null;
    }

    /// <summary>
    /// Divides one number by another
    /// </summary>
    /// <param name="args">The numbers to divide</param>
    /// <returns>The quotient of the numbers</returns>
    public static KnNode Divide(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return args.Length > 0 ? args[0] : KnNull.Null;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            if (int2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KnInt64(int1.Value / int2.Value);
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            if (double2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KnDouble(double1.Value / double2.Value);
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            if (i2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KnDouble(d1.Value / i2.Value);
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            if (d2.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new KnDouble(i1.Value / d2.Value);
        }

        // Default fallback
        return KnNull.Null;
    }

    public static KnNode BiggerThan(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return KnBoolean.False;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return (int1.Value > int2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return (double1.Value > double2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return (d1.Value > i2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return (i1.Value > d2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        // Default fallback
        return KnBoolean.False;
    }

    public static KnNode BiggerThanOrEqual(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return KnBoolean.False;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return (int1.Value >= int2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return (double1.Value >= double2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return (d1.Value >= i2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return (i1.Value >= d2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        // Default fallback
        return KnBoolean.False;
    }

    public static KnNode LowerThan(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return KnBoolean.False;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return (int1.Value < int2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return (double1.Value < double2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return (d1.Value < i2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return (i1.Value < d2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        // Default fallback
        return KnBoolean.False;
    }

    public static KnNode LowerThanOrEqual(params KnNode[] args)
    {
        if (args.Length < 2)
        {
            return KnBoolean.False;
        }

        // Handle KnNode types
        if (args[0] is KnInt64 int1 && args[1] is KnInt64 int2)
        {
            return (int1.Value <= int2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble double1 && args[1] is KnDouble double2)
        {
            return (double1.Value <= double2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnDouble d1 && args[1] is KnInt64 i2)
        {
            return (d1.Value <= i2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        if (args[0] is KnInt64 i1 && args[1] is KnDouble d2)
        {
            return (i1.Value <= d2.Value) ? KnBoolean.True : KnBoolean.False;
        }

        // Default fallback
        return KnBoolean.False;
    }
}