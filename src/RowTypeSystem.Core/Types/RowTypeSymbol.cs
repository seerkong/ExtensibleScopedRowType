using System.Collections.Immutable;
using System.Linq;

namespace RowTypeSystem.Core.Types;

/// <summary>
/// Represents a generic row type with type parameters
/// </summary>
public sealed class GenericRowTypeSymbol : GenericTypeSymbol
{
    private readonly ImmutableArray<RowMember> _members;
    private readonly bool _isOpen;

    public GenericRowTypeSymbol(
        string name,
        ImmutableArray<TypeParameter> typeParameters,
        ImmutableArray<RowMember> members,
        bool isOpen)
        : base(name, typeParameters)
    {
        _members = members;
        _isOpen = isOpen;
    }

    public ImmutableArray<RowMember> Members => _members;
    public bool IsOpen => _isOpen;

    public override TypeSymbol Instantiate(ImmutableArray<TypeSymbol> typeArguments)
    {
        ValidateTypeArguments(typeArguments);

        // Create substitution map
        var substitutions = new Dictionary<string, TypeSymbol>();
        for (int i = 0; i < TypeParameters.Length; i++)
        {
            var parameter = TypeParameters[i];
            var argument = typeArguments[i];

            if (parameter.IsRowParameter && argument is not RowTypeSymbol)
            {
                throw new ArgumentException(
                    $"Type argument '{argument.Name}' for parameter '{parameter.Name}' must be a row type.");
            }

            substitutions[parameter.Name] = argument;
        }

        // Substitute type parameters in members
        var instantiatedMembers = ImmutableArray.CreateBuilder<RowMember>();
        var spreadRows = new List<RowTypeSymbol>();

        foreach (var member in _members)
        {
            if (member.IsSpreadParameter)
            {
                // Handle spread parameter ..Q
                var paramName = member.Name.TrimStart('.'); // Remove ".." prefix
                if (substitutions.TryGetValue(paramName, out var substitution))
                {
                    if (substitution is RowTypeSymbol rowType)
                    {
                        spreadRows.Add(rowType);
                    }
                    else
                    {
                        throw new ArgumentException($"Spread parameter '{paramName}' must be instantiated with a row type");
                    }
                }
                continue;
            }

            var instantiatedType = SubstituteType(member.Type, substitutions);
            instantiatedMembers.Add(member with { Type = instantiatedType });
        }

        // Create base row type
        var typeArgumentNames = string.Join(",", typeArguments.Select(static t => t.Name));
        var instanceName = $"{Name}<{typeArgumentNames}>";
        var baseRow = new RowTypeSymbol(instanceName, instantiatedMembers.ToImmutable(), _isOpen);

        // Merge with spread rows
        var result = baseRow;
        foreach (var spreadRow in spreadRows)
        {
            // Instead of using Append which changes the name, manually merge members
            var mergedMembers = result.Members.AddRange(spreadRow.Members);
            result = new RowTypeSymbol(instanceName, mergedMembers, result.IsOpen || spreadRow.IsOpen);
        }

        return result;
    }

    private static TypeSymbol SubstituteType(TypeSymbol type, Dictionary<string, TypeSymbol> substitutions)
    {
        if (type is TypeParameter typeParam && substitutions.TryGetValue(typeParam.Name, out var substitution))
        {
            return substitution;
        }

        if (type is FunctionTypeSymbol funcType)
        {
            var newParams = funcType.Parameters.Select(p => SubstituteType(p, substitutions)).ToImmutableArray();
            var newReturn = SubstituteType(funcType.ReturnType, substitutions);
            return new FunctionTypeSymbol(funcType.Name, newParams, newReturn, funcType.Effects);
        }

        return type;
    }
}

public sealed class RowTypeSymbol : TypeSymbol
{
    private readonly ImmutableArray<RowMember> _members;

    public RowTypeSymbol(string name, IEnumerable<RowMember> members, bool isOpen)
        : base(name)
    {
        _members = members.ToImmutableArray();
        IsOpen = isOpen;
    }

    public bool IsOpen { get; }

    public ImmutableArray<RowMember> Members => _members;

    public RowMember? Resolve(string name, string? origin = null)
    {
        foreach (var member in _members)
        {
            if (member.Name == name && (origin is null || member.Origin == origin))
            {
                if (member.IsVirtual)
                {
                    throw new InvalidOperationException($"Member '{name}' declared virtual in {member.Origin} requires an override.");
                }

                return member;
            }
        }

        return null;
    }

    public RowTypeSymbol Append(RowTypeSymbol other)
    {
        var members = _members.AddRange(other._members);
        var open = IsOpen || other.IsOpen;
        return new RowTypeSymbol($"{Name}&{other.Name}", members, open);
    }

    public IEnumerable<RowMember> EnumerateByName(string name)
        => _members.Where(m => m.Name == name);
}
