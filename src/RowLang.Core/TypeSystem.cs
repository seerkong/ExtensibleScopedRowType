using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RowLang.Core.Runtime;
using RowLang.Core.Types;

namespace RowLang.Core;

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
        _registry.Register(symbol.Rows);
        return definition;
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
}
