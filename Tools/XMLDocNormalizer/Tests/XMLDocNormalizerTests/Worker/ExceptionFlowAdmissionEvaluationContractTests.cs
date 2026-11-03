using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Characterizes existing admission/production boundaries without implementing another phase.</summary>
public sealed class ExceptionFlowAdmissionEvaluationContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameConcreteContextAcrossRootsReusesCompletedProductionButKeepsRootProjection(bool reverse)
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "try { Shared<int>(); } catch (System.NullReferenceException) {}"),
            Root("B", "Shared<int>();"),
            ("/P5E/Shared.cs", "static partial class Entry { static void Shared<T>() { Leaf<T>(); Leaf<T>(); } static void Leaf<T>() { throw null; } }")
        ]);
        var run = fixture.Session();
        ExceptionFlowSummaryAnalysisSession? session = null;
        int sharedCold = 0;
        session = new(run.Environment, symbol =>
        {
            if (symbol.Name != "Shared") return;
            sharedCold++;
            // This callback is in schema admission, before first concrete publication.
            Assert.Equal(0, Graph(session!).Count);
        });
        string first = reverse ? "B" : "A", second = reverse ? "A" : "B";
        var one = session.Analyze(fixture.Root(first));
        Assert.True(one.ProfileOutcome!.Complete, string.Join("\n", one.ProfileOutcome.Details));
        var before = Summaries(Graph(session));
        var shared = Assert.Single(before.Keys.Where(key => key.Symbol.Name == "Shared"));
        var sharedSummary = before[shared];
        int count = before.Count;
        var two = session.Analyze(fixture.Root(second));
        Assert.True(two.ProfileOutcome!.Complete, string.Join("\n", two.ProfileOutcome.Details));
        Assert.Same(sharedSummary, Summaries(Graph(session))[shared]);
        Assert.Equal(count + 1, Graph(session).Count); // Only the other root is new.
        Assert.Null(Graph(session).DequeuePendingOrDefault());
        Assert.Equal(1, sharedCold); // Real P2C replay, not another test-side implementation.
        Assert.NotEqual(one.ProfileOutcome.Root, two.ProfileOutcome.Root);
        Assert.NotEqual(one.ProfileOutcome.InventoryDigest, two.ProfileOutcome.InventoryDigest);
        var caught = reverse ? two : one;
        var escaping = reverse ? one : two;
        Assert.Empty(caught.ThrownExceptions);
        Assert.Equal("NullReferenceException", Assert.Single(escaping.ThrownExceptions).Name);
        foreach (string name in new[] { "A", "B" })
        {
            var cold = fixture.Session();
            Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(cold.Session.Analyze(fixture.Root(name)))),
                JsonSerializer.Serialize(AdmissionCharacterization.Flow(session.Analyze(fixture.Root(name)))));
        }
    }

    [Fact]
    public void ParameterFactsChangeConcreteSourcesWithoutChangingDefinitionOrClosedTypeAdmission()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5E/Facts.cs", """
            static class Entry {
                static void A() { Check("known"); Check(null); }
                static void Check(object? value) { if (value == null) throw new System.ArgumentNullException(); }
            }
            """)]);
        var run = fixture.Session();
        var method = fixture.Symbol("Check");
        var unknown = new ExceptionFlowCallContext(method);
        var nonNull = new ExceptionFlowCallContext(method,
            new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull });
        var first = Local(method, run.Environment, unknown);
        var second = Local(method, run.Environment, nonNull);
        Assert.Contains(first.Fragment.Sources, source => source.ExceptionType.Name == "ArgumentNullException");
        Assert.Empty(second.Fragment.Sources);
        Assert.NotEqual(new ExceptionFlowCallableKey(method, unknown), new ExceptionFlowCallableKey(method, nonNull));
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.True(admission.Outcome.Complete, string.Join("\n", admission.Outcome.Details));
        Assert.True(admission.Seal.ContainsContext(method, unknown));
        Assert.True(admission.Seal.ContainsContext(method, nonNull));
        var result = run.Session.Analyze(fixture.Root("A"));
        Assert.True(result.ProfileOutcome!.Complete);
        var checks = Summaries(Graph(run.Session)).Keys.Where(key => key.Symbol.Name == "Check").ToArray();
        Assert.Equal(2, checks.Length);
        Assert.NotEqual(checks[0].ContextKey, checks[1].ContextKey);
    }

    [Fact]
    public void GroundContextMembershipDoesNotAuthorizeAnUndiscoveredGenericTransition()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5E/Maps.cs",
            "static class Entry { static void A() { Shared<int>(); } static void Shared<T>() {} }")]);
        var run = fixture.Session();
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.True(admission.Outcome.Complete);
        var definition = fixture.Symbol("Shared");
        var allowed = definition.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_Int32));
        var late = definition.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_String));
        Assert.True(admission.Seal.ContainsContext(definition, new(late)));
        var graph = new ExceptionFlowSummaryGraph();
        graph.BeginProduction(admission.Seal, run.Environment);
        try
        {
            var context = new ExceptionFlowCallContext(fixture.Symbol("A"));
            var root = new ExceptionFlowCallableKey(fixture.Symbol("A"), context);
            graph.GetOrAdd(root, context);
            graph.BuildingKey = root;
            graph.BuildingContext = context;
            graph.BuildingCompilation = fixture.Compilation;
            Assert.True(admission.Seal.ContainsTransition(root, definition, allowed));
            Assert.False(admission.Seal.ContainsTransition(root, definition, late));
            graph.RegisterTarget(allowed, new(allowed));
            int count = graph.Count;
            var failure = Assert.Throws<ExceptionFlowProfileViolation>(() => graph.RegisterTarget(late, new(late)));
            Assert.Equal("TargetOrAnalysisCoverageIncomplete", failure.Reason.Category);
            Assert.Equal(count, graph.Count);
        }
        finally { graph.EndProduction(false); }
        Assert.Equal(0, graph.Count);
    }

    [Fact]
    public void RuntimeTargetDiscoveryMustPrecedeSealAndLateTargetCannotEnterProduction()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5E/Runtime.cs", """
            interface IService<T> { void Run(); }
            sealed class IntService : IService<int> { public void Run() {} }
            sealed class StringService : IService<string> { public void Run() {} }
            static class Entry { static void A(IService<int> service) { service.Run(); } }
            """)]);
        var run = fixture.Session();
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.True(admission.Outcome.Complete, string.Join("\n", admission.Outcome.Details));
        var declarations = AdmissionCharacterization.Field<Dictionary<string, ISymbol>>(admission, "declarations");
        Assert.Contains(declarations.Values, symbol => symbol.ContainingType.Name == "IntService" && symbol.Name == "Run");
        Assert.DoesNotContain(declarations.Values, symbol => symbol.ContainingType.Name == "StringService");
        var late = fixture.Compilation.GetTypeByMetadataName("StringService")!.GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.False(admission.Seal.ContainsContext(late, new(late)));
        var graph = new ExceptionFlowSummaryGraph();
        graph.BeginProduction(admission.Seal, run.Environment);
        try
        {
            var context = new ExceptionFlowCallContext(fixture.Symbol("A"));
            var key = new ExceptionFlowCallableKey(fixture.Symbol("A"), context);
            graph.GetOrAdd(key, context);
            graph.BuildingKey = key;
            graph.BuildingContext = context;
            graph.BuildingCompilation = fixture.Compilation;
            Assert.Throws<ExceptionFlowProfileViolation>(() => graph.RegisterTarget(late, new(late)));
            Assert.Equal(1, graph.Count);
        }
        finally { graph.EndProduction(false); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GrowthTopologyIsDiscoveredBeforeSealEvenWhenFinalCatchFlowIsEmpty(bool containing)
    {
        string helper = containing
            ? "static class Grow<T> { internal static void Run() { Grow<System.Collections.Generic.List<T>>.Run(); } }"
            : "static class Grow { internal static void Run<T>() { Run<System.Collections.Generic.List<T>>(); } }";
        string call = containing ? "Grow<int>.Run();" : "Grow.Run<int>();";
        var fixture = new AdmissionCharacterization.Fixture([("/P5E/Growth.cs",
            helper + " static class Entry { static void A() { try { " + call + " } catch {} } }")]);
        var run = fixture.Session();
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.False(admission.Outcome.Complete);
        Assert.Contains(admission.Outcome.Reasons, reason => reason.Category == "PositiveConstructorCycle");
        Assert.Throws<InvalidOperationException>(() => admission.Seal);
        var diagnostics = AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
        Assert.Empty(diagnostics[admission.Outcome.Root].Fragment.CallEdges);
        Assert.Empty(diagnostics[admission.Outcome.Root].Fragment.UncertainTargets);
        Assert.False(run.Session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.Equal(0, Graph(run.Session).Count);
    }

    [Fact]
    public void EmptyFinalTopologyCannotEraseEarlyCoverageRequiredByAdmission()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5E/Coverage.cs",
            "static class Entry { static void A() { try { dynamic value = 1; value.Run(); } catch {} } }")]);
        var run = fixture.Session();
        var local = Local(fixture.Symbol("A"), run.Environment, new(fixture.Symbol("A")));
        Assert.Empty(local.Fragment.Sources);
        Assert.Empty(local.Fragment.CallEdges);
        Assert.Empty(local.Fragment.UncertainTargets);
        Assert.Contains(true, local.Recorder.ObservedUncertainty);
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.False(admission.Outcome.Complete);
        Assert.Contains(admission.Outcome.Reasons, reason => reason.Category == "TargetOrAnalysisCoverageIncomplete");
        Assert.Throws<InvalidOperationException>(() => admission.Seal);
        Assert.Equal(0, Graph(run.Session).Count);
    }

    [Fact]
    public void SupportingGrowthInvalidatesOldSealAndRequiresFreshAdmissionAndSchemaFacts()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared();"), ("/P5E/Shared.cs", "static partial class Entry { static void Shared() {} }")]);
        var run = fixture.Session();
        int shared = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment, symbol => { if (symbol.Name == "Shared") shared++; });
        var before = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts);
        Assert.True(before.Outcome.Complete);
        var support = new AdmissionCharacterization.Fixture([("/P5E/Support.cs", "class Support {}")], assemblyName: "P5E.Support");
        run.Context.RegisterSupportingSource(support.Compilation);
        Assert.False(before.Seal.ScopeUnchanged(run.Environment.GetAnalysisScopes().Select(scope => scope.Compilation)));
        Assert.Throws<ExceptionFlowProfileViolation>(() => new ExceptionFlowSummaryGraph().BeginProduction(before.Seal, run.Environment));
        var after = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts);
        Assert.True(after.Outcome.Complete);
        Assert.NotEqual(before.Outcome.ScopeDigest, after.Outcome.ScopeDigest);
        Assert.Equal(2, shared);
    }

    [Fact]
    public void DocumentationFailurePreventsSealAndPublicationAndExplicitRetryKeepsAcquisitionFresh()
    {
        var provider = new ControlledDocumentation();
        var dependency = new AdmissionCharacterization.Fixture([("/P5E/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")], assemblyName: "P5E.External");
        using var image = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(image).Success);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Known(); External.Api.Run();"),
            ("/P5E/Known.cs", "static partial class Entry { static void Known() {} }")
        ], MetadataReferences.Default.Append(MetadataReference.CreateFromImage(image.ToArray(), documentation: provider)));
        var run = fixture.Session();
        var recorder = new AdmissionObservationRecorder();
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.Throws<DocumentationFailure>(() => ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(
            fixture.Symbol("A"), run.Environment, recorder, fragment, new(fixture.Symbol("A"))));
        Assert.Single(recorder.Keys);
        Assert.Single(fragment.CallEdges);
        Assert.Empty(recorder.ObservedUncertainty);
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment);
        ExceptionFlowFiniteOrbitAdmission? partial = null;
        Assert.Throws<DocumentationFailure>(() => partial = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts));
        Assert.Null(partial);
        Assert.Empty(AdmissionCharacterization.Field<IDictionary>(facts, "completed"));
        Assert.Equal(0, Graph(run.Session).Count);
        int attempts = provider.Requests;
        provider.Throw = false;
        var retry = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts);
        Assert.True(provider.Requests > attempts);
        Assert.False(retry.Outcome.Complete); // Normal metadata-NoBody is still outside.
        Assert.Throws<InvalidOperationException>(() => retry.Seal);
        Assert.NotEmpty(AdmissionCharacterization.Field<IDictionary>(facts, "completed"));
    }

    private static (ExceptionFlowSummaryFragment Fragment, AdmissionObservationRecorder Recorder) Local(
        ISymbol definition, ExceptionFlowSemanticEnvironment environment, ExceptionFlowCallContext context)
    {
        var fragment = new ExceptionFlowSummaryFragment();
        var recorder = new AdmissionObservationRecorder();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, environment, recorder, fragment, context));
        return (fragment, recorder);
    }

    private static ExceptionFlowSummaryGraph Graph(ExceptionFlowSummaryAnalysisSession session)
        => AdmissionCharacterization.Field<ExceptionFlowSummaryGraph>(session, "graph");
    private static Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary> Summaries(ExceptionFlowSummaryGraph graph)
        => AdmissionCharacterization.Field<Dictionary<ExceptionFlowCallableKey, ExceptionFlowSummary>>(graph, "summaries");
    private static (string Path, string Text) Root(string name, string body)
        => ($"/P5E/{name}.cs", "static partial class Entry { /// <summary>Root.</summary>\n static void " + name + "() { " + body + " } }");
    private sealed class DocumentationFailure : System.Exception;
    private sealed class ControlledDocumentation : DocumentationProvider
    {
        internal int Requests { get; private set; }
        internal bool Throw { get; set; } = true;
        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture,
            CancellationToken cancellationToken = default)
        { Requests++; if (Throw) throw new DocumentationFailure(); return string.Empty; }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
