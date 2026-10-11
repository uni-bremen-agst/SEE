using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Composes existing successful source receipts into one exact assembly-bound project; no new acquisition/validation pipeline.</summary>
    internal static class HistoricalWorkerScopeProjection
    {
        internal static bool TryProject(ExternalCSharpSyntaxTreeSet set, ExternalCSharpSyntaxTree root,
            ExternalCSharpCompilationConfiguration configuration, ExternalCompilationProvenanceDescriptor provenance,
            WorkerCompilationContext context, ExceptionAnalysisMode mode, out WorkerCompilationContext projected)
        {
            projected = context;
            string? name = mode switch
            {
                ExceptionAnalysisMode.Direct => "direct",
                ExceptionAnalysisMode.ProjectTransitive => "project-transitive",
                ExceptionAnalysisMode.SolutionTransitive => "solution-transitive",
                _ => null
            };
            if (name == null || !ExternalCSharpSyntaxTreeSetFactory.TryGetOrigin(set, out var original, out var sources)
                || !ReferenceEquals(original, configuration) || sources.Length is < 1 or > WorkerProtocol.MaximumProjectSources
                || !sources.Any(source => ReferenceEquals(source, root)) || context.ParseContext == null)
            { return false; }
            var documents = new List<WorkerSourceDocument>();
            foreach (var source in sources)
            {
                if (!ExternalCSharpSyntaxTreeFactory.TryGetConfiguration(source, out var sourceConfiguration)
                    || !ReferenceEquals(sourceConfiguration, configuration)
                    || !provenance.PortablePdb.Documents.Any(document => ReferenceEquals(document, source.Document))
                    || !HistoricalWorkerParseProjection.TryProject(sourceConfiguration!, out var parse))
                { return false; }
                // Exact decoded P5I text; UTF8 hash binds IPC, not original byte/checksum validation.
                string text = source.Text.ToString();
                documents.Add(new(source.Document.Name, text, WorkerProtocol.SourceHash(text),
                    ParseContext: parse, AssemblyName: context.AssemblyName));
            }
            projected = context with
            {
                AnalysisMode = name,
                Scope = new("single-project-v1", context.AssemblyName, sources.Select(source => source.Document.Name).ToArray(), root.Document.Name),
                AdditionalSources = documents.Where(document => document.LogicalPath != root.Document.Name).ToArray()
            };
            return true;
        }
    }
}
