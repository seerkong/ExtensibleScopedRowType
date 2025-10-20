using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RowLang.Core.Syntax;
using RowLang.Core.Runtime;
using RowLang.Core.Types;

namespace RowLang.Core.Scripting;

public sealed class RowLangModule
{
    private readonly TypeSystem _typeSystem;

    internal RowLangModule(TypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
    }

    public TypeSystem TypeSystem => _typeSystem;

    public RowLang.Core.Runtime.ExecutionContext CreateExecutionContext() => new RowLang.Core.Runtime.ExecutionContext(_typeSystem);
}

public static class RowLangScript
{
    public static RowLangModule Compile(string source)
    {
        var parser = new SExprParser(source);
        var nodes = parser.Parse();

        if (nodes.Length == 0)
        {
            throw new InvalidOperationException("Script does not contain any forms.");
        }

        var forms = nodes;
        if (nodes.Length == 1 && nodes[0] is SExprList moduleList && TryMatchIdentifier(moduleList, 0, "module"))
        {
            forms = moduleList.Elements[1..];
        }

        var typeSystem = new TypeSystem();
        var builder = new ModuleBuilder(typeSystem);
        foreach (var form in forms)
        {
            builder.Process(form);
        }

        return new RowLangModule(typeSystem);
    }

    private static bool TryMatchIdentifier(SExprList list, int index, string name)
    {
        if (index >= list.Elements.Length)
        {
            return false;
        }

        if (list.Elements[index] is not SExprIdentifier identifier)
        {
            return false;
        }

        return string.Equals(identifier.QualifiedName, name, StringComparison.Ordinal);
    }

    private sealed class ModuleBuilder
    {
        private readonly TypeSystem _typeSystem;
        private readonly TypeRegistry _registry;

        public ModuleBuilder(TypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            _registry = typeSystem.Registry;
        }

        public void Process(SExprNode node)
        {
            if (node.PrefixAnnotations.Count > 0 || node.PostfixAnnotations.Count > 0)
            {
                throw new InvalidOperationException("Annotations are not supported on top-level forms.");
            }

            if (node is not SExprList list || list.Elements.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException("Top-level form must be a non-empty list.");
            }

            var head = ExpectIdentifier(list.Elements[0], "form head");
            switch (head.QualifiedName)
            {
                case "effect":
                    DefineEffect(list);
                    break;
                case "class":
                    DefineClass(list);
                    break;
                case "row-type":
                    DefineRowType(list);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown top-level form '{head.QualifiedName}'.");
            }
        }

        private void DefineEffect(SExprList list)
        {
            if (list.Elements.Length != 2)
            {
                throw new InvalidOperationException("(effect <name>) expects exactly one argument.");
            }

            var effectName = ExpectIdentifier(list.Elements[1], "effect name").QualifiedName;
            _registry.GetOrCreateEffect(effectName);
        }

        private void DefineClass(SExprList list)
        {
            if (list.Elements.Length < 2)
            {
                throw new InvalidOperationException("(class <name> ...) requires a class name.");
            }

            var className = ExpectIdentifier(list.Elements[1], "class name").QualifiedName;
            var isOpen = true;
            var isTrait = false;
            var bases = new List<(string Name, InheritanceKind Inheritance, AccessModifier Access)>();
            var members = new List<RowMember>();
            var methods = new List<MethodBody>();

            foreach (var clauseNode in list.Elements[2..])
            {
                if (clauseNode is not SExprList clause || clause.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Class clause must be a non-empty list.");
                }

                var clauseHead = ExpectIdentifier(clause.Elements[0], "class clause");
                switch (clauseHead.QualifiedName)
                {
                    case "open":
                        isOpen = true;
                        break;
                    case "closed":
                    case "sealed":
                        isOpen = false;
                        break;
                    case "trait":
                        isTrait = true;
                        break;
                    case "bases":
                        ParseBases(clause, bases);
                        break;
                    case "method":
                        var method = ParseMethod(className, clause);
                        members.Add(method.Member);
                        methods.Add(method);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown class clause '{clauseHead.QualifiedName}'.");
                }
            }

            _typeSystem.DefineClass(className, members, isOpen, bases, methods, isTrait);
        }

        private void DefineRowType(SExprList list)
        {
            if (list.Elements.Length < 2)
            {
                throw new InvalidOperationException("(row-type <name> ...) requires a name.");
            }

            var rowName = ExpectIdentifier(list.Elements[1], "row type name").QualifiedName;
            var isOpen = true;
            var baseRows = new List<RowTypeSymbol>();
            var members = new List<RowMember>();

            foreach (var clauseNode in list.Elements[2..])
            {
                if (clauseNode is not SExprList clause || clause.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Row type clause must be a non-empty list.");
                }

                var clauseHead = ExpectIdentifier(clause.Elements[0], "row type clause");
                switch (clauseHead.QualifiedName)
                {
                    case "open":
                        isOpen = true;
                        break;
                    case "closed":
                    case "sealed":
                        isOpen = false;
                        break;
                    case "extends":
                        foreach (var entry in clause.Elements[1..])
                        {
                            var baseName = ExpectIdentifier(entry, "row type base").QualifiedName;
                            var symbol = _registry.Require(baseName);
                            if (symbol is not RowTypeSymbol row)
                            {
                                throw new InvalidOperationException($"Type '{baseName}' is not a row type.");
                            }

                            baseRows.Add(row);
                            if (row.IsOpen)
                            {
                                isOpen = true;
                            }
                        }

                        break;
                    case "method":
                        var methodSpec = ParseMethodSpecification(rowName, clause, allowBody: false);
                        if (methodSpec.BodyNode is not null)
                        {
                            throw new InvalidOperationException($"Row type method '{methodSpec.Name}' cannot declare a body.");
                        }

                        members.Add(RowMemberBuilder.Method(rowName, methodSpec.Name, methodSpec.Signature, methodSpec.Qualifier));
                        break;
                    case "field":
                        members.Add(ParseField(rowName, clause));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown row type clause '{clauseHead.QualifiedName}'.");
                }
            }

            var aggregated = new List<RowMember>();
            foreach (var baseRow in baseRows)
            {
                aggregated.AddRange(baseRow.Members);
            }

            aggregated.AddRange(members);

            _typeSystem.DefineRowType(rowName, aggregated, isOpen);
        }

        private void ParseBases(SExprList clause, List<(string, InheritanceKind, AccessModifier)> bases)
        {
            foreach (var node in clause.Elements[1..])
            {
                if (node is not SExprList baseSpec || baseSpec.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("(bases ...) entries must be lists.");
                }

                var baseName = ExpectIdentifier(baseSpec.Elements[0], "base name").QualifiedName;
                var inheritance = InheritanceKind.Real;
                var access = AccessModifier.Public;

                if (baseSpec.Elements.Length > 1)
                {
                    inheritance = ParseInheritance(ExpectIdentifier(baseSpec.Elements[1], "inheritance kind"));
                }

                if (baseSpec.Elements.Length > 2)
                {
                    access = ParseAccess(ExpectIdentifier(baseSpec.Elements[2], "access modifier"));
                }

                bases.Add((baseName, inheritance, access));
            }
        }

        private static InheritanceKind ParseInheritance(SExprIdentifier identifier)
            => identifier.QualifiedName switch
            {
                "virtual" => InheritanceKind.Virtual,
                "real" => InheritanceKind.Real,
                _ => throw new InvalidOperationException($"Unknown inheritance kind '{identifier.QualifiedName}'."),
            };

        private static AccessModifier ParseAccess(SExprIdentifier identifier)
            => identifier.QualifiedName switch
            {
                "private" => AccessModifier.Private,
                "protected" => AccessModifier.Protected,
                "internal" => AccessModifier.Internal,
                "public" => AccessModifier.Public,
                _ => throw new InvalidOperationException($"Unknown access modifier '{identifier.QualifiedName}'."),
            };

        private MethodBody ParseMethod(string className, SExprList clause)
        {
            var specification = ParseMethodSpecification(className, clause, allowBody: true);
            if (specification.BodyNode is null)
            {
                throw new InvalidOperationException($"Method '{specification.Name}' is missing a body.");
            }

            var implementation = CompileBody(specification.BodyNode, specification.Signature.ReturnType);
            return MethodBuilder.FromLambda(
                specification.Owner,
                specification.Name,
                specification.Signature,
                implementation,
                specification.Qualifier);
        }

        private MethodSpecification ParseMethodSpecification(string owner, SExprList clause, bool allowBody)
        {
            if (clause.Elements.Length < 2)
            {
                throw new InvalidOperationException("(method <name> ...) requires a method name.");
            }

            var methodName = ExpectIdentifier(clause.Elements[1], "method name").QualifiedName;
            var parameters = new List<ParameterSyntax>();
            TypeSymbol? returnType = null;
            var effects = new List<EffectSymbol>();
            RowQualifier qualifier = RowQualifier.Default;
            SExprNode? bodyNode = null;

            foreach (var element in clause.Elements[2..])
            {
                if (element is not SExprList section || section.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Method section must be a non-empty list.");
                }

                var sectionHead = ExpectIdentifier(section.Elements[0], "method section");
                switch (sectionHead.QualifiedName)
                {
                    case "params":
                        parameters.AddRange(ParseParameters(section));
                        break;
                    case "return":
                        if (section.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(return <type>) expects exactly one argument.");
                        }

                        returnType = ResolveType(section.Elements[1], "return type");
                        break;
                    case "effects":
                        foreach (var effectNode in section.Elements[1..])
                        {
                            var effectName = ExpectIdentifier(effectNode, "effect name").QualifiedName;
                            effects.Add(_registry.GetOrCreateEffect(effectName));
                        }

                        break;
                    case "qualifier":
                        if (section.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(qualifier <name>) expects exactly one argument.");
                        }

                        qualifier = ParseQualifier(ExpectIdentifier(section.Elements[1], "qualifier"));
                        break;
                    case "body":
                        if (!allowBody)
                        {
                            throw new InvalidOperationException("Row type methods cannot declare bodies.");
                        }

                        if (section.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(body <expr>) expects exactly one expression.");
                        }

                        bodyNode = section.Elements[1];
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown method section '{sectionHead.QualifiedName}'.");
                }
            }

            if (returnType is null)
            {
                throw new InvalidOperationException($"Method '{methodName}' is missing a return type.");
            }

            var signature = _registry.CreateFunctionType(
                $"{owner}::{methodName}",
                parameters.Select(static p => p.Type),
                returnType,
                effects);

            return new MethodSpecification(
                owner,
                methodName,
                parameters.ToImmutableArray(),
                signature,
                qualifier,
                bodyNode);
        }

        private sealed record ParameterSyntax(string? Name, TypeSymbol Type);

        private sealed record MethodSpecification(
            string Owner,
            string Name,
            ImmutableArray<ParameterSyntax> Parameters,
            FunctionTypeSymbol Signature,
            RowQualifier Qualifier,
            SExprNode? BodyNode);

        private IEnumerable<ParameterSyntax> ParseParameters(SExprList section)
        {
            foreach (var paramNode in section.Elements[1..])
            {
                switch (paramNode)
                {
                    case SExprIdentifier identifier when identifier.TypeAnnotation is not null:
                    {
                        var type = ResolveType(identifier.TypeAnnotation, "parameter type");
                        yield return new ParameterSyntax(identifier.QualifiedName, type);
                        break;
                    }
                    case SExprList list when !list.Elements.IsDefaultOrEmpty:
                    {
                        var type = ResolveType(list.Elements[0], "parameter type");
                        string? name = null;
                        if (list.Elements.Length > 1)
                        {
                            name = ExpectIdentifier(list.Elements[1], "parameter name").QualifiedName;
                        }

                        yield return new ParameterSyntax(name, type);
                        break;
                    }
                    default:
                    {
                        var type = ResolveType(paramNode, "parameter type");
                        yield return new ParameterSyntax(null, type);
                        break;
                    }
                }
            }
        }

        private RowQualifier ParseQualifier(SExprIdentifier identifier)
            => identifier.QualifiedName switch
            {
                "inherit" => RowQualifier.Inherit,
                "virtual" => RowQualifier.Virtual,
                "override" => RowQualifier.Override,
                "final" => RowQualifier.Final,
                _ => throw new InvalidOperationException($"Unknown qualifier '{identifier.QualifiedName}'."),
            };

        private RowMember ParseField(string owner, SExprList clause)
        {
            if (clause.Elements.Length < 2)
            {
                throw new InvalidOperationException("(field <name> ~ <type>) requires a member specification.");
            }

            var descriptor = clause.Elements[1];
            string fieldName;
            TypeSymbol fieldType;

            switch (descriptor)
            {
                case SExprIdentifier identifier when identifier.TypeAnnotation is not null:
                    fieldName = identifier.QualifiedName;
                    fieldType = ResolveType(identifier.TypeAnnotation, "field type");
                    break;
                case SExprList list when !list.Elements.IsDefaultOrEmpty:
                    fieldType = ResolveType(list.Elements[0], "field type");
                    if (list.Elements.Length < 2)
                    {
                        throw new InvalidOperationException("Field name is required when using list specification.");
                    }

                    fieldName = ExpectIdentifier(list.Elements[1], "field name").QualifiedName;
                    break;
                default:
                    throw new InvalidOperationException("Field specification must be a typed identifier or list.");
            }

            var qualifier = RowQualifier.Default;
            foreach (var option in clause.Elements[2..])
            {
                if (option is not SExprList optionList || optionList.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Field option must be a non-empty list.");
                }

                var optionHead = ExpectIdentifier(optionList.Elements[0], "field option");
                switch (optionHead.QualifiedName)
                {
                    case "qualifier":
                        if (optionList.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(qualifier <name>) expects exactly one argument.");
                        }

                        qualifier = ParseQualifier(ExpectIdentifier(optionList.Elements[1], "qualifier"));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown field option '{optionHead.QualifiedName}'.");
                }
            }

            return RowMemberBuilder.Field(owner, fieldName, fieldType, qualifier);
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileBody(SExprNode node, TypeSymbol returnType)
        {
            if (node is SExprList list && !list.Elements.IsDefaultOrEmpty)
            {
                var head = ExpectIdentifier(list.Elements[0], "body expression");
                switch (head.QualifiedName)
                {
                    case "const":
                        if (list.Elements.Length != 3)
                        {
                            throw new InvalidOperationException("(const <type> <value>) expects exactly two arguments.");
                        }

                        return CompileConst(list.Elements[1], list.Elements[2], returnType);
                }
            }

            throw new InvalidOperationException("Unsupported method body expression.");
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileConst(
            SExprNode typeNode,
            SExprNode valueNode,
            TypeSymbol returnType)
        {
            var typeName = ExpectIdentifier(typeNode, "const type").QualifiedName;
            return typeName switch
            {
                "str" => CompileStringConst(valueNode, returnType),
                "int" => CompileIntConst(valueNode, returnType),
                "bool" => CompileBoolConst(valueNode, returnType),
                _ => throw new InvalidOperationException($"Unsupported const type '{typeName}'."),
            };
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileStringConst(SExprNode node, TypeSymbol returnType)
        {
            EnsureReturnCompatibility(returnType, _registry.String, "str");
            var text = node switch
            {
                SExprIdentifier identifier => identifier.QualifiedName,
                SExprString str => str.Value,
                _ => throw new InvalidOperationException($"Expected string literal but found '{node}'."),
            };
            var value = new StringValue(text);
            return (_, _) => value;
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileIntConst(SExprNode node, TypeSymbol returnType)
        {
            EnsureReturnCompatibility(returnType, _registry.Int, "int");
            var text = ExpectIdentifier(node, "int literal").QualifiedName;
            if (!int.TryParse(text, out var parsed))
            {
                throw new InvalidOperationException($"Invalid int literal '{text}'.");
            }

            var value = new IntValue(parsed);
            return (_, _) => value;
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileBoolConst(SExprNode node, TypeSymbol returnType)
        {
            EnsureReturnCompatibility(returnType, _registry.Bool, "bool");
            var text = ExpectIdentifier(node, "bool literal").QualifiedName;
            if (!bool.TryParse(text, out var parsed))
            {
                throw new InvalidOperationException($"Invalid bool literal '{text}'.");
            }

            var value = new BoolValue(parsed);
            return (_, _) => value;
        }

        private void EnsureReturnCompatibility(TypeSymbol declared, TypeSymbol required, string description)
        {
            if (ReferenceEquals(declared, required))
            {
                return;
            }

            if (declared.Name.Equals(required.Name, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(declared.Name, "any", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cannot return {description} from method declared with return type '{declared.Name}'.");
            }
        }

        private TypeSymbol ResolveType(SExprNode node, string description)
        {
            var identifier = ExpectIdentifier(node, description);
            return _registry.Require(identifier.QualifiedName);
        }

        private static SExprIdentifier ExpectIdentifier(SExprNode node, string description)
        {
            if (node is not SExprIdentifier identifier)
            {
                throw new InvalidOperationException($"Expected identifier for {description} but found '{node}'.");
            }

            if (identifier.PrefixAnnotations.Count > 0 || identifier.PostfixAnnotations.Count > 0)
            {
                throw new InvalidOperationException($"Annotations are not supported on identifier '{identifier.QualifiedName}'.");
            }

            return identifier;
        }
    }
}
