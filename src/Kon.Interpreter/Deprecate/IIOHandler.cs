namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Interface for IO operations
/// </summary>
public interface IIOHandler
{
    /// <summary>
    /// Writes a line of text to the output
    /// </summary>
    /// <param name="text">The text to write</param>
    void WriteLine(string text);

    /// <summary>
    /// Reads a line of text from the input
    /// </summary>
    /// <returns>The line of text</returns>
    string ReadLine();
}