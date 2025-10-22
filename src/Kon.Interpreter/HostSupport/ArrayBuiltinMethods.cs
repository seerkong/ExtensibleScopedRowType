using Kon.Core.Node;
using Kon.Interpreter.Models;
using Kon.Interpreter.Runtime;
using System;

namespace Kon.Interpreter.HostSupport;

/// <summary>
/// Builtin methods for KnArray type.
/// All methods receive 'self' as the first parameter.
/// </summary>
public static class ArrayBuiltinMethods
{
    /// <summary>
    /// Registers all builtin methods for KnArray.
    /// </summary>
    public static void Register(BuiltinMethodRegistry registry)
    {
        registry.RegisterMethod("KnArray", "Count", new KnHostFunction(new HostFunction("Count", Count)));
        registry.RegisterMethod("KnArray", "Length", new KnHostFunction(new HostFunction("Length", Length)));
        registry.RegisterMethod("KnArray", "Get", new KnHostFunction(new HostFunction("Get", Get)));
        registry.RegisterMethod("KnArray", "Push", new KnHostFunction(new HostFunction("Push", Push)));
        registry.RegisterMethod("KnArray", "Pop", new KnHostFunction(new HostFunction("Pop", Pop)));
        registry.RegisterMethod("KnArray", "Unshift", new KnHostFunction(new HostFunction("Unshift", Unshift)));
        registry.RegisterMethod("KnArray", "Shift", new KnHostFunction(new HostFunction("Shift", Shift)));
        registry.RegisterMethod("KnArray", "Top", new KnHostFunction(new HostFunction("Top", Top)));
        registry.RegisterMethod("KnArray", "IsEmpty", new KnHostFunction(new HostFunction("IsEmpty", IsEmpty)));
    }

    /// <summary>
    /// Returns the number of elements in the array.
    /// Usage: (array~Count)
    /// </summary>
    private static KnNode Count(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Count expects an array as the first argument (self)");
        }

        return new KnInt64(array.Size());
    }

    /// <summary>
    /// Returns the length of the array (alias for Count).
    /// Usage: (array~Length)
    /// </summary>
    private static KnNode Length(params KnNode[] args)
    {
        return Count(args);
    }

    /// <summary>
    /// Gets an element at the specified index.
    /// Usage: (array~Get 0)
    /// </summary>
    private static KnNode Get(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Get expects an array as the first argument (self) and an index");
        }

        if (args[1] is not KnInt64 index)
        {
            throw new ArgumentException("Get expects an integer index as the second argument");
        }

        var idx = (int)index.Value;
        if (idx < 0 || idx >= array.Size())
        {
            throw new IndexOutOfRangeException($"Index {idx} is out of range for array of size {array.Size()}");
        }

        return array.Get(idx);
    }

    /// <summary>
    /// Appends an element to the end of the array.
    /// Usage: (array~Push value)
    /// </summary>
    private static KnNode Push(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Push expects an array as the first argument (self) and a value");
        }

        array.Push(args[1]);
        return array;
    }

    /// <summary>
    /// Removes and returns the last element of the array.
    /// Usage: (array~Pop)
    /// </summary>
    private static KnNode Pop(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Pop expects an array as the first argument (self)");
        }

        if (array.Size() == 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty array");
        }

        return array.Pop();
    }

    /// <summary>
    /// Inserts an element at the beginning of the array.
    /// Usage: (array~Unshift value)
    /// </summary>
    private static KnNode Unshift(params KnNode[] args)
    {
        if (args.Length < 2 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Unshift expects an array as the first argument (self) and a value");
        }

        array.Unshift(args[1]);
        return array;
    }

    /// <summary>
    /// Removes and returns the first element of the array.
    /// Usage: (array~Shift)
    /// </summary>
    private static KnNode Shift(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Shift expects an array as the first argument (self)");
        }

        if (array.Size() == 0)
        {
            throw new InvalidOperationException("Cannot shift from an empty array");
        }

        return array.Shift();
    }

    /// <summary>
    /// Returns the last element of the array without removing it.
    /// Usage: (array~Top)
    /// </summary>
    private static KnNode Top(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnArray array)
        {
            throw new ArgumentException("Top expects an array as the first argument (self)");
        }

        if (array.Size() == 0)
        {
            throw new InvalidOperationException("Cannot get top from an empty array");
        }

        return array.Top();
    }

    /// <summary>
    /// Checks if the array is empty.
    /// Usage: (array~IsEmpty)
    /// </summary>
    private static KnNode IsEmpty(params KnNode[] args)
    {
        if (args.Length < 1 || args[0] is not KnArray array)
        {
            throw new ArgumentException("IsEmpty expects an array as the first argument (self)");
        }

        return new KnBoolean(array.Size() == 0);
    }
}
