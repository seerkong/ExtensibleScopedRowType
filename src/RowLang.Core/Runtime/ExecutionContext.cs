using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RowLang.Core.Types;

namespace RowLang.Core.Runtime;

public sealed class ExecutionContext
{
    private readonly TypeSystem _typeSystem;
    private readonly ConcurrentDictionary<string, Value> _globals = new();
    private readonly Stack<ImmutableHashSet<EffectSymbol>> _effectScopes = new();

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

    public IDisposable PushEffectScope(params EffectSymbol[] allowed)
        => PushEffectScope((IEnumerable<EffectSymbol>)allowed);

    public IDisposable PushEffectScope(IEnumerable<EffectSymbol> allowed)
    {
        var scope = allowed.ToImmutableHashSet();
        _effectScopes.Push(scope);
        return new EffectScope(this, scope);
    }

    private void PopEffectScope(ImmutableHashSet<EffectSymbol> scope)
    {
        if (_effectScopes.Count == 0)
        {
            throw new InvalidOperationException("Effect scope underflow.");
        }

        var current = _effectScopes.Pop();
        if (!current.SetEquals(scope))
        {
            throw new InvalidOperationException("Effect scope imbalance detected.");
        }
    }

    private ImmutableHashSet<EffectSymbol> GetAllowedEffects()
    {
        if (_effectScopes.Count == 0)
        {
            return ImmutableHashSet<EffectSymbol>.Empty;
        }

        var builder = ImmutableHashSet.CreateBuilder<EffectSymbol>();
        foreach (var scope in _effectScopes)
        {
            builder.UnionWith(scope);
        }

        return builder.ToImmutable();
    }

    private void EnsureEffectsAllowed(FunctionTypeSymbol signature)
    {
        if (signature.Effects.IsDefaultOrEmpty)
        {
            return;
        }

        var allowed = GetAllowedEffects();
        foreach (var effect in signature.Effects)
        {
            if (!allowed.Contains(effect))
            {
                throw new InvalidOperationException($"Effect '{effect.Name}' required by '{signature.Name}' is not permitted in the current scope.");
            }
        }
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
            var forwarded = traitMember with { Qualifier = RowQualifier.Default };
            list.Insert(0, new RowImplementation(forwarded, target.Function));
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

    /// <summary>
    /// Invokes a member with type projection support: (instance as TargetType)::memberName
    /// </summary>
    public Value InvokeWithProjection(ObjectValue instance, string targetTypeName, string memberName, params Value[] arguments)
    {
        var targetType = _typeSystem.RequireClassSymbol(targetTypeName);
        var projection = new TypeProjection(instance.Class, targetType);
        
        if (!projection.IsValidProjection(_typeSystem))
        {
            throw new InvalidOperationException($"Invalid projection: {instance.Class.Name} cannot be viewed as {targetTypeName}");
        }

        // For trait projections, find the trait implementation
        if (targetType.IsTrait)
        {
            return InvokeTraitMember(instance, targetType, memberName, arguments);
        }

        // For class projections, use origin-specific dispatch
        return Invoke(instance, memberName, targetTypeName, arguments);
    }

    private Value InvokeTraitMember(ObjectValue instance, ClassTypeSymbol traitType, string memberName, Value[] arguments)
    {
        if (!instance.Rows.TryGetValue(memberName, out var implementations))
        {
            throw new InvalidOperationException($"Member '{memberName}' does not exist on {instance.Class.Name}.");
        }

        // Find the topmost implementation (trait semantics: always points to top)
        var topImplementation = implementations.FirstOrDefault();
        if (topImplementation == null)
        {
            throw new InvalidOperationException($"No implementation found for trait member '{traitType.Name}::{memberName}'");
        }

        EnsureEffectsAllowed(topImplementation.Function.Signature);
        return topImplementation.Function.Body(new InvocationContext(this, instance), arguments);
    }

    public Value Invoke(ObjectValue instance, string memberName, string? origin, params Value[] arguments)
    {
        if (!instance.Rows.TryGetValue(memberName, out var implementations))
        {
            throw new InvalidOperationException($"Member '{memberName}' does not exist on {instance.Class.Name}.");
        }

        var originSpecified = origin is not null;

        foreach (var implementation in implementations)
        {
            if (originSpecified && implementation.Member.Origin != origin)
            {
                continue;
            }

            if (!IsAccessible(implementation.Member, instance, originSpecified))
            {
                if (originSpecified && implementation.Member.Origin == origin)
                {
                    throw new InvalidOperationException($"Member '{memberName}' from {implementation.Member.Origin} is not accessible.");
                }

                continue;
            }

            if (implementation.Member.IsVirtual)
            {
                if (originSpecified)
                {
                    throw new InvalidOperationException($"Member '{memberName}' from {implementation.Member.Origin} is declared virtual and cannot be invoked.");
                }

                continue;
            }

            var function = implementation.Function;
            EnsureEffectsAllowed(function.Signature);
            return function.Body(new InvocationContext(this, instance), arguments);
        }

        if (!originSpecified && implementations.Any(m => m.Member.IsVirtual))
        {
            throw new InvalidOperationException($"Member '{memberName}' requires an override but none was provided.");
        }

        throw new InvalidOperationException($"No matching member '{memberName}' found for origin '{origin ?? "<default>"}'.");
    }

    private bool IsAccessible(RowMember member, ObjectValue instance, bool originSpecified)
    {
        return member.Access switch
        {
            AccessModifier.Public => true,
            AccessModifier.Internal => true,
            AccessModifier.Protected => originSpecified && IsDerivedFrom(instance.Class, member.Origin),
            AccessModifier.Private => originSpecified && string.Equals(instance.Class.Name, member.Origin, StringComparison.Ordinal),
            _ => false,
        };
    }

    private bool IsDerivedFrom(ClassTypeSymbol candidate, string ancestorName)
    {
        if (string.Equals(candidate.Name, ancestorName, StringComparison.Ordinal))
        {
            return true;
        }

        var ancestor = _typeSystem.RequireClassSymbol(ancestorName);
        return _typeSystem.IsSubtype(candidate, ancestor);
    }

    private sealed class EffectScope : IDisposable
    {
        private readonly ExecutionContext _context;
        private readonly ImmutableHashSet<EffectSymbol> _scope;
        private bool _disposed;

        public EffectScope(ExecutionContext context, ImmutableHashSet<EffectSymbol> scope)
        {
            _context = context;
            _scope = scope;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _context.PopEffectScope(_scope);
        }
    }
}
