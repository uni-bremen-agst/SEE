using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Test-only complete scope/graph observation in the genuine Historical host, never source execution or reporting import.</summary>
internal static class B14ScopeProbe
{
    internal static void Observe(CSharpCompilation compilation, string rootPath, string mode)
    {
        if (compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) { throw new InvalidOperationException("Test compilation failed."); }
        var assembly = Assembly.Load("XMLDocNormalizer.ExceptionFlow.Historical");
        const string prefix = "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.";
        var environmentType = assembly.GetType(prefix + "ExceptionFlowSemanticEnvironment", true)!;
        var environment = Activator.CreateInstance(environmentType, BindingFlags.Instance | BindingFlags.NonPublic, null, [compilation], null)!;
        var sessionType = assembly.GetType(prefix + "ExceptionFlowSummaryAnalysisSession", true)!;
        var session = sessionType.GetMethod("CreateSummaryAnalysisSession", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, [environment])!;
        var root = compilation.SyntaxTrees.Single(tree => tree.FilePath == rootPath).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
        var adapter = assembly.GetType(prefix + "Canonical.RoslynCanonicalExceptionFlowAdapter", true)!;
        var canonical = adapter.GetMethod("CreateAnalysisResult", BindingFlags.Static | BindingFlags.Public)!;
        var graph = sessionType.GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false) }
        };
        object Analyze() => mode == "direct"
            ? assembly.GetType(prefix + "ExceptionFlowLocalSourceAnalyzer", true)!.GetMethod("AnalyzeDirectlyThrownExceptions", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [root, environment])!
            : sessionType.GetMethod("Analyze", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(session, [root])!;
        string Result() => JsonSerializer.Serialize(canonical.Invoke(null, [Analyze()]), options);
        string[] Summaries()
        {
            var entries = (IDictionary)graph.GetType().GetField("summaries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph)!;
            var create = adapter.GetMethod("TryCreateSummary", BindingFlags.Static | BindingFlags.Public)!;
            var result = new List<string>();
            foreach (DictionaryEntry entry in entries)
            {
                object?[] args = [entry.Key, entry.Value, null];
                if (!(bool)create.Invoke(null, args)!) { throw new InvalidOperationException("Incomplete canonical graph."); }
                result.Add(JsonSerializer.Serialize(args[2], options));
            }
            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
        var getModel = environmentType.GetMethod("TryGetSemanticModel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var ownership = compilation.SyntaxTrees.Select(tree =>
        {
            object?[] first = [tree, null]; object?[] second = [tree, null];
            if (!(bool)getModel.Invoke(environment, first)! || !(bool)getModel.Invoke(environment, second)!
                || !ReferenceEquals(first[1], second[1]) || !ReferenceEquals(((SemanticModel)first[1]!).Compilation, compilation))
            { throw new InvalidOperationException("Unstable/foreign model ownership."); }
            return new { LogicalPath = tree.FilePath, AssemblyName = compilation.AssemblyName, Owned = true, StableModel = true };
        }).ToArray();
        var foreign = CSharpSyntaxTree.ParseText(compilation.SyntaxTrees.First().GetText().ToString(), path: compilation.SyntaxTrees.First().FilePath);
        object?[] foreignArgs = [foreign, null];
        if ((bool)getModel.Invoke(environment, foreignArgs)!) { throw new InvalidOperationException("Foreign tree accepted."); }
        var foreignCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(foreign);
        var localMethod = compilation.GetSemanticModel(root.SyntaxTree).GetDeclaredSymbol(root)!;
        var resolve = environmentType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name == "TryResolveSupportingSourceMethod" && method.GetParameters().Length == 3);
        object?[] foreignBinding = [localMethod, foreignCompilation, null];
        if ((bool)resolve.Invoke(environment, foreignBinding)!) { throw new InvalidOperationException("Foreign binding compilation accepted."); }
        string firstResult = Result(); var summaries = Summaries(); int count = (int)graph.GetType().GetProperty("Count")!.GetValue(graph)!;
        string repeat = Result();
        using var document = JsonDocument.Parse(firstResult);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            ActualHistoricalHost = true,
            Mode = mode,
            RootLogicalPath = rootPath,
            Ownership = ownership,
            ForeignTreeRejected = true,
            ForeignBindingRejected = true,
            Result = document.RootElement,
            GraphCount = count,
            RepeatedGraphCount = (int)graph.GetType().GetProperty("Count")!.GetValue(graph)!,
            Summaries = summaries,
            RepeatedSummaries = Summaries(),
            RepeatedResultEqual = firstResult == repeat,
            SubmittedSourceExecuted = false,
            CompilerMvids = new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId }
        }, options));
    }
}
