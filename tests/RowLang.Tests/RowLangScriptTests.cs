using System;
using System.Linq;
using RowLang.Core.Runtime;
using RowLang.Core.Scripting;
using RowLang.Core.Types;
using Xunit;

namespace RowLang.Tests;

public class RowLangScriptTests
{
    [Fact]
    public void CompilesClassAndEffect()
    {
        const string source = """
        (module
          (effect async)
          (class File
            (open)
            (method read
              (return str)
              (effects async)
              (body (const str payload)))))
        """;

        var module = RowLangScript.Compile(source);
        var typeSystem = module.TypeSystem;
        var fileClass = typeSystem.RequireClassSymbol("File");

        Assert.True(typeSystem.IsSubtype(fileClass, typeSystem.RequireClassSymbol("object")));

        var context = module.CreateExecutionContext();
        var effect = typeSystem.Registry.GetOrCreateEffect("async");
        var instance = context.Instantiate("File");

        Assert.Throws<InvalidOperationException>(() => context.Invoke(instance, "read"));

        using (context.PushEffectScope(effect))
        {
            var value = (StringValue)context.Invoke(instance, "read");
            Assert.Equal("payload", value.Value);
        }
    }

    [Fact]
    public void DefinesRowTypesAndExtendsRows()
    {
        const string source = """
        (module
          (row-type T1
            (field count ~ int)
            (method a
              (return int)))
          (row-type T2
            (method b
              (return str)))
          (row-type T3
            (extends T1 T2)
            (method c
              (return bool))))
        """;

        var module = RowLangScript.Compile(source);
        var registry = module.TypeSystem.Registry;

        var t3 = Assert.IsType<RowTypeSymbol>(registry.Require("T3"));
        Assert.True(t3.IsOpen);

        var signatures = t3.Members.Select(m => $"{m.Origin}::{m.Name}").ToArray();
        Assert.Contains("T1::count", signatures);
        Assert.Contains("T1::a", signatures);
        Assert.Contains("T2::b", signatures);
        Assert.Contains("T3::c", signatures);
    }

    [Fact]
    public void MethodSupportsTypedParameters()
    {
        const string source = """
        (module
          (class Echo
            (open)
            (method identity
              (params value ~ int)
              (return int)
              (body value))))
        """;

        var module = RowLangScript.Compile(source);
        var typeSystem = module.TypeSystem;
        var echo = typeSystem.RequireClassSymbol("Echo");

        var method = echo.Rows.Resolve("identity");
        Assert.NotNull(method);

        var signature = Assert.IsType<FunctionTypeSymbol>(method!.Type);
        Assert.Single(signature.Parameters);
        Assert.Equal("int", signature.Parameters[0].Name);

        var context = module.CreateExecutionContext();
        var instance = context.Instantiate("Echo");
        var result = (IntValue)context.Invoke(instance, "identity", new IntValue(42));
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void MethodSupportsLetCallAndNewExpressions()
    {
        const string source = """
        (module
          (class Greeter
            (open)
            (method greet
              (return str)
              (body (const str hello))))
          (class EnthusiasticGreeter
            (open)
            (bases (Greeter))
            (method greet
              (qualifier override)
              (return str)
              (body
                (let ((base (call self Greeter::greet)))
                  base))))
          (class GreeterFactory
            (open)
            (method make
              (return str)
              (body
                (let ((other (new EnthusiasticGreeter)))
                  (call other greet))))))
        """;

        var module = RowLangScript.Compile(source);
        var context = module.CreateExecutionContext();

        var factory = context.Instantiate("GreeterFactory");
        var result = (StringValue)context.Invoke(factory, "make");
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void MethodSupportsArithmeticAndConcatPrimitives()
    {
        const string source = """
        (module
          (class Calculator
            (open)
            (method compute
              (return int)
              (body (+ (const int 40) (const int 2))))
            (method shout
              (return str)
              (body
                (let ((value (+ (const int 1) (const int 2))))
                  (concat (const str "Result: ") value))))))
        """;

        var module = RowLangScript.Compile(source);
        var context = module.CreateExecutionContext();
        var calculator = context.Instantiate("Calculator");

        var numeric = (IntValue)context.Invoke(calculator, "compute");
        Assert.Equal(42, numeric.Value);

        var message = (StringValue)context.Invoke(calculator, "shout");
        Assert.Equal("Result: 3", message.Value);
    }
}
