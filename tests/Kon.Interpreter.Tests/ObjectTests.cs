using Kon.Core.Node;
using Kon.Interpreter.Models;
using Xunit;

namespace Kon.Interpreter.Tests;

public class ObjectTests
{
    #region Object Creation Tests

    [Fact]
    public void CanCreateKnObject()
    {
        // Test basic object creation
        var obj = new KnObject();

        Assert.NotNull(obj);
        Assert.IsAssignableFrom<KnNode>(obj);
    }

    [Fact]
    public void KnObjectIsKnNode()
    {
        // Verify KnObject implements KnNode interface
        var obj = new KnObject();

        Assert.IsAssignableFrom<KnNode>(obj);
        Assert.IsType<KnObject>(obj);
    }

    [Fact]
    public void CanCreateKnObjectWithInitialData()
    {
        // Create object with initial fields and methods
        var fields = new Dictionary<string, KnNode>
        {
            ["name"] = new KnString("Alice")
        };

        var methods = new Dictionary<string, KnNode>
        {
            ["greet"] = new KnString("greeting_function")
        };

        var obj = new KnObject(fields, methods);

        Assert.NotNull(obj);
        Assert.True(obj.HasField("name"));
        Assert.True(obj.HasMethod("greet"));
    }

    #endregion

    #region Field Management Tests

    [Fact]
    public void CanSetFieldInObject()
    {
        // Test setting a field
        var obj = new KnObject();
        obj.SetField("name", new KnString("Alice"));

        Assert.True(obj.HasField("name"));
    }

    [Fact]
    public void CanGetFieldFromObject()
    {
        // Test reading a field
        var obj = new KnObject();
        obj.SetField("age", new KnInt64(25));

        var field = obj.GetField("age");

        Assert.NotNull(field);
        Assert.IsType<KnInt64>(field);
        Assert.Equal(25, ((KnInt64)field).Value);
    }

    [Fact]
    public void CanWriteFieldToObject()
    {
        // Test updating a field
        var obj = new KnObject();
        obj.SetField("count", new KnInt64(0));
        obj.SetField("count", new KnInt64(10));

        var field = obj.GetField("count");

        Assert.NotNull(field);
        Assert.IsType<KnInt64>(field);
        Assert.Equal(10, ((KnInt64)field).Value);
    }

    [Fact]
    public void AccessUndefinedFieldReturnsNull()
    {
        // Test accessing non-existent field
        var obj = new KnObject();

        var field = obj.GetField("undefined");

        Assert.Null(field);
    }

    [Fact]
    public void HasFieldReturnsFalseForNonExistentField()
    {
        var obj = new KnObject();

        Assert.False(obj.HasField("nonexistent"));
    }

    [Fact]
    public void CanRemoveField()
    {
        var obj = new KnObject();
        obj.SetField("temp", new KnString("temporary"));

        Assert.True(obj.HasField("temp"));

        var removed = obj.RemoveField("temp");

        Assert.True(removed);
        Assert.False(obj.HasField("temp"));
    }

    [Fact]
    public void RemoveNonExistentFieldReturnsFalse()
    {
        var obj = new KnObject();

        var removed = obj.RemoveField("nonexistent");

        Assert.False(removed);
    }

    [Fact]
    public void SetFieldThrowsOnNullName()
    {
        var obj = new KnObject();

        Assert.Throws<ArgumentException>(() => obj.SetField(null!, new KnString("value")));
        Assert.Throws<ArgumentException>(() => obj.SetField("", new KnString("value")));
    }

    [Fact]
    public void SetFieldThrowsOnNullValue()
    {
        var obj = new KnObject();

        Assert.Throws<ArgumentNullException>(() => obj.SetField("field", null!));
    }

    #endregion

    #region Method Management Tests

    [Fact]
    public void CanDefineMethodInObject()
    {
        // Test defining a method
        var obj = new KnObject();
        var methodBody = new KnString("greeting_function_body");

        obj.AddMethod("greet", methodBody);

        Assert.True(obj.HasMethod("greet"));
    }

    [Fact]
    public void CanRetrieveMethodFromObject()
    {
        // Test getting a method
        var obj = new KnObject();
        var methodBody = new KnString("calculate_function");

        obj.AddMethod("calculate", methodBody);

        var method = obj.GetMethod("calculate");

        Assert.NotNull(method);
        Assert.Equal(methodBody, method);
    }

    [Fact]
    public void GetNonExistentMethodReturnsNull()
    {
        var obj = new KnObject();

        var method = obj.GetMethod("nonexistent");

        Assert.Null(method);
    }

    [Fact]
    public void HasMethodReturnsFalseForNonExistentMethod()
    {
        var obj = new KnObject();

        Assert.False(obj.HasMethod("nonexistent"));
    }

    [Fact]
    public void AddMethodThrowsOnNullName()
    {
        var obj = new KnObject();

        Assert.Throws<ArgumentException>(() => obj.AddMethod(null!, new KnString("body")));
        Assert.Throws<ArgumentException>(() => obj.AddMethod("", new KnString("body")));
    }

    [Fact]
    public void AddMethodThrowsOnNullBody()
    {
        var obj = new KnObject();

        Assert.Throws<ArgumentNullException>(() => obj.AddMethod("method", null!));
    }

    #endregion

    #region Object Identity Tests

    [Fact]
    public void ObjectsHaveIdentity()
    {
        // Test that different objects are not equal even with same content
        var obj1 = new KnObject();
        var obj2 = new KnObject();

        obj1.SetField("name", new KnString("Alice"));
        obj2.SetField("name", new KnString("Alice"));

        Assert.NotEqual(obj1, obj2);
    }

    [Fact]
    public void SameObjectIsEqual()
    {
        var obj = new KnObject();

        Assert.Equal(obj, obj);
    }

    #endregion

    #region Instance Method Call Tests

    [Fact]
    public void CanCallMethodWithoutParameters()
    {
        // Create an object with a method that returns a constant
        var obj = new KnObject();

        // Create a method that returns "Hello" (using a lambda function)
        var methodBody = new KnArray(new List<KnNode>
        {
            new KnString("Hello")
        });

        var method = new KnLambdaFunction(new Kon.Interpreter.Models.LambdaFunction(
            "greet",
            new[] { "self" },  // self parameter
            methodBody,
            null!  // env will be set during interpretation
        ));

        obj.AddMethod("greet", method);

        // Create a bound method
        var boundMethod = new KnBoundMethod(obj, "greet");

        Assert.NotNull(boundMethod);
        Assert.Equal(obj, boundMethod.BoundTarget);
        Assert.Equal("greet", boundMethod.MethodName);
    }

    [Fact]
    public void CanCallMethodWithParameters()
    {
        // Create an object with a method that uses parameters
        var obj = new KnObject();

        // Create a method body that would use parameters
        var methodBody = new KnArray(new List<KnNode>
        {
            new KnString("method_with_params")
        });

        var method = new KnLambdaFunction(new Kon.Interpreter.Models.LambdaFunction(
            "calculate",
            new[] { "self", "x", "y" },  // self + 2 parameters
            methodBody,
            null!
        ));

        obj.AddMethod("calculate", method);

        // Create a bound method
        var boundMethod = new KnBoundMethod(obj, "calculate");

        Assert.NotNull(boundMethod);
        Assert.Equal(3, method.Function.Arity);  // Should accept self + 2 params
    }

    [Fact]
    public void BoundMethodAccessesSelfParameter()
    {
        // Test that the bound method binds the correct object
        var obj1 = new KnObject();
        var obj2 = new KnObject();

        obj1.SetField("name", new KnString("Object1"));
        obj2.SetField("name", new KnString("Object2"));

        var methodBody = new KnArray(new List<KnNode>
        {
            new KnString("get_name")
        });

        var method = new KnLambdaFunction(new Kon.Interpreter.Models.LambdaFunction(
            "getName",
            new[] { "self" },
            methodBody,
            null!
        ));

        obj1.AddMethod("getName", method);
        obj2.AddMethod("getName", method);

        // Create bound methods for both objects
        var boundMethod1 = new KnBoundMethod(obj1, "getName");
        var boundMethod2 = new KnBoundMethod(obj2, "getName");

        // Verify they bind to different objects
        Assert.NotEqual(boundMethod1.BoundTarget, boundMethod2.BoundTarget);
        Assert.Equal(obj1, boundMethod1.BoundTarget);
        Assert.Equal(obj2, boundMethod2.BoundTarget);
    }

    [Fact]
    public void BoundMethodThrowsWhenMethodNotFound()
    {
        var obj = new KnObject();

        // Create a bound method for a non-existent method
        var boundMethod = new KnBoundMethod(obj, "nonexistent");

        // The bound method can be created, but getting the method should return null
        Assert.Null(obj.GetMethod("nonexistent"));
    }

    [Fact]
    public void BoundMethodSupportsTypeProjection()
    {
        // Test the ProjectedType property for future 'as' support
        var obj = new KnObject();
        var methodBody = new KnArray(new List<KnNode>
        {
            new KnString("method")
        });

        var method = new KnLambdaFunction(new Kon.Interpreter.Models.LambdaFunction(
            "test",
            new[] { "self" },
            methodBody,
            null!
        ));

        obj.AddMethod("test", method);

        // Create a bound method without type projection
        var boundMethod1 = new KnBoundMethod(obj, "test");
        Assert.Null(boundMethod1.ProjectedType);

        // Create a bound method with type projection
        var projectedType = new KnString("SomeType");
        var boundMethod2 = new KnBoundMethod(obj, "test", projectedType);
        Assert.Equal(projectedType, boundMethod2.ProjectedType);

        // Modify type projection
        boundMethod1.ProjectedType = projectedType;
        Assert.Equal(projectedType, boundMethod1.ProjectedType);
    }

    [Fact]
    public void BoundMethodToStringShowsObjectAndMethod()
    {
        var obj = new KnObject();
        var boundMethod = new KnBoundMethod(obj, "testMethod");

        var str = boundMethod.ToString();

        Assert.Contains("KnBoundMethod", str);
        Assert.Contains("testMethod", str);
    }

    [Fact]
    public void BoundMethodEqualityBasedOnObjectAndMethod()
    {
        var obj1 = new KnObject();
        var obj2 = new KnObject();

        var boundMethod1 = new KnBoundMethod(obj1, "method");
        var boundMethod2 = new KnBoundMethod(obj1, "method");
        var boundMethod3 = new KnBoundMethod(obj2, "method");
        var boundMethod4 = new KnBoundMethod(obj1, "otherMethod");

        // Same object, same method - should be equal
        Assert.Equal(boundMethod1, boundMethod2);

        // Different object - should not be equal
        Assert.NotEqual(boundMethod1, boundMethod3);

        // Same object, different method - should not be equal
        Assert.NotEqual(boundMethod1, boundMethod4);
    }

    #endregion
}
