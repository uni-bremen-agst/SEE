using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Real marker publication/hits with separate producer sentinels and fresh root consumers.</summary>
public sealed class ExceptionFlowMetadataNoBodySharingTests
{
    [Theory]
    [InlineData("method")]
    [InlineData("constructor")]
    [InlineData("getter")]
    [InlineData("setter")]
    public void FirstSuccessPublishesAndDifferentRootsSkipOnlyTheRepeatedProducer(string member)
    {
        var fixture = Consumer(Emit(Dependency().Compilation), member);
        var run = fixture.Session();
        int visits = 0, produced = 0;
        bool Selected(ISymbol symbol) => symbol.ContainingType.Name == "Api" && member switch {
            "constructor" => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor },
            "getter" => symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet },
            "setter" => symbol is IMethodSymbol { MethodKind: MethodKind.PropertySet },
            _ => symbol.Name == "Run"
        };
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            symbol => { if (Selected(symbol)) visits++; }, symbol => {
                if (Selected(symbol) && ++produced > 1) throw new ProducerWasRepeated();
            });
        var first = Discover(fixture, run.Environment, facts, "A");
        Assert.Equal(1, produced);
        Assert.Contains(Markers(facts), Selected);
        var second = Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(1, produced);
        Assert.Equal(2, visits); // Existing P2C-cold visit seam remains independent of this producer.
        Assert.NotEqual(first.Outcome.Root, second.Outcome.Root);
        Assert.NotEqual(first.Outcome.InventoryDigest, second.Outcome.InventoryDigest);
        Assert.Equal(first.Outcome.ScopeDigest, second.Outcome.ScopeDigest);
        var definition = Markers(facts).Single(Selected);
        string id = ExceptionFlowFiniteOrbitAdmission.BodyId(definition);
        Assert.False(Diagnostics(first)[id].HasBody);
        Assert.False(Diagnostics(second)[id].HasBody);
        Assert.NotSame(Diagnostics(first)[id].Fragment, Diagnostics(second)[id].Fragment);
        Assert.Equal(AdmissionCharacterization.Fragment(Diagnostics(first)[id].Fragment), AdmissionCharacterization.Fragment(Diagnostics(second)[id].Fragment));
        Assert.Contains(first.Outcome.Reasons, reason => reason.Witness == id + " / executable body unavailable");
        Assert.Contains(second.Outcome.Reasons, reason => reason.Witness == id + " / executable body unavailable");
        for (int index = 0; index < 6; index++) Discover(fixture, run.Environment, facts, index % 2 == 0 ? "A" : "B");
        Assert.Equal(1, produced);
        Assert.Single(Markers(facts).Where(Selected));
        Assert.All(Markers(facts), symbol => {
            Assert.True(SymbolEqualityComparer.Default.Equals(symbol, symbol.OriginalDefinition));
            Assert.All(symbol.Locations, location => Assert.True(location.IsInMetadata));
        });
    }

    [Fact]
    public void ActualP2CReplayAndMarkerHitKeepRegistrationsAndObservationsFresh()
    {
        var fixture = Consumer(Emit(Dependency().Compilation), "method", shared: true);
        var run = fixture.Session();
        var cold = new Dictionary<string, int>();
        int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            symbol => cold[symbol.Name] = cold.GetValueOrDefault(symbol.Name) + 1,
            symbol => { if (symbol.Name == "Run" && ++produced > 1) throw new ProducerWasRepeated(); });
        var first = Discover(fixture, run.Environment, facts, "A");
        var second = Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(1, produced);
        Assert.Equal(1, cold["Shared"]); Assert.Equal(1, cold["Leaf"]);
        Assert.Equal(2, cold["Run"]);
        string id = ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("Shared"));
        var a = Diagnostics(first)[id].Fragment; var b = Diagnostics(second)[id].Fragment;
        Assert.NotSame(a, b);
        Assert.Equal(AdmissionCharacterization.Fragment(a), AdmissionCharacterization.Fragment(b));
        Assert.Equal(2, a.CallEdges.Count); Assert.Equal(2, b.CallEdges.Count);
        for (int index = 0; index < 2; index++) Assert.NotSame(a.CallEdges[index].Target, b.CallEdges[index].Target);
        var firstEdges = Edges(first).Where(edge => edge.From == id).ToArray();
        var secondEdges = Edges(second).Where(edge => edge.From == id).ToArray();
        Assert.Equal(firstEdges.Select(edge => edge.Witness), secondEdges.Select(edge => edge.Witness));
        Assert.Equal(2, secondEdges.Length);
        Assert.Same(firstEdges[0].Assignments, firstEdges[1].Assignments); // Empty-array partition unchanged.
        Assert.NotSame(Edges(first).First(edge => edge.To.Contains("Run", StringComparison.Ordinal)).Assignments,
            Edges(second).First(edge => edge.To.Contains("Run", StringComparison.Ordinal)).Assignments);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NewViewRetiresMarkerEvenWhenTheExactSchemaAndCompilationStayFixed(bool supportingGrowth)
    {
        var fixture = Consumer(Emit(Dependency().Compilation)); var run = fixture.Session();
        int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") produced++; });
        Discover(fixture, run.Environment, facts, "A");
        var oldView = run.Environment.GetAnalysisScopes();
        var original = Markers(facts).Single();
        if (supportingGrowth)
            run.Context.RegisterSupportingSource(Dependency("P5C.Added").Compilation);
        else
            typeof(ProjectClosureSemanticContext).GetField("analysisCompilationScopes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(run.Context, run.Context.GetAnalysisCompilationScopes().ToArray());
        Assert.NotSame(oldView, run.Environment.GetAnalysisScopes());
        facts.Begin(run.Environment);
        Assert.Empty(Markers(facts));
        Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(2, produced);
        Assert.True(SymbolEqualityComparer.Default.Equals(original, Assert.Single(Markers(facts))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnotherSessionOrCompilationCannotInheritMarkers(bool clone)
    {
        var fixture = Consumer(Emit(Dependency().Compilation)); var first = fixture.Session();
        first.Session.Analyze(fixture.Root("A"));
        var firstFacts = Facts(first.Session);
        Assert.Single(Markers(firstFacts));
        var compilation = clone ? fixture.Compilation.Clone() : fixture.Compilation;
        var environment = clone ? new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(
            fixture.Root("A").SyntaxTree, compilation)) : first.Environment;
        int produced = 0;
        var fresh = new ExceptionFlowFiniteOrbitAdmission.FactStore(environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") produced++; });
        Discover(fixture, environment, fresh, "B");
        Assert.Equal(1, produced);
        Assert.Single(Markers(fresh)); Assert.NotSame(MarkerSet(firstFacts), MarkerSet(fresh));
        if (clone) Assert.Throws<InvalidOperationException>(() => firstFacts.Begin(environment));
    }

    [Fact]
    public void SameNominalMetadataIdUnderDifferentExactOwnersCannotHitTheOtherMarker()
    {
        var a = Consumer(Emit(Dependency().Compilation), assembly: "P5C.Consumer.A");
        var b = Consumer(Emit(Dependency(extra: "public static void Extra() {}").Compilation), assembly: "P5C.Consumer.B");
        var run = a.Session(); run.Context.RegisterSupportingSource(b.Compilation);
        var owners = new List<IAssemblySymbol>();
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") owners.Add(symbol.ContainingAssembly); });
        Discover(a, run.Environment, facts, "A");
        Discover(b, run.Environment, facts, "B");
        Assert.Equal(2, owners.Count); Assert.NotSame(owners[0], owners[1]);
        var markers = Markers(facts).Where(symbol => symbol.Name == "Run").ToArray();
        Assert.Equal(2, markers.Length);
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.BodyId(markers[0]), ExceptionFlowFiniteOrbitAdmission.BodyId(markers[1]));
        Discover(a, run.Environment, facts, "B"); Discover(b, run.Environment, facts, "A");
        Assert.Equal(2, owners.Count);
    }

    [Fact]
    public void SamePeSymbolInTwoCompilersHasTwoIndependentMarkerKeys()
    {
        var reference = Emit(Dependency().Compilation);
        var a = Consumer(reference, assembly: "P5C.Compiler.A");
        var b = Consumer(reference, assembly: "P5C.Compiler.B");
        var run = a.Session(); run.Context.RegisterSupportingSource(b.Compilation);
        int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") produced++; });
        Discover(a, run.Environment, facts, "A"); Discover(b, run.Environment, facts, "B");
        Assert.Equal(2, produced);
        Assert.Equal(2, MarkerSet(facts).Count);
        Assert.Contains(MarkerSet(facts), key => ReferenceEquals(key.Compilation, a.Compilation));
        Assert.Contains(MarkerSet(facts), key => ReferenceEquals(key.Compilation, b.Compilation));
        Discover(b, run.Environment, facts, "A"); Discover(a, run.Environment, facts, "B");
        Assert.Equal(2, produced);
    }

    [Fact]
    public void QualificationRejectsSourceConstructedAndForeignMetadataSymbols()
    {
        var dependency = Dependency();
        var fixture = Consumer(Emit(dependency.Compilation)); var run = fixture.Session();
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment);
        var world = facts.Begin(run.Environment);
        var definition = fixture.Compilation.GetTypeByMetadataName("External.Api`1")!.GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.True(facts.QualifiesMetadataNoBody(run.Environment, world, definition, fixture.Compilation));
        Assert.False(facts.QualifiesMetadataNoBody(run.Environment, world, definition.Construct(
            fixture.Compilation.GetSpecialType(SpecialType.System_Int32)), fixture.Compilation));
        Assert.False(facts.QualifiesMetadataNoBody(run.Environment, world, dependency.Symbol("Run"), fixture.Compilation));
        var other = Consumer(Emit(Dependency("P5C.Foreign").Compilation));
        var foreign = other.Compilation.GetTypeByMetadataName("External.Api`1")!.GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.False(facts.QualifiesMetadataNoBody(run.Environment, world, foreign, fixture.Compilation));
        Assert.False(facts.QualifiesMetadataNoBody(run.Environment, world, definition, null));
        Assert.Empty(Markers(facts)); // Qualification alone is never publication.
    }

    [Fact]
    public void UnprovenUniqueCompilerAssociationBypassesTheMarker()
    {
        var fixture = Consumer(Emit(Dependency().Compilation)); var run = fixture.Session();
        var scope = run.Context.GetAnalysisCompilationScopes().Single();
        typeof(ProjectClosureSemanticContext).GetField("analysisCompilationScopes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(run.Context, new[] { scope, scope });
        int visits = 0, produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            symbol => { if (symbol.Name == "Run") visits++; }, _ => produced++);
        Discover(fixture, run.Environment, facts, "A"); Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(2, visits); Assert.Equal(0, produced); Assert.Empty(Markers(facts));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceAndCompilationReferenceFallbackCannotConsumeAMetadataMarker(bool compilationReference)
    {
        var dependency = Dependency();
        MetadataReference reference = compilationReference ? dependency.Compilation.ToMetadataReference() : Emit(dependency.Compilation);
        var fixture = Consumer(reference); var run = fixture.Session();
        int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") produced++; });
        var before = Discover(fixture, run.Environment, facts, "A");
        Assert.False(before.Outcome.Complete);
        Assert.Equal(compilationReference ? 0 : 1, produced);
        Assert.Equal(compilationReference ? 0 : 1, Markers(facts).Count);
        run.Context.RegisterSupportingSource(dependency.Compilation);
        var after = Discover(fixture, run.Environment, facts, "B");
        Assert.True(after.Outcome.Complete, string.Join("\n", after.Outcome.Details));
        Assert.Empty(Markers(facts));
        var source = Diagnostics(after).Single(pair => pair.Key == ExceptionFlowFiniteOrbitAdmission.BodyId(
            dependency.Compilation.GetTypeByMetadataName("External.Api`1")!.GetMembers("Run").Single()));
        Assert.True(source.Value.HasBody);
    }

    [Fact]
    public void ViewChangeAfterLocalSuccessPreventsPublicationIntoTheNewEpoch()
    {
        var fixture = Consumer(Emit(Dependency().Compilation)); var run = fixture.Session();
        int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => {
                if (symbol.Name == "Run" && ++produced == 1)
                    run.Context.RegisterSupportingSource(Dependency("P5C.Growth").Compilation);
            });
        Discover(fixture, run.Environment, facts, "A");
        Assert.Equal(1, produced); Assert.Empty(Markers(facts));
        Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(2, produced); Assert.Single(Markers(facts));
    }

    [Fact]
    public void ProducerExceptionPublishesNothingAndExplicitRetryBuildsBeforeReuse()
    {
        var fixture = Consumer(Emit(Dependency().Compilation)); var run = fixture.Session();
        int produced = 0; bool fail = true;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name != "Run") return; produced++; if (fail) throw new ProducerWasRepeated(); });
        Assert.Throws<ProducerWasRepeated>(() => Discover(fixture, run.Environment, facts, "A"));
        Assert.Empty(Markers(facts));
        Assert.Empty(AdmissionCharacterization.Field<IDictionary>(facts, "completed"));
        fail = false; Discover(fixture, run.Environment, facts, "A");
        Assert.Single(Markers(facts)); Assert.Equal(2, produced);
        fail = true; Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(2, produced);
    }

    [Fact]
    public void LaterRealPeReadFailureDiscardsStagedMarkerAndRetryDoesNotUseAPartialSuccess()
    {
        using var metadata = Image(Dependency().Compilation);
        var reference = new ProbeReference(metadata);
        var fixture = Consumer(reference); var run = fixture.Session();
        bool failAfterProducer = true; int produced = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment,
            metadataNoBodyProduced: symbol => { if (symbol.Name == "Run") { produced++; reference.Fail = failAfterProducer; } });
        Assert.Throws<PeReadFailure>(() => Discover(fixture, run.Environment, facts, "A"));
        Assert.Equal(1, produced); Assert.Empty(Markers(facts));
        reference.Fail = false; failAfterProducer = false;
        Discover(fixture, run.Environment, facts, "A");
        Assert.Equal(2, produced); Assert.Single(Markers(facts));
        Discover(fixture, run.Environment, facts, "B");
        Assert.Equal(2, produced);
        reference.Fail = true;
        Assert.Throws<PeReadFailure>(() => Discover(fixture, run.Environment, facts, "B"));
        Assert.Single(Markers(facts)); // An older completed marker survives, but cannot hide fresh PE consumption.
    }

    [Fact]
    public void StoreHasOnlySuccessfulSchemaKeysAndIsReleasedWithItsSession()
    {
        var fixture = Consumer(Emit(Dependency().Compilation));
        var reference = ReleasedMarkers(fixture);
        for (int index = 0; index < 3 && reference.IsAlive; index++) {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        }
        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReleasedMarkers(AdmissionCharacterization.Fixture fixture)
    {
        var run = fixture.Session();
        for (int index = 0; index < 6; index++) run.Session.Analyze(fixture.Root(index % 2 == 0 ? "A" : "B"));
        var markers = MarkerSet(Facts(run.Session));
        Assert.Single(markers);
        Assert.IsAssignableFrom<IMethodSymbol>(Assert.Single(markers).Definition);
        return new WeakReference(markers);
    }

    private static ExceptionFlowFiniteOrbitAdmission Discover(AdmissionCharacterization.Fixture fixture,
        ExceptionFlowSemanticEnvironment environment, ExceptionFlowFiniteOrbitAdmission.FactStore facts, string root)
        => ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(root), environment, facts);
    private static ExceptionFlowFiniteOrbitAdmission.FactStore Facts(ExceptionFlowSummaryAnalysisSession session)
        => AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(session, "admissionFacts");
    private static HashSet<(Compilation Compilation, ISymbol Definition)> MarkerSet(ExceptionFlowFiniteOrbitAdmission.FactStore facts)
        => AdmissionCharacterization.Field<HashSet<(Compilation Compilation, ISymbol Definition)>>(facts, "metadataNoBody");
    private static IReadOnlyList<ISymbol> Markers(ExceptionFlowFiniteOrbitAdmission.FactStore facts)
        => MarkerSet(facts).Select(key => key.Definition).ToArray();
    private static Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> Diagnostics(ExceptionFlowFiniteOrbitAdmission admission)
        => AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
    private static List<Edge> Edges(ExceptionFlowFiniteOrbitAdmission admission)
        => AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
    private static AdmissionCharacterization.Fixture Consumer(MetadataReference reference, string member = "method", bool shared = false,
        string assembly = "P5C.Consumer")
    {
        string Body(string type) => member switch {
            "constructor" => $"_ = new External.Api<{type}>();",
            "getter" => $"_ = new External.Api<{type}>().Value;",
            "setter" => $"new External.Api<{type}>().Value = 1;",
            _ => $"{(shared ? "Shared(); " : "")}External.Api<{type}>.Run<{type}>();"
        };
        return new([
            Root("A", Body("int")), Root("B", Body("string")),
            ("/P5C/Shared.cs", "public static partial class Entry { static void Shared() { Leaf(); Leaf(); } static void Leaf() {} }")
        ], MetadataReferences.Default.Append(reference), assemblyName: assembly);
    }
    private static (string Path, string Text) Root(string name, string body) => ($"/P5C/{name}.cs", $$"""
        public static partial class Entry {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);
    private static AdmissionCharacterization.Fixture Dependency(string name = "P5C.Metadata", string extra = "") => new([
        ("/P5C/Api.cs", $$"""
        namespace External { public class Api<T> {
            public Api() {}
            public int Value { get; set; }
            public static void Run<U>() {}
            {{extra}}
        } }
        """)
    ], assemblyName: name);
    private static PortableExecutableReference Emit(Compilation compilation)
    {
        using var stream = new MemoryStream(); Assert.True(compilation.Emit(stream).Success);
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
    private static AssemblyMetadata Image(Compilation compilation)
    {
        using var stream = new MemoryStream(); Assert.True(compilation.Emit(stream).Success);
        return AssemblyMetadata.CreateFromImage(stream.ToArray());
    }
    private sealed class ProducerWasRepeated : System.Exception;
    private sealed class PeReadFailure : System.Exception;
    private sealed class ProbeReference(AssemblyMetadata metadata) : PortableExecutableReference(MetadataReferenceProperties.Assembly)
    {
        internal bool Fail { get; set; }
        protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;
        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties) => throw new NotSupportedException();
        protected override Metadata GetMetadataImpl() { if (Fail) throw new PeReadFailure(); return metadata; }
    }
}
