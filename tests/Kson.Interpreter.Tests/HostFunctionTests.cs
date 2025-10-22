using System.Collections.Generic;
using System.Threading.Tasks;
using Kson.Core;
using Kson.Core.Node;
using Kson.Interpreter.HostSupport;
using Xunit;

namespace Kson.Interpreter.Tests;

public class HostFunctionTests
{
    [Fact]
    public void CanExecuteHostMathFunctions()
    {
        // Evaluate a simple expression using math functions
        var result = KsonInterpreter.EvaluateBlockSync("(3 4 :*)");
        Assert.Equal(12, ((KsInt64)result).Value);

        result = KsonInterpreter.EvaluateBlockSync("(10 2 :/)");
        Assert.Equal(5, ((KsInt64)result).Value);
    }

    [Fact]
    public void CanExecuteHostIoFunctions()
    {
        var runtime = KsonInterpreter.CreateRuntime();
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