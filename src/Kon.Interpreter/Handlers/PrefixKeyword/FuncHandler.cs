using Kon.Core.Node;
using Kon.Interpreter.Models;
using Kon.Interpreter.Runtime;
using static Kon.Interpreter.Runtime.Env;

namespace Kon.Interpreter.Handlers.PrefixKeyword;

/// <summary>
/// Handler for function operations
/// </summary>
public static class FuncHandler
{
    /// <summary>
    /// Expands a function definition
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="node">The function node</param>
    public static void ExpandDeclareFunc(InterpreterRuntime runtime, KnChainNode node)
    {
        // Extract function name
        string? funcName = null;
        if (node.Name != null)
        {
            funcName = node.Name.Value;
        }

        // Extract parameters
        var parameters = new List<string>();

        if (node.InOutTable != null)
        {
            List<KnNode> inputItems = node.InOutTable.GetInputNodes();
            for (var i = 0; i < inputItems.Count; i++)
            {
                var param = inputItems[i];
                if (param is KnWord word)
                {
                    parameters.Add(word.GetFullNameStr());
                }
            }
        }

        // Extract function body
        var funcBody = node.Body;

        // Create the lambda function

        // When capturing environment, we need to ensure the lexical scope is preserved
        var currentEnv = runtime.GetCurEnv();
        var capturedEnv = runtime.EnvTree.CreateLexicalScope(
            currentEnv, Env.EnvType.Process,
            $"lambda_{funcName}");

        var func = new LambdaFunction(
            funcName,
            parameters.ToArray(),
            funcBody,
            capturedEnv);
        var funcNode = new KnLambdaFunction(func);

        // Define the function in the current environment if it has a name
        if (!string.IsNullOrEmpty(funcName))
        {
            currentEnv.Define(funcName, funcNode);
        }

        // Push the function onto the stack
        runtime.GetCurrentFiber().OperandStack.PushValue(funcNode);
    }

    /// <summary>
    /// Applies a function to arguments from the frame top
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunApplyToFrameTop(InterpreterRuntime runtime, Instruction instruction)
    {
        var func = runtime.GetCurrentFiber().OperandStack.PopValue();
        var args = runtime.GetCurrentFiber().OperandStack.PeekAndClearFrameAllValues();

        RunApplyToFunc(runtime, func, args);
    }

    /// <summary>
    /// Applies a function to arguments from the frame bottom
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="instruction">The instruction</param>
    public static void RunApplyToFrameBottom(InterpreterRuntime runtime, Instruction instruction)
    {
        var args = runtime.GetCurrentFiber().OperandStack.PeekAndClearFrameAllValues();

        if (args.Count == 0)
        {
            return;
        }

        var func = args[0];
        var funcArgs = args.Skip(1).ToList();

        RunApplyToFunc(runtime, func, funcArgs);
    }

    /// <summary>
    /// Applies a function to arguments
    /// </summary>
    /// <param name="state">The interpreter state</param>
    /// <param name="func">The function to apply</param>
    /// <param name="args">The arguments to apply</param>
    private static void RunApplyToFunc(InterpreterRuntime runtime, KnNode func, List<KnNode> args)
    {
        if (func is KnLambdaFunction lambdaFunc)
        {
            // Execute lambda function
            var lambda = lambdaFunc.Function;

            if (lambda.Arity > args.Count)
            {
                // Not enough arguments, push the function back
                runtime.GetCurrentFiber().OperandStack.PushValue(func);
                foreach (var arg in args)
                {
                    runtime.GetCurrentFiber().OperandStack.PushValue(arg);
                }
                return;
            }

            // Create a new environment for the function
            var defEnv = (func as KnLambdaFunction).Function.DefinitionEnv;
            var childEnv = EnvHandler.MakeSubLocalEnvUnderEnv(
                runtime, defEnv);
            // Bind parameters to arguments
            // TODO 支持变长参数
            for (var i = 0; i < lambda.Parameters.Length; i++)
            {
                if (i < args.Count)
                {
                    childEnv.Define(lambda.Parameters[i], args[i]);
                }
                else
                {
                    childEnv.Define(lambda.Parameters[i], KnNull.Null);
                }
            }

            // Execute the function body
            runtime.OpBatchStart();
            runtime.AddOp(OpCode.Ctrl_MakeContExcludeTopNInstruction, 2);   // jump to Env_Rise
            runtime.AddOp(OpCode.Env_SetLocalEnv, "return");
            runtime.AddOp(OpCode.Node_RunBlock, lambdaFunc.Function.Body);
            runtime.AddOp(OpCode.Env_Rise);
            runtime.OpBatchCommit();
        }
        else if (func is KnHostFunction hostFunc)
        {
            // Execute host function
            if (hostFunc.HostFunction is Models.HostFunction hf)
            {
                var result = hf.Invoke(args.ToArray());
                runtime.GetCurrentFiber().OperandStack.PushValue(result as KnNode ?? KnNull.Null);
            }
            else
            {
                runtime.GetCurrentFiber().OperandStack.PushValue(KnNull.Null);
            }
        }
        else if (func is KnContinuation cont)
        {
            ContinuationHandler.RestoreContinuationAppendOperandStack(runtime, cont, args);
        }
        else if (func is KnBoundMethod boundMethod)
        {
            // Execute bound method
            KnNode? method = null;

            // Try to get method from KnObject first
            if (boundMethod.BoundTarget is KnObject targetObject)
            {
                method = targetObject.GetMethod(boundMethod.MethodName);
            }

            // If not found, try builtin method registry
            if (method == null)
            {
                method = runtime.BuiltinMethodRegistry.GetMethod(boundMethod.BoundTarget, boundMethod.MethodName);
            }

            if (method == null)
            {
                throw new Exception($"Method '{boundMethod.MethodName}' not found on type {boundMethod.BoundTarget.GetType().Name}");
            }

            // Create argument list with 'self' as the first parameter
            var methodArgs = new List<KnNode> { boundMethod.BoundTarget };
            methodArgs.AddRange(args);

            // Execute the method as a function (recursively call RunApplyToFunc)
            RunApplyToFunc(runtime, method, methodArgs);
        }
        else
        {
            throw new Exception("not supported function type");
        }
    }
}