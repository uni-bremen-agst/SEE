using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Characterizes the existing inventory consumers; implements no materialization.</summary>
public sealed class ExceptionFlowAssemblyInventoryTests
{
    private const string Source = "class Entry { static void A() { Shared(); } static void B() { Shared(); } "
        + "static void Shared() { Leaf(); } static void Leaf() {} static void Accept<T>() {} }";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DifferentRootsInAFixedViewHaveEqualInventoriesAndSeparateConsumers(bool reverse)
    {
        var fixture = Fixture(); var run = fixture.Session();
        var view = run.Environment.GetAnalysisScopes();
        var a = New(run.Environment); var b = New(run.Environment);
        string[] roots = reverse ? ["B", "A"] : ["A", "B"];
        Add(a, fixture.Symbol(roots[0])); Add(b, fixture.Symbol(roots[1]));
        Assert.Same(view, run.Environment.GetAnalysisScopes());
        Equal(Expected(fixture.Compilation), Known(a)); Equal(Known(a), Known(b));
        Assert.NotSame(Known(a), Known(b));
        Assert.NotEqual(Assert.Single(Declarations(a).Keys), Assert.Single(Declarations(b).Keys));
        var first = run.Session.Analyze(fixture.Root(roots[0]));
        var second = run.Session.Analyze(fixture.Root(roots[1]));
        Assert.True(first.ProfileOutcome!.Complete); Assert.True(second.ProfileOutcome!.Complete);
        Equal(Known(a), Known(ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(roots[1]), run.Environment)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void RepeatedTargetsReenumerateTheSameInputBeforeDefinitionDeduplication(int count)
    {
        var fixture = Fixture(); var run = fixture.Session(); var probe = Probe(run.Context);
        var admission = New(run.Environment);
        Assert.Equal(1, probe.Enumerations);
        Frame(admission, fixture.Symbol("A"));
        Assert.Equal(2, probe.Enumerations);
        var expected = Expected(fixture.Compilation); var consumer = Known(admission);
        for (int i = 0; i < count; i++)
        {
            admission.RegisterTarget(fixture.Symbol("Leaf"), new(fixture.Symbol("Leaf")));
            Equal(expected, consumer); Assert.Same(consumer, Known(admission));
        }
        Assert.Equal(2 + count, probe.Enumerations);
        Assert.Equal(2, admission.DefinitionCount);
        Assert.Equal(count, AdmissionCharacterization.Field<IList>(admission, "edges").Count);
    }

    [Fact]
    public void RealSupportingGrowthRefreshesOnlyAtTheExistingConsumerPointAndPreservesEarlierReasons()
    {
        var fixture = Fixture(); var run = fixture.Session();
        var before = run.Environment.GetAnalysisScopes();
        var a = New(run.Environment); var b = New(run.Environment);
        Frame(a, fixture.Symbol("A")); Frame(b, fixture.Symbol("B"));
        var support = Compilation("P4C.Support", "public class Foreign {}", "/P4C/Support.cs");
        var foreign = support.GetTypeByMetadataName("Foreign")!;
        Term(a, foreign); Assert.Single(Reasons(a));
        run.Context.RegisterSupportingSource(support);
        var after = run.Environment.GetAnalysisScopes();
        Assert.NotSame(before, after); Assert.Single(before); Assert.Equal(2, after.Count);
        Assert.DoesNotContain(Key(support.Assembly), Known(a));
        Term(a, foreign); // Merely reading a type must not silently refresh the old consumer.
        Assert.Equal(2, Reasons(a).Count);
        var target = fixture.Symbol("Accept").Construct(foreign);
        a.RegisterTarget(target, new(target));
        Equal(Expected(fixture.Compilation, support), Known(a));
        Equal(Expected(fixture.Compilation), Known(b));
        Assert.Equal(2, Reasons(a).Count); Term(a, foreign); Assert.Equal(2, Reasons(a).Count);
        Equal(Known(a), Known(New(run.Environment)));
        Add(b, fixture.Symbol("Leaf")); Equal(Known(a), Known(b)); Assert.NotSame(Known(a), Known(b));
        Assert.All(Reasons(a), reason => Assert.Equal("OwnerBindingFailure", reason.Category));
        run.Context.RegisterSupportingSource(support); Assert.Same(after, run.Environment.GetAnalysisScopes());
    }

    [Fact]
    public void RealSupportingGrowthCanReplaceTheViewWithoutChangingNominalInventoryValues()
    {
        var support = Compilation("P4C.AlreadyReferenced", "public class Dependency {}", "/P4C/Dependency.cs");
        var fixture = Fixture([Emit(support)]); var run = fixture.Session();
        var before = run.Environment.GetAnalysisScopes(); var admission = New(run.Environment);
        Assert.Contains(Key(support.Assembly), Known(admission));
        string[] keys = Known(admission).Order(StringComparer.Ordinal).ToArray();
        run.Context.RegisterSupportingSource(support); var after = run.Environment.GetAnalysisScopes();
        Assert.NotSame(before, after); Assert.Single(before); Assert.Equal(2, after.Count);
        var probe = Probe(run.Context); Add(admission, fixture.Symbol("A"));
        Assert.Equal(1, probe.Enumerations); Assert.Equal(2, Assert.Single(probe.Orders).Count);
        Equal(keys, Known(admission)); Equal(keys, Known(New(run.Environment)));
    }

    [Theory]
    [InlineData("clone")]
    [InlineData("independent")]
    [InlineData("references")]
    public void CompilationObjectIdentityIsStrongerThanNominalInventoryEquality(string change)
    {
        var fixture = Fixture();
        var other = change switch
        {
            "clone" => fixture.Compilation.Clone(),
            "references" => fixture.Compilation.AddReferences(Emit("P4C.Added", "public class Added {}")),
            _ => Compilation(fixture.Compilation.AssemblyName!, Source, "/P4C/Entry.cs")
        };
        Assert.NotSame(fixture.Compilation, other);
        var first = fixture.Session(); var context = Context(other); var second = new ExceptionFlowSemanticEnvironment(context);
        Assert.NotSame(first.Environment.GetAnalysisScopes(), second.GetAnalysisScopes());
        Equal(Expected(other), Known(New(second)));
        Assert.Equal(change != "references", Known(New(first.Environment)).SetEquals(Known(New(second))));
        Assert.Same(fixture.Compilation, Assert.Single(first.Environment.GetAnalysisScopes()).Compilation);
        Assert.Same(other, Assert.Single(second.GetAnalysisScopes()).Compilation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnvironmentsAndSessionsOwnTheirConsumersEvenWhenTheHostViewIsShared(bool separateContext)
    {
        var fixture = Fixture(); var first = fixture.Session();
        var second = new ExceptionFlowSemanticEnvironment(separateContext ? Context(fixture.Compilation) : first.Context);
        var session = new ExceptionFlowSummaryAnalysisSession(second);
        Assert.Equal(!separateContext, ReferenceEquals(first.Environment.GetAnalysisScopes(), second.GetAnalysisScopes()));
        var a = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), first.Environment);
        var b = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("B"), second);
        Equal(Known(a), Known(b)); Assert.NotSame(Known(a), Known(b));
        Known(a).Add("test-only consumer sentinel"); Assert.DoesNotContain("test-only consumer sentinel", Known(b));
        Assert.True(session.Analyze(fixture.Root("B")).ProfileOutcome!.Complete);
        Assert.DoesNotContain("test-only consumer sentinel", Known(New(first.Environment)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopeEnumerationKeepsItsInputOrderWhileTheConsumerUsesSetSemantics(bool reverse)
    {
        var a = Compilation("P4C.A", "class A {}", "/P4C/A.cs");
        var b = Compilation("P4C.B", "class B {}", "/P4C/B.cs");
        var context = new ProjectClosureSemanticContext(new[] { b, a }.Select(SemanticCompilationScope.CreateSupportingSourceDependency).ToArray(), []);
        var environment = new ExceptionFlowSemanticEnvironment(context);
        Assert.Equal(new[] { a, b }, environment.GetAnalysisScopes().Select(s => s.Compilation));
        var ordered = context.GetAnalysisCompilationScopes().ToArray(); if (reverse) Array.Reverse(ordered);
        var probe = new SnapshotProbe(ordered); ReplaceSnapshot(context, probe);
        var inventory = Known(New(environment));
        Assert.Equal(ordered.Select(s => s.Compilation), Assert.Single(probe.Orders));
        Equal(Expected(a, b), inventory);
        var otherContext = new ProjectClosureSemanticContext(ordered.Reverse().ToArray(), []);
        Equal(inventory, Known(New(new(otherContext))));
        // Failure order is the enumeration order, not an invented sorted-key order.
        var failing = new SnapshotProbe(ordered) { FailBeforeIndex = 1 };
        ReplaceSnapshot(otherContext, failing);
        Assert.Throws<InventoryReadFailure>(() => New(new(otherContext)));
        Assert.Same(ordered[0].Compilation, Assert.Single(Assert.Single(failing.Orders)));
    }

    [Fact]
    public void OwnAssemblyPrecedesBoundReferencesAndReferenceOrderDoesNotChangeTheSet()
    {
        var referenceA = Emit("P4C.RefA", "public class RefA {}");
        var referenceB = Emit("P4C.RefB", "public class RefB {}");
        var first = Compilation("P4C.Owner", Source, "/P4C/Entry.cs").AddReferences(referenceA, referenceB);
        var second = first.WithReferences(first.References.Reverse());
        var firstView = new ExceptionFlowSemanticEnvironment(Context(first));
        var secondView = new ExceptionFlowSemanticEnvironment(Context(second));
        var inventory = Known(New(firstView)); var reordered = Known(New(secondView));
        Equal(inventory, reordered);
        // Observe the actual product consumer, not the test's projection helper.
        Assert.Equal(Key(first.Assembly), inventory.First());
        Assert.Equal(new[] { Key(first.Assembly) }.Concat(first.SourceModule.ReferencedAssemblySymbols.Select(Key))
            .Distinct(StringComparer.Ordinal), inventory);
        Assert.False(inventory.SequenceEqual(reordered));
        Assert.Equal(new[] { "P4C.RefA", "P4C.RefB" }, first.SourceModule.ReferencedAssemblySymbols
            .Where(s => s.Name.StartsWith("P4C.Ref", StringComparison.Ordinal)).Select(s => s.Name));
        Assert.Equal(new[] { "P4C.RefB", "P4C.RefA" }, second.SourceModule.ReferencedAssemblySymbols
            .Where(s => s.Name.StartsWith("P4C.Ref", StringComparison.Ordinal)).Select(s => s.Name));
        var root = first.SyntaxTrees.Single().GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "A");
        var left = new ExceptionFlowSummaryAnalysisSession(firstView).Analyze(root);
        var right = new ExceptionFlowSummaryAnalysisSession(secondView).Analyze(root);
        Assert.True(left.ProfileOutcome!.Complete); Assert.True(right.ProfileOutcome!.Complete);
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(left)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(right)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplicateReferencePathsAndRepeatedScopeAssembliesCollapseOnlyNominalKeys(bool aliases)
    {
        var reference = Emit("P4C.Duplicate", "public class Duplicate {}");
        var duplicate = aliases ? reference.WithAliases(["alias"]) : reference;
        var compilation = Compilation("P4C.Owner", Source, "/P4C/Entry.cs").AddReferences(reference, duplicate);
        var context = Context(compilation); var environment = new ExceptionFlowSemanticEnvironment(context);
        var scopes = context.GetAnalysisCompilationScopes();
        ReplaceSnapshot(context, scopes.Concat(scopes).ToArray());
        var admission = New(environment); Equal(Expected(compilation), Known(admission));
        var bound = Assert.Single(compilation.SourceModule.ReferencedAssemblySymbols, a => a.Name == "P4C.Duplicate");
        string key = Key(bound);
        Assert.Single(Known(admission), value => value == key);
        if (aliases) Assert.Null(compilation.GetAssemblyOrModuleSymbol(reference)); // Roslyn binds the last alias variant.
        else Assert.Same(bound, compilation.GetAssemblyOrModuleSymbol(reference));
        Assert.Same(bound, compilation.GetAssemblyOrModuleSymbol(duplicate));
        Assert.True(Raw(compilation).Concat(Raw(compilation)).Count(value => value == key) >= 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeAndCompilationReferencesEnterTheSameNominalAssemblyInventory(bool pe)
    {
        var dependency = Compilation("P4C.Dependency", "public class Dependency {}", "/P4C/Dependency.cs");
        MetadataReference reference = pe ? Emit(dependency) : dependency.ToMetadataReference();
        var compilation = Compilation("P4C.Owner", Source, "/P4C/Entry.cs").AddReferences(reference);
        var symbol = Assert.IsAssignableFrom<IAssemblySymbol>(compilation.GetAssemblyOrModuleSymbol(reference));
        Assert.Equal(Key(dependency.Assembly), Key(symbol));
        Assert.Contains(symbol, compilation.SourceModule.ReferencedAssemblySymbols, SymbolEqualityComparer.Default);
        var inventory = Known(New(new(Context(compilation))));
        Equal(Expected(compilation), inventory); Assert.Contains(Key(symbol), inventory);
        var counterpart = compilation.ReplaceReference(reference, pe ? dependency.ToMetadataReference() : Emit(dependency));
        Equal(inventory, Known(New(new(Context(counterpart)))));
    }

    [Fact]
    public void NominalInventoryEqualityDoesNotSuppressHeldPeModuleConflictChecks()
    {
        var first = Emit("P4C.Same", "public class First {}");
        var second = Emit("P4C.Same", "public class Second {}");
        var fixture = Fixture([first]); var run = fixture.Session();
        var support = Compilation("P4C.Support", "class Support {}", "/P4C/Support.cs").AddReferences(second);
        var one = (IAssemblySymbol)fixture.Compilation.GetAssemblyOrModuleSymbol(first)!;
        var two = (IAssemblySymbol)support.GetAssemblyOrModuleSymbol(second)!;
        Assert.Equal(Key(one), Key(two));
        Assert.NotEqual(((AssemblyMetadata)first.GetMetadata()).GetModules()[0].GetModuleVersionId(),
            ((AssemblyMetadata)second.GetMetadata()).GetModules()[0].GetModuleVersionId());
        run.Context.RegisterSupportingSource(support);
        Assert.Single(Known(New(run.Environment)), value => value == Key(one));
        var result = run.Session.Analyze(fixture.Root("A"));
        Assert.False(result.ProfileOutcome!.Complete);
        Assert.Contains(result.ProfileOutcome.Reasons, r => r.Category == "OwnerBindingFailure"
            && r.Witness.StartsWith("Conflicting metadata module worlds:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedConstructorEnumerationPublishesNoRootOrFactsAndAnExplicitRetrySucceeds(bool earlierSuccess)
    {
        var fixture = Fixture(); var run = fixture.Session();
        if (earlierSuccess) Assert.True(run.Session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        int factsBefore = Facts(run.Session).Count;
        run.Context.RegisterSupportingSource(Compilation("P4C.Support", "class Support {}", "/P4C/Support.cs"));
        var probe = Probe(run.Context); probe.FailBeforeIndex = 1;
        ExceptionFlowFiniteOrbitProfile.Certificate? outcome = null;
        Assert.Throws<InventoryReadFailure>(() => outcome = run.Session.Analyze(fixture.Root("B")).ProfileOutcome);
        Assert.Null(outcome); Assert.Equal(factsBefore, Facts(run.Session).Count);
        Assert.Single(Assert.Single(probe.Orders)); // First scope completed before the controlled read failure.
        probe.FailBeforeIndex = null;
        var result = run.Session.Analyze(fixture.Root("B"));
        var cold = new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(run.Context)).Analyze(fixture.Root("B"));
        Assert.True(result.ProfileOutcome!.Complete);
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(cold)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(result)));
    }

    [Fact]
    public void FailedRefreshCanLeaveOnlyARootLocalPrefixAndRetryMustReadAgain()
    {
        var fixture = Fixture(); var run = fixture.Session(); var admission = New(run.Environment);
        Frame(admission, fixture.Symbol("A")); int definitions = admission.DefinitionCount;
        var middle = Compilation("P4C.B", "class Middle {}", "/P4C/B.cs");
        var last = Compilation("P4C.C", "class Last {}", "/P4C/C.cs");
        run.Context.RegisterSupportingSource(middle); run.Context.RegisterSupportingSource(last);
        var probe = Probe(run.Context); probe.FailBeforeIndex = 2;
        Assert.Throws<InventoryReadFailure>(() => Add(admission, fixture.Symbol("B")));
        Assert.Equal(definitions, admission.DefinitionCount);
        Assert.Contains(Key(middle.Assembly), Known(admission)); Assert.DoesNotContain(Key(last.Assembly), Known(admission));
        Assert.Throws<InvalidOperationException>(() => admission.Outcome);
        Assert.Empty(Facts(run.Session));
        int reads = probe.Enumerations; probe.FailBeforeIndex = null;
        Add(admission, fixture.Symbol("B")); Assert.Equal(reads + 1, probe.Enumerations);
        Equal(Expected(fixture.Compilation, middle, last), Known(admission));
    }

    [Fact]
    public void LazyPeBindingFailureReturnsNoInventoryAndRetryDoesNotMemoizeFailure()
    {
        var dependency = Compilation("P4C.Lazy", "public class LazyDependency {}", "/P4C/Lazy.cs");
        using var stream = new MemoryStream(); Assert.True(dependency.Emit(stream).Success);
        using var metadata = AssemblyMetadata.CreateFromImage(stream.ToArray());
        var reference = new ProbeReference(metadata) { Fail = true };
        // Do not request diagnostics or symbols before the constructor: binding is deliberately lazy.
        var compilation = Compilation("P4C.Owner", Source, "/P4C/Entry.cs").AddReferences(reference);
        var environment = new ExceptionFlowSemanticEnvironment(Context(compilation));
        var session = new ExceptionFlowSummaryAnalysisSession(environment);
        ExceptionFlowFiniteOrbitAdmission? result = null;
        Assert.Throws<InventoryReadFailure>(() => result = New(environment));
        Assert.Null(result); Assert.Empty(Facts(session)); Assert.Equal(1, reference.Reads);
        reference.Fail = false;
        result = New(environment); Assert.True(reference.Reads > 1);
        Assert.Contains(Key(dependency.Assembly), Known(result)); Equal(Expected(compilation), Known(result));
        int reads = reference.Reads;
        var root = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "A");
        var symbol = compilation.GetSemanticModel(root.SyntaxTree).GetDeclaredSymbol(root)!;
        Add(result, symbol); Add(result, symbol);
        Assert.Equal(reads, reference.Reads); // Repeated bound-symbol inventory does not reread PE metadata.
        Assert.True(session.Analyze(root).ProfileOutcome!.Complete);
    }

    [Fact]
    public void RealP2CFactReplayStillRegistersTargetsAndConsumesTheAssemblyInventory()
    {
        var fixture = Fixture(); var run = fixture.Session();
        Dictionary<string, int> cold = [];
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol => cold[symbol.Name] = cold.GetValueOrDefault(symbol.Name) + 1);
        Assert.True(session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.True(session.Analyze(fixture.Root("B")).ProfileOutcome!.Complete);
        Assert.Equal(1, cold["Shared"]); Assert.Equal(1, cold["Leaf"]); // Actual completed-fact hits.
        var fact = Facts(session)[fixture.Symbol("Shared")];
        var probe = Probe(run.Context); var admission = New(run.Environment); Frame(admission, fixture.Symbol("Shared"));
        var consumer = Known(admission); consumer.Clear(); consumer.Add("test-only root sentinel");
        int enumerations = probe.Enumerations;
        fact.Replay(admission); // Actual published product fact, not a test replayer.
        Assert.Equal(enumerations + 1, probe.Enumerations); // Shared has exactly one RegisterEvent (Leaf).
        var expected = Expected(fixture.Compilation); expected.Add("test-only root sentinel"); Equal(expected, consumer);
        Assert.Equal(2, admission.DefinitionCount); Assert.Single(AdmissionCharacterization.Field<IList>(admission, "edges"));
        Assert.Equal(1, cold["Shared"]);
    }

    [Fact]
    public void InventoryAndDigestCanUseTheHostViewBoundaryWithoutSharingAPrivateMarker()
    {
        var fixture = Fixture(); var run = fixture.Session(); var before = run.Environment.GetAnalysisScopes();
        int computes = 0;
        string Compute(ExceptionFlowSemanticEnvironment environment) { computes++; return ExceptionFlowFiniteOrbitAdmission.ScopeDigest(environment); }
        var first = Known(New(run.Environment)); string digest = run.Environment.GetScopeDigest(Compute);
        Assert.Same(digest, run.Environment.GetScopeDigest(Compute)); Assert.Equal(1, computes);
        var support = Compilation("P4C.Support", "class Support {}", "/P4C/Support.cs"); run.Context.RegisterSupportingSource(support);
        var after = run.Environment.GetAnalysisScopes(); Assert.NotSame(before, after);
        var grown = Known(New(run.Environment)); Assert.True(first.IsSubsetOf(grown)); Assert.Contains(Key(support.Assembly), grown);
        Assert.NotEqual(digest, run.Environment.GetScopeDigest(Compute)); Assert.Equal(2, computes);
        var replacement = run.Context.GetAnalysisCompilationScopes().ToArray(); ReplaceSnapshot(run.Context, replacement);
        Assert.NotSame(after, run.Environment.GetAnalysisScopes());
        Equal(grown, Known(New(run.Environment))); run.Environment.GetScopeDigest(Compute); Assert.Equal(3, computes);
    }

    private static AdmissionCharacterization.Fixture Fixture(MetadataReference[]? extra = null) => new(
        [("/P4C/Entry.cs", Source)], MetadataReferences.Default.Concat(extra ?? []), assemblyName: "P4C.A");
    private static CSharpCompilation Compilation(string name, string source, string path) => CSharpCompilation.Create(name,
        [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12), path)],
        MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    private static ProjectClosureSemanticContext Context(Compilation compilation) => ProjectClosureSemanticContext.CreateSingleCompilationContext(compilation.SyntaxTrees.First(), compilation);
    private static PortableExecutableReference Emit(string name, string source) => Emit(Compilation(name, source, "/P4C/Dependency.cs"));
    private static PortableExecutableReference Emit(Compilation compilation)
    { using var stream = new MemoryStream(); Assert.True(compilation.Emit(stream).Success); return MetadataReference.CreateFromImage(stream.ToArray()); }
    private static string Key(IAssemblySymbol assembly) => JsonSerializer.Serialize(RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly));
    private static string[] Raw(Compilation compilation) => new[] { Key(compilation.Assembly) }.Concat(compilation.SourceModule.ReferencedAssemblySymbols.Select(Key)).ToArray();
    // Independent expectation via reference binding, not a replacement inventory implementation/cache.
    private static HashSet<string> Expected(params Compilation[] compilations) => compilations.SelectMany(c =>
        new[] { Key(c.Assembly) }.Concat(c.References.Select(c.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>().Select(Key))).ToHashSet(StringComparer.Ordinal);
    private static void Equal(IEnumerable<string> expected, IEnumerable<string> actual) => Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    private static HashSet<string> Known(ExceptionFlowFiniteOrbitAdmission admission) => AdmissionCharacterization.Field<HashSet<string>>(admission, "knownAssemblies");
    private static Dictionary<string, ISymbol> Declarations(ExceptionFlowFiniteOrbitAdmission admission) => AdmissionCharacterization.Field<Dictionary<string, ISymbol>>(admission, "declarations");
    private static List<ExceptionFlowFiniteOrbitProfile.Reason> Reasons(ExceptionFlowFiniteOrbitAdmission admission) => AdmissionCharacterization.Field<List<ExceptionFlowFiniteOrbitProfile.Reason>>(admission, "reasons");
    private static Dictionary<ISymbol, ExceptionFlowFiniteOrbitAdmission.LocalFact> Facts(ExceptionFlowSummaryAnalysisSession session) =>
        AdmissionCharacterization.Field<Dictionary<ISymbol, ExceptionFlowFiniteOrbitAdmission.LocalFact>>(
            AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(session, "admissionFacts"), "completed");
    private static ExceptionFlowFiniteOrbitAdmission New(ExceptionFlowSemanticEnvironment environment) => (ExceptionFlowFiniteOrbitAdmission)Call(() =>
        Activator.CreateInstance(typeof(ExceptionFlowFiniteOrbitAdmission), BindingFlags.Instance | BindingFlags.NonPublic, null, [environment], null)!)!;
    private static object? Invoke(ExceptionFlowFiniteOrbitAdmission admission, string method, object argument) => Call(() => typeof(ExceptionFlowFiniteOrbitAdmission)
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(admission, [argument]));
    private static string Add(ExceptionFlowFiniteOrbitAdmission admission, ISymbol symbol) => (string)Invoke(admission, "AddDefinition", symbol)!;
    private static void Frame(ExceptionFlowFiniteOrbitAdmission admission, ISymbol symbol) => typeof(ExceptionFlowFiniteOrbitAdmission)
        .GetField("currentDefinition", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(admission, Add(admission, symbol));
    private static void Term(ExceptionFlowFiniteOrbitAdmission admission, ITypeSymbol type) => Invoke(admission, "ToTerm", type);
    private static object? Call(Func<object?> call)
    { try { return call(); } catch (TargetInvocationException e) when (e.InnerException != null) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; } }
    private static void ReplaceSnapshot(ProjectClosureSemanticContext context, IReadOnlyList<SemanticCompilationScope> snapshot) => typeof(ProjectClosureSemanticContext)
        .GetField("analysisCompilationScopes", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(context, snapshot);
    private static SnapshotProbe Probe(ProjectClosureSemanticContext context)
    { var probe = new SnapshotProbe(context.GetAnalysisCompilationScopes().ToArray()); ReplaceSnapshot(context, probe); return probe; }

    private sealed class InventoryReadFailure : Exception;
    // Test input at Roslyn's existing PE-reference abstraction, not a product hook or MVID oracle.
    private sealed class ProbeReference(AssemblyMetadata metadata) : PortableExecutableReference(MetadataReferenceProperties.Assembly)
    {
        internal int Reads { get; private set; }
        internal bool Fail { get; set; }
        protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;
        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties) => throw new NotSupportedException();
        protected override Metadata GetMetadataImpl()
        { Reads++; if (Fail) throw new InventoryReadFailure(); return metadata; }
    }
    // Test-owned readonly membership; counters/fault injection never enter product code.
    // Normal hosts return arrays. The failure models the enumeration boundary, not a PE/MVID failure.
    private sealed class SnapshotProbe(SemanticCompilationScope[] scopes) : IReadOnlyList<SemanticCompilationScope>
    {
        public int Count => scopes.Length;
        public SemanticCompilationScope this[int index] => scopes[index];
        internal int Enumerations { get; private set; }
        internal int? FailBeforeIndex { get; set; }
        internal List<List<Compilation>> Orders { get; } = [];
        public IEnumerator<SemanticCompilationScope> GetEnumerator()
        {
            Enumerations++; List<Compilation> order = []; Orders.Add(order);
            for (int i = 0; i < scopes.Length; i++)
            { if (FailBeforeIndex == i) throw new InventoryReadFailure(); order.Add(scopes[i].Compilation); yield return scopes[i]; }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
