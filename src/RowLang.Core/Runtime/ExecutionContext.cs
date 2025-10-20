using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RowLang.Core.Types;

namespace RowLang.Core.Runtime;

public sealed class ExecutionContext
{
    private readonly TypeSystem _typeSystem;
    private readonly ConcurrentDictionary<string, Value> _globals = new();

    public ExecutionContext(TypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
        RuntimeTypeRegistry.Initialize(_typeSystem.Registry);
    }

    public TypeSystem Types => _typeSystem;

    public Value? GetGlobal(string name)
    {
        _globals.TryGetValue(name, out var value);
        return value;
    }

    public void SetGlobal(string name, Value value)
    {
        _globals[name] = value;
    }

    public ObjectValue Instantiate(string className)
    {
        var classDefinition = _typeSystem.RequireClass(className);
        var rows = new Dictionary<string, List<RowImplementation>>();
        var pendingTraits = new List<RowMember>();
        var pendingVirtualBases = new List<RowMember>();
        var inheritPlaceholders = new List<(List<RowImplementation> List, int Index, RowMember Member)>();

        bool RequiresVirtualOverride(RowMember member)
        {
            if (member.Origin == classDefinition.Type.Name)
            {
                return false;
            }

            foreach (var baseRef in classDefinition.Type.Bases)
            {
                if (baseRef.Type.Name == member.Origin)
                {
                    return baseRef.Inheritance == InheritanceKind.Virtual;
                }
            }

            return false;
        }

        foreach (var member in classDefinition.Type.Rows.Members)
        {
            if (!rows.TryGetValue(member.Name, out var list))
            {
                list = new List<RowImplementation>();
                rows[member.Name] = list;
            }

            var originDefinition = _typeSystem.RequireClass(member.Origin);

            if (originDefinition.Type.IsTrait)
            {
                pendingTraits.Add(member);
                continue;
            }

            if (RequiresVirtualOverride(member))
            {
                pendingVirtualBases.Add(member);
                continue;
            }

            var method = originDefinition.Methods.FirstOrDefault(
                m => m.Member.Origin == member.Origin && m.Member.Name == member.Name);
            if (method is null)
            {
                if (member.IsInherit)
                {
                    if (member.Type is not FunctionTypeSymbol inheritSignature)
                    {
                        throw new InvalidOperationException($"Inherited member {member.Origin}::{member.Name} must be a method.");
                    }

                    var placeholder = new FunctionValue(
                        inheritSignature,
                        static (_, _) => throw new InvalidOperationException("Inherited method forwarding unresolved."));
                    inheritPlaceholders.Add((list, list.Count, member));
                    list.Add(new RowImplementation(member, placeholder));
                    continue;
                }

                throw new InvalidOperationException($"No method body found for {member.Origin}::{member.Name}.");
            }

            list.Add(new RowImplementation(
                member,
                new FunctionValue((FunctionTypeSymbol)member.Type, method.Implementation)));
        }

        foreach (var (list, index, member) in inheritPlaceholders)
        {
            var target = list.Skip(index + 1).FirstOrDefault();
            if (target is null)
            {
                throw new InvalidOperationException($"Inherited member {member.Origin}::{member.Name} has no target implementation to forward to.");
            }

            if (member.Type is not FunctionTypeSymbol inheritSignature)
            {
                throw new InvalidOperationException($"Inherited member {member.Origin}::{member.Name} must be a method.");
            }

            var forward = new FunctionValue(
                inheritSignature,
                (ctx, args) => target.Function.Body(ctx, args));

            list[index] = list[index] with { Function = forward };
        }

        foreach (var traitMember in pendingTraits)
        {
            if (!rows.TryGetValue(traitMember.Name, out var list) || list.Count == 0)
            {
                throw new InvalidOperationException($"Trait member {traitMember.Origin}::{traitMember.Name} has no backing implementation.");
            }

            var target = list[0];
            list.Insert(0, new RowImplementation(traitMember, target.Function));
        }

        foreach (var virtualMember in pendingVirtualBases)
        {
            if (!rows.TryGetValue(virtualMember.Name, out var list) || list.Count == 0)
            {
                throw new InvalidOperationException($"Virtual base member {virtualMember.Origin}::{virtualMember.Name} requires an override.");
            }
        }

        return new ObjectValue(
            classDefinition.Type,
            rows.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<RowImplementation>)kvp.Value));
    }

    public Value Invoke(ObjectValue instance, string memberName, params Value[] arguments)
        => Invoke(instance, memberName, origin: null, arguments);

    public Value Invoke(ObjectValue instance, string memberName, string? origin, params Value[] arguments)
    {
        if (!instance.Rows.TryGetValue(memberName, out var implementations))
        {
            throw new InvalidOperationException($"Member '{memberName}' does not exist on {instance.Class.Name}.");
        }

        foreach (var implementation in implementations)
        {
            if (origin is not null && implementation.Member.Origin != origin)
            {
                continue;
            }

            if (implementation.Member.IsVirtual)
            {
                if (origin is not null)
                {
                    throw new InvalidOperationException($"Member '{memberName}' from {implementation.Member.Origin} is declared virtual and cannot be invoked.");
                }

                continue;
            }

            return implementation.Function.Body(new InvocationContext(this, instance), arguments);
        }

        if (origin is null && implementations.Any(m => m.Member.IsVirtual))
        {
            throw new InvalidOperationException($"Member '{memberName}' requires an override but none was provided.");
        }

        throw new InvalidOperationException($"No matching member '{memberName}' found for origin '{origin ?? "<default>"}'.");
    }
}
