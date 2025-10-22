using System;
using System.Collections.Generic;
using Kson.Core.Node;
using Kson.Interpreter.Runtime;

namespace Kson.Interpreter;

/// <summary>
/// Registry for handler functions and keywords
/// 不是单例，也不是静态类的原因是，能够支持不同的运行时有不同的功能
/// </summary>
public class ExtensionRegistry
{
    // Map of instruction handlers
    private readonly Dictionary<string, Action<KsonInterpreterRuntime, Instruction>> InstructionHandlerMap = new();

    // Map of prefix keyword expanders
    private readonly Dictionary<string, Action<KsonInterpreterRuntime, KsChainNode>> PrefixKeywordExpanderMap = new();

    // Map of infix keyword expanders
    private readonly Dictionary<string, Action<KsonInterpreterRuntime, KsChainNode>> InfixKeywordExpanderMap = new();

    /// <summary>
    /// Gets an instruction handler
    /// </summary>
    /// <param name="name">The name of the handler</param>
    /// <returns>The handler function</returns>
    public Action<KsonInterpreterRuntime, Instruction> GetInstructionHandler(string name)
    {
        return InstructionHandlerMap.TryGetValue(name, out var handler) ? handler : null;
    }

    /// <summary>
    /// Registers an instruction handler
    /// </summary>
    /// <param name="name">The name of the handler</param>
    /// <param name="handler">The handler function</param>
    public void RegisterInstructionHandler(string name, Action<KsonInterpreterRuntime, Instruction> handler)
    {
        InstructionHandlerMap[name] = handler;
    }

    /// <summary>
    /// Checks if a name is a prefix keyword
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <returns>True if the name is a prefix keyword, false otherwise</returns>
    public bool IsPrefixKeyword(string name)
    {
        return PrefixKeywordExpanderMap.ContainsKey(name);
    }

    /// <summary>
    /// Checks if a name is an infix keyword
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <returns>True if the name is an infix keyword, false otherwise</returns>
    public bool IsInfixKeyword(string name)
    {
        return InfixKeywordExpanderMap.ContainsKey(name);
    }

    /// <summary>
    /// Gets a prefix keyword expander
    /// </summary>
    /// <param name="name">The name of the expander</param>
    /// <returns>The expander function</returns>
    public Action<KsonInterpreterRuntime, KsChainNode> GetPrefixKeywordExpander(string name)
    {
        return PrefixKeywordExpanderMap.TryGetValue(name, out var expander) ? expander : null;
    }

    /// <summary>
    /// Gets an infix keyword expander
    /// </summary>
    /// <param name="name">The name of the expander</param>
    /// <returns>The expander function</returns>
    public Action<KsonInterpreterRuntime, KsChainNode> GetInfixKeywordExpander(string name)
    {
        return InfixKeywordExpanderMap.TryGetValue(name, out var expander) ? expander : null;
    }

    /// <summary>
    /// Registers a prefix keyword expander
    /// </summary>
    /// <param name="name">The name of the expander</param>
    /// <param name="expander">The expander function</param>
    public void RegisterPrefixKeywordExpander(string name, Action<KsonInterpreterRuntime, KsChainNode> expander)
    {
        PrefixKeywordExpanderMap[name] = expander;
    }

    /// <summary>
    /// Registers an infix keyword expander
    /// </summary>
    /// <param name="name">The name of the expander</param>
    /// <param name="expander">The expander function</param>
    public void RegisterInfixKeywordExpander(string name, Action<KsonInterpreterRuntime, KsChainNode> expander)
    {
        InfixKeywordExpanderMap[name] = expander;
    }
}