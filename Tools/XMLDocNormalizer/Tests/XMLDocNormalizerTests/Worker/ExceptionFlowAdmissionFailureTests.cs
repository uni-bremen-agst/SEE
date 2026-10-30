using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Exercises existing discovery exception and production rollback boundaries (K7/K13).</summary>
public sealed class ExceptionFlowAdmissionFailureTests
{
    [Fact]
    public async Task AnalyzerExceptionAfterRegistrationReturnsNoPartialCertificateAndPreservesEarlierRoot()
    {
        var provider = new FailingDocumentationProvider();
        var dependency = new AdmissionCharacterization.Fixture([("/P2B/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")]);
        using var image = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(image).Success);
        var reference = MetadataReference.CreateFromImage(image.ToArray(), documentation: provider);
        var fixture = new AdmissionCharacterization.Fixture([
            RootFile("A", "Known(); External.Api.Run();"), RootFile("B", "Known();"),
            ("/P2B/Helpers.cs", "public static partial class Entry { static void Known() { throw new System.InvalidOperationException(); } }")
        ], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        string before = fixture.Capture("B", run.Context, run.Session);
        var graph = AdmissionCharacterization.Field<ExceptionFlowSummaryGraph>(run.Session, "graph");
        int count = graph.Count;

        var recorder = new AdmissionObservationRecorder();
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.Throws<DocumentationFailure>(() => ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(
            fixture.Symbol("A"), run.Environment, recorder, fragment, new(fixture.Symbol("A"))));
        Assert.Single(recorder.Keys);
        Assert.Single(fragment.CallEdges);
        Assert.Contains("Known", recorder.Events[0], StringComparison.Ordinal);
        Assert.Empty(recorder.ObservedUncertainty); // No final Observe after the provider throws.

        ExceptionFlowAnalysisResult? partial = null;
        Assert.Throws<DocumentationFailure>(() => partial = run.Session.Analyze(fixture.Root("A")));
        Assert.Null(partial);
        Assert.Equal(count, graph.Count);
        var routed = await new ExceptionFlowAnalysisRouter().AnalyzeAsync(new(
            ExceptionFlowAnalyzerSelection.Current, new(fixture.Root("A"), run.Session)));
        Assert.False(routed.Succeeded);
        Assert.Equal(ExceptionFlowAnalysisRoutingFailureCode.CurrentExecutionFailure, routed.RoutingFailure!.Code);
        Assert.Contains("P2B documentation failure", routed.RoutingFailure.Message, StringComparison.Ordinal);
        Assert.Null(routed.Result);
        Assert.Null(routed.CurrentAnalysis);
        Assert.True(provider.Requests >= 3);
        Assert.Equal(before, fixture.Capture("B", run.Context, run.Session));
        Assert.Equal(count, graph.Count);
    }

    [Fact]
    public void FailedProductionRemovesOnlyNewNodesAndLeavesLaterColdEvidenceUnchanged()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            RootFile("A", "Known();"), RootFile("B", "Allowed();"),
            ("/P2B/Helpers.cs", """
            public static partial class Entry
            {
                static void Known() { throw new System.InvalidOperationException(); }
                static void Allowed() { throw new System.ArgumentException(); }
                static void Undiscovered() {}
            }
            """)
        ]);
        var run = fixture.Session();
        string beforeA = fixture.Capture("A", run.Context, run.Session);
        var graph = AdmissionCharacterization.Field<ExceptionFlowSummaryGraph>(run.Session, "graph");
        int oldCount = graph.Count;
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("B"), run.Environment);
        Assert.True(admission.Outcome.Complete);
        graph.BeginProduction(admission.Seal, run.Environment);
        var context = new ExceptionFlowCallContext(fixture.Symbol("B"));
        var key = new ExceptionFlowCallableKey(fixture.Symbol("B"), context);
        graph.GetOrAdd(key, context);
        graph.BuildingKey = key;
        graph.BuildingContext = context;
        graph.BuildingCompilation = fixture.Compilation;
        graph.RegisterTarget(fixture.Symbol("Allowed"), new(fixture.Symbol("Allowed")));
        Assert.Equal(oldCount + 2, graph.Count);
        var failure = Assert.Throws<ExceptionFlowProfileViolation>(() =>
            graph.RegisterTarget(fixture.Symbol("Undiscovered"), new(fixture.Symbol("Undiscovered"))));
        Assert.Equal("TargetOrAnalysisCoverageIncomplete", failure.Reason.Category);
        graph.EndProduction(false);
        Assert.Equal(oldCount, graph.Count);
        Assert.Null(graph.DequeuePendingOrDefault());
        Assert.Null(graph.BuildingKey);
        Assert.Null(graph.BuildingContext);
        Assert.Null(graph.BuildingCompilation);
        Assert.Equal(beforeA, fixture.Capture("A", run.Context, run.Session));
        var cold = fixture.Session();
        Assert.Equal(fixture.Capture("B", cold.Context, cold.Session), fixture.Capture("B", run.Context, run.Session));
    }

    private static (string Path, string Text) RootFile(string name, string body) => ($"/P2B/{name}.cs", $$"""
        public static partial class Entry
        {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);

    private sealed class DocumentationFailure() : System.Exception("P2B documentation failure");

    private sealed class FailingDocumentationProvider : DocumentationProvider
    {
        internal int Requests { get; private set; }
        protected override string GetDocumentationForSymbol(string documentationMemberID,
            CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            Requests++;
            throw new DocumentationFailure();
        }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
