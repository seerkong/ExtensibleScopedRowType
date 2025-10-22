using System;

namespace Kon.Core.Node;

/// <summary>
/// Represents a method bound to a target instance.
/// This type is created when accessing a method on any KnNode (e.g., obj.method, array.Count).
/// The bound method can later be invoked with arguments, automatically passing
/// the bound target as the first 'self' parameter.
///
/// Supports type projection via the ProjectedType property for future (obj as Type)::method syntax.
/// </summary>
public class KnBoundMethod : KnNodeBase
{
    /// <summary>
    /// The target instance this method is bound to.
    /// This will be passed as the first 'self' parameter when the method is invoked.
    /// Can be any KnNode (KnObject, KnArray, KnMap, etc.)
    /// </summary>
    public KnNode BoundTarget { get; }

    /// <summary>
    /// The name of the method to be invoked.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Optional type projection for future (obj as Type)::method syntax.
    /// When set, method lookup should be constrained to this type's scope.
    /// </summary>
    public KnNode? ProjectedType { get; set; }

    /// <summary>
    /// Creates a new bound method instance.
    /// </summary>
    /// <param name="boundTarget">The target to bind to (can be any KnNode)</param>
    /// <param name="methodName">The method name</param>
    /// <param name="projectedType">Optional type projection for scoped method resolution</param>
    public KnBoundMethod(KnNode boundTarget, string methodName, KnNode? projectedType = null)
    {
        BoundTarget = boundTarget ?? throw new ArgumentNullException(nameof(boundTarget));

        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        MethodName = methodName;
        ProjectedType = projectedType;
    }

    public override string ToString()
    {
        var typeInfo = ProjectedType != null ? $" as {ProjectedType}" : "";
        return $"KnBoundMethod({BoundTarget}::{MethodName}{typeInfo})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not KnBoundMethod other)
        {
            return false;
        }

        // Two bound methods are equal if they bind the same target and method
        return ReferenceEquals(BoundTarget, other.BoundTarget) &&
               MethodName == other.MethodName &&
               Equals(ProjectedType, other.ProjectedType);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BoundTarget, MethodName, ProjectedType);
    }
}
