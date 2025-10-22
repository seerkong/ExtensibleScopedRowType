using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace RowTypeSystem.Core.Types;

internal static class C3Linearization
{
    public static ImmutableArray<ClassTypeSymbol> Compute(ClassTypeSymbol type)
    {
        if (type.Bases.Length == 0)
        {
            return ImmutableArray.Create(type);
        }

        var sequences = new List<List<ClassTypeSymbol>>
        {
            new() { type }
        };

        foreach (var baseRef in type.Bases)
        {
            sequences.Add(baseRef.Type.MethodResolutionOrder.ToList());
        }

        sequences.Add(type.Bases.Select(b => b.Type).ToList());

        var result = new List<ClassTypeSymbol>();
        while (sequences.Count > 0)
        {
            ClassTypeSymbol? candidate = null;

            foreach (var seq in sequences)
            {
                if (seq.Count == 0)
                {
                    continue;
                }

                var head = seq[0];
                if (sequences.All(s => s.Count == 0 || s == seq || !s.Skip(1).Contains(head)))
                {
                    candidate = head;
                    break;
                }
            }

            if (candidate is null)
            {
                throw new InvalidOperationException($"Cannot compute consistent MRO for {type.Name}.");
            }

            result.Add(candidate);

            foreach (var seq in sequences.ToList())
            {
                seq.RemoveAll(t => t == candidate);
                if (seq.Count == 0)
                {
                    sequences.Remove(seq);
                }
            }
        }

        return result.ToImmutableArray();
    }
}
