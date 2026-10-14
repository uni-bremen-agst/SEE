using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Roslyn-to-neutral fact projection only. No detector relevance, Finding or mode guard policy.</summary>
    internal static class HistoricalDeclaredPolicyFacts
    {
        internal static WorkerDeclaredPolicyEvidence? Create(WorkerAnalysisInput input, CSharpCompilation compilation,
            MethodDeclarationSyntax member, ExceptionFlowSemanticEnvironment environment)
        {
            var paths = input.Context!.Scope!.OrderedSourcePaths!;
            var exceptionBase = compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionBase == null) { return null; }
            var declarations = new List<WorkerExceptionDeclaration>();
            foreach (var type in new ExceptionFlowSemanticScope(compilation).SourceTypes)
            {
                if (!type.InheritsFromOrEquals(exceptionBase)) { continue; }
                // v1 preserves exact nongeneric definitions; no constructed/definition key guessing.
                for (var container = type; container != null; container = container.ContainingType)
                { if (container.IsGenericType) { return null; } }
                var references = type.DeclaringSyntaxReferences;
                if (references.Length == 0 || references.Any(reference => !compilation.SyntaxTrees.Contains(reference.SyntaxTree))) { return null; }
                string[] ownedPaths = paths.Where(path => references.Any(reference => reference.SyntaxTree.FilePath == path)).ToArray();
                if (ownedPaths.Length == 0) { return null; }
                declarations.Add(new(RoslynCanonicalIdentityFactory.CreateTypeIdentity(type), ownedPaths));
                if (declarations.Count > WorkerDeclaredPolicyContract.MaximumDeclarations) { return null; }
            }
            var direct = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
                ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(member, environment));
            if (!declarations.Select(item => item.Type!).ToHashSet().SetEquals(input.Context.DeclaredPolicy!.ExpectedDeclarations!)) { return null; }
            if (direct.Uncertainties.Length != 0 || direct.Entries.Any(entry => entry.PathsTruncated
                || entry.EvidenceKind != CanonicalExceptionFlowEvidenceKind.Proven)) { return null; }
            return new(WorkerDeclaredPolicyContract.Profile, input.Context.DeclaredPolicy!.Mode, paths.ToArray(),
                declarations.Count, declarations.OrderBy(declaration => JsonSerializer.Serialize(declaration.Type, WorkerProtocol.JsonOptions), StringComparer.Ordinal).ToArray(), direct);
        }
    }
}
