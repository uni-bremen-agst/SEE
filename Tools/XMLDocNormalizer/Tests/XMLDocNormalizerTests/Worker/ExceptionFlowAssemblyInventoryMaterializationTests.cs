using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Observes complete inventory projections separately from live consumers and conflict checks.</summary>
public sealed class ExceptionFlowAssemblyInventoryMaterializationTests
{
    private const string Source = "class Entry { static void A() { Shared(); } static void B() { Shared(); } "
        + "static void Shared() { Leaf(); } static void Leaf() { throw null; } }";

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void MultipleConsumersAndRepeatedTargetsBuildTheFixedInventoryOnce(int count)
    {
        var fixture = Fixture(); var run = fixture.Session(); var probe = Probe(run.Context);
        var first = New(run.Environment); var value = Value(run.Environment);
        Assert.Equal(1, probe.InventoryReads); Equal(Expected(fixture.Compilation), value);
        Assert.Same(run.Environment.GetAnalysisScopes(), Field(run.Environment, "assemblyInventoryView"));
        probe.ForbidInventory = true;
        Frame(first, fixture.Symbol("A"));
        var second = New(run.Environment); Frame(second, fixture.Symbol("B"));
        var firstConsumer = Known(first); var secondConsumer = Known(second);
        firstConsumer.Add("root-only sentinel");
        for (int i = 0; i < count; i++)
        {
            first.RegisterTarget(fixture.Symbol("Leaf"), new(fixture.Symbol("Leaf")));
            second.RegisterTarget(fixture.Symbol("Leaf"), new(fixture.Symbol("Leaf")));
        }
        Assert.Same(value, Value(run.Environment)); Assert.Equal(1, probe.InventoryReads);
        Assert.NotSame(firstConsumer, secondConsumer); Assert.DoesNotContain("root-only sentinel", secondConsumer);
        Assert.DoesNotContain("root-only sentinel", value); Equal(Expected(fixture.Compilation), secondConsumer);
        Assert.Equal(2, first.DefinitionCount); Assert.Equal(2, second.DefinitionCount);
        Assert.Equal(count, AdmissionCharacterization.Field<IList>(first, "edges").Count);
        Assert.Equal(count, AdmissionCharacterization.Field<IList>(second, "edges").Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealColdAndP2CReplayRootsReuseInventoryButKeepConflictChecksFresh(bool reverse)
    {
        using var input = new PeInput(); var fixture = input.Fixture; var run = fixture.Session();
        var probe = Probe(run.Context); New(run.Environment); probe.ForbidInventory = true;
        var value = Value(run.Environment); input.Reference.Reads = 0;
        Dictionary<string, int> cold = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        {
            cold[symbol.Name] = cold.GetValueOrDefault(symbol.Name) + 1;
            if ((symbol.Name is "Shared" or "Leaf") && cold[symbol.Name] > 1)
                throw new InvalidOperationException("Expected a genuine completed P2C fact hit.");
        });
        string[] roots = reverse ? ["B", "A", "B"] : ["A", "B", "A"];
        foreach (string root in roots)
        {
            var result = session.Analyze(fixture.Root(root));
            Assert.True(result.ProfileOutcome!.Complete);
            var independent = fixture.Session().Session.Analyze(fixture.Root(root));
            Assert.Equal(Flow(independent), Flow(result));
            // The independent controls use their own environment and the same held PE input.
            Assert.Same(value, Value(run.Environment)); Assert.Equal(1, probe.InventoryReads);
        }
        Assert.Equal(1, cold["Shared"]); Assert.Equal(1, cold["Leaf"]);
        Assert.True(probe.OtherReads > 0);
        // A second pass has no digest miss; each root still performs the separate PE conflict read.
        int reads = input.Reference.Reads;
        foreach (string root in roots) session.Analyze(fixture.Root(root));
        Assert.Equal(reads + roots.Length, input.Reference.Reads);
        Assert.Equal(1, probe.InventoryReads);
    }

    [Fact]
    public void ActualPublishedFactReplaysTargetsIntoAFreshConsumerWithoutRebuilding()
    {
        var fixture = Fixture(); var run = fixture.Session(); var probe = Probe(run.Context);
        run.Session.Analyze(fixture.Root("A")); run.Session.Analyze(fixture.Root("B"));
        var facts = AdmissionCharacterization.Field<Dictionary<ISymbol, ExceptionFlowFiniteOrbitAdmission.LocalFact>>(
            AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(run.Session, "admissionFacts"), "completed");
        var fact = facts[fixture.Symbol("Shared")]; var value = Value(run.Environment);
        var admission = New(run.Environment); Frame(admission, fixture.Symbol("Shared"));
        Known(admission).Clear(); Known(admission).Add("root-only sentinel");
        probe.ForbidInventory = true; fact.Replay(admission);
        Assert.Equal(1, probe.InventoryReads); Assert.Same(value, Value(run.Environment));
        Assert.Equal(2, admission.DefinitionCount); Assert.Single(AdmissionCharacterization.Field<IList>(admission, "edges"));
        var expected = Expected(fixture.Compilation); expected.Add("root-only sentinel"); Equal(expected, Known(admission));
        Assert.DoesNotContain("root-only sentinel", value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealSupportingGrowthRebuildsEvenWhenNominalKeysAreEqual(bool equalKeys)
    {
        var support = Compilation("P4D.Support", "public class Support {}", "/P4D/Support.cs");
        var fixture = Fixture(equalKeys ? [Emit(support)] : []); var run = fixture.Session();
        var before = Probe(run.Context); var first = New(run.Environment); var a = Value(run.Environment);
        Assert.Equal(1, before.InventoryReads);
        run.Context.RegisterSupportingSource(support);
        var after = Probe(run.Context); Assert.NotSame(before, after);
        var second = New(run.Environment); var b = Value(run.Environment);
        Assert.Equal(1, after.InventoryReads); Assert.NotSame(a, b);
        Assert.Same(after, Field(run.Environment, "assemblyInventoryView"));
        Equal(Expected(fixture.Compilation, support), b);
        Assert.Equal(equalKeys, a.ToHashSet(StringComparer.Ordinal).SetEquals(b));
        Equal(a, Known(first)); Equal(b, Known(second));
        after.ForbidInventory = true;
        Add(first, fixture.Symbol("A")); Add(second, fixture.Symbol("B")); New(run.Environment);
        Assert.Equal(1, after.InventoryReads); Assert.Same(b, Value(run.Environment));
        Equal(b, Known(first)); Assert.NotSame(Known(first), Known(second));
    }

    [Fact]
    public void NewSnapshotWithTheSameExactCompilationsStillBuildsAgain()
    {
        var fixture = Fixture(); var run = fixture.Session(); var first = Probe(run.Context);
        New(run.Environment); var a = Value(run.Environment);
        var second = new InventoryProbe(first.ToArray()); Replace(run.Context, second);
        New(run.Environment); var b = Value(run.Environment);
        Assert.Equal(1, first.InventoryReads); Assert.Equal(1, second.InventoryReads);
        Equal(a, b); Assert.NotSame(a, b); Assert.Same(second, Field(run.Environment, "assemblyInventoryView"));
        second.ForbidInventory = true; New(run.Environment); Assert.Same(b, Value(run.Environment));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DifferentEnvironmentsCannotShareMaterializationEvenForEqualCompilations(bool clone)
    {
        var fixture = Fixture(); var first = fixture.Session(); var firstProbe = Probe(first.Context);
        New(first.Environment); var a = Value(first.Environment);
        var compilation = clone ? fixture.Compilation.Clone() : fixture.Compilation;
        var context = clone ? Context(compilation) : first.Context;
        var probe = clone ? Probe(context) : firstProbe;
        var second = new ExceptionFlowSemanticEnvironment(context);
        Assert.Null(Field(second, "assemblyInventory")); Assert.Null(Field(second, "assemblyInventoryView"));
        New(second); var b = Value(second); Equal(a, b); Assert.NotSame(a, b);
        Assert.Equal(clone ? 1 : 2, probe.InventoryReads);
        Assert.Equal(!clone, ReferenceEquals(first.Environment.GetAnalysisScopes(), second.GetAnalysisScopes()));
    }

    [Fact]
    public void SessionsMayReuseTheSameEnvironmentInventoryWhileFactsRemainSessionOwned()
    {
        var fixture = Fixture(); var run = fixture.Session(); var probe = Probe(run.Context);
        run.Session.Analyze(fixture.Root("A")); var a = Value(run.Environment); probe.ForbidInventory = true;
        int inspected = 0;
        var second = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol => { if (symbol.Name == "Shared") inspected++; });
        Assert.True(second.Analyze(fixture.Root("B")).ProfileOutcome!.Complete);
        Assert.Equal(1, inspected); Assert.Equal(1, probe.InventoryReads); Assert.Same(a, Value(run.Environment));
        Assert.NotSame(Field(run.Session, "admissionFacts"), Field(second, "admissionFacts"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailureRetiresValueAndMarkerAndRetryBuildsBeforeReuse(bool previousSuccess)
    {
        var fixture = Fixture(); var run = fixture.Session();
        if (previousSuccess) New(run.Environment);
        run.Context.RegisterSupportingSource(Compilation("P4D.Support", "class Support {}", "/P4D/Support.cs"));
        var probe = Probe(run.Context); probe.FailBeforeIndex = 1;
        Assert.Throws<InventoryReadFailure>(() => New(run.Environment));
        Assert.Equal(1, probe.InventoryReads); Assert.Single(Assert.Single(probe.InventoryOrders));
        Assert.Null(Field(run.Environment, "assemblyInventory")); Assert.Null(Field(run.Environment, "assemblyInventoryView"));
        probe.FailBeforeIndex = null; New(run.Environment);
        Assert.Equal(2, probe.InventoryReads); Assert.Equal(2, probe.InventoryOrders[1].Count);
        var value = Value(run.Environment); Equal(Expected(probe.Select(s => s.Compilation).ToArray()), value);
        probe.ForbidInventory = true; New(run.Environment); Assert.Equal(2, probe.InventoryReads);
        Assert.Same(value, Value(run.Environment)); Assert.Same(probe, Field(run.Environment, "assemblyInventoryView"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeAndCompilationReferenceKeysAreMaterializedWithoutChangingProjection(bool pe)
    {
        var support = Compilation("P4D.Dependency", "public class Dependency {}", "/P4D/Dependency.cs");
        var fixture = Fixture([pe ? Emit(support) : support.ToMetadataReference()]); var run = fixture.Session();
        var probe = Probe(run.Context); New(run.Environment); var value = Value(run.Environment);
        Assert.Contains(Key(support.Assembly), value); Equal(Expected(fixture.Compilation), value);
        probe.ForbidInventory = true; New(run.Environment); Assert.Same(value, Value(run.Environment));
        Assert.Equal(1, probe.InventoryReads);
    }

    [Fact]
    public void WarmInventoryDoesNotSuppressALaterConflictReadFailure()
    {
        using var input = new PeInput(); var run = input.Fixture.Session(); var probe = Probe(run.Context);
        Assert.True(run.Session.Analyze(input.Fixture.Root("A")).ProfileOutcome!.Complete);
        var value = Value(run.Environment); int reads = input.Reference.Reads;
        probe.ForbidInventory = true; input.Reference.Fail = true;
        Assert.Throws<IOException>(() => run.Session.Analyze(input.Fixture.Root("B")));
        Assert.Equal(reads + 1, input.Reference.Reads); Assert.Equal(1, probe.InventoryReads);
        Assert.Same(value, Value(run.Environment)); // A completed pure value survives a later root failure.
        input.Reference.Fail = false;
        Assert.True(run.Session.Analyze(input.Fixture.Root("B")).ProfileOutcome!.Complete);
        Assert.Equal(reads + 2, input.Reference.Reads); Assert.Equal(1, probe.InventoryReads);
    }

    [Fact]
    public void NominalDeduplicationAfterGrowthStillRejectsDifferentHeldModuleImages()
    {
        var peA = Emit(Compilation("P4D.Same", "public class First {}", "/P4D/First.cs"));
        var peB = Emit(Compilation("P4D.Same", "public class Second {}", "/P4D/Second.cs"));
        var fixture = Fixture([peA]); var run = fixture.Session();
        Assert.True(run.Session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        var a = Value(run.Environment);
        var support = Compilation("P4D.Support", "class Support {}", "/P4D/Support.cs").AddReferences(peB);
        Assert.NotEqual(((AssemblyMetadata)peA.GetMetadata()).GetModules()[0].GetModuleVersionId(),
            ((AssemblyMetadata)peB.GetMetadata()).GetModules()[0].GetModuleVersionId());
        run.Context.RegisterSupportingSource(support); var probe = Probe(run.Context);
        var first = run.Session.Analyze(fixture.Root("B")); var b = Value(run.Environment);
        Assert.NotSame(a, b); Assert.Equal(1, probe.InventoryReads);
        var assembly = (IAssemblySymbol)fixture.Compilation.GetAssemblyOrModuleSymbol(peA)!;
        Assert.Single(b, key => key == Key(assembly));
        Assert.False(first.ProfileOutcome!.Complete);
        Assert.Contains(first.ProfileOutcome.Reasons, reason => reason.Category == "OwnerBindingFailure"
            && reason.Witness.StartsWith("Conflicting metadata module worlds:", StringComparison.Ordinal));
        probe.ForbidInventory = true;
        var repeated = run.Session.Analyze(fixture.Root("B"));
        Assert.Equal(Flow(first), Flow(repeated)); Assert.Equal(1, probe.InventoryReads);
        Assert.Same(b, Value(run.Environment));
    }

    [Fact]
    public void ViewRefreshReleasesThePreviousInventoryAndView()
    {
        var released = RefreshedEnvironment(); Collect(released.View, released.Value);
        Assert.False(released.View.IsAlive); Assert.False(released.Value.IsAlive); GC.KeepAlive(released.Environment);
    }

    [Fact]
    public void EnvironmentReleaseCollectsTheInventoryWhileTheHostContextLives()
    {
        var released = ReleasedEnvironment(); Collect(released.Environment, released.Value);
        Assert.False(released.Environment.IsAlive); Assert.False(released.Value.IsAlive); GC.KeepAlive(released.Context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ExceptionFlowSemanticEnvironment Environment, WeakReference View, WeakReference Value) RefreshedEnvironment()
    {
        var run = Fixture().Session(); New(run.Environment);
        var view = new WeakReference(run.Environment.GetAnalysisScopes()); var value = new WeakReference(Value(run.Environment));
        run.Context.RegisterSupportingSource(Compilation("P4D.Support", "class Support {}", "/P4D/Support.cs"));
        New(run.Environment); return (run.Environment, view, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ProjectClosureSemanticContext Context, WeakReference Environment, WeakReference Value) ReleasedEnvironment()
    {
        var run = Fixture().Session(); New(run.Environment);
        return (run.Context, new WeakReference(run.Environment), new WeakReference(Value(run.Environment)));
    }

    private static void Collect(params WeakReference[] values)
    { for (int i = 0; i < 3 && values.Any(value => value.IsAlive); i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } }
    private static AdmissionCharacterization.Fixture Fixture(MetadataReference[]? extra = null) => new(
        [("/P4D/Entry.cs", Source)], MetadataReferences.Default.Concat(extra ?? []), assemblyName: "P4D.Entry");
    private static CSharpCompilation Compilation(string name, string source, string path) => CSharpCompilation.Create(name,
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path)], MetadataReferences.Default,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    private static ProjectClosureSemanticContext Context(Compilation compilation) => ProjectClosureSemanticContext.CreateSingleCompilationContext(compilation.SyntaxTrees.First(), compilation);
    private static PortableExecutableReference Emit(Compilation compilation)
    { using var stream = new MemoryStream(); Assert.True(compilation.Emit(stream).Success); return MetadataReference.CreateFromImage(stream.ToArray()); }
    private static string Key(IAssemblySymbol assembly) => JsonSerializer.Serialize(RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly));
    private static HashSet<string> Expected(params Compilation[] compilations) => compilations.SelectMany(c => new[] { Key(c.Assembly) }
        .Concat(c.References.Select(c.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>().Select(Key))).ToHashSet(StringComparer.Ordinal);
    private static void Equal(IEnumerable<string> expected, IEnumerable<string> actual) => Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    private static string Flow(XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult result) => JsonSerializer.Serialize(AdmissionCharacterization.Flow(result));
    private static object? Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
    private static string[] Value(ExceptionFlowSemanticEnvironment environment) => Assert.IsType<string[]>(Field(environment, "assemblyInventory"));
    private static HashSet<string> Known(ExceptionFlowFiniteOrbitAdmission admission) => AdmissionCharacterization.Field<HashSet<string>>(admission, "knownAssemblies");
    private static ExceptionFlowFiniteOrbitAdmission New(ExceptionFlowSemanticEnvironment environment) => (ExceptionFlowFiniteOrbitAdmission)Call(() =>
        Activator.CreateInstance(typeof(ExceptionFlowFiniteOrbitAdmission), BindingFlags.Instance | BindingFlags.NonPublic, null, [environment], null)!)!;
    private static string Add(ExceptionFlowFiniteOrbitAdmission admission, ISymbol symbol) => (string)Call(() => typeof(ExceptionFlowFiniteOrbitAdmission)
        .GetMethod("AddDefinition", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(admission, [symbol]))!;
    private static void Frame(ExceptionFlowFiniteOrbitAdmission admission, ISymbol symbol) => typeof(ExceptionFlowFiniteOrbitAdmission)
        .GetField("currentDefinition", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(admission, Add(admission, symbol));
    private static object? Call(Func<object?> call)
    { try { return call(); } catch (TargetInvocationException e) when (e.InnerException != null) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
    private static void Replace(ProjectClosureSemanticContext context, IReadOnlyList<SemanticCompilationScope> snapshot) => typeof(ProjectClosureSemanticContext)
        .GetField("analysisCompilationScopes", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(context, snapshot);
    private static InventoryProbe Probe(ProjectClosureSemanticContext context)
    { var probe = new InventoryProbe(context.GetAnalysisCompilationScopes().ToArray()); Replace(context, probe); return probe; }

    private sealed class InventoryReadFailure : Exception;
    // Stack inspection is confined to the test input: unrelated scope consumers remain live.
    private sealed class InventoryProbe(SemanticCompilationScope[] scopes) : IReadOnlyList<SemanticCompilationScope>
    {
        public int Count => scopes.Length;
        public SemanticCompilationScope this[int index] => scopes[index];
        internal int InventoryReads { get; private set; }
        internal int OtherReads { get; private set; }
        internal bool ForbidInventory { get; set; }
        internal int? FailBeforeIndex { get; set; }
        internal List<List<Compilation>> InventoryOrders { get; } = [];
        public IEnumerator<SemanticCompilationScope> GetEnumerator()
        {
            bool inventory = new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType is { } type
                && type.DeclaringType == typeof(ExceptionFlowFiniteOrbitAdmission)
                && type.Name.StartsWith("<AssemblyInventory>", StringComparison.Ordinal));
            if (!inventory) { OtherReads++; return ((IEnumerable<SemanticCompilationScope>)scopes).GetEnumerator(); }
            InventoryReads++;
            if (ForbidInventory) throw new InventoryReadFailure();
            List<Compilation> order = []; InventoryOrders.Add(order); return Read(order).GetEnumerator();
        }
        private IEnumerable<SemanticCompilationScope> Read(List<Compilation> order)
        { for (int i = 0; i < scopes.Length; i++) { if (FailBeforeIndex == i) throw new InventoryReadFailure(); order.Add(scopes[i].Compilation); yield return scopes[i]; } }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class PeInput : IDisposable
    {
        private readonly AssemblyMetadata metadata;
        internal CountingReference Reference { get; }
        internal AdmissionCharacterization.Fixture Fixture { get; }
        internal PeInput()
        {
            using var stream = new MemoryStream();
            Assert.True(Compilation("P4D.Pe", "public class Dependency {}", "/P4D/Dependency.cs").Emit(stream).Success);
            metadata = AssemblyMetadata.CreateFromImage(stream.ToArray()); Reference = new(metadata);
            Fixture = ExceptionFlowAssemblyInventoryMaterializationTests.Fixture([Reference]);
        }
        public void Dispose() => metadata.Dispose();
    }
    private sealed class CountingReference(AssemblyMetadata metadata) : PortableExecutableReference(MetadataReferenceProperties.Assembly)
    {
        internal int Reads { get; set; }
        internal bool Fail { get; set; }
        protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;
        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties) => throw new NotSupportedException();
        protected override Metadata GetMetadataImpl()
        { Reads++; if (Fail) throw new IOException("P4D live conflict read sentinel"); return metadata; }
    }
}
