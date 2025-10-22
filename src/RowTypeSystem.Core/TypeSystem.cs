using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RowTypeSystem.Core.Runtime;
using RowTypeSystem.Core.Types;

namespace RowTypeSystem.Core;

public sealed class TypeSystem
{
    private readonly TypeRegistry _registry = new();
    private readonly Dictionary<string, ClassDefinition> _classes = new();

    public TypeSystem()
    {
        DefineClass(
            name: "object",
            members: Array.Empty<RowMember>(),
            isOpen: true,
            bases: Array.Empty<(string Name, InheritanceKind Inheritance, AccessModifier Access)>(),
            methodBodies: Array.Empty<MethodBody>());
    }

    public TypeRegistry Registry => _registry;

    public ClassDefinition DefineClass(
        string name,
        IEnumerable<RowMember> members,
        bool isOpen,
        IEnumerable<(string Name, InheritanceKind Inheritance, AccessModifier Access)> bases,
        IEnumerable<MethodBody> methodBodies,
        bool isTrait = false)
    {
        var declared = new RowTypeSymbol(name + ".decl", members, isOpen);

        var baseRefs = new List<BaseTypeReference>();
        foreach (var entry in bases)
        {
            if (!_classes.TryGetValue(entry.Name, out var baseType))
            {
                throw new InvalidOperationException($"Base type '{entry.Name}' is not defined.");
            }

            baseRefs.Add(new BaseTypeReference(baseType.Type, entry.Inheritance, entry.Access));
        }

        ImmutableArray<ClassTypeSymbol> MroFactory(ClassTypeSymbol type)
        {
            return C3Linearization.Compute(type);
        }

        RowTypeSymbol RowFactory(ClassTypeSymbol type)
        {
            return RowTypeBuilder.BuildForClass(type);
        }

        var symbol = new ClassTypeSymbol(name, declared, baseRefs, isTrait, MroFactory, RowFactory);
        var definition = new ClassDefinition(symbol, methodBodies.ToImmutableArray());
        _classes[name] = definition;
        _registry.RegisterLazy(symbol.Name + ".rows", () => symbol.Rows);
        return definition;
    }

    public GenericRowTypeSymbol DefineGenericRowType(
        string name,
        IEnumerable<TypeParameter> typeParameters,
        IEnumerable<RowMember> members,
        bool isOpen)
    {
        var genericRow = new GenericRowTypeSymbol(
            name,
            typeParameters.ToImmutableArray(),
            members.ToImmutableArray(),
            isOpen);

        _registry.Register(genericRow);
        return genericRow;
    }

    public RowTypeSymbol InstantiateGenericRowType(
        GenericRowTypeSymbol genericType,
        params TypeSymbol[] typeArguments)
    {
        var instantiated = (RowTypeSymbol)genericType.Instantiate(typeArguments.ToImmutableArray());
        _registry.Register(instantiated);
        return instantiated;
    }

    public RowTypeSymbol DefineRowType(string name, IEnumerable<RowMember> members, bool isOpen)
    {
        var row = new RowTypeSymbol(name, members, isOpen);
        _registry.Register(row);
        return row;
    }

    public ClassDefinition RequireClass(string name)
    {
        if (_classes.TryGetValue(name, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Class '{name}' is not registered.");
    }

    public ClassTypeSymbol RequireClassSymbol(string name) => RequireClass(name).Type;

    public RowTypeSymbol MergeRows(string resultName, params RowTypeSymbol[] rows)
    {
        if (rows.Length == 0)
        {
            throw new ArgumentException("At least one row type is required.", nameof(rows));
        }

        var merged = rows[0];
        for (var i = 1; i < rows.Length; i++)
        {
            merged = merged.Append(rows[i]);
        }

        return new RowTypeSymbol(resultName, merged.Members, merged.IsOpen);
    }

    public bool IsSubtype(ClassTypeSymbol candidate, ClassTypeSymbol target)
        => IsSubtype(candidate.Rows, target.Rows);

    public bool IsSubtype(RowTypeSymbol candidate, RowTypeSymbol target)
    {
        var remaining = new List<RowMember>();
        foreach (var member in candidate.Members)
        {
            if (!MemberCanSatisfyRequirement(member))
            {
                continue;
            }

            remaining.Add(member);
        }

        foreach (var required in target.Members)
        {
            var matchIndex = FindCompatibleMemberIndex(remaining, required);
            if (matchIndex < 0)
            {
                return false;
            }

            remaining.RemoveAt(matchIndex);
        }

        return true;
    }

    private static bool MemberCanSatisfyRequirement(RowMember member) => !member.IsVirtual;

    private int FindCompatibleMemberIndex(List<RowMember> available, RowMember required)
    {
        for (var i = 0; i < available.Count; i++)
        {
            var candidate = available[i];
            if (candidate.Name != required.Name)
            {
                continue;
            }

            if (!AreTypesCompatible(candidate.Type, required.Type))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private bool AreTypesCompatible(TypeSymbol candidate, TypeSymbol required)
    {
        if (ReferenceEquals(candidate, required) || candidate.Name == required.Name)
        {
            return true;
        }

        if (candidate is FunctionTypeSymbol candidateFunction && required is FunctionTypeSymbol requiredFunction)
        {
            if (candidateFunction.Parameters.Length != requiredFunction.Parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < candidateFunction.Parameters.Length; i++)
            {
                if (!AreTypesCompatible(candidateFunction.Parameters[i], requiredFunction.Parameters[i]))
                {
                    return false;
                }
            }

            if (!AreTypesCompatible(candidateFunction.ReturnType, requiredFunction.ReturnType))
            {
                return false;
            }

            if (!EffectSetsEqual(candidateFunction.Effects, requiredFunction.Effects))
            {
                return false;
            }

            return true;
        }

        if (candidate is RowTypeSymbol candidateRow && required is RowTypeSymbol requiredRow)
        {
            return IsSubtype(candidateRow, requiredRow);
        }

        return false;
    }

    private static bool EffectSetsEqual(ImmutableArray<EffectSymbol> left, ImmutableArray<EffectSymbol> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        if (left.IsDefaultOrEmpty && right.IsDefaultOrEmpty)
        {
            return true;
        }

        var leftNames = left.Select(e => e.Name).OrderBy(static n => n).ToArray();
        var rightNames = right.Select(e => e.Name).OrderBy(static n => n).ToArray();
        return leftNames.SequenceEqual(rightNames);
    }
}
