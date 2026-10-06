using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

if (args.Length is not (2 or 3))
{
    Console.Error.WriteLine("Usage: ContextualBoundaryAudit <solution> <output-json> [--extraction-plan | --verify-extraction=<baseline-ref> | --composition-baseline=<ref>]");
    return 2;
}

if (args.Length == 3 && args[2] != "--extraction-plan"
    && !args[2].StartsWith("--verify-extraction=", StringComparison.Ordinal)
    && !args[2].StartsWith("--composition-baseline=", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Unknown audit option: " + args[2]);
    return 2;
}

MSBuildLocator.RegisterDefaults();
using MSBuildWorkspace workspace = MSBuildWorkspace.Create();
List<string> workspaceDiagnostics = [];
workspace.RegisterWorkspaceFailedHandler(e => workspaceDiagnostics.Add(e.Diagnostic.ToString()));
string solutionPath = Path.GetFullPath(args[0]);
string rootDirectory = Path.GetDirectoryName(solutionPath)!;
Solution solution = await workspace.OpenSolutionAsync(solutionPath);
Project project = solution.Projects.Single(p => p.Name == "XMLDocNormalizer");
Compilation compilation = (await project.GetCompilationAsync())!;
if (args.Length == 3 && args[2].StartsWith("--composition-baseline=", StringComparison.Ordinal))
{
    compilation = CompositionBaseline.Load(compilation, rootDirectory, args[2]["--composition-baseline=".Length..]);
}
Dictionary<string, Callable> nodes = new(StringComparer.Ordinal);
Dictionary<string, ISymbol> referencedSymbols = new(StringComparer.Ordinal);
List<Reference> references = [];
List<Edge> calls = [];
List<object> declarations = [];
List<object> localState = [];
List<object> mutations = [];
List<object> delegateSites = [];
List<Edge> initializerEdges = [];

string Id(ISymbol symbol) => symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
string Relative(string path) => Path.GetRelativePath(rootDirectory, path).Replace('\\', '/');
int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

foreach (SyntaxTree tree in compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal))
{
    if (tree.FilePath.Contains("/obj/", StringComparison.Ordinal)
        || tree.FilePath.Replace((char)92, '/').Contains("/obj/", StringComparison.Ordinal))
    {
        continue;
    }

    SemanticModel model = compilation.GetSemanticModel(tree);
    foreach (SyntaxNode syntax in tree.GetRoot().DescendantNodes().Where(
                 n => n is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax
                     or LocalFunctionStatementSyntax))
    {
        if (model.GetDeclaredSymbol(syntax) is not IMethodSymbol method)
        {
            continue;
        }

        nodes[Id(method)] = new Callable(
            Id(method), method.ContainingType.ToDisplayString(), Relative(tree.FilePath),
            Line(syntax), syntax.Kind().ToString(), method.Name,
            method.DeclaredAccessibility.ToString(), method.IsStatic, syntax, method);
    }

    foreach (VariableDeclaratorSyntax syntax in tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
    {
        if (model.GetDeclaredSymbol(syntax) is IFieldSymbol field)
        {
            declarations.Add(new
            {
                Id = Id(field),
                Owner = field.ContainingType.ToDisplayString(),
                Type = field.Type.ToDisplayString(),
                field.IsStatic,
                field.IsReadOnly,
                File = Relative(tree.FilePath),
                Line = Line(syntax),
                Initializer = syntax.Initializer?.Value.ToString()
            });
            if (syntax.Initializer != null)
            {
                foreach (SyntaxNode use in syntax.Initializer.DescendantNodes())
                {
                    ISymbol? symbol = model.GetSymbolInfo(use).Symbol;
                    if (symbol is IMethodSymbol target
                        && use is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
                            or IdentifierNameSyntax)
                    {
                        initializerEdges.Add(new Edge(Id(field), Id(target),
                            target.ContainingType.ToDisplayString(), "FieldInitializer",
                            Relative(tree.FilePath), Line(use),
                            target.Locations.Any(l => l.IsInSource)));
                    }
                }
            }
        }
    }
}

foreach (Callable caller in nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal))
{
    SemanticModel model = compilation.GetSemanticModel(caller.Syntax.SyntaxTree);
    // Lambdas execute within their enclosing callable's composition. Local functions
    // have separate nodes; do not also attribute their bodies to the outer method.
    IEnumerable<SyntaxNode> body = caller.Syntax.DescendantNodes(
        n => n == caller.Syntax || n is not LocalFunctionStatementSyntax);
    foreach (SyntaxNode syntax in body)
    {
        if (syntax is VariableDeclaratorSyntax variable
            && model.GetDeclaredSymbol(variable) is ILocalSymbol local)
        {
            localState.Add(new
            {
                Caller = caller.Id,
                local.Name,
                Type = local.Type.ToDisplayString(),
                File = Relative(variable.SyntaxTree.FilePath),
                Line = Line(variable),
                Initializer = variable.Initializer?.Value.ToString()
            });
        }

        if (syntax is AnonymousFunctionExpressionSyntax lambda)
        {
            delegateSites.Add(new
            {
                Caller = caller.Id,
                File = Relative(lambda.SyntaxTree.FilePath),
                Line = Line(lambda),
                Syntax = lambda.ToString(),
                Kind = "Lambda"
            });
        }

        if (syntax is InvocationExpressionSyntax invocation)
        {
            SymbolInfo info = model.GetSymbolInfo(invocation);
            IMethodSymbol? target = info.Symbol as IMethodSymbol
                ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] as IMethodSymbol : null);
            if (target != null)
            {
                AddCall(target.ReducedFrom ?? target, "Invocation", syntax);
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    ISymbol? receiver = model.GetSymbolInfo(memberAccess.Expression).Symbol;
                    if (receiver is IFieldSymbol or IParameterSymbol or ILocalSymbol)
                    {
                        mutations.Add(new
                        {
                            Caller = caller.Id,
                            Receiver = receiver.Name,
                            ReceiverId = Id(receiver),
                            ReceiverKind = receiver.Kind.ToString(),
                            Method = target.Name,
                            Site = invocation.ToString(),
                            File = Relative(syntax.SyntaxTree.FilePath),
                            Line = Line(syntax)
                        });
                    }
                }
            }
        }
        else if (syntax is ConstructorInitializerSyntax initializer
                 && model.GetSymbolInfo(initializer).Symbol is IMethodSymbol initializerTarget)
        {
            AddCall(initializerTarget, "ConstructorInitializer", syntax);
        }
        else if (syntax is BaseObjectCreationExpressionSyntax creation
                 && model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor)
        {
            AddCall(constructor, "Construction", syntax);
        }
        else if (syntax is IdentifierNameSyntax or GenericNameSyntax)
        {
            ISymbol? symbol = model.GetSymbolInfo(syntax).Symbol;
            if (symbol is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol)
            {
                referencedSymbols[Id(symbol)] = symbol;
                SyntaxNode use = syntax;
                if (syntax.Parent is MemberAccessExpressionSyntax access && access.Name == syntax)
                {
                    use = access;
                }

                bool write = use.Parent is AssignmentExpressionSyntax assignment && assignment.Left == use;
                bool compound = use.Parent is AssignmentExpressionSyntax compoundAssignment
                    && !compoundAssignment.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression);
                bool mutation = use.Parent is PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax;
                references.Add(new Reference(caller.Id, Id(symbol), symbol.Kind.ToString(),
                    symbol.ContainingType?.ToDisplayString(), SymbolType(symbol), write || compound || mutation,
                    !write || compound || mutation, Relative(caller.Syntax.SyntaxTree.FilePath), Line(syntax)));
                if (symbol is IPropertySymbol property)
                {
                    if ((!write || compound || mutation) && property.GetMethod != null)
                    {
                        AddCall(property.GetMethod, "PropertyGet", syntax);
                    }

                    if ((write || compound || mutation) && property.SetMethod != null)
                    {
                        AddCall(property.SetMethod, "PropertySet", syntax);
                    }
                }
            }
            else if (symbol is IMethodSymbol method
                     && !syntax.Ancestors().OfType<InvocationExpressionSyntax>()
                         .Any(i => i.Expression.Span.Contains(syntax.Span)))
            {
                AddCall(method.ReducedFrom ?? method, "MethodReference", syntax);
            }
        }
    }

    void AddCall(IMethodSymbol target, string kind, SyntaxNode site)
    {
        IMethodSymbol normalized = target.OriginalDefinition;
        referencedSymbols[Id(normalized)] = normalized;
        calls.Add(new Edge(caller.Id, Id(normalized), normalized.ContainingType.ToDisplayString(),
            kind, Relative(site.SyntaxTree.FilePath), Line(site),
            normalized.Locations.Any(l => l.IsInSource)));
    }
}

List<Edge> uniqueCalls = calls.DistinctBy(e => (e.Caller, e.Callee, e.Kind))
    .OrderBy(e => e.Caller, StringComparer.Ordinal).ThenBy(e => e.Callee, StringComparer.Ordinal)
    .ThenBy(e => e.Kind, StringComparer.Ordinal).ToList();
HashSet<string> methodIds = nodes.Values.Where(n => n.Kind == "MethodDeclaration")
    .Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
List<Edge> primary = uniqueCalls.Where(e => e.Kind == "Invocation"
    && methodIds.Contains(e.Caller) && methodIds.Contains(e.Callee)).ToList();
List<List<string>> primarySccs = Tarjan(methodIds, primary);
string evaluationOwner = nodes.Values.Any(n => n.Symbol.ContainingType.Name == "ExceptionFlowContextualFactEvaluator")
    ? "ExceptionFlowContextualFactEvaluator" : "ExceptionFlowAnalyzer";
List<string> core = primarySccs.Single(c => c.Any(id =>
    id.Contains(evaluationOwner + ".GetExpressionValueFacts(", StringComparison.Ordinal))
    && c.Count > 1);
HashSet<string> coreIds = core.ToHashSet(StringComparer.Ordinal);
List<List<string>> expandedSccs = Tarjan(nodes.Keys,
    uniqueCalls.Where(e => nodes.ContainsKey(e.Callee)).ToList());
List<string> expandedCore = expandedSccs.Single(c => c.Contains(core[0]));
List<Edge> ingress = uniqueCalls.Where(e => !coreIds.Contains(e.Caller) && coreIds.Contains(e.Callee)).ToList();
List<Edge> egress = uniqueCalls.Where(e => coreIds.Contains(e.Caller) && !coreIds.Contains(e.Callee)).ToList();
HashSet<string> reachable = new(coreIds, StringComparer.Ordinal);
HashSet<string> initializedFields = new(StringComparer.Ordinal);
Queue<string> queue = new(coreIds.Order(StringComparer.Ordinal));
while (queue.TryDequeue(out string? id))
{
    foreach (Edge edge in uniqueCalls.Where(e => e.Caller == id && nodes.ContainsKey(e.Callee)))
    {
        if (reachable.Add(edge.Callee))
        {
            queue.Enqueue(edge.Callee);
        }
    }

    foreach (Reference field in references.Where(r => r.Caller == id && r.Kind == "Field"))
    {
        initializedFields.Add(field.Id);
        foreach (Edge edge in initializerEdges.Where(e => e.Caller == field.Id && nodes.ContainsKey(e.Callee)))
        {
            if (reachable.Add(edge.Callee))
            {
                queue.Enqueue(edge.Callee);
            }
        }
    }
}

List<Edge> reachableEdges = uniqueCalls.Where(e => reachable.Contains(e.Caller)).ToList();
HashSet<string> reachableOwners = reachable.Where(nodes.ContainsKey).Select(id => nodes[id].Owner)
    .ToHashSet(StringComparer.Ordinal);
List<Edge> typeEdges = uniqueCalls.Where(e => reachable.Contains(e.Caller)
    && nodes.ContainsKey(e.Callee) && nodes[e.Caller].Owner != nodes[e.Callee].Owner)
    .Select(e => e with { Caller = nodes[e.Caller].Owner, Callee = nodes[e.Callee].Owner })
    .DistinctBy(e => (e.Caller, e.Callee)).ToList();
foreach (Edge edge in initializerEdges.Where(e => initializedFields.Contains(e.Caller)
             && nodes.ContainsKey(e.Callee)))
{
    string owner = referencedSymbols[edge.Caller].ContainingType.ToDisplayString();
    if (owner != nodes[edge.Callee].Owner)
    {
        typeEdges.Add(edge with { Caller = owner, Callee = nodes[edge.Callee].Owner });
    }
}

typeEdges = typeEdges.DistinctBy(e => (e.Caller, e.Callee)).ToList();
Dictionary<string, string> logicalOwners = nodes.Values.Select(n => n.Symbol.ContainingType)
    .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
    .ToDictionary(t => t.ToDisplayString(), TopOwner, StringComparer.Ordinal);
List<Edge> logicalEdges = typeEdges.Select(e => e with
{
    Caller = logicalOwners.GetValueOrDefault(e.Caller, e.Caller),
    Callee = logicalOwners.GetValueOrDefault(e.Callee, e.Callee)
}).Where(e => e.Caller != e.Callee).DistinctBy(e => (e.Caller, e.Callee)).ToList();
var output = new
{
    Schema = "ContextualBoundary.MeasuredBoundary.v2",
    EvaluationOwner = evaluationOwner,
    Composition = CompositionAudit.Measure(compilation, nodes.Values, uniqueCalls),
    ExtractionVerification = args.Length == 3 && args[2].StartsWith("--verify-extraction=", StringComparison.Ordinal)
        ? ExtractionVerification.Measure(compilation, rootDirectory, args[2]["--verify-extraction=".Length..]) : null,
    Solution = Path.GetFileName(solutionPath),
    MethodGraphScope = "All source MethodDeclarationSyntax in the active XMLDocNormalizer compilation; normalized bound InvocationExpressionSyntax targets; distinct directed caller/callee edges",
    ExpandedScope = "All source methods, constructors, accessors and local functions; invocations, constructions, property access and method references; lambdas attributed to enclosing callable; reachable closure includes referenced field initializer targets",
    Limitations = "Static call/dependency audit, not runtime points-to analysis. Metadata implementation bodies are not traversed. Delegate/virtual calls retain declared targets; explicit source method references are recorded. Dynamic binding is not inferred.",
    WorkspaceDiagnostics = workspaceDiagnostics,
    CompilationErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.ToString()).ToArray(),
    AllMethodCount = methodIds.Count,
    AllMethodEdgeCount = primary.Count,
    MethodSccs = primarySccs.OrderByDescending(c => c.Count).Select(c => new { Size = c.Count, Methods = c.Order(StringComparer.Ordinal) }),
    Scc = new
    {
        MethodCount = core.Count,
        InternalEdgeCount = primary.Count(e => coreIds.Contains(e.Caller) && coreIds.Contains(e.Callee)),
        ExpandedMethodCount = expandedCore.Count,
        ExpandedMembersAdded = expandedCore.Except(core).Order(StringComparer.Ordinal).ToArray(),
        ExpandedMembersRemoved = core.Except(expandedCore).Order(StringComparer.Ordinal).ToArray(),
        Members = core.Order(StringComparer.Ordinal).Select(id => new
        {
            nodes[id].Id,
            nodes[id].Owner,
            nodes[id].File,
            nodes[id].Line,
            nodes[id].Name,
            nodes[id].Visibility,
            nodes[id].IsStatic,
            ReturnType = nodes[id].Symbol.ReturnType.ToDisplayString(),
            InternalCallers = primary.Where(e => e.Callee == id && coreIds.Contains(e.Caller)).Select(e => e.Caller).Order(StringComparer.Ordinal).ToArray(),
            InternalCallees = primary.Where(e => e.Caller == id && coreIds.Contains(e.Callee)).Select(e => e.Callee).Order(StringComparer.Ordinal).ToArray(),
            ExternalCallers = ingress.Where(e => e.Callee == id).ToArray(),
            ExternalDependencies = egress.Where(e => e.Caller == id).ToArray(),
            StateReferences = references.Where(r => r.Caller == id).DistinctBy(r => (r.Id, r.Read, r.Write)).ToArray(),
            Parameters = nodes[id].Symbol.Parameters.Select(p => new { p.Name, Type = p.Type.ToDisplayString(), RefKind = p.RefKind.ToString() })
        }),
        InternalEdges = primary.Where(e => coreIds.Contains(e.Caller) && coreIds.Contains(e.Callee)).ToArray()
    },
    Ingress = ingress.Select(e => new { Edge = e, Caller = Describe(nodes[e.Caller]) }),
    Egress = egress,
    EgressOwners = egress.GroupBy(e => e.Owner).OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => new
        {
            Owner = g.Key,
            DistinctMethodEdges = g.DistinctBy(e => (e.Caller, e.Callee)).Count(),
            InvocationEdges = g.Count(e => e.Kind == "Invocation"),
            Methods = g.Select(e => e.Callee).Distinct().Order(StringComparer.Ordinal),
            Kinds = g.Select(e => e.Kind).Distinct().Order(StringComparer.Ordinal)
        }),
    ReachableSourceMethods = reachable.Order(StringComparer.Ordinal).Select(id => Describe(nodes[id])),
    ReachableAnalyzerOutsideScc = reachable.Where(id => !coreIds.Contains(id)
        && nodes[id].Owner.Contains("ExceptionFlowAnalyzer", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal).Select(id => Describe(nodes[id])),
    ReachableSourceEdges = reachableEdges.Where(e => nodes.ContainsKey(e.Callee)),
    ComponentEdges = typeEdges,
    ComponentCycles = Tarjan(reachableOwners, typeEdges).Where(c => c.Count > 1)
        .Select(c => c.Order(StringComparer.Ordinal)),
    LogicalComponentEdges = logicalEdges,
    LogicalComponentCycles = Tarjan(logicalEdges.SelectMany(e => new[] { e.Caller, e.Callee }),
        logicalEdges).Where(c => c.Count > 1).Select(c => c.Order(StringComparer.Ordinal)),
    DirectState = references.Where(r => coreIds.Contains(r.Caller))
        .DistinctBy(r => (r.Caller, r.Id, r.Read, r.Write)).OrderBy(r => r.Id, StringComparer.Ordinal),
    ReachableState = references.Where(r => reachable.Contains(r.Caller) && r.Kind != "Parameter")
        .DistinctBy(r => (r.Caller, r.Id, r.Read, r.Write)).OrderBy(r => r.Id, StringComparer.Ordinal),
    FieldDeclarations = declarations,
    InitializerDependencies = initializerEdges.DistinctBy(e => (e.Caller, e.Callee)),
    ReachableInitializerDependencies = initializerEdges.Where(e => initializedFields.Contains(e.Caller))
        .DistinctBy(e => (e.Caller, e.Callee)),
    LocalState = localState,
    ReceiverOperations = mutations,
    DelegateSites = delegateSites,
    AllSourceNodes = nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal).Select(Describe),
    AllStateAccesses = references.Where(r => r.Kind != "Parameter").DistinctBy(r => (r.Caller, r.Id, r.Read, r.Write)),
    AnalyzerPartials = compilation.SyntaxTrees.Count(tree =>
        Path.GetFileName(tree.FilePath).StartsWith("ExceptionFlowAnalyzer", StringComparison.Ordinal)),
    AnalyzerNonblankSloc = compilation.SyntaxTrees.Where(tree =>
        Path.GetFileName(tree.FilePath).StartsWith("ExceptionFlowAnalyzer", StringComparison.Ordinal))
        .Sum(tree => tree.GetText().Lines.Count(line => !string.IsNullOrWhiteSpace(line.ToString())))
};
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
await File.WriteAllTextAsync(args[1], JsonSerializer.Serialize(output,
    new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
Console.WriteLine($"SCC {core.Count}/{output.Scc.InternalEdgeCount}; expanded {expandedCore.Count}; ingress {ingress.Count}; egress {egress.Count}; reachable Analyzer outside SCC {output.ReachableAnalyzerOutsideScc.Count()}");
Console.WriteLine($"Compilation errors {output.CompilationErrors.Length}; expanded type cycles {output.ComponentCycles.Count()}; inter-component cycles {output.LogicalComponentCycles.Count()}");
if (args.Length == 3 && args[2] == "--extraction-plan")
{
    ExtractionPlan.Write(compilation, coreIds, ingress.Select(e => e.Callee).ToHashSet(StringComparer.Ordinal),
        rootDirectory, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "extraction-plan.json"));
}

return output.CompilationErrors.Length == 0 ? 0 : 1;

static object Describe(Callable node) => new
{
    node.Id,
    node.Owner,
    node.File,
    node.Line,
    node.Name,
    node.Kind,
    node.Visibility,
    node.IsStatic
};

static string SymbolType(ISymbol symbol) => symbol switch
{
    IFieldSymbol f => f.Type.ToDisplayString(),
    IPropertySymbol p => p.Type.ToDisplayString(),
    IParameterSymbol p => p.Type.ToDisplayString(),
    ILocalSymbol l => l.Type.ToDisplayString(),
    _ => ""
};

static string TopOwner(INamedTypeSymbol symbol)
{
    while (symbol.ContainingType != null)
    {
        symbol = symbol.ContainingType;
    }

    return symbol.ToDisplayString();
}

static List<List<string>> Tarjan(IEnumerable<string> ids, List<Edge> edges)
{
    HashSet<string> all = ids.ToHashSet(StringComparer.Ordinal);
    Dictionary<string, List<string>> outgoing = all.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
    foreach (Edge edge in edges)
    {
        if (all.Contains(edge.Caller) && all.Contains(edge.Callee))
        {
            outgoing[edge.Caller].Add(edge.Callee);
        }
    }

    Dictionary<string, int> indexes = new(StringComparer.Ordinal);
    Dictionary<string, int> low = new(StringComparer.Ordinal);
    Stack<string> stack = new();
    HashSet<string> active = new(StringComparer.Ordinal);
    List<List<string>> components = [];
    int index = 0;
    foreach (string id in all.Order(StringComparer.Ordinal))
    {
        if (!indexes.ContainsKey(id))
        {
            Visit(id);
        }
    }

    return components;

    void Visit(string id)
    {
        indexes[id] = low[id] = index++;
        stack.Push(id);
        active.Add(id);
        foreach (string target in outgoing[id].Distinct().Order(StringComparer.Ordinal))
        {
            if (!indexes.ContainsKey(target))
            {
                Visit(target);
                low[id] = Math.Min(low[id], low[target]);
            }
            else if (active.Contains(target))
            {
                low[id] = Math.Min(low[id], indexes[target]);
            }
        }

        if (low[id] != indexes[id])
        {
            return;
        }

        List<string> component = [];
        string member;
        do
        {
            member = stack.Pop();
            active.Remove(member);
            component.Add(member);
        }
        while (member != id);
        components.Add(component);
    }
}

sealed record Callable(string Id, string Owner, string File, int Line, string Kind,
    string Name, string Visibility, bool IsStatic, SyntaxNode Syntax, IMethodSymbol Symbol);
sealed record Edge(string Caller, string Callee, string Owner, string Kind, string File, int Line, bool Source);
sealed record Reference(string Caller, string Id, string Kind, string? Owner, string Type,
    bool Write, bool Read, string File, int Line);

