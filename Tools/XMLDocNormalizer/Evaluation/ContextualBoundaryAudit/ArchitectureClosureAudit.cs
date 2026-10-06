using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Measures whole-source type/member dependencies, source ownership and state for
/// A6. The active host partial is recorded explicitly, not silently traversed into
/// the proposed historical source boundary.
/// </summary>
internal static class ArchitectureClosureAudit
{
    internal static object Measure(Compilation compilation, IEnumerable<Callable> sourceNodes,
        IEnumerable<Edge> methodEdges, string root)
    {
        Callable[] nodes = sourceNodes.ToArray();
        Edge[] calls = methodEdges.ToArray();
        Dictionary<string, INamedTypeSymbol> types = new(StringComparer.Ordinal);
        List<TypeUse> uses = [];
        List<object> state = [];
        List<object> declarations = [];
        Dictionary<string, string> hashes = new(StringComparer.Ordinal);
        string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
        string Id(ISymbol symbol) => symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        string Owner(INamedTypeSymbol symbol)
        {
            while (symbol.ContainingType != null)
            {
                symbol = symbol.ContainingType;
            }
            return Id(symbol);
        }
        void AddType(string owner, ITypeSymbol? type, string file, int line, string kind)
        {
            if (type is IArrayTypeSymbol array)
            {
                AddType(owner, array.ElementType, file, line, kind);
            }
            if (type is not INamedTypeSymbol named)
            {
                return;
            }
            uses.Add(new(owner, Owner(named), file, line, kind,
                named.Locations.Any(location => location.IsInSource),
                named.ContainingNamespace.ToDisplayString(), named.ContainingAssembly.Identity.ToString()));
            foreach (ITypeSymbol argument in named.TypeArguments)
            {
                AddType(owner, argument, file, line, kind + "GenericArgument");
            }
        }
        foreach (SyntaxTree tree in compilation.SyntaxTrees.OrderBy(tree => tree.FilePath, StringComparer.Ordinal))
        {
            string file = Relative(tree.FilePath);
            if (file.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }
            SemanticModel model = compilation.GetSemanticModel(tree);
            hashes[file] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tree.GetText().ToString())));
            foreach (SyntaxNode declaration in tree.GetRoot().DescendantNodes().Where(node =>
                node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
            {
                if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
                {
                    continue;
                }
                string owner = Owner(symbol);
                INamedTypeSymbol topLevel = symbol;
                while (topLevel.ContainingType != null)
                {
                    topLevel = topLevel.ContainingType;
                }
                types[owner] = topLevel;
                declarations.Add(new
                {
                    Id = Id(symbol), Owner = owner, File = file,
                    Line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Kind = symbol.TypeKind.ToString(), Visibility = symbol.DeclaredAccessibility.ToString(),
                    symbol.IsStatic, symbol.IsSealed, symbol.IsAbstract,
                    BaseType = symbol.BaseType?.ToDisplayString(), Interfaces = symbol.Interfaces.Select(Id).ToArray()
                });
                // Positional records own synthesized properties whose source
                // declaration is a parameter, not PropertyDeclarationSyntax.
                if (declaration is RecordDeclarationSyntax record && record.ParameterList != null)
                {
                    foreach (ParameterSyntax parameter in record.ParameterList.Parameters)
                    {
                        IPropertySymbol? property = symbol.GetMembers(parameter.Identifier.ValueText)
                            .OfType<IPropertySymbol>().SingleOrDefault();
                        if (property != null)
                        {
                            state.Add(new
                            {
                                Id = Id(property), Owner = owner, File = file,
                                Line = parameter.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                Type = property.Type.ToDisplayString(), property.IsStatic,
                                IsReadOnly = property.IsReadOnly, IsConst = false,
                                Visibility = property.DeclaredAccessibility.ToString(),
                                Initializer = (string?)null, Kind = "PositionalRecordProperty"
                            });
                        }
                    }
                }
                foreach (SyntaxNode syntax in declaration.DescendantNodes(node =>
                    node == declaration || node is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)))
                {
                    int line = syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    ISymbol? target = model.GetSymbolInfo(syntax).Symbol;
                    if (target is INamedTypeSymbol targetType)
                    {
                        AddType(owner, targetType, file, line, "SymbolType");
                    }
                    else if (target?.ContainingType != null)
                    {
                        AddType(owner, target.ContainingType, file, line,
                            target.Kind == SymbolKind.Method ? "MethodOrDelegate" : "Member");
                    }
                    if (syntax is TypeSyntax or ExpressionSyntax)
                    {
                        AddType(owner, model.GetTypeInfo(syntax).Type, file, line, "BoundType");
                    }
                    if (syntax is VariableDeclaratorSyntax variable
                        && model.GetDeclaredSymbol(variable) is IFieldSymbol field)
                    {
                        state.Add(new
                        {
                            Id = Id(field), Owner = owner, File = file, Line = line,
                            Type = field.Type.ToDisplayString(), field.IsStatic, field.IsReadOnly, field.IsConst,
                            Visibility = field.DeclaredAccessibility.ToString(),
                            Initializer = variable.Initializer?.Value.ToString()
                        });
                    }
                    if (syntax is PropertyDeclarationSyntax property
                        && model.GetDeclaredSymbol(property) is IPropertySymbol propertySymbol
                        && property.AccessorList?.Accessors.Any(accessor => accessor.Body == null
                            && accessor.ExpressionBody == null) == true)
                    {
                        state.Add(new
                        {
                            Id = Id(propertySymbol), Owner = owner, File = file, Line = line,
                            Type = propertySymbol.Type.ToDisplayString(), propertySymbol.IsStatic,
                            IsReadOnly = propertySymbol.IsReadOnly, IsConst = false,
                            Visibility = propertySymbol.DeclaredAccessibility.ToString(),
                            Initializer = property.Initializer?.Value.ToString()
                        });
                    }
                }
            }
        }
        // A nested declaration's top-level owner is also encountered separately;
        // use its declared symbol directly rather than resolving display names.
        TypeUse[] uniqueUses = uses.Distinct().OrderBy(use => use.Caller, StringComparer.Ordinal)
            .ThenBy(use => use.Callee, StringComparer.Ordinal).ThenBy(use => use.File, StringComparer.Ordinal)
            .ThenBy(use => use.Line).ThenBy(use => use.Kind, StringComparer.Ordinal).ToArray();
        const string flowPrefix = "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.";
        const string host = "src/XMLDocNormalizer/Execution/Semantic/ProjectClosureExceptionFlowSemanticEnvironment.cs";
        string[] flowOwners = types.Keys.Where(owner => owner.StartsWith(flowPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        HashSet<string> core = flowOwners.ToHashSet(StringComparer.Ordinal);
        Queue<string> queue = new(flowOwners);
        while (queue.TryDequeue(out string? owner))
        {
            foreach (string dependency in uniqueUses.Where(use => use.Caller == owner && use.Source && use.File != host)
                .Select(use => use.Callee).Distinct())
            {
                if (core.Add(dependency))
                {
                    queue.Enqueue(dependency);
                }
            }
        }
        var typeEdges = uniqueUses.Where(use => use.Source && use.Caller != use.Callee)
            .Select(use => new ComponentEdge(use.Caller, use.Callee)).Distinct().ToArray();
        var coreEdges = uniqueUses.Where(use => use.Source && use.File != host && core.Contains(use.Caller)
                && use.Caller != use.Callee)
            .Select(use => new ComponentEdge(use.Caller, use.Callee)).Distinct().ToArray();
        var callTypeEdges = calls.Where(edge => edge.Source && nodes.Any(node => node.Id == edge.Callee))
            .Select(edge => new ComponentEdge(Owner(nodes.First(node => node.Id == edge.Caller).Symbol.ContainingType),
                Owner(nodes.First(node => node.Id == edge.Callee).Symbol.ContainingType)))
            .Where(edge => edge.Caller != edge.Callee).Distinct().ToArray();
        string[] lower = flowOwners.Where(owner => owner.EndsWith("FactsProvider", StringComparison.Ordinal)
            || owner.EndsWith("Facts", StringComparison.Ordinal) || owner.EndsWith("Resolver", StringComparison.Ordinal)
            || owner.EndsWith("FactProjector", StringComparison.Ordinal)
            || owner.EndsWith("ArgumentMapper", StringComparison.Ordinal)
            || owner.EndsWith("FactDiscovery", StringComparison.Ordinal)
            || owner.EndsWith("FactEvaluator", StringComparison.Ordinal)
            || owner.EndsWith("RuntimeDispatchClassifier", StringComparison.Ordinal)
            || owner.EndsWith("CatchSemantics", StringComparison.Ordinal)
            || owner.EndsWith("SemanticScope", StringComparison.Ordinal)).ToArray();
        string analyzer = flowPrefix + "ExceptionFlowAnalyzer";
        return new
        {
            Schema = "ExceptionFlow.ArchitectureClosure.Measurement.v1",
            Scope = "Whole production source compilation, exact bound symbols/types/signatures/member/delegate uses; method graph includes invocations, method references, constructions and properties. Proposed source closure starts at every Flow owner and cuts only the explicitly recorded active host partial file. Nested implementation types fold to top-level owner.",
            SourceHashes = hashes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new { File = pair.Key, Sha256 = pair.Value }).ToArray(),
            TypeDeclarations = declarations,
            TypeUses = uniqueUses,
            AllSourceTypeEdges = typeEdges,
            AllSourceTypeCycles = Cycles(types.Keys, typeEdges),
            ProposedCoreOwners = core.Order(StringComparer.Ordinal).ToArray(),
            ProposedCoreTypeEdges = coreEdges,
            ProposedCoreTypeCycles = Cycles(core, coreEdges),
            FlowOwners = flowOwners,
            LowerFactResolverOwners = lower,
            LowerToAnalyzerTypeUses = uniqueUses.Where(use => lower.Contains(use.Caller) && use.Callee == analyzer).ToArray(),
            FlowToAnalyzerTypeUses = uniqueUses.Where(use => use.Caller.StartsWith(flowPrefix, StringComparison.Ordinal)
                && use.Caller != analyzer && use.Callee == analyzer).ToArray(),
            MethodEdges = calls,
            MethodOwnerEdges = callTypeEdges,
            MethodOwnerCycles = Cycles(types.Keys, callTypeEdges),
            State = state,
            HostImplementationCut = host,
            HostImplementationDependencies = uniqueUses.Where(use => use.File == host).ToArray(),
            RoslynUses = uniqueUses.Where(use => core.Contains(use.Caller) && use.File != host
                && use.Namespace.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)).ToArray(),
            CoreMetadataMembers = calls.Where(edge => !edge.Source
                && core.Contains(Owner(nodes.First(node => node.Id == edge.Caller).Symbol.ContainingType))).ToArray()
        };
    }

    private static string[][] Cycles(IEnumerable<string> owners, IEnumerable<ComponentEdge> edges)
    {
        Dictionary<string, string[]> graph = owners.Distinct().ToDictionary(owner => owner,
            owner => edges.Where(edge => edge.Caller == owner).Select(edge => edge.Callee).Distinct().ToArray());
        HashSet<string> assigned = new(StringComparer.Ordinal);
        List<string[]> result = [];
        HashSet<string> Reach(string start)
        {
            HashSet<string> reached = new(StringComparer.Ordinal) { start };
            Queue<string> pending = new([start]);
            while (pending.TryDequeue(out string? current))
            {
                foreach (string next in graph.GetValueOrDefault(current) ?? [])
                {
                    if (reached.Add(next))
                    {
                        pending.Enqueue(next);
                    }
                }
            }
            return reached;
        }
        foreach (string owner in graph.Keys.Order(StringComparer.Ordinal))
        {
            if (assigned.Contains(owner))
            {
                continue;
            }
            string[] component = Reach(owner).Where(other => other != owner && Reach(other).Contains(owner))
                .Append(owner).Order(StringComparer.Ordinal).ToArray();
            foreach (string item in component)
            {
                assigned.Add(item);
            }
            if (component.Length > 1)
            {
                result.Add(component);
            }
        }
        return result.ToArray();
    }

    private sealed record TypeUse(string Caller, string Callee, string File, int Line, string Kind,
        bool Source, string Namespace, string Assembly);
    private sealed record ComponentEdge(string Caller, string Callee);
}
