using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using RowLang.Core.Syntax;
using RowLang.Core.Runtime;
using RowLang.Core.Types;

namespace RowLang.Core.Scripting;

public sealed class RowLangModule
{
    private readonly TypeSystem _typeSystem;

    internal RowLangModule(TypeSystem typeSystem, ImmutableArray<RunDirective> runs)
    {
        _typeSystem = typeSystem;
        RunDirectives = runs;
    }

    public TypeSystem TypeSystem => _typeSystem;

    public ImmutableArray<RunDirective> RunDirectives { get; }

    public RowLang.Core.Runtime.ExecutionContext CreateExecutionContext() => new RowLang.Core.Runtime.ExecutionContext(_typeSystem);

    public IEnumerable<(RunDirective Directive, Value Result)> ExecuteRuns(RowLang.Core.Runtime.ExecutionContext context)
    {
        foreach (var directive in RunDirectives)
        {
            IDisposable? scope = null;
            try
            {
                if (!directive.AllowedEffects.IsDefaultOrEmpty && directive.AllowedEffects.Length > 0)
                {
                    scope = context.PushEffectScope(directive.AllowedEffects);
                }

                var instance = context.Instantiate(directive.ClassName);
                Value result = directive.Origin is null
                    ? context.Invoke(instance, directive.MemberName)
                    : context.Invoke(instance, directive.MemberName, directive.Origin);
                yield return (directive, result);
            }
            finally
            {
                scope?.Dispose();
            }
        }
    }
}

public sealed record RunDirective(string ClassName, string MemberName, string? Origin, ImmutableArray<EffectSymbol> AllowedEffects);

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

        return new RowLangModule(typeSystem, builder.CollectRuns());
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
        private readonly List<RunDirective> _runs = new();

        public ModuleBuilder(TypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            _registry = typeSystem.Registry;
        }

        public ImmutableArray<RunDirective> CollectRuns() => _runs.ToImmutableArray();

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
                case "run":
                    DefineRun(list);
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

        private void DefineRun(SExprList list)
        {
            if (list.Elements.Length < 3)
            {
                throw new InvalidOperationException("(run <class> <member> ...) requires at least two arguments.");
            }

            var classIdentifier = ExpectIdentifier(list.Elements[1], "run class");
            var memberIdentifier = ExpectIdentifier(list.Elements[2], "run member");

            if (classIdentifier.TypeAnnotation is not null)
            {
                throw new InvalidOperationException("Run class identifier cannot include a type annotation.");
            }

            if (memberIdentifier.TypeAnnotation is not null)
            {
                throw new InvalidOperationException("Run member identifier cannot include a type annotation.");
            }

            var origin = memberIdentifier.Namespace.IsDefaultOrEmpty
                ? null
                : string.Join("::", memberIdentifier.Namespace);

            var effects = ImmutableArray.CreateBuilder<EffectSymbol>();

            foreach (var sectionNode in list.Elements[3..])
            {
                if (sectionNode is not SExprList section || section.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Run option must be a non-empty list.");
                }

                var sectionHead = ExpectIdentifier(section.Elements[0], "run option");
                switch (sectionHead.QualifiedName)
                {
                    case "effects":
                        foreach (var effectNode in FlattenArgumentNodes(section.Elements.Skip(1)))
                        {
                            var effectName = ExpectIdentifier(effectNode, "run effect").QualifiedName;
                            effects.Add(_registry.GetOrCreateEffect(effectName));
                        }

                        break;
                    default:
                        throw new InvalidOperationException($"Unknown run option '{sectionHead.QualifiedName}'.");
                }
            }

            _runs.Add(new RunDirective(classIdentifier.QualifiedName, memberIdentifier.Name, origin, effects.ToImmutable()));
        }

        private (string Name, string? DeclaredRowType, List<(string Name, InheritanceKind Inheritance, AccessModifier Access)> Bases) ParseClassHeader(SExprNode node)
        {
            var identifier = ExpectIdentifier(node, "class name", allowAnnotations: true);
            string? declaredRowTypeName = null;
            if (identifier.TypeAnnotation is not null)
            {
                declaredRowTypeName = ExpectIdentifier(identifier.TypeAnnotation, "class row type", allowAnnotations: true).QualifiedName;
            }

            var bases = new List<(string Name, InheritanceKind Inheritance, AccessModifier Access)>();
            foreach (var annotation in identifier.PostfixAnnotations)
            {
                if (annotation is not SExprList list || list.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Class annotation must be a non-empty list.");
                }

                var head = ExpectIdentifier(list.Elements[0], "class annotation");
                switch (head.QualifiedName)
                {
                    case "extends":
                        foreach (var entry in list.Elements[1..])
                        {
                            var baseName = ExpectIdentifier(entry, "base class name").QualifiedName;
                            bases.Add((baseName, InheritanceKind.Real, AccessModifier.Public));
                        }

                        break;
                    default:
                        throw new InvalidOperationException($"Unknown class annotation '{head.QualifiedName}'.");
                }
            }

            return (identifier.QualifiedName, declaredRowTypeName, bases);
        }

        private static void EnsureDefaultBase(List<(string Name, InheritanceKind Inheritance, AccessModifier Access)> bases)
        {
            if (bases.Count == 0)
            {
                bases.Add(("object", InheritanceKind.Real, AccessModifier.Public));
            }
        }

        private void DefineClass(SExprList list)
        {
            if (list.Elements.Length < 2)
            {
                throw new InvalidOperationException("(class <name> ...) requires a class name.");
            }

            var (className, declaredRowTypeName, annotatedBases) = ParseClassHeader(list.Elements[1]);
            var isOpen = true;
            var isTrait = false;
            var bases = new List<(string Name, InheritanceKind Inheritance, AccessModifier Access)>(annotatedBases);
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

            EnsureDefaultBase(bases);
            var definition = _typeSystem.DefineClass(className, members, isOpen, bases, methods, isTrait);

            if (declaredRowTypeName is not null)
            {
                if (_registry.Require(declaredRowTypeName) is not RowTypeSymbol expectedRow)
                {
                    throw new InvalidOperationException($"Type '{declaredRowTypeName}' is not a row type.");
                }

                if (!_typeSystem.IsSubtype(definition.Type.Rows, expectedRow))
                {
                    throw new InvalidOperationException($"Class '{className}' does not satisfy declared row type '{declaredRowTypeName}'.");
                }
            }
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
                if (clauseNode is SExprIdentifier identifier)
                {
                    if (TryParseRowSpread(identifier, baseRows, ref isOpen))
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"Unexpected identifier '{identifier.QualifiedName}' in row-type '{rowName}'.");
                }

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
                        foreach (var entry in FlattenArgumentNodes(clause.Elements.Skip(1)))
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

                        members.Add(RowMemberBuilder.Method(rowName, methodSpec.Name, methodSpec.Signature, methodSpec.Qualifier, methodSpec.Access));
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

        private bool TryParseRowSpread(SExprIdentifier identifier, List<RowTypeSymbol> baseRows, ref bool isOpen)
        {
            var name = identifier.QualifiedName;
            if (!name.StartsWith("..", StringComparison.Ordinal))
            {
                return false;
            }

            var target = name[2..];
            if (string.Equals(target, "never", StringComparison.Ordinal))
            {
                isOpen = false;
                return true;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("Row spread must specify a target type, e.g. '..SomeRow'.");
            }

            var symbol = _registry.Require(target);
            if (symbol is not RowTypeSymbol rowType)
            {
                throw new InvalidOperationException($"Row spread target '{target}' is not a row type.");
            }

            baseRows.Add(rowType);
            if (rowType.IsOpen)
            {
                isOpen = true;
            }

            return true;
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

            var implementation = CompileBody(specification);
            return MethodBuilder.FromLambda(
                specification.Owner,
                specification.Name,
                specification.Signature,
                implementation,
                specification.Qualifier,
                specification.Access);
        }

        private MethodSpecification ParseMethodSpecification(string owner, SExprList clause, bool allowBody)
        {
            if (clause.Elements.Length < 2)
            {
                throw new InvalidOperationException("(method <name> ...) requires a method name.");
            }

            var methodIdentifier = ExpectIdentifier(clause.Elements[1], "method name", allowAnnotations: true);
            var methodName = methodIdentifier.QualifiedName;
            RowMemberReference? rowReference = null;
            if (methodIdentifier.TypeAnnotation is not null)
            {
                var annotationIdentifier = ExpectIdentifier(methodIdentifier.TypeAnnotation, "method row reference", allowAnnotations: true);
                if (annotationIdentifier.Parts.Length < 2)
                {
                    throw new InvalidOperationException($"Row reference '{annotationIdentifier.QualifiedName}' must include an origin and member name.");
                }

                var rowTypeName = string.Join("::", annotationIdentifier.Parts[..^1]);
                var memberName = annotationIdentifier.Parts[^1];
                rowReference = new RowMemberReference(rowTypeName, memberName);
            }

            var parameters = new List<ParameterSyntax>();
            TypeSymbol? returnType = null;
            var effects = new List<EffectSymbol>();
            RowQualifier qualifier = RowQualifier.Default;
            AccessModifier accessModifier = AccessModifier.Public;
            SExprNode? bodyNode = null;

            foreach (var element in clause.Elements[2..])
            {
                if (element is not SExprList section || section.Elements.IsDefaultOrEmpty)
                {
                    if (bodyNode is null)
                    {
                        bodyNode = element;
                        continue;
                    }

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
                        foreach (var effectNode in FlattenArgumentNodes(section.Elements.Skip(1)))
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
                    case "access":
                        if (section.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(access <modifier>) expects exactly one argument.");
                        }

                        accessModifier = ParseAccess(ExpectIdentifier(section.Elements[1], "access modifier"));
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
                        if (bodyNode is null && allowBody)
                        {
                            bodyNode = element;
                            break;
                        }

                        throw new InvalidOperationException($"Unknown method section '{sectionHead.QualifiedName}'.");
                }
            }

            if (returnType is null)
            {
                if (rowReference is null)
                {
                    throw new InvalidOperationException($"Method '{methodName}' is missing a return type.");
                }
            }

            FunctionTypeSymbol? referencedSignature = null;
            if (rowReference is RowMemberReference reference)
            {
                var member = ResolveRowMember(reference);
                if (member.Type is not FunctionTypeSymbol function)
                {
                    throw new InvalidOperationException($"Row member '{reference.RowTypeName}::{reference.MemberName}' is not a method.");
                }

                referencedSignature = function;
                if (returnType is null)
                {
                    returnType = function.ReturnType;
                }
                else
                {
                    EnsureReturnCompatibility(returnType, function.ReturnType, "row reference return type");
                }

                if (parameters.Count == 0)
                {
                    parameters.AddRange(function.Parameters.Select(type => new ParameterSyntax(null, type)));
                }
                else
                {
                    EnsureParameterListCompatibility(parameters, function);
                }

                if (effects.Count == 0 && !function.Effects.IsDefaultOrEmpty)
                {
                    effects.AddRange(function.Effects);
                }
                else if (!function.Effects.IsDefaultOrEmpty && !EffectsMatch(effects, function))
                {
                    throw new InvalidOperationException($"Method '{methodName}' effect list does not match row reference '{reference.RowTypeName}::{reference.MemberName}'.");
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

            if (referencedSignature is not null)
            {
                EnsureSignatureCompatibility(signature, referencedSignature, methodName);
            }

            return new MethodSpecification(
                owner,
                methodName,
                parameters.ToImmutableArray(),
                signature,
                qualifier,
                accessModifier,
                bodyNode,
                rowReference);
        }

        private RowMember ResolveRowMember(RowMemberReference reference)
        {
            var typeSymbol = _registry.Require(reference.RowTypeName);
            if (typeSymbol is not RowTypeSymbol rowType)
            {
                throw new InvalidOperationException($"Row type '{reference.RowTypeName}' is not defined.");
            }

            var member = rowType.Members.FirstOrDefault(
                m => string.Equals(m.Name, reference.MemberName, StringComparison.Ordinal)
                     && string.Equals(m.Origin, reference.RowTypeName, StringComparison.Ordinal));

            if (member is null)
            {
                member = rowType.Members.FirstOrDefault(
                    m => string.Equals(m.Name, reference.MemberName, StringComparison.Ordinal));
            }

            if (member is null)
            {
                throw new InvalidOperationException($"Row member '{reference.RowTypeName}::{reference.MemberName}' does not exist.");
            }

            return member;
        }

        private static void EnsureParameterListCompatibility(List<ParameterSyntax> parameters, FunctionTypeSymbol reference)
        {
            if (parameters.Count != reference.Parameters.Length)
            {
                throw new InvalidOperationException("Parameter list does not match referenced row member signature.");
            }

            for (var i = 0; i < parameters.Count; i++)
            {
                var declared = parameters[i].Type;
                var required = reference.Parameters[i];
                if (ReferenceEquals(declared, required))
                {
                    continue;
                }

                if (string.Equals(declared.Name, required.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(declared.Name, "any", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Parameter type does not match referenced row member signature.");
                }
            }
        }

        private static bool EffectsMatch(List<EffectSymbol> effects, FunctionTypeSymbol reference)
        {
            if (effects.Count != reference.Effects.Length)
            {
                return false;
            }

            for (var i = 0; i < effects.Count; i++)
            {
                if (!string.Equals(effects[i].Name, reference.Effects[i].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureSignatureCompatibility(FunctionTypeSymbol candidate, FunctionTypeSymbol reference, string methodName)
        {
            if (candidate.Parameters.Length != reference.Parameters.Length)
            {
                throw new InvalidOperationException($"Method '{methodName}' signature does not match referenced row member.");
            }

            for (var i = 0; i < candidate.Parameters.Length; i++)
            {
                if (!ReferenceEquals(candidate.Parameters[i], reference.Parameters[i])
                    && !string.Equals(candidate.Parameters[i].Name, reference.Parameters[i].Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Method '{methodName}' parameter types do not match referenced row member.");
                }
            }

            if (!ReferenceEquals(candidate.ReturnType, reference.ReturnType)
                && !string.Equals(candidate.ReturnType.Name, reference.ReturnType.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Method '{methodName}' return type does not match referenced row member.");
            }

            if (candidate.Effects.Length != reference.Effects.Length)
            {
                throw new InvalidOperationException($"Method '{methodName}' effect list does not match referenced row member.");
            }

            for (var i = 0; i < candidate.Effects.Length; i++)
            {
                if (!string.Equals(candidate.Effects[i].Name, reference.Effects[i].Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Method '{methodName}' effect list does not match referenced row member.");
                }
            }
        }

        private sealed record ParameterSyntax(string? Name, TypeSymbol Type);

        private sealed record MethodSpecification(
            string Owner,
            string Name,
            ImmutableArray<ParameterSyntax> Parameters,
            FunctionTypeSymbol Signature,
            RowQualifier Qualifier,
            AccessModifier Access,
            SExprNode? BodyNode,
            RowMemberReference? RowReference);

        private sealed record RowMemberReference(string RowTypeName, string MemberName);

        private sealed class Scope
        {
            private readonly Scope? _parent;
            private readonly Dictionary<string, Value> _values;

            private Scope(Scope? parent)
            {
                _parent = parent;
                _values = new Dictionary<string, Value>(StringComparer.Ordinal);
            }

            public static Scope CreateRoot() => new(null);

            public Scope CreateChild() => new(this);

            public void Set(string name, Value value) => _values[name] = value;

            public bool TryGet(string name, out Value value)
            {
                if (_values.TryGetValue(name, out value))
                {
                    return true;
                }

                if (_parent is not null)
                {
                    return _parent.TryGet(name, out value);
                }

                value = default!;
                return false;
            }
        }

        private sealed class MethodBodyInterpreter
        {
            private readonly ModuleBuilder _builder;
            private readonly MethodSpecification _specification;
            private readonly Dictionary<string, int> _parameterIndex;

            public MethodBodyInterpreter(ModuleBuilder builder, MethodSpecification specification)
            {
                _builder = builder;
                _specification = specification;
                _parameterIndex = specification.Parameters
                    .Select((parameter, index) => (parameter, index))
                    .Where(tuple => !string.IsNullOrEmpty(tuple.parameter.Name))
                    .ToDictionary(tuple => tuple.parameter.Name!, tuple => tuple.index, StringComparer.Ordinal);
            }

            public Value Invoke(InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                if (arguments.Count != _specification.Signature.Parameters.Length)
                {
                    throw new InvalidOperationException(
                        $"Method '{_specification.Owner}::{_specification.Name}' expects {_specification.Signature.Parameters.Length} argument(s) but received {arguments.Count}.");
                }

                var scope = Scope.CreateRoot();
                foreach (var (name, index) in _parameterIndex)
                {
                    scope.Set(name, arguments[index]);
                }

                var result = Evaluate(_specification.BodyNode!, scope, invocation, arguments);
                _builder.EnsureReturnCompatibility(_specification.Signature.ReturnType, result.Type, result.Type.Name);
                return result;
            }

            private Value Evaluate(SExprNode node, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                return node switch
                {
                    SExprIdentifier identifier => EvaluateIdentifier(identifier, scope, invocation),
                    SExprString str => new StringValue(str.Value),
                    SExprList list => EvaluateList(list, scope, invocation, arguments),
                    SExprArray array => EvaluateArray(array, scope, invocation, arguments),
                    SExprObject obj => EvaluateObject(obj, scope, invocation, arguments),
                    _ => throw new InvalidOperationException($"Unsupported expression '{node}'."),
                };
            }

            private Value EvaluateIdentifier(SExprIdentifier identifier, Scope scope, InvocationContext invocation)
            {
                if (!identifier.Namespace.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException(
                        $"Qualified identifier '{identifier.QualifiedName}' cannot be used as a value in '{_specification.Owner}::{_specification.Name}'.");
                }

                var name = identifier.QualifiedName;
                if (name.Contains('.', StringComparison.Ordinal))
                {
                    return EvaluateDottedIdentifier(name, scope, invocation);
                }

                if (string.Equals(name, "self", StringComparison.Ordinal) || string.Equals(name, "this", StringComparison.Ordinal))
                {
                    return invocation.Self ?? throw new InvalidOperationException("Method invoked without a self instance.");
                }
                if (string.Equals(name, "true", StringComparison.Ordinal))
                {
                    return new BoolValue(true);
                }

                if (string.Equals(name, "false", StringComparison.Ordinal))
                {
                    return new BoolValue(false);
                }

                if (scope.TryGet(name, out var scoped))
                {
                    return scoped;
                }

                if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    return new IntValue(number);
                }

                throw new InvalidOperationException(
                    $"Unknown identifier '{name}' in method '{_specification.Owner}::{_specification.Name}'.");
            }

            private Value EvaluateDottedIdentifier(string expression, Scope scope, InvocationContext invocation)
            {
                var parts = expression.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    throw new InvalidOperationException("Empty dotted identifier.");
                }

                var current = EvaluateIdentifier(new SExprIdentifier(ImmutableArray.Create(parts[0])), scope, invocation);
                for (var i = 1; i < parts.Length; i++)
                {
                    var part = parts[i];
                    current = current switch
                    {
                        MapValue map when map.Properties.TryGetValue(part, out var value) => value,
                        _ => throw new InvalidOperationException($"Identifier '{expression}' cannot resolve member '{part}'."),
                    };
                }

                return current;
            }

            private Value EvaluateList(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                if (list.Elements.IsDefaultOrEmpty)
                {
                    throw new InvalidOperationException("Encountered empty expression list.");
                }

                var head = ModuleBuilder.ExpectIdentifier(list.Elements[0], "expression head");
                switch (head.QualifiedName)
                {
                    case "const":
                        return EvaluateConst(list);
                    case "let":
                        return EvaluateLet(list, scope, invocation, arguments);
                    case "call":
                        return EvaluateCall(list, scope, invocation, arguments);
                    case "new":
                        return EvaluateNew(list, invocation);
                    case "+":
                        return EvaluateAddition(list, scope, invocation, arguments);
                    case "-":
                        return EvaluateSubtraction(list, scope, invocation, arguments);
                    case "*":
                        return EvaluateMultiplication(list, scope, invocation, arguments);
                    case "/":
                        return EvaluateDivision(list, scope, invocation, arguments);
                    case "concat":
                        return EvaluateConcat(list, scope, invocation, arguments);
                    case "$":
                        return EvaluateSend(list, scope, invocation, arguments);
                }

                if (!head.Namespace.IsDefaultOrEmpty)
                {
                    return EvaluateQualifiedInvocation(list, head, scope, invocation, arguments);
                }

                throw new InvalidOperationException($"Unknown expression form '{head.QualifiedName}'.");
            }

            private Value EvaluateConst(SExprList list)
            {
                if (list.Elements.Length != 3)
                {
                    throw new InvalidOperationException("(const <type> <value>) expects exactly two arguments.");
                }

                var typeName = ModuleBuilder.ExpectIdentifier(list.Elements[1], "const type").QualifiedName;
                var valueNode = list.Elements[2];

                return typeName switch
                {
                    "str" => new StringValue(valueNode switch
                    {
                        SExprIdentifier identifier => identifier.QualifiedName,
                        SExprString str => str.Value,
                        _ => throw new InvalidOperationException($"Expected string literal but found '{valueNode}'."),
                    }),
                    "int" => CreateIntValue(valueNode),
                    "bool" => CreateBoolValue(valueNode),
                    _ => throw new InvalidOperationException($"Unsupported const type '{typeName}'."),
                };
            }

            private static Value CreateIntValue(SExprNode node)
            {
                var text = ModuleBuilder.ExpectIdentifier(node, "int literal").QualifiedName;
                if (!int.TryParse(text, out var parsed))
                {
                    throw new InvalidOperationException($"Invalid int literal '{text}'.");
                }

                return new IntValue(parsed);
            }

            private static Value CreateBoolValue(SExprNode node)
            {
                var text = ModuleBuilder.ExpectIdentifier(node, "bool literal").QualifiedName;
                if (!bool.TryParse(text, out var parsed))
                {
                    throw new InvalidOperationException($"Invalid bool literal '{text}'.");
                }

                return new BoolValue(parsed);
            }

            private Value EvaluateLet(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                if (list.Elements.Length < 3)
                {
                    throw new InvalidOperationException("(let (<bindings>) <body...>) requires bindings and a body.");
                }

                if (list.Elements[1] is not SExprList bindings)
                {
                    throw new InvalidOperationException("Let bindings must be provided as a list.");
                }

                var child = scope.CreateChild();
                foreach (var bindingNode in bindings.Elements)
                {
                    if (bindingNode is not SExprList binding || binding.Elements.Length != 2)
                    {
                        throw new InvalidOperationException("Each let binding must have the form (name value).");
                    }

                    var name = ModuleBuilder.ExpectIdentifier(binding.Elements[0], "binding name").QualifiedName;
                    var value = Evaluate(binding.Elements[1], child, invocation, arguments);
                    child.Set(name, value);
                }

                Value? result = null;
                for (var i = 2; i < list.Elements.Length; i++)
                {
                    result = Evaluate(list.Elements[i], child, invocation, arguments);
                }

                if (result is null)
                {
                    throw new InvalidOperationException("Let expression requires at least one body expression.");
                }

                return result;
            }

            private Value EvaluateCall(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                if (list.Elements.Length < 3)
                {
                    throw new InvalidOperationException("(call <target> <member> ...) requires at least a target and member.");
                }

                var target = Evaluate(list.Elements[1], scope, invocation, arguments);
                if (target is not ObjectValue instance)
                {
                    throw new InvalidOperationException("call target must evaluate to an object.");
                }

                var member = ModuleBuilder.ExpectIdentifier(list.Elements[2], "member name");
                var origin = member.Namespace.IsDefaultOrEmpty
                    ? null
                    : string.Join("::", member.Namespace);
                var memberName = member.Name;

                var evaluatedArgs = new Value[list.Elements.Length - 3];
                for (var i = 3; i < list.Elements.Length; i++)
                {
                    evaluatedArgs[i - 3] = Evaluate(list.Elements[i], scope, invocation, arguments);
                }

                return origin is null
                    ? invocation.Execution.Invoke(instance, memberName, evaluatedArgs)
                    : invocation.Execution.Invoke(instance, memberName, origin, evaluatedArgs);
            }

            private Value EvaluateSend(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                if (list.Elements.Length < 3)
                {
                    throw new InvalidOperationException("($ <target> <member> ...) requires at least a target and member.");
                }

                var target = Evaluate(list.Elements[1], scope, invocation, arguments);
                if (target is not ObjectValue instance)
                {
                    throw new InvalidOperationException("Send target must evaluate to an object.");
                }

                var member = ModuleBuilder.ExpectIdentifier(list.Elements[2], "member name", allowAnnotations: true);
                var origin = member.Namespace.IsDefaultOrEmpty ? null : string.Join("::", member.Namespace);
                var memberName = member.Name;

                var evaluatedArgs = new Value[list.Elements.Length - 3];
                for (var i = 3; i < list.Elements.Length; i++)
                {
                    evaluatedArgs[i - 3] = Evaluate(list.Elements[i], scope, invocation, arguments);
                }

                return origin is null
                    ? invocation.Execution.Invoke(instance, memberName, evaluatedArgs)
                    : invocation.Execution.Invoke(instance, memberName, origin, evaluatedArgs);
            }

            private Value EvaluateNew(SExprList list, InvocationContext invocation)
            {
                if (list.Elements.Length != 2)
                {
                    throw new InvalidOperationException("(new <ClassName>) expects exactly one argument.");
                }

                var classIdentifier = ModuleBuilder.ExpectIdentifier(list.Elements[1], "class name");
                return invocation.Execution.Instantiate(classIdentifier.QualifiedName);
            }

            private Value EvaluateArray(SExprArray array, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                var builder = ImmutableArray.CreateBuilder<Value>(array.Elements.Length);
                foreach (var element in array.Elements)
                {
                    builder.Add(Evaluate(element, scope, invocation, arguments));
                }

                return new ListValue(builder.ToImmutable());
            }

            private Value EvaluateObject(SExprObject obj, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, Value>();
                builder.KeyComparer = StringComparer.Ordinal;

                foreach (var property in obj.Properties)
                {
                    var key = ResolvePropertyKey(property.Key);
                    var value = Evaluate(property.Value, scope, invocation, arguments);
                    builder[key] = value;
                }

                return new MapValue(builder.ToImmutable());
            }

            private Value EvaluateQualifiedInvocation(
                SExprList list,
                SExprIdentifier head,
                Scope scope,
                InvocationContext invocation,
                IReadOnlyList<Value> arguments)
            {
                if (list.Elements.Length < 2)
                {
                    throw new InvalidOperationException("Qualified invocation requires a target expression.");
                }

                var target = Evaluate(list.Elements[1], scope, invocation, arguments);
                if (target is not ObjectValue instance)
                {
                    throw new InvalidOperationException("Qualified invocation target must evaluate to an object.");
                }

                var origin = string.Join("::", head.Namespace);
                var memberName = head.Name;
                var evaluatedArgs = new Value[list.Elements.Length - 2];
                for (var i = 2; i < list.Elements.Length; i++)
                {
                    evaluatedArgs[i - 2] = Evaluate(list.Elements[i], scope, invocation, arguments);
                }

                return invocation.Execution.Invoke(instance, memberName, origin, evaluatedArgs);
            }

            private Value EvaluateAddition(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                EnsureOperandCount(list, minimum: 2, form: "+");
                var sum = 0;
                for (var i = 1; i < list.Elements.Length; i++)
                {
                    sum += EvaluateIntOperand(list.Elements[i], scope, invocation, arguments);
                }

                return new IntValue(sum);
            }

            private Value EvaluateSubtraction(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                EnsureOperandCount(list, minimum: 1, form: "-");
                var initial = EvaluateIntOperand(list.Elements[1], scope, invocation, arguments);
                if (list.Elements.Length == 2)
                {
                    return new IntValue(-initial);
                }

                var result = initial;
                for (var i = 2; i < list.Elements.Length; i++)
                {
                    result -= EvaluateIntOperand(list.Elements[i], scope, invocation, arguments);
                }

                return new IntValue(result);
            }

            private Value EvaluateMultiplication(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                EnsureOperandCount(list, minimum: 2, form: "*");
                var product = 1;
                for (var i = 1; i < list.Elements.Length; i++)
                {
                    product *= EvaluateIntOperand(list.Elements[i], scope, invocation, arguments);
                }

                return new IntValue(product);
            }

            private Value EvaluateDivision(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                EnsureOperandCount(list, minimum: 2, form: "/");
                var result = EvaluateIntOperand(list.Elements[1], scope, invocation, arguments);
                for (var i = 2; i < list.Elements.Length; i++)
                {
                    var divisor = EvaluateIntOperand(list.Elements[i], scope, invocation, arguments);
                    if (divisor == 0)
                    {
                        throw new InvalidOperationException("Division by zero.");
                    }

                    result /= divisor;
                }

                return new IntValue(result);
            }

            private Value EvaluateConcat(SExprList list, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                EnsureOperandCount(list, minimum: 1, form: "concat");
                var builder = new StringBuilder();
                for (var i = 1; i < list.Elements.Length; i++)
                {
                    var value = Evaluate(list.Elements[i], scope, invocation, arguments);
                    builder.Append(CoerceToString(value));
                }

                return new StringValue(builder.ToString());
            }

            private static string ResolvePropertyKey(SExprNode node)
            {
                return node switch
                {
                    SExprIdentifier identifier => identifier.QualifiedName,
                    SExprString str => str.Value,
                    _ => throw new InvalidOperationException($"Object property key must be an identifier or string but found '{node}'."),
                };
            }

            private static void EnsureOperandCount(SExprList list, int minimum, string form)
            {
                if (list.Elements.Length <= minimum)
                {
                    throw new InvalidOperationException($"({form} ...) expects at least {minimum} operand(s).");
                }
            }

            private int EvaluateIntOperand(SExprNode node, Scope scope, InvocationContext invocation, IReadOnlyList<Value> arguments)
            {
                var value = Evaluate(node, scope, invocation, arguments);
                return value switch
                {
                    IntValue number => number.Value,
                    _ => throw new InvalidOperationException($"Expected int expression but found '{value.Type.Name}'."),
                };
            }

            private string CoerceToString(Value value)
            {
                return value switch
                {
                    StringValue str => str.Value,
                    IntValue number => number.Value.ToString(CultureInfo.InvariantCulture),
                    BoolValue boolean => boolean.Value ? "true" : "false",
                    AnyValue any when any.Value is string s => s,
                    AnyValue any when any.Value is null => string.Empty,
                    ListValue list => "[" + string.Join(" ", list.Elements.Select(CoerceToString)) + "]",
                    MapValue map => "{" + string.Join(" ", map.Properties.Select(kvp => $"{kvp.Key}={CoerceToString(kvp.Value)}")) + "}",
                    ObjectValue obj => $"<{obj.Class.Name}>",
                    _ => throw new InvalidOperationException($"Cannot convert value of type '{value.Type.Name}' to string."),
                };
            }
        }

        private static IEnumerable<SExprNode> FlattenArgumentNodes(IEnumerable<SExprNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is SExprArray array)
                {
                    foreach (var element in array.Elements)
                    {
                        yield return element;
                    }

                    continue;
                }

                yield return node;
            }
        }

        private IEnumerable<ParameterSyntax> ParseParameters(SExprList section)
        {
            foreach (var paramNode in FlattenArgumentNodes(section.Elements.Skip(1)))
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
            var access = AccessModifier.Public;
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
                    case "access":
                        if (optionList.Elements.Length != 2)
                        {
                            throw new InvalidOperationException("(access <modifier>) expects exactly one argument.");
                        }

                        access = ParseAccess(ExpectIdentifier(optionList.Elements[1], "access modifier"));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown field option '{optionHead.QualifiedName}'.");
                }
            }

            return RowMemberBuilder.Field(owner, fieldName, fieldType, qualifier, access);
        }

        private Func<InvocationContext, IReadOnlyList<Value>, Value> CompileBody(MethodSpecification specification)
        {
            if (specification.BodyNode is null)
            {
                throw new InvalidOperationException($"Method '{specification.Name}' is missing a body.");
            }

            var interpreter = new MethodBodyInterpreter(this, specification);
            return interpreter.Invoke;
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

        private static SExprIdentifier ExpectIdentifier(SExprNode node, string description, bool allowAnnotations = false)
        {
            if (node is not SExprIdentifier identifier)
            {
                throw new InvalidOperationException($"Expected identifier for {description} but found '{node}'.");
            }

            if (!allowAnnotations)
            {
                if (identifier.TypeAnnotation is not null
                    || identifier.PrefixAnnotations.Count > 0
                    || identifier.PostfixAnnotations.Count > 0)
                {
                    throw new InvalidOperationException($"Annotations are not supported on identifier '{identifier.QualifiedName}'.");
                }
            }

            return identifier;
        }
    }
}
