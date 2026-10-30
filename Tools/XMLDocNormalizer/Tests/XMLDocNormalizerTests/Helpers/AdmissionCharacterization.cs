using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Helpers;

/// <summary>Reads existing admission products; contains no store or replay implementation.</summary>
internal static class AdmissionCharacterization
{
    internal static string TypeKey(ITypeSymbol type) => CanonicalIdentityKeyWriter.Write(
        RoslynCanonicalIdentityFactory.CreateTypeIdentity(type));

    internal static T Field<T>(object owner, string name) => (T)(owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(owner) ?? throw new InvalidOperationException(name));

    internal static string Fragment(ExceptionFlowSummaryFragment fragment) => JsonSerializer.Serialize(new
    {
        Sources = fragment.Sources.Select(source => new
        {
            Type = TypeKey(source.ExceptionType), source.Kind, Steps = source.LocalPath.Steps
        }).ToArray(),
        Calls = fragment.CallEdges.Select(edge => new
        {
            Target = ExceptionFlowFiniteOrbitAdmission.BodyId(edge.Target.Symbol),
            edge.CallSiteStep,
            Caught = edge.CaughtExceptionTypes.Select(TypeKey).Order(StringComparer.Ordinal).ToArray()
        }).ToArray(),
        Uncertain = fragment.UncertainTargets.Order(StringComparer.Ordinal).ToArray()
    });

    internal static object Flow(ExceptionFlowAnalysisResult result) => new
    {
        Canonical = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(result),
        Certificate = result.ProfileOutcome,
        Details = result.ProfileOutcome?.Details,
        Ground = result.ProfileDiagnostics is not { } ground ? null : new
        {
            Proven = ground.ProvenLowerWitnesses.Select(TypeKey).ToArray(),
            External = ground.ExternalDocumentation.Select(TypeKey).ToArray(),
            ground.HasEscapingUncertainty, ground.NormalFindingAdmissionBlocked, ground.Proof
        }
    };

    internal sealed class Fixture
    {
        internal Fixture(IEnumerable<(string Path, string Text)> sources,
            IEnumerable<MetadataReference>? references = null,
            CSharpParseOptions? parse = null, CSharpCompilationOptions? options = null,
            string assemblyName = "P2B.Characterization")
        {
            Compilation = CSharpCompilation.Create(assemblyName,
                sources.Select(source => CSharpSyntaxTree.ParseText(source.Text,
                    parse ?? CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12), source.Path)),
                references ?? MetadataReferences.Default,
                options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        }

        internal CSharpCompilation Compilation { get; }
        internal MethodDeclarationSyntax Root(string name) => Assert.Single(Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()),
            method => method.Identifier.ValueText == name);

        internal IMethodSymbol Symbol(string name) => (IMethodSymbol)Compilation
            .GetSemanticModel(Root(name).SyntaxTree).GetDeclaredSymbol(Root(name))!;

        internal (ProjectClosureSemanticContext Context, ExceptionFlowSemanticEnvironment Environment,
            ExceptionFlowSummaryAnalysisSession Session) Session()
        {
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(
                Compilation.SyntaxTrees.First(), Compilation);
            var environment = new ExceptionFlowSemanticEnvironment(context);
            return (context, environment, new ExceptionFlowSummaryAnalysisSession(environment));
        }

        internal string Capture(string name, ProjectClosureSemanticContext context,
            ExceptionFlowSummaryAnalysisSession session)
        {
            var root = Root(name);
            // Each documented root owns a separate tree: detector evaluation cannot prewarm another root.
            Assert.Single(root.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>());
            var result = session.Analyze(root);
            var coverage = new AnalysisCoverage();
            coverage.Begin("P2B", ExceptionAnalysisMode.SolutionTransitive);
            AnalysisCoverageObserver.ObserveCompilation(coverage, Compilation, session);
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree,
                root.SyntaxTree.FilePath, Compilation.GetSemanticModel(root.SyntaxTree), context,
                new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive },
                session, coverage);
            return JsonSerializer.Serialize(new { Flow = Flow(result), Findings = findings, Coverage = coverage.Report() });
        }
    }
}
