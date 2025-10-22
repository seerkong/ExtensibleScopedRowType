using Kon.Core.Node;
using Kon.Interpreter.Runtime;
using System;

namespace Kon.Interpreter.Handlers.Call;

/// <summary>
/// Handler for getting properties/methods from objects
/// </summary>
public static class GetPropertyHandler
{
    /// <summary>
    /// Handles property/method access on a KnNode.
    /// Pops the target from the operand stack and creates a KnBoundMethod.
    /// Supports both KnObject instance methods and builtin methods on KnArray, KnMap, etc.
    /// </summary>
    /// <param name="runtime">The interpreter runtime</param>
    /// <param name="instruction">The instruction containing the chain node</param>
    public static void RunGetProperty(InterpreterRuntime runtime, Instruction instruction)
    {
        // Get the chain node from instruction memo
        var chainNode = (KnChainNode)instruction.Memo;

        // Get the property/method name from the chain node's Core
        if (chainNode.Core is not KnWord propertyName)
        {
            throw new Exception($"Expected property name to be a word, got {chainNode.Core?.GetType().Name}");
        }

        var propertyNameStr = propertyName.Value;

        // Pop the target from the operand stack
        var targetNode = runtime.GetCurrentFiber().OperandStack.PopValue();

        // Try to find the method:
        // 1. If it's a KnObject, check object's own methods first
        // 2. Otherwise (or if not found), check builtin method registry

        // KnNode? method = null;

        // if (targetNode is KnObject targetObject)
        // {
        //     method = targetObject.GetMethod(propertyNameStr);
        // }

        // // If not found on object, or target is not a KnObject, try builtin methods
        // if (method == null)
        // {
        //     method = runtime.BuiltinMethodRegistry.GetMethod(targetNode, propertyNameStr);
        // }

        // if (method == null)
        // {
        //     throw new Exception($"Method '{propertyNameStr}' not found on type {targetNode.GetType().Name}");
        // }

        // Create a bound method
        var boundMethod = new KnBoundMethod(targetNode, propertyNameStr);

        // Push the bound method back onto the stack
        runtime.OpBatchStart();
        runtime.AddOp(OpCode.ValStack_PushValue, boundMethod);
        runtime.OpBatchCommit();
    }
}
