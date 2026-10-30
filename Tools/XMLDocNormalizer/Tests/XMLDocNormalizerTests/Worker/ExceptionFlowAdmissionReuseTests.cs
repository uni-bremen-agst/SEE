using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Exercises real session-owned replay with cold-inspection sentinels, independently of P2B expectations.</summary>
public sealed class ExceptionFlowAdmissionReuseTests
{
    [Theory]
    [InlineData("AB")]
    [InlineData("BA")]
    [InlineData("ABAABB")]
    public void OverlappingClosureIsInspectedOnceWithIdenticalFullRootEvidence(string order)
    {
        var fixture = Fixture();
        var cold = new Dictionary<string, string>();
        foreach (string name in new[] { "A", "B" })
        {
            var baseline = fixture.Session();
            cold[name] = fixture.Capture(name, baseline.Context, baseline.Session);
        }
        var run = fixture.Session();
        Dictionary<string, int> inspected = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        {
            inspected[symbol.Name] = inspected.GetValueOrDefault(symbol.Name) + 1;
            if (symbol.Name is "Shared" or "Leaf" && inspected[symbol.Name] > 1)
                throw new InvalidOperationException("Shared closure was acquired twice.");
        });
        foreach (char name in order)
            Assert.Equal(cold[name.ToString()], fixture.Capture(name.ToString(), run.Context, session));
        Assert.Equal(1, inspected["Shared"]);
        Assert.Equal(1, inspected["Leaf"]);
    }

    [Theory]
    [InlineData("typed")]
    [InlineData("all")]
    [InlineData("filtered")]
    [InlineData("rethrow")]
    public void ReplayedObservationsPreserveGenericEdgeMultiplicityAndPreCatchEvidence(string catchKind)
    {
        string handler = catchKind switch
        {
            "typed" => "catch (System.Exception) {}",
            "filtered" => "catch (System.Exception) when (false) {}",
            "rethrow" => "catch { throw; }",
            _ => "catch {}"
        };
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared<int>();"), Root("B", "Shared<string>();"),
            ("/P2C/Shared.cs", $$"""
            public static partial class Entry
            {
                static void Shared<T>() { try { Leaf<T>(); Leaf<T>(); } {{handler}} }
                static void Leaf<T>() { throw new Fault<T>(); }
            }
            class Fault<T> : System.Exception {}
            """)
        ]);
        var run = fixture.Session();
        int shared = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        {
            if (symbol.Name == "Shared" && ++shared > 1) throw new InvalidOperationException("Schema was reacquired.");
        });
        session.Analyze(fixture.Root("A"));
        var baseline = fixture.Session();
        Assert.Equal(fixture.Capture("B", baseline.Context, baseline.Session), fixture.Capture("B", run.Context, session));
        Assert.Equal(1, shared);
    }

    [Fact]
    public void ConcreteProductionContextsRemainSeparateWhileOriginalSchemaIsReused()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Box<int>.Run<string>();"), Root("B", "Box<string>.Run<int>();"),
            ("/P2C/Generic.cs", "public class Fault<T,U> : System.Exception {} public static class Box<T> { public static void Run<U>() { throw new Fault<T,U>(); } }")
        ]);
        var run = fixture.Session();
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment);
        var first = session.Analyze(fixture.Root("A"));
        var second = session.Analyze(fixture.Root("B"));
        Assert.True(first.ProfileOutcome!.Complete);
        Assert.True(second.ProfileOutcome!.Complete);
        string firstType = AdmissionCharacterization.TypeKey(Assert.Single(first.ThrownExceptions));
        string secondType = AdmissionCharacterization.TypeKey(Assert.Single(second.ThrownExceptions));
        Assert.NotEqual(firstType, secondType);
        var baseline = fixture.Session();
        Assert.Equal(Flow(baseline.Session.Analyze(fixture.Root("B"))), Flow(second));
    }

    [Fact]
    public void NewSessionAndEqualDigestCloneCannotInheritFacts()
    {
        var fixture = Fixture();
        var run = fixture.Session();
        var first = new ExceptionFlowSummaryAnalysisSession(run.Environment);
        first.Analyze(fixture.Root("A"));
        int sameEnvironmentInspections = 0;
        var second = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Shared") sameEnvironmentInspections++; });
        second.Analyze(fixture.Root("B"));
        Assert.Equal(1, sameEnvironmentInspections);
        Assert.NotSame(Store(first), Store(second));

        var clone = fixture.Compilation.Clone();
        var environment = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(
            fixture.Root("A").SyntaxTree, clone));
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment), ExceptionFlowFiniteOrbitAdmission.ScopeDigest(environment));
        int cloneInspections = 0;
        var third = new ExceptionFlowSummaryAnalysisSession(environment, symbol =>
        {
            if (symbol.Name != "Shared") return;
            cloneInspections++;
            Assert.Same(clone.Assembly, symbol.ContainingAssembly);
            Assert.NotSame(fixture.Compilation.Assembly, symbol.ContainingAssembly);
        });
        third.Analyze(fixture.Root("B"));
        Assert.Equal(1, cloneInspections);
    }

    [Fact]
    public void EqualCanonicalNamesCannotMoveFactsToAnotherOwnerEnvironment()
    {
        AdmissionCharacterization.Fixture Owner(string path) => new([
            (path, "class Entry { public static void A() { Shared(); } static void Shared() {} }")
        ], assemblyName: "P2C.SameOwnerName");
        var first = Owner("/P2C/First.cs");
        var second = Owner("/P2C/Second.cs");
        var run = first.Session();
        List<IAssemblySymbol> owners = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Shared") owners.Add(symbol.ContainingAssembly); });
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.BodyId(first.Symbol("Shared")),
            ExceptionFlowFiniteOrbitAdmission.BodyId(second.Symbol("Shared")));
        Assert.True(session.Analyze(first.Root("A")).ProfileOutcome!.Complete);
        // The host rejects a second source owner with the same assembly identity.
        Assert.Throws<InvalidOperationException>(() => run.Context.RegisterSupportingSource(second.Compilation));
        var other = second.Session();
        var facts = AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(session, "admissionFacts");
        Assert.Throws<InvalidOperationException>(() => ExceptionFlowFiniteOrbitAdmission.Discover(
            second.Root("A"), other.Environment, facts));
        var otherSession = new ExceptionFlowSummaryAnalysisSession(other.Environment, symbol =>
        { if (symbol.Name == "Shared") owners.Add(symbol.ContainingAssembly); });
        Assert.True(otherSession.Analyze(second.Root("A")).ProfileOutcome!.Complete);
        Assert.Equal(2, owners.Count);
        Assert.Same(first.Compilation.Assembly, owners[0]);
        Assert.Same(second.Compilation.Assembly, owners[1]);
        session.Analyze(first.Root("A"));
        otherSession.Analyze(second.Root("A"));
        Assert.Equal(2, owners.Count);
    }

    [Fact]
    public void ScopeGrowthRetiresCompletedFactsBeforeNextRoot()
    {
        var fixture = Fixture();
        var run = fixture.Session();
        int count = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Shared") count++; });
        session.Analyze(fixture.Root("A"));
        var before = run.Environment.GetAnalysisScopes();
        var support = new AdmissionCharacterization.Fixture([("/P2C/Other.cs", "class Other {}")], assemblyName: "P2C.Other");
        run.Context.RegisterSupportingSource(support.Compilation);
        Assert.NotSame(before, run.Environment.GetAnalysisScopes());
        session.Analyze(fixture.Root("B"));
        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThrowingRootPublishesNoPartialFactsAndPreservesEarlierSuccess(bool successFirst)
    {
        var provider = new FailingProvider();
        var reference = ExternalReference(provider);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared(); Failing();"), Root("B", "Shared();"),
            ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() {} static void Failing() { External.Api.Run(); } }")
        ], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        int shared = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Shared") shared++; });
        string? before = successFirst ? fixture.Capture("B", run.Context, session) : null;
        Assert.Throws<DocumentationFailure>(() => session.Analyze(fixture.Root("A")));
        Assert.True(provider.Requests > 0);
        if (!successFirst) Assert.Empty(Entries(session));
        var baseline = fixture.Session();
        string after = fixture.Capture("B", run.Context, session);
        Assert.Equal(before ?? fixture.Capture("B", baseline.Context, baseline.Session), after);
        Assert.Equal(successFirst ? 1 : 2, shared);
    }

    [Fact]
    public void OutsideRootCanPublishOnlyCompletedLocalFactsWithoutPoisoningLaterRoot()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared(); Bodyless();"), Root("B", "Shared();"),
            ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() {} static extern void Bodyless(); }")
        ]);
        var run = fixture.Session();
        int shared = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Shared") shared++; });
        Assert.False(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.NotEmpty(Entries(session));
        var baseline = fixture.Session();
        Assert.Equal(fixture.Capture("B", baseline.Context, baseline.Session), fixture.Capture("B", run.Context, session));
        Assert.Equal(1, shared);
    }

    [Fact]
    public void DemandRemainsFreshWhilePureSiblingIsReusedIncludingLatePlan()
    {
        using var workspace = new ExternalReconstructionPlanTestWorkspace();
        var dependency = workspace.Prepare("P2C.Acquired", [new SourceInput("/_/External.cs",
            "namespace External { public static class Api { public static void Run() {} } }")]);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared(); External.Api.Run();"),
            ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() {} }")
        ], MetadataReferences.Default.Append(dependency.Reference));
        var run = fixture.Session();
        Dictionary<string, int> inspected = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { inspected[symbol.Name] = inspected.GetValueOrDefault(symbol.Name) + 1; });
        Assert.False(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.False(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.Equal(2, inspected["A"]);
        Assert.Equal(1, inspected["Shared"]);
        var assembly = (IAssemblySymbol)fixture.Compilation.GetAssemblyOrModuleSymbol(dependency.Reference)!;
        Assert.True(run.Context.TryGetExternalAssemblyReferenceDescriptor(fixture.Compilation, assembly, out var descriptor));
        Assert.True(run.Context.TryRegisterExternalSupportingSourceReconstructionPlan(dependency.CreatePlan(descriptor)));
        Assert.True(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.Equal(3, inspected["A"]);
    }

    [Fact]
    public void ExternalDocumentationReadsRemainFreshWhilePureSiblingIsReused()
    {
        using var workspace = new ExternalReconstructionPlanTestWorkspace();
        var dependency = workspace.Prepare("P2C.Documented", [new SourceInput("/_/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")]);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared(); try { External.Api.Run(); } catch {}"),
            ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() {} }")
        ], MetadataReferences.Default.Append(dependency.Reference));
        var run = fixture.Session();
        Dictionary<string, int> inspected = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { inspected[symbol.Name] = inspected.GetValueOrDefault(symbol.Name) + 1; });
        var absent = session.Analyze(fixture.Root("A"));
        string sidecar = Path.ChangeExtension(dependency.Reference.FilePath!, ".xml");
        File.WriteAllText(sidecar, "<doc><members><member name=\"M:External.Api.Run\"><exception cref=\"T:System.InvalidOperationException\">Failure.</exception></member></members></doc>");
        var present = session.Analyze(fixture.Root("A"));
        Assert.NotEqual(absent.ProfileOutcome!.InventoryDigest, present.ProfileOutcome!.InventoryDigest);
        Assert.Equal(absent.ProfileOutcome.ScopeDigest, present.ProfileOutcome.ScopeDigest);
        File.Delete(sidecar);
        Assert.Equal(Flow(absent), Flow(session.Analyze(fixture.Root("A"))));
        Assert.Equal(3, inspected["A"]);
        Assert.Equal(1, inspected["Shared"]);
    }

    [Fact]
    public void RepeatedRootsAndPositiveGrowthKeepOneFactPerOriginalDefinition()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared(); Grow<int>();"), Root("B", "Shared(); Grow<string>();"),
            ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() {} static void Grow<T>() { Grow<System.Collections.Generic.List<T>>(); } }")
        ]);
        var run = fixture.Session();
        int growth = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        { if (symbol.Name == "Grow") growth++; });
        Assert.False(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        for (int index = 0; index < 16; index++)
            Assert.False(session.Analyze(fixture.Root(index % 2 == 0 ? "B" : "A")).ProfileOutcome!.Complete);
        Assert.Equal(1, growth);
        Assert.Equal(4, Entries(session).Count);
        foreach (ISymbol definition in Entries(session).Keys)
            Assert.True(SymbolEqualityComparer.Default.Equals(definition, definition.OriginalDefinition));
    }

    [Fact]
    public void StoreCannotRetainSessionAfterItsOwnerIsReleased()
    {
        var fixture = Fixture();
        WeakReference reference = ReleasedStore(fixture);
        for (int index = 0; index < 3 && reference.IsAlive; index++)
        { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReleasedStore(AdmissionCharacterization.Fixture fixture)
    {
        var run = fixture.Session();
        run.Session.Analyze(fixture.Root("A"));
        return new WeakReference(Store(run.Session));
    }

    private static object Store(ExceptionFlowSummaryAnalysisSession session)
        => AdmissionCharacterization.Field<object>(session, "admissionFacts");
    private static IDictionary Entries(ExceptionFlowSummaryAnalysisSession session)
        => AdmissionCharacterization.Field<IDictionary>(Store(session), "completed");
    private static string Flow(ExceptionFlowAnalysisResult result) => JsonSerializer.Serialize(AdmissionCharacterization.Flow(result));
    private static AdmissionCharacterization.Fixture Fixture() => new([
        Root("A", "Shared(); throw new System.InvalidOperationException();"),
        Root("B", "Shared(); throw new System.ArgumentException();"),
        ("/P2C/Shared.cs", "public static partial class Entry { static void Shared() { Leaf(); } static void Leaf() {} }")
    ]);
    private static (string Path, string Text) Root(string name, string body) => ($"/P2C/{name}.cs", $$"""
        public static partial class Entry
        {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);

    private static PortableExecutableReference ExternalReference(DocumentationProvider provider)
    {
        var dependency = new AdmissionCharacterization.Fixture([("/P2C/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")], assemblyName: "P2C.External");
        using var stream = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(stream).Success);
        return MetadataReference.CreateFromImage(stream.ToArray(), documentation: provider);
    }
    private sealed class DocumentationFailure : System.Exception;
    private sealed class FailingProvider : DocumentationProvider
    {
        internal int Requests { get; private set; }
        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture,
            CancellationToken cancellationToken = default)
        { Requests++; throw new DocumentationFailure(); }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
