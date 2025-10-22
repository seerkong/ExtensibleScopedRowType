using Kon.Core.Node;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Provides string functions to the interpreter
/// </summary>
public static partial class StringFunctions
{
    /// <summary>
    /// Concatenates two strings
    /// </summary>
    /// <param name="args">The strings to concatenate</param>
    /// <returns>The concatenated string</returns>
    public static KnNode Concat(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnString(string.Empty);
        }

        var result = new System.Text.StringBuilder();

        foreach (var arg in args)
        {
            if (arg is KnString str)
            {
                result.Append(str.Value);
            }
            else
            {
                result.Append(arg?.ToString() ?? "null");
            }
        }

        return new KnString(result.ToString());
    }

    /// <summary>
    /// Gets the length of a string
    /// </summary>
    /// <param name="args">The string to get the length of</param>
    /// <returns>The length of the string</returns>
    public static KnNode Length(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnInt64(0);
        }

        if (args[0] is KnString str)
        {
            return new KnInt64(str.Value.Length);
        }
        if (args[0] is KnArray array)
        {
            return new KnInt64(array.Size());
        }

        return new KnInt64(args[0]?.ToString().Length ?? 0);
    }

    /// <summary>
    /// Converts a string to uppercase
    /// </summary>
    /// <param name="args">The string to convert</param>
    /// <returns>The uppercase string</returns>
    public static KnNode ToUpper(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnString(string.Empty);
        }

        if (args[0] is KnString str)
        {
            return new KnString(str.Value.ToUpperInvariant());
        }
        return new KnString(args[0]?.ToString().ToUpperInvariant() ?? string.Empty);
    }

    /// <summary>
    /// Converts a string to lowercase
    /// </summary>
    /// <param name="args">The string to convert</param>
    /// <returns>The lowercase string</returns>
    public static KnNode ToLower(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnString(string.Empty);
        }

        if (args[0] is KnString str)
        {
            return new KnString(str.Value.ToLowerInvariant());
        }
        return new KnString(args[0]?.ToString().ToLowerInvariant() ?? string.Empty);
    }

    /// <summary>
    /// Trims whitespace from the beginning and end of a string
    /// </summary>
    /// <param name="args">The string to trim</param>
    /// <returns>The trimmed string</returns>
    public static KnNode Trim(params KnNode[] args)
    {
        if (args.Length < 1)
        {
            return new KnString(string.Empty);
        }

        if (args[0] is KnString str)
        {
            return new KnString(str.Value.Trim());
        }
        return new KnString(args[0]?.ToString().Trim() ?? string.Empty);
    }
}