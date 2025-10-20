using System.Collections.Immutable;
using System.Linq;

namespace RowLang.Core.Types;

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
