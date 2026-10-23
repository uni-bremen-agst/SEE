using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Helpers;

/// <summary>Assertions for explicitly approved A10 coverage-outside fixtures, not an analysis entry point.</summary>
internal static class ExceptionFlowFiniteOrbitAssertions
{
    internal static void CoverageOutside(string source, IReadOnlyList<Finding> findings,
        ExceptionAnalysisMode mode, params MetadataReference[] additionalReferences)
    {
        Assert.True(mode is ExceptionAnalysisMode.ProjectTransitive or ExceptionAnalysisMode.SolutionTransitive);
        // Reconstruct exactly the single-compilation input used by CheckAssert.
        // This checks the typed production certificate as well as the actual
        // detector output; absence of a Finding alone is never admission proof.
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("InMemoryAssembly", [tree],
            MetadataReferences.Default.Concat(additionalReferences), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var root = Assert.Single(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText == "M"));
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
        var result = new ExceptionFlowSummaryAnalysisSession(new(context)).Analyze(root);
        CoverageOutside(result);
        Assert.Equal(ExceptionFlowFiniteOrbitSeal.BodyId(compilation.GetSemanticModel(tree).GetDeclaredSymbol(root)!), result.ProfileOutcome!.Root);
        Assert.DoesNotContain(findings, finding => finding.Smell.ID is "DOC610" or "DOC611" or "DOC632");
    }

    internal static void CoverageOutside(ExceptionFlowAnalyzerTestRun run) => CoverageOutside(run.Result);

    internal static void CoverageOutside(ExceptionFlowAnalysisResult result)
    {
        var outcome = Assert.IsType<ExceptionFlowFiniteOrbitProfile.Certificate>(result.ProfileOutcome);
        Assert.Equal("A9-FiniteOrbit-v1", outcome.Profile);
        Assert.False(outcome.Complete);
        Assert.True(outcome.NormalFindingAdmissionBlocked);
        Assert.Contains(outcome.Reasons, reason => reason.Category == "TargetOrAnalysisCoverageIncomplete");
        Assert.False(string.IsNullOrWhiteSpace(outcome.Root));
        Assert.False(string.IsNullOrWhiteSpace(outcome.ScopeDigest));
        Assert.False(string.IsNullOrWhiteSpace(outcome.InventoryDigest));
        Assert.False(string.IsNullOrWhiteSpace(outcome.InventoryWitness));
        Assert.Empty(result.ThrownExceptions);
        Assert.Empty(result.ExternalDocumentationEvidenceExceptions);
        Assert.True(result.HasUncertainPaths);
        var diagnostics = Assert.IsType<ExceptionFlowProfileDiagnostics>(result.ProfileDiagnostics);
        Assert.True(diagnostics.NormalFindingAdmissionBlocked);
        Assert.True(diagnostics.HasEscapingUncertainty);
        Assert.NotEmpty(diagnostics.Proof);
    }

    internal static void ExternalDocumentationOutside(ExceptionFlowAnalyzerTestRun run, string metadataName)
    {
        CoverageOutside(run);
        var exceptionType = run.GetRequiredType(metadataName);
        Assert.Empty(run.Result.GetExceptionPaths(exceptionType));
        Assert.Empty(run.Result.GetExternalDocumentationEvidencePaths(exceptionType));
        var expected = RoslynCanonicalIdentityFactory.CreateTypeIdentity(exceptionType);
        Assert.Single(run.Result.ProfileDiagnostics!.ExternalDocumentation.Where(type =>
            RoslynCanonicalIdentityFactory.CreateTypeIdentity(type).Equals(expected)));
        Assert.DoesNotContain(run.Result.ProfileDiagnostics.ProvenLowerWitnesses, type =>
            RoslynCanonicalIdentityFactory.CreateTypeIdentity(type).Equals(expected));
        Assert.Contains(run.Result.ProfileDiagnostics.Proof, proof => proof.Contains(run.Result.ProfileOutcome!.InventoryDigest, StringComparison.Ordinal));
    }
}
