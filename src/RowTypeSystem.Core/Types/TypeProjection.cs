using System.Collections.Immutable;
using System.Linq;

namespace RowTypeSystem.Core.Types;

/// <summary>
/// Represents a type projection (T as U)
/// </summary>
public sealed class TypeProjection : TypeSymbol
{
    public TypeProjection(TypeSymbol sourceType, TypeSymbol targetType)
        : base($"({sourceType.Name} as {targetType.Name})")
    {
        SourceType = sourceType;
        TargetType = targetType;
    }

    /// <summary>
    /// The original type being projected from
    /// </summary>
    public TypeSymbol SourceType { get; }

    /// <summary>
    /// The target type being projected to
    /// </summary>
    public TypeSymbol TargetType { get; }

    /// <summary>
    /// Validates that the projection is legal (source can be viewed as target)
    /// </summary>
    public bool IsValidProjection(TypeSystem typeSystem)
    {
        // For row types, check if source is a subtype of target
        if (SourceType is RowTypeSymbol sourceRow && TargetType is RowTypeSymbol targetRow)
        {
            return typeSystem.IsSubtype(sourceRow, targetRow);
        }

        // For class types, check inheritance relationship
        if (SourceType is ClassTypeSymbol sourceClass && TargetType is ClassTypeSymbol targetClass)
        {
            if (targetClass.IsTrait)
            {
                return sourceClass.MethodResolutionOrder.Contains(targetClass);
            }

            return typeSystem.IsSubtype(sourceClass, targetClass) ||
                   sourceClass.MethodResolutionOrder.Contains(targetClass);
        }

        return false;
    }
}
