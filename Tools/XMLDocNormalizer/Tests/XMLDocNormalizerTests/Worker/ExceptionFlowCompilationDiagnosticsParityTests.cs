using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Records the existing A9/A10 projection across repeated roots in one environment.</summary>
public sealed class ExceptionFlowCompilationDiagnosticsParityTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("complete")]
    [InlineData("packaging")]
    [InlineData("binding-error")]
    [InlineData("outside-with-ground-facts")]
    public void RepeatedRootsPreserveCertificatesGroundFactsFindingsAndCoverage(string scenario)
    {
        string tail = scenario switch
        {
            "binding-error" => "static void Broken() { Missing(); }",
            "outside-with-ground-facts" => "static void Unknown() { System.Console.WriteLine(); }",
            _ => string.Empty
        };
        string body = scenario == "outside-with-ground-facts" ? "Helper(); Unknown();" : "Helper();";
        string source = """
            class Entry
            {
                /// <summary>First root.</summary>
                /// <exception cref="System.InvalidOperationException">Failure.</exception>
                public static void First() { BODY }
                /// <summary>Second root.</summary>
                /// <exception cref="System.ArgumentException">Failure.</exception>
                public static void Second() { Helper(); }
                static void Helper() { throw new System.InvalidOperationException(); }
                TAIL
            }
            """.Replace("BODY", body).Replace("TAIL", tail);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "/P1D/Entry.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("P1D.Parity", [tree], MetadataReferences.Default,
            new CSharpCompilationOptions(scenario == "packaging" ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));
        ProjectClosureSemanticContext context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
        ExceptionFlowSemanticEnvironment environment = new(context);
        ExceptionFlowSummaryAnalysisSession session = new(environment);
        MemberDeclarationSyntax[] roots = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(member => XmlDocUtils.TryGetDocComment(member) != null).ToArray();
        Assert.Equal(2, roots.Length);

        string Capture(ExceptionFlowSummaryAnalysisSession active)
        {
            AnalysisCoverage coverage = new();
            coverage.Begin("P1DParity", ExceptionAnalysisMode.SolutionTransitive);
            AnalysisCoverageObserver.ObserveCompilation(coverage, compilation);
            var flows = roots.Select(member =>
            {
                ExceptionFlowAnalysisResult result = active.Analyze(member);
                var certificate = result.ProfileOutcome!;
                Assert.Equal("A9-FiniteOrbit-v1", certificate.Profile);
                if (scenario == "binding-error")
                {
                    Assert.False(certificate.Complete);
                    Assert.Contains(certificate.Reasons, reason => reason.Category == "RepresentationOutsideGrammar"
                        && reason.Witness == ExceptionFlowFiniteOrbitAdmission.BodyId(compilation.GetSemanticModel(tree).GetDeclaredSymbol(member)!)
                            + " / source compilation has errors");
                }
                else if (scenario is "complete" or "packaging") Assert.True(certificate.Complete);
                var ground = result.ProfileDiagnostics;
                return new
                {
                    certificate.Profile, certificate.Query, certificate.Root, certificate.ScopeDigest,
                    certificate.InventoryDigest, certificate.InventoryWitness, certificate.Complete,
                    Reasons = certificate.Reasons.Select(reason => new { reason.Category, reason.Witness }).ToArray(),
                    certificate.InformationQuality, certificate.MayMiss, certificate.MayAdd, certificate.NormalFindingAdmissionBlocked,
                    Thrown = Types(result.ThrownExceptions), External = Types(result.ExternalDocumentationEvidenceExceptions),
                    Uncertain = result.UncertainTargets.Order(StringComparer.Ordinal).ToArray(),
                    Ground = ground == null ? null : new
                    {
                        Proven = Types(ground.ProvenLowerWitnesses), External = Types(ground.ExternalDocumentation),
                        ground.HasEscapingUncertainty, ground.Proof, ground.NormalFindingAdmissionBlocked
                    }
                };
            }).ToArray();
            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath,
                compilation.GetSemanticModel(tree), context,
                new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, active, coverage);
            return JsonSerializer.Serialize(new
            {
                Flows = flows,
                Findings = findings.Select(finding => new
                {
                    Id = finding.Smell.ID, Severity = finding.Smell.Severity.ToString(), finding.FilePath,
                    finding.Line, finding.Column, finding.TagName, finding.Message, finding.Snippet
                }).ToArray(),
                Coverage = coverage.Report()
            });
        }

        string first = Capture(session);
        Assert.Equal(first, Capture(session));
        Assert.Equal(first, Capture(new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(context))));
        output.WriteLine("P1D_PARITY " + scenario + " " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first))));
        output.WriteLine(first);
    }

    private static string[] Types(IEnumerable<INamedTypeSymbol> types) => types
        .Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Order(StringComparer.Ordinal).ToArray();
}
