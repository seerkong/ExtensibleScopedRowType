using System.Collections.Immutable;

namespace RowTypeSystem.Core.Types;

/// <summary>
/// Represents a type parameter in a generic type definition
/// </summary>
public sealed class TypeParameter : TypeSymbol
{
    public TypeParameter(string name, ImmutableArray<TypeSymbol> constraints = default)
        : base(name)
    {
        Constraints = constraints.IsDefault ? ImmutableArray<TypeSymbol>.Empty : constraints;
    }

    /// <summary>
    /// Type constraints for this parameter (e.g., where T : SomeType)
    /// </summary>
    public ImmutableArray<TypeSymbol> Constraints { get; }

    /// <summary>
    /// Whether this is a row parameter (can be spread with ..)
    /// </summary>
    public bool IsRowParameter { get; init; }

    public override string ToString() => IsRowParameter ? $"..{Name}" : Name;
}

/// <summary>
/// Represents a generic type with type parameters
/// </summary>
public abstract class GenericTypeSymbol : TypeSymbol
{
    protected GenericTypeSymbol(string name, ImmutableArray<TypeParameter> typeParameters)
        : base(name)
    {
        TypeParameters = typeParameters;
    }

    public ImmutableArray<TypeParameter> TypeParameters { get; }

    /// <summary>
    /// Creates a concrete type by substituting type arguments
    /// </summary>
    public abstract TypeSymbol Instantiate(ImmutableArray<TypeSymbol> typeArguments);

    protected void ValidateTypeArguments(ImmutableArray<TypeSymbol> typeArguments)
    {
        if (typeArguments.Length != TypeParameters.Length)
        {
            throw new ArgumentException(
                $"Expected {TypeParameters.Length} type arguments, got {typeArguments.Length}");
        }

        for (int i = 0; i < TypeParameters.Length; i++)
        {
            var parameter = TypeParameters[i];
            var argument = typeArguments[i];

            // Validate constraints
            foreach (var constraint in parameter.Constraints)
            {
                if (!IsCompatibleWithConstraint(argument, constraint))
                {
                    throw new ArgumentException(
                        $"Type argument '{argument.Name}' does not satisfy constraint '{constraint.Name}' for parameter '{parameter.Name}'");
                }
            }
        }
    }

    private static bool IsCompatibleWithConstraint(TypeSymbol argument, TypeSymbol constraint)
    {
        // Basic compatibility check - can be enhanced with proper subtyping
        return argument.Name == constraint.Name ||
               argument is RowTypeSymbol && constraint.Name == "row" ||
               constraint.Name == "any";
    }
}