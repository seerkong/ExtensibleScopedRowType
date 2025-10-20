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
            foreach (var member in ancestor.DeclaredRows.Members)
            {
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
}
