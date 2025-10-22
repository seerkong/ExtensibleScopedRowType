using Kon.Interpreter.HostSupport;

public class IODefaultHandler : IIOHandler
{
    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    public string ReadLine()
    {
        return Console.ReadLine();
    }
}