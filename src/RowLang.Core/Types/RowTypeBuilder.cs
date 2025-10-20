using System;
using System.Collections.Generic;
using System.Linq;

namespace RowLang.Core.Types;

public static class RowTypeBuilder
{
    public static RowTypeSymbol BuildForClass(ClassTypeSymbol type)
    {
        var members = new List<RowMember>();
        var existing = new HashSet<(string Name, string Origin)>();
        var blockedByVirtual = new HashSet<string>();
        var finalMembers = new HashSet<string>();
        var overridesAwaitingBase = new HashSet<string>();
        var inheritsAwaitingBase = new HashSet<string>();

        foreach (var ancestor in type.MethodResolutionOrder)
        {
            foreach (var original in ancestor.DeclaredRows.Members)
            {
                var member = ApplyEffectiveAccess(type, original);
                var key = (member.Name, member.Origin);
                if (existing.Contains(key))
                {
                    continue;
                }

                if (finalMembers.Contains(member.Name))
                {
                    continue;
                }

                if (member.IsVirtual)
                {
                    blockedByVirtual.Add(member.Name);
                    RemoveExistingMembers(type, ancestor, members, existing, member.Name);
                    members.Add(member);
                    existing.Add(key);
                    overridesAwaitingBase.Remove(member.Name);
                    continue;
                }

                if (member.IsOverride || member.IsInherit)
                {
                    if (finalMembers.Contains(member.Name))
                    {
                        throw new InvalidOperationException($"Cannot override final member '{member.Name}' in {type.Name}.");
                    }

                    if (!blockedByVirtual.Contains(member.Name) && members.All(m => m.Name != member.Name))
                    {
                        if (member.IsOverride)
                        {
                            overridesAwaitingBase.Add(member.Name);
                        }
                        else
                        {
                            inheritsAwaitingBase.Add(member.Name);
                        }
                        members.Add(member);
                        existing.Add(key);
                    }
                    else
                    {
                        blockedByVirtual.Remove(member.Name);
                        overridesAwaitingBase.Remove(member.Name);
                        inheritsAwaitingBase.Remove(member.Name);
                        members.Add(member);
                        existing.Add(key);
                    }
                }
                else if (blockedByVirtual.Contains(member.Name))
                {
                    // waiting for an override
                    continue;
                }
                else
                {
                    if (member.IsFinal && members.Any(m => m.Name == member.Name && m.Origin != member.Origin))
                    {
                        throw new InvalidOperationException($"Cannot override final member '{member.Name}' in {type.Name}.");
                    }

                    members.Add(member);
                    existing.Add(key);
                    overridesAwaitingBase.Remove(member.Name);
                    inheritsAwaitingBase.Remove(member.Name);
                }

                if (member.IsFinal)
                {
                    finalMembers.Add(member.Name);
                }
            }
        }

        if (overridesAwaitingBase.Count > 0)
        {
            throw new InvalidOperationException($"Override specified without base implementation for: {string.Join(", ", overridesAwaitingBase)}");
        }

        if (inheritsAwaitingBase.Count > 0)
        {
            throw new InvalidOperationException($"inherit specified without base implementation for: {string.Join(", ", inheritsAwaitingBase)}");
        }

        return new RowTypeSymbol(type.Name + ".rows", members, type.DeclaredRows.IsOpen);
    }

    private static void RemoveExistingMembers(
        ClassTypeSymbol targetType,
        ClassTypeSymbol ancestor,
        List<RowMember> members,
        HashSet<(string Name, string Origin)> existing,
        string name)
    {
        for (var i = members.Count - 1; i >= 0; i--)
        {
            var candidate = members[i];
            if (!string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                continue;
            }

            var originClass = targetType.MethodResolutionOrder.FirstOrDefault(t => string.Equals(t.Name, candidate.Origin, StringComparison.Ordinal));

            var keep = string.Equals(candidate.Origin, targetType.Name, StringComparison.Ordinal);
            if (!keep && originClass is not null && !ReferenceEquals(originClass, ancestor))
            {
                keep = originClass.MethodResolutionOrder.Contains(ancestor);
            }

            if (keep && !string.Equals(candidate.Origin, ancestor.Name, StringComparison.Ordinal))
            {
                continue;
            }

            existing.Remove((candidate.Name, candidate.Origin));
            members.RemoveAt(i);
        }
    }

    private static RowMember ApplyEffectiveAccess(ClassTypeSymbol type, RowMember member)
    {
        var pathAccess = ComputeAccessModifier(type, member.Origin);
        var effective = MinAccess(member.Access, pathAccess);
        return member with { Access = effective };
    }

    private static AccessModifier ComputeAccessModifier(ClassTypeSymbol type, string ancestorName)
    {
        if (string.Equals(type.Name, ancestorName, StringComparison.Ordinal))
        {
            return AccessModifier.Public;
        }

        if (TryComputeAccess(type, ancestorName, out var access))
        {
            return access;
        }

        return AccessModifier.Public;
    }

    private static bool TryComputeAccess(ClassTypeSymbol type, string targetName, out AccessModifier access)
    {
        foreach (var baseRef in type.Bases)
        {
            if (string.Equals(baseRef.Type.Name, targetName, StringComparison.Ordinal))
            {
                access = baseRef.AccessModifier;
                return true;
            }

            if (TryComputeAccess(baseRef.Type, targetName, out var downstream))
            {
                access = MinAccess(baseRef.AccessModifier, downstream);
                return true;
            }
        }

        access = AccessModifier.Public;
        return false;
    }

    private static AccessModifier MinAccess(AccessModifier first, AccessModifier second)
        => (AccessModifier)Math.Min((int)first, (int)second);
}
