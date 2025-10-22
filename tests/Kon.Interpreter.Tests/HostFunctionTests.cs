using System.Collections.Generic;
using System.Threading.Tasks;
using Kon.Core;
using Kon.Core.Node;
using Kon.Interpreter.HostSupport;
using Xunit;

namespace Kon.Interpreter.Tests;

public class HostFunctionTests
{
    [Fact]
    public void CanExecuteHostMathFunctions()
    {
        // Evaluate a simple expression using math functions
        var result = KonInterpreter.EvaluateBlockSync("(3 4 :*)");
        Assert.Equal(12, ((KnInt64)result).Value);

        result = KonInterpreter.EvaluateBlockSync("(10 2 :/)");
        Assert.Equal(5, ((KnInt64)result).Value);
    }

    [Fact]
    public void CanExecuteHostIoFunctions()
    {
        var runtime = KonInterpreter.CreateRuntime();
        // TODO Custom io effect handler to capture output
        // new TestIOHandler(capturedOutput)
        // Call a print function
    }

    // Helper IO Handler for testing
    private class TestIOHandler : IIOHandler
    {
        private readonly List<string> _capturedOutput;

        public TestIOHandler(List<string> capturedOutput)
        {
            _capturedOutput = capturedOutput;
        }

        public void WriteLine(string text)
        {
            _capturedOutput.Add(text);
        }

        public string ReadLine()
        {
            return string.Empty;
        }
    }
}