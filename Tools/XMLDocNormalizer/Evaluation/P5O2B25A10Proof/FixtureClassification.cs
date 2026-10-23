using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Read-only preflight observations and explicit A9 contract checks, NOT production admission.</summary>
internal static class FixtureClassification
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static object P(object value, string name) => value.GetType().GetProperty(name, All)!.GetValue(value)!;
    private static object New(Type type, params object?[] arguments) => Activator.CreateInstance(type, All, null, arguments, null)!;
    private static Type Type(Assembly assembly, string suffix) => assembly.GetTypes().Single(t => t.FullName == suffix);

    internal static void Run(string tool, string bin)
    {
        bool expanded = File.Exists(Path.Combine(tool, "artifacts/p5o2b25a10/expanded-semantic-fixtures-r3.json"));
        bool all = File.Exists(Path.Combine(tool, "artifacts/p5o2b25a10/all-finding-fixtures-r1.json"));
        string path = Path.Combine(tool, expanded ? "artifacts/p5o2b25a10/expanded-semantic-classification-r2.json" : all ? "artifacts/p5o2b25a10/all-finding-classification-r1.json" : "artifacts/p5o2b25a10/full-fixture-classification-r2.json");
        if (File.Exists(path)) throw new InvalidOperationException("Preserve fixture classification.");
        AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
        var tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
        var main = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizer.dll"));
        var references = (IEnumerable<MetadataReference>)Type(tests, "XMLDocNormalizerTests.Helpers.MetadataReferences").GetProperty("Default", All)!.GetValue(null)!;
        var contextType = Type(main, "XMLDocNormalizer.Execution.Semantic.ProjectClosureSemanticContext");
        var environmentType = Type(main, "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSemanticEnvironment");
        var sessionType = Type(main, "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSummaryAnalysisSession");
        var evaluatorType = Type(main, "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSummaryGraphEvaluator");
        var finder = Type(tests, "XMLDocNormalizerTests.Helpers.CheckAssert").GetMethods(All)
            .Single(m => m.Name == "FindSemanticExceptionFindingsForSource" && m.GetParameters().Length == 3);
        var multiFinder = Type(tests, "XMLDocNormalizerTests.Helpers.CheckAssert").GetMethod("FindSemanticExceptionFindingsForSources", All)!;
        using var input = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, expanded ? "artifacts/p5o2b25a10/expanded-semantic-fixtures-r3.json" : all ? "artifacts/p5o2b25a10/all-finding-fixtures-r1.json" : "artifacts/p5o2b25a10/full-fixture-preflight-r3.json")));
        var rows = new List<object>();
        foreach (JsonElement capture in input.RootElement.GetProperty("Captures").EnumerateArray())
        {
            string caseId = capture.GetProperty("Case").GetString()!;
            string mode = capture.GetProperty("Mode").GetString()!;
            string source = capture.GetProperty("Source").GetString()!;
            if (mode is "PrimitiveDiagnostic" or "DiagnosticNative")
            {
                rows.Add(new { Case = caseId, Mode = mode, Classification = "diagnostic / low-level", Reason = "Existing finite signature/direct/recursive diagnostic helper, no normal certified output" });
                continue;
            }
            if (capture.TryGetProperty("SupportingProjectSource", out var dependency))
            {
                var fixture = ((Solution Solution, Project ReportingProject, Document ReportingDocument))Type(tests, "XMLDocNormalizerTests.Helpers.SolutionTestBuilder")
                    .GetMethod("CreateTwoProjectSolution", All)!.Invoke(null, [source, dependency.GetString()])!;
                var targetCompilation = fixture.ReportingProject.GetCompilationAsync().GetAwaiter().GetResult()!;
                var targetTree = fixture.ReportingDocument.GetSyntaxTreeAsync().GetAwaiter().GetResult()!;
                object choice = Enum.Parse(finder.GetParameters()[1].ParameterType, mode);
                object targetContext = Type(main, "XMLDocNormalizer.Execution.Semantic.ProjectClosureSemanticContextBuilder")
                    .GetMethod("Build", All)!.Invoke(null, [new[] { fixture.ReportingProject }, choice])!;
                if (mode == "Direct" || (mode == "ProjectTransitiveDeclaredExceptions"
                    && !(bool)contextType.GetMethod("HasDeclaredExceptionTypesInReportingScope", All)!.Invoke(targetContext, null)!))
                {
                    rows.Add(new { Case = caseId, Mode = mode, Classification = "unrelated", Reason = "Original two-project mode/declared guard does not request Session.Analyze" });
                    continue;
                }
                object targetEnvironment = New(environmentType, targetContext);
                object targetSession = New(sessionType, targetEnvironment);
                var targetMethod = targetTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M");
                object?[] buildArgs = [targetMethod, null, null];
                if (!(bool)sessionType.GetMethod("TryBuildTransitiveSummaryGraph", All)!.Invoke(targetSession, buildArgs)!) throw new InvalidOperationException("Original project fixture failed");
                object graph = buildArgs[1]!;
                var evidence = InspectGraph(graph);
                rows.Add(new { Case = caseId, Mode = mode, Classification = evidence.Count == 0 ? "EXACT / admitted" : "OUTSIDE PROFILE",
                    Reason = evidence.Count == 0 ? "Original two-project fixture, closed nongeneric root, finite source graph, no incomplete dependency" : "TargetOrAnalysisCoverageIncomplete",
                    Evidence = evidence, GraphNodes = (int)P(graph, "Count"), FixtureKind = "Original SolutionTestBuilder, original ProjectClosureSemanticContextBuilder and actual CompilationReference closure" });
                continue;
            }
            var extra = new List<MetadataReference>();
            if (capture.TryGetProperty("ReferenceDescriptors", out var descriptors))
                foreach (var descriptor in descriptors.EnumerateArray())
                {
                    if (descriptor.TryGetProperty("Source", out var text) && text.ValueKind == JsonValueKind.String)
                        extra.Add((MetadataReference)Type(tests, "XMLDocNormalizerTests.Helpers.ExternalDocumentationReferenceTestHelper").GetMethod("Create", All)!.Invoke(null, [text.GetString()])!);
                    else if (descriptor.TryGetProperty("Path", out var file) && file.ValueKind == JsonValueKind.String)
                        extra.Add(MetadataReference.CreateFromFile(file.GetString()!));
                }
            else if (capture.TryGetProperty("AdditionalReferences", out var extraPaths))
                foreach (var file in extraPaths.EnumerateArray()) if (file.ValueKind == JsonValueKind.String) extra.Add(MetadataReference.CreateFromFile(file.GetString()!));
            var sourceFiles = new List<(string FileName, string Source)>();
            if (capture.TryGetProperty("SourceFiles", out var files) && files.ValueKind == JsonValueKind.Array)
                sourceFiles.AddRange(files.EnumerateArray().Select(f => (f.GetProperty("Path").GetString()!, f.GetProperty("Text").GetString()!)));
            if (sourceFiles.Count == 0) sourceFiles.Add(("InMemory.cs", source));
            var trees = sourceFiles.Select(f => CSharpSyntaxTree.ParseText(f.Source, path: f.FileName)).ToArray();
            var compilation = CSharpCompilation.Create("InMemoryAssembly", trees, references.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray();
            if (compileErrors.Length > 0)
            {
                rows.Add(new { Case = caseId, Mode = mode, Classification = "OUTSIDE PROFILE", Reason = "RepresentationOutsideGrammar: original reconstructed fixture contains invalid semantic input", Errors = compileErrors,
                    InputReconstructed = true, FixtureRepaired = false });
                continue;
            }
            object context = contextType.GetMethod("CreateSingleCompilationContext", All)!.Invoke(null, [trees[0], compilation])!;
            bool requested = mode != "Direct" && (mode != "ProjectTransitiveDeclaredExceptions"
                || (bool)contextType.GetMethod("HasDeclaredExceptionTypesInReportingScope", All)!.Invoke(context, null)!);
            object?[]? findingObjects = null;
            if (mode != "SolutionTransitiveNative")
            {
                object choice = Enum.Parse(finder.GetParameters()[1].ParameterType, mode);
                object findings = sourceFiles.Count == 1 ? finder.Invoke(null, [source, choice, extra.ToArray()])!
                    : multiFinder.Invoke(null, [choice, sourceFiles.ToArray()])!;
                findingObjects = ((IEnumerable)findings).Cast<object?>().ToArray();
            }
            var findingsData = findingObjects?.Select(f => new { Id = (string)P(P(f!, "Smell"), "ID"), Message = (string)P(f!, "Message") }).ToArray();
                if (!requested)
            {
                rows.Add(new { Case = caseId, Mode = mode, Classification = "unrelated", Reason = mode == "Direct" ? "Direct entry does not invoke Session.Analyze"
                    : "Original declared-exception guard has no reporting-local exception type; no summary requested", Findings = findingsData });
                continue;
            }
            var members = trees[0].GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
                .Where(m => m is MethodDeclarationSyntax or PropertyDeclarationSyntax or ConstructorDeclarationSyntax or IndexerDeclarationSyntax)
                .Where(m => mode == "SolutionTransitiveNative"
                    ? m is MethodDeclarationSyntax method && method.Identifier.ValueText == capture.GetProperty("RootMethod").GetString()
                    : m.GetLeadingTrivia().Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)))
                .ToArray();
            var roots = new List<object>();
            bool outside = false;
            foreach (var member in members)
            {
                var model = compilation.GetSemanticModel(member.SyntaxTree);
                ISymbol symbol = model.GetDeclaredSymbol(member)!;
                object environment = New(environmentType, context);
                object session = New(sessionType, environment);
                object?[] arguments = [member, null, null];
                if (!(bool)sessionType.GetMethod("TryBuildTransitiveSummaryGraph", All)!.Invoke(session, arguments)!)
                    throw new InvalidOperationException("Original raw build failed: " + caseId);
                object graph = arguments[1]!;
                var evidence = InspectGraph(graph);
                object evaluator = New(evaluatorType);
                object evaluated = evaluatorType.GetMethod("Evaluate", All)!.Invoke(evaluator, [graph, arguments[2], compilation])!;
                var projectedTypes = ((IEnumerable)P(evaluated, "ThrownExceptions")).Cast<object>().ToArray();
                bool truncated = projectedTypes.Any(t => (bool)evaluated.GetType().GetMethod("ArePathsTruncated", All)!.Invoke(evaluated, [t])!);
                if (truncated) evidence.Add("TargetOrAnalysisCoverageIncomplete: required result paths are truncated");
                if (RootIsOpen(symbol)) evidence.Add("RepresentationOutsideGrammar: root binder environment is open");
                var growth = GrowthWitnesses(graph, compilation);
                evidence.AddRange(growth);
                bool rejected = evidence.Count > 0;
                outside |= rejected;
                roots.Add(new { Root = symbol.ToDisplayString(), Classification = rejected ? "OUTSIDE PROFILE" : "EXACT / admitted",
                    Reason = rejected ? "Finite input coverage/binding/growth witness, before any future production registration"
                        : "Closed root, complete original finite transfer graph; no positive binder dependency cycle",
                    Evidence = evidence, GraphNodes = (int)P(graph, "Count"),
                    ObservedThrownTypes = projectedTypes.Cast<ITypeSymbol>().Select(t => t.ToDisplayString()).ToArray(),
                    ObservedPathCounts = projectedTypes.Select(t => ((IEnumerable)evaluated.GetType().GetMethod("GetExceptionPaths", All)!.Invoke(evaluated, [t])!).Cast<object>().Count()).ToArray() });
            }
            rows.Add(new { Case = caseId, Mode = mode, Classification = members.Length == 0 ? "unrelated" : outside ? "OUTSIDE PROFILE" : "EXACT / admitted",
                Reason = members.Length == 0 ? "No documented executable member analyzed by original detector" : "Per-root A9 contract checks below",
                Roots = roots, Findings = findingsData, OriginalEntry = capture.GetProperty("Entry").GetString(), AdditionalReferencesReconstructed = extra.Count });
        }
        File.WriteAllText(path, JsonSerializer.Serialize(new { Kind = "Read-only fixture-level A9 preflight, not production admission validation", Rows = rows,
            ProductOrTestFilesChanged = false, UnresolvedRows = rows.Count(r => JsonSerializer.Serialize(r).Contains("\"UNRESOLVED\"", StringComparison.Ordinal)) }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Classified {rows.Count} reconstructed calls: {path}");
    }

    private static List<string> InspectGraph(object graph)
    {
        var dictionary = (IDictionary)graph.GetType().GetField("summaries", All)!.GetValue(graph)!;
        var reasons = new SortedSet<string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            var symbol = (ISymbol)P(entry.Key, "Symbol");
            if (!(bool)P(entry.Value!, "HasExecutableBody")) reasons.Add("TargetOrAnalysisCoverageIncomplete: no modeled executable body for " + symbol.ToDisplayString());
            foreach (string uncertain in (IEnumerable)P(entry.Value!, "UncertainTargets")) reasons.Add("TargetOrAnalysisCoverageIncomplete: " + uncertain);
        }
        return reasons.ToList();
    }

    private static bool RootIsOpen(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method && method.TypeArguments.Any(t => t is ITypeParameterSymbol)) return true;
        for (INamedTypeSymbol? type = symbol.ContainingType; type != null; type = type.ContainingType)
            if (type.TypeArguments.Any(t => t is ITypeParameterSymbol)) return true;
        return false;
    }

    // Uses scoped compiler symbols for semantic equality, not display-name substitution.
    private static List<string> GrowthWitnesses(object graph, Compilation compilation)
    {
        var dictionary = (IDictionary)graph.GetType().GetField("summaries", All)!.GetValue(graph)!;
        var bodies = dictionary.Keys.Cast<object>().Select(k => (ISymbol)P(k, "Symbol")).Distinct(SymbolEqualityComparer.Default).ToArray();
        var slots = new List<(ISymbol Body, ITypeParameterSymbol Parameter)>();
        int Slot(ISymbol body, ITypeParameterSymbol parameter)
        {
            int old = slots.FindIndex(s => SymbolEqualityComparer.Default.Equals(s.Body, body.OriginalDefinition)
                && SymbolEqualityComparer.Default.Equals(s.Parameter, parameter.OriginalDefinition));
            if (old >= 0) return old;
            slots.Add((body.OriginalDefinition, (ITypeParameterSymbol)parameter.OriginalDefinition)); return slots.Count - 1;
        }
        var edges = new List<(int From, int To, int Weight)>();
        foreach (var body in bodies)
            foreach (var declaration in body.DeclaringSyntaxReferences)
            {
                if (!compilation.SyntaxTrees.Contains(declaration.SyntaxTree)) continue;
                var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var invocation in declaration.GetSyntax().DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol called) continue;
                    ISymbol target = called.OriginalDefinition;
                    if (!bodies.Any(b => SymbolEqualityComparer.Default.Equals(b, target))) continue;
                    var bindings = called.TypeParameters.Zip(called.TypeArguments).ToList();
                    for (INamedTypeSymbol? type = called.ContainingType; type != null; type = type.ContainingType)
                        bindings.AddRange(type.OriginalDefinition.TypeParameters.Zip(type.TypeArguments));
                    foreach (var binding in bindings)
                    {
                        int destination = Slot(target, binding.First);
                        void Occurrences(ITypeSymbol term, int depth)
                        {
                            if (term is ITypeParameterSymbol parameter) edges.Add((Slot(body, parameter), destination, depth));
                            else if (term is INamedTypeSymbol named)
                            {
                                foreach (var argument in named.TypeArguments) Occurrences(argument, depth + 1);
                                if (named.ContainingType != null) Occurrences(named.ContainingType, depth + 1);
                            }
                        }
                        Occurrences(binding.Second, 0);
                    }
                }
            }
        int count = slots.Count;
        var reachable = new bool[count, count];
        foreach (var edge in edges) reachable[edge.From, edge.To] = true;
        for (int k = 0; k < count; k++) for (int i = 0; i < count; i++) for (int j = 0; j < count; j++) reachable[i, j] |= reachable[i, k] && reachable[k, j];
        return edges.Where(e => e.Weight > 0 && (e.From == e.To || reachable[e.To, e.From]))
            .Select(e => "PositiveConstructorCycle: " + slots[e.From].Body.ToDisplayString() + " -> " + slots[e.To].Body.ToDisplayString()).Distinct().ToList();
    }
}
