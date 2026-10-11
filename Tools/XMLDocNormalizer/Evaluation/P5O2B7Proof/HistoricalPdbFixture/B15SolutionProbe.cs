using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>Test-only observer of the actual Worker reconstruction and Historical host, not an alternate compiler path.</summary>
internal static class B15SolutionProbe
{
    internal static void Run()
    {
        string text = Console.In.ReadToEnd();
        if (text.Length > 96 * 1024 * 1024) { throw new InvalidOperationException("Test request exceeds the graph budget."); }
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll"));
        var worker = Assembly.LoadFrom(path); // Only this isolated pinned-Historical test process loads the Worker.
        var protocol = worker.GetType("XMLDocNormalizer.HistoricalWorker.WorkerProtocol", true)!;
        var options = (JsonSerializerOptions)protocol.GetField("JsonOptions", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var inputType = worker.GetType("XMLDocNormalizer.HistoricalWorker.WorkerAnalysisInput", true)!;
        using var json = JsonDocument.Parse(text);
        var graph = json.RootElement.GetProperty("context").GetProperty("solution");
        string targetIdentity = graph.GetProperty("target").GetRawText();
        var reconstruct = worker.GetType("XMLDocNormalizer.HistoricalWorker.HistoricalAnalysis", true)!
            .GetMethod("Reconstruct", BindingFlags.Static | BindingFlags.NonPublic)!;
        CSharpCompilation? target = null;
        var supporting = new List<Compilation>();
        foreach (var node in graph.GetProperty("projects").EnumerateArray())
        {
            var input = JsonSerializer.Deserialize(node.GetProperty("input").GetRawText(), inputType, options)!;
            object?[] args = [input, null, null, null];
            if (reconstruct.Invoke(null, args) != null) { throw new InvalidOperationException("Actual Worker reconstruction failed."); }
            var compilation = (CSharpCompilation)args[2]!;
            if (node.GetProperty("identity").GetRawText() == targetIdentity) { target = compilation; }
            else { supporting.Add(compilation); }
        }
        B14ScopeProbe.Observe(target ?? throw new InvalidOperationException("Missing target."),
            json.RootElement.GetProperty("logicalPath").GetString()!, "solution-transitive", supporting);
    }
}
