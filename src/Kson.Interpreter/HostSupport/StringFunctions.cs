using Kson.Core.Node;

namespace Kson.Interpreter.HostSupport;

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
    public static KsNode Concat(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsString(string.Empty);
        }

        var result = new System.Text.StringBuilder();

        foreach (var arg in args)
        {
            if (arg is KsString str)
            {
                result.Append(str.Value);
            }
            else
            {
                result.Append(arg?.ToString() ?? "null");
            }
        }

        return new KsString(result.ToString());
    }

    /// <summary>
    /// Gets the length of a string
    /// </summary>
    /// <param name="args">The string to get the length of</param>
    /// <returns>The length of the string</returns>
    public static KsNode Length(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsInt64(0);
        }

        if (args[0] is KsString str)
        {
            return new KsInt64(str.Value.Length);
        }
        if (args[0] is KsArray array)
        {
            return new KsInt64(array.Size());
        }

        return new KsInt64(args[0]?.ToString().Length ?? 0);
    }

    /// <summary>
    /// Converts a string to uppercase
    /// </summary>
    /// <param name="args">The string to convert</param>
    /// <returns>The uppercase string</returns>
    public static KsNode ToUpper(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsString(string.Empty);
        }

        if (args[0] is KsString str)
        {
            return new KsString(str.Value.ToUpperInvariant());
        }
        return new KsString(args[0]?.ToString().ToUpperInvariant() ?? string.Empty);
    }

    /// <summary>
    /// Converts a string to lowercase
    /// </summary>
    /// <param name="args">The string to convert</param>
    /// <returns>The lowercase string</returns>
    public static KsNode ToLower(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsString(string.Empty);
        }

        if (args[0] is KsString str)
        {
            return new KsString(str.Value.ToLowerInvariant());
        }
        return new KsString(args[0]?.ToString().ToLowerInvariant() ?? string.Empty);
    }

    /// <summary>
    /// Trims whitespace from the beginning and end of a string
    /// </summary>
    /// <param name="args">The string to trim</param>
    /// <returns>The trimmed string</returns>
    public static KsNode Trim(params KsNode[] args)
    {
        if (args.Length < 1)
        {
            return new KsString(string.Empty);
        }

        if (args[0] is KsString str)
        {
            return new KsString(str.Value.Trim());
        }
        return new KsString(args[0]?.ToString().Trim() ?? string.Empty);
    }
}