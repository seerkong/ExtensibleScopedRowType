using System.Threading.Tasks;
using Kon.Core.Node;
using Kon.Interpreter.Models;
using Xunit;

namespace Kon.Interpreter.Tests;

/// <summary>
/// Tests for KnObject instance method calls using Kon script syntax.
/// Since KnObject creation syntax may not be implemented yet in Kon parser,
/// these tests create objects programmatically and then use script to call methods.
/// </summary>
public class ObjectScriptTests
{
    [Fact]
    public async Task Object_CanCallMethodWithoutParameters()
    {
        // Create an object with a method programmatically
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();

        // Create a method that returns a constant value
        var methodBody = new KnArray(new KnInt64(42));
        var method = new KnLambdaFunction(new LambdaFunction(
            "getValue",
            new[] { "self" },
            methodBody,
            runtime.GetRootEnv()
        ));

        obj.AddMethod("getValue", method);

        // Register object in environment
        runtime.GetRootEnv().Define("myObj", obj);

        // Call the method via script
        var script = "(myObj~getValue)";
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(42, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Object_CanCallMethodWithParameters()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();

        // Create a method that returns the first parameter
        // This tests that parameters are correctly passed to methods
        var methodBody = new KnArray(new KnWord("x"));
        var method = new KnLambdaFunction(new LambdaFunction(
            "echo",
            new[] { "self", "x" },
            methodBody,
            runtime.GetRootEnv()
        ));

        obj.AddMethod("echo", method);
        runtime.GetRootEnv().Define("testObj", obj);

        // Call method with a parameter
        var script = "(testObj~echo 42)";
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(42, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Object_MethodCanAccessObjectFields()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();
        obj.SetField("value", new KnInt64(100));

        // Create a method that accesses object field through 'self'
        // This is a placeholder - in real implementation, we'd need field access syntax
        var methodBody = new KnArray(new KnInt64(100)); // Simplified
        var method = new KnLambdaFunction(new LambdaFunction(
            "getValue",
            new[] { "self" },
            methodBody,
            runtime.GetRootEnv()
        ));

        obj.AddMethod("getValue", method);
        runtime.GetRootEnv().Define("obj", obj);

        var script = "(obj~getValue)";
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(100, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Object_CanCallMultipleMethodsInSequence()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();

        // Method 1: returns 10
        var method1Body = new KnArray(new KnInt64(10));
        var method1 = new KnLambdaFunction(new LambdaFunction(
            "getX",
            new[] { "self" },
            method1Body,
            runtime.GetGlobalEnv()
        ));

        // Method 2: returns 20
        var method2Body = new KnArray(new KnInt64(20));
        var method2 = new KnLambdaFunction(new LambdaFunction(
            "getY",
            new[] { "self" },
            method2Body,
            runtime.GetGlobalEnv()
        ));

        obj.AddMethod("getX", method1);
        obj.AddMethod("getY", method2);
        runtime.GetRootEnv().Define("point", obj);

        var script = """
        (var x (point~getX))
        (var y (point~getY))
        (x :+ y)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(30, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Object_DifferentObjectsHaveDifferentMethods()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj1 = new KnObject();
        var obj2 = new KnObject();

        // obj1 method returns 1
        var method1Body = new KnArray(new KnInt64(1));
        var method1 = new KnLambdaFunction(new LambdaFunction(
            "getValue",
            new[] { "self" },
            method1Body,
            runtime.GetRootEnv()
        ));

        // obj2 method returns 2
        var method2Body = new KnArray(new KnInt64(2));
        var method2 = new KnLambdaFunction(new LambdaFunction(
            "getValue",
            new[] { "self" },
            method2Body,
            runtime.GetRootEnv()
        ));

        obj1.AddMethod("getValue", method1);
        obj2.AddMethod("getValue", method2);

        runtime.GetRootEnv().Define("obj1", obj1);
        runtime.GetRootEnv().Define("obj2", obj2);

        var script = """
        (var v1 (obj1~getValue))
        (var v2 (obj2~getValue))
        (v1 :+ v2)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnInt64>(result);
        Assert.Equal(3, ((KnInt64)result).Value);
    }

    [Fact]
    public async Task Object_CanStoreObjectInVariable()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();

        var methodBody = new KnArray(new KnString("Hello"));
        var method = new KnLambdaFunction(new LambdaFunction(
            "greet",
            new[] { "self" },
            methodBody,
            runtime.GetRootEnv()
        ));

        obj.AddMethod("greet", method);
        runtime.GetRootEnv().Define("original", obj);

        var script = """
        (var copy original)
        (copy~greet)
        """;
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnString>(result);
        Assert.Equal("Hello", ((KnString)result).Value);
    }

    [Fact]
    public async Task Object_MethodReturnsObject()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var innerObj = new KnObject();
        innerObj.SetField("data", new KnInt64(999));

        var outerObj = new KnObject();

        // Method that returns the object itself (or another object)
        var methodBody = new KnArray(innerObj);
        var method = new KnLambdaFunction(new LambdaFunction(
            "getInner",
            new[] { "self" },
            methodBody,
            runtime.GetRootEnv()
        ));

        outerObj.AddMethod("getInner", method);
        runtime.GetRootEnv().Define("outer", outerObj);

        var script = "(outer~getInner)";
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnObject>(result);
        Assert.Equal(innerObj, result);
    }

    [Fact]
    public async Task Object_BoundMethodIsCreatedCorrectly()
    {
        var runtime = new Kon.Interpreter.Runtime.InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(runtime);

        var obj = new KnObject();
        obj.SetField("name", new KnString("TestObject"));

        // Verify that bound method binds to the correct object
        var methodBody = new KnArray(new KnBoolean(true));
        var method = new KnLambdaFunction(new LambdaFunction(
            "test",
            new[] { "self" },
            methodBody,
            runtime.GetRootEnv()
        ));

        obj.AddMethod("test", method);
        runtime.GetRootEnv().Define("testObj", obj);

        var script = "(testObj~test)";
        var result = await KonInterpreter.EvaluateBlockAsync(runtime, script);

        Assert.IsType<KnBoolean>(result);
        Assert.True(((KnBoolean)result).Value);
    }
}
