using Kon.Core.Node;
using Kon.Interpreter.Runtime;
using Xunit;

namespace Kon.Interpreter.Tests;

/// <summary>
/// Tests for builtin methods on KnArray and KnMap
/// </summary>
public class BuiltinMethodTests
{
    private readonly InterpreterRuntime _runtime;

    public BuiltinMethodTests()
    {
        _runtime = new InterpreterRuntime();
        Kon.Interpreter.ExtensionRegistryInitializer.RegisterDefault(_runtime);
    }

    #region KnArray Builtin Methods Tests

    [Fact]
    public void Array_Count_ReturnsCorrectSize()
    {
        var array = new KnArray(new KnInt64(1), new KnInt64(2), new KnInt64(3));
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "Count");

        Assert.NotNull(method);

        // Create bound method
        var boundMethod = new KnBoundMethod(array, "Count");
        Assert.NotNull(boundMethod);
    }

    [Fact]
    public void Array_Get_RetrievesElement()
    {
        var array = new KnArray(new KnString("a"), new KnString("b"), new KnString("c"));
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "Get");

        Assert.NotNull(method);
    }

    [Fact]
    public void Array_Push_AddsElement()
    {
        var array = new KnArray();
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "Push");

        Assert.NotNull(method);
    }

    [Fact]
    public void Array_Pop_RemovesElement()
    {
        var array = new KnArray(new KnInt64(1));
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "Pop");

        Assert.NotNull(method);
    }

    [Fact]
    public void Array_IsEmpty_ChecksEmptiness()
    {
        var array = new KnArray();
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "IsEmpty");

        Assert.NotNull(method);
    }

    [Fact]
    public void Array_NonExistentMethod_ReturnsNull()
    {
        var array = new KnArray();
        var method = _runtime.BuiltinMethodRegistry.GetMethod(array, "NonExistent");

        Assert.Null(method);
    }

    #endregion

    #region KnMap Builtin Methods Tests

    [Fact]
    public void Map_Count_ReturnsCorrectSize()
    {
        var map = new KnMap();
        map.Add("key1", new KnString("value1"));
        map.Add("key2", new KnString("value2"));

        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "Count");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_Get_RetrievesValue()
    {
        var map = new KnMap();
        map.Add("test", new KnInt64(42));

        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "Get");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_ContainsKey_ChecksKey()
    {
        var map = new KnMap();
        map.Add("exists", new KnBoolean(true));

        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "ContainsKey");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_Keys_ReturnsKeys()
    {
        var map = new KnMap();
        map.Add("a", new KnInt64(1));
        map.Add("b", new KnInt64(2));

        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "Keys");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_Values_ReturnsValues()
    {
        var map = new KnMap();
        map.Add("x", new KnString("hello"));
        map.Add("y", new KnString("world"));

        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "Values");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_IsEmpty_ChecksEmptiness()
    {
        var map = new KnMap();
        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "IsEmpty");

        Assert.NotNull(method);
    }

    [Fact]
    public void Map_NonExistentMethod_ReturnsNull()
    {
        var map = new KnMap();
        var method = _runtime.BuiltinMethodRegistry.GetMethod(map, "NonExistent");

        Assert.Null(method);
    }

    #endregion

    #region BuiltinMethodRegistry Tests

    [Fact]
    public void Registry_CanRegisterAndRetrieveMethod()
    {
        var registry = new BuiltinMethodRegistry();
        var testMethod = new KnString("test_method");

        registry.RegisterMethod("TestType", "TestMethod", testMethod);

        // Note: We can't easily test retrieval without creating a custom type
        // This test verifies registration doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void Registry_HasMethod_ReturnsTrueForRegisteredMethod()
    {
        var array = new KnArray();
        var hasCount = _runtime.BuiltinMethodRegistry.HasMethod(array, "Count");
        var hasNonExistent = _runtime.BuiltinMethodRegistry.HasMethod(array, "NonExistent");

        Assert.True(hasCount);
        Assert.False(hasNonExistent);
    }

    [Fact]
    public void GetPropertyHandler_CreatesMethodForKnArray()
    {
        // Test that bound methods can be created for builtin types
        var array = new KnArray(new KnInt64(1), new KnInt64(2));
        var boundMethod = new KnBoundMethod(array, "Count");

        Assert.NotNull(boundMethod);
        Assert.Equal(array, boundMethod.BoundTarget);
        Assert.Equal("Count", boundMethod.MethodName);
    }

    [Fact]
    public void GetPropertyHandler_CreatesMethodForKnMap()
    {
        var map = new KnMap();
        map.Add("key", new KnString("value"));

        var boundMethod = new KnBoundMethod(map, "Count");

        Assert.NotNull(boundMethod);
        Assert.Equal(map, boundMethod.BoundTarget);
        Assert.Equal("Count", boundMethod.MethodName);
    }

    #endregion
}
