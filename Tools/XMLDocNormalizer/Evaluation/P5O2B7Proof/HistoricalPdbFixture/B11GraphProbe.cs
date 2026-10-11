using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Test-only graph observation in the existing isolated Historical fixture; no production host or IPC extension.</summary>
internal static class B11GraphProbe
{
    internal static void Run(string path)
    {
        string source = Console.In.ReadToEnd();
        if (source.Length > 32768 || string.IsNullOrWhiteSpace(path) || path.Length > 4096 || path.Any(char.IsControl))
        { throw new ArgumentException("Test graph input is outside the bounded profile."); }
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path);
        string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(file => allowed.Contains(Path.GetFileName(file), StringComparer.Ordinal)).OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(file => MetadataReference.CreateFromFile(file)).ToArray();
        var compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        if (compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) { throw new InvalidOperationException("Test graph compilation failed."); }
        var assembly = Assembly.Load("XMLDocNormalizer.ExceptionFlow.Historical");
        const string prefix = "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.";
        var environmentType = assembly.GetType(prefix + "ExceptionFlowSemanticEnvironment", throwOnError: true)!;
        var environment = Activator.CreateInstance(environmentType, BindingFlags.Instance | BindingFlags.NonPublic, null, [compilation], null)!;
        var sessionType = assembly.GetType(prefix + "ExceptionFlowSummaryAnalysisSession", throwOnError: true)!;
        var session = sessionType.GetMethod("CreateSummaryAnalysisSession", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, [environment])!;
        var member = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
        var analyze = sessionType.GetMethod("Analyze", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var adapter = assembly.GetType(prefix + "Canonical.RoslynCanonicalExceptionFlowAdapter", throwOnError: true)!;
        var canonical = adapter.GetMethod("CreateAnalysisResult", BindingFlags.Static | BindingFlags.Public)!;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };
        object result = canonical.Invoke(null, [analyze.Invoke(session, [member])])!;
        object graph = sessionType.GetField("graph", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
        int count = (int)graph.GetType().GetProperty("Count")!.GetValue(graph)!;
        string json = JsonSerializer.Serialize(result, result.GetType(), options);
        object repeated = canonical.Invoke(null, [analyze.Invoke(session, [member])])!;
        int repeatedCount = (int)graph.GetType().GetProperty("Count")!.GetValue(graph)!;
        using var document = JsonDocument.Parse(json);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            TestOnly = true,
            ActualHistoricalHost = true,
            GraphCount = count,
            RepeatedGraphCount = repeatedCount,
            RepeatedResultEqual = json == JsonSerializer.Serialize(repeated, repeated.GetType(), options),
            Result = document.RootElement,
            SubmittedSourceExecuted = false,
            CompilerMvids = new[] { typeof(Compilation).Assembly.ManifestModule.ModuleVersionId, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId }
        }, options));
    }
}
