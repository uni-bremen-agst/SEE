using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Bounded causal controls and exact source joins; no private-state instrumentation.</summary>
public sealed class ExceptionFlowRootReasonFindingAttributionTests(ITestOutputHelper output)
{
    private const string Doc = "/// <summary>Root.</summary>\n";
    private const string Tag = "/// <exception cref=\"System.InvalidOperationException\">Documented.</exception>\n";
    private const string Target = "TargetOrAnalysisCoverageIncomplete";
    private const string Owner = "OwnerBindingFailure";
    private const string Scope = "ScopeMapNotClosed";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypeTagChangesFindingEligibilityWithoutChangingTheNoBodyCause(bool tagged)
    {
        var fixture = Fixture(("Type", Doc + (tagged ? Tag : "") + "class Entry {}"));
        var run = fixture.Session();
        var capture = Capture("type-" + tagged, fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        Assert.Single(capture.Result.ProfileOutcome!.Reasons);
        AssertNoBodyReason(capture, capture.Symbol!);
        Assert.False(Producer(capture.Symbol!, run.Environment).HasBody);
        Assert.Equal(["OutsideProfile", "TargetCoverageIncomplete"], capture.RootCoverage.ReasonCodes);
        Assert.Equal(tagged ? 1 : 0, capture.Findings.Length);
        if (tagged) Assert.Equal("DOC631", Assert.Single(capture.Findings).Smell.ID);
        // The binding/producer facts, not the presence of a tag, determine the query outcome.
        var noTag = Fixture(("Type", Doc + "class Entry {}"));
        var noTagRun = noTag.Session();
        var baseline = noTagRun.Session.Analyze(OneRoot(noTag));
        Assert.Equal(baseline.ProfileOutcome!.Root, capture.Result.ProfileOutcome.Root);
        Assert.Equal(baseline.ProfileOutcome.Reasons, capture.Result.ProfileOutcome.Reasons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteMethodDistinguishesExactPositiveAndExactNegativeEvidence(bool throws)
    {
        var fixture = Fixture(("Method", "class Entry {\n" + Doc + Tag + "static void Root() { " +
            (throws ? "throw new System.InvalidOperationException();" : "") + " } }"));
        var run = fixture.Session();
        var capture = Capture("complete-" + throws, fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        Assert.True(capture.Result.ProfileOutcome!.Complete);
        Assert.Empty(capture.Result.ProfileOutcome.Reasons);
        Assert.Equal("Complete", capture.RootCoverage.Completion);
        Assert.Equal(throws, capture.Result.ThrownExceptions.Any(t => t.Name == "InvalidOperationException"));
        if (throws) Assert.Empty(capture.Findings);
        else Assert.Equal("DOC632", Assert.Single(capture.Findings).Smell.ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitAndImplicitConstructorQueriesRetainInitializerEvidence(bool implicitConstructor)
    {
        string declaration = "class Item { int value = Fail(); static int Fail() => throw new System.InvalidOperationException();\n" +
            (implicitConstructor ? "" : Doc + Tag + "public Item() {}\n") + "}";
        var fixture = Fixture(("Constructor", implicitConstructor
            ? declaration + "\nclass Entry {\n" + Doc + Tag + "static void Root() { _ = new Item(); } }"
            : declaration));
        var run = fixture.Session();
        var capture = Capture("constructor-" + implicitConstructor, fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        Assert.True(capture.Result.ProfileOutcome!.Complete);
        Assert.Contains(capture.Result.ThrownExceptions, t => t.Name == "InvalidOperationException");
        Assert.Empty(capture.Findings);
        var type = fixture.Compilation.GetTypeByMetadataName("Item")!;
        var constructor = Assert.Single(type.InstanceConstructors);
        Assert.Equal(implicitConstructor, constructor.IsImplicitlyDeclared);
        Assert.True(Producer(constructor, run.Environment).HasBody);
        var typeBody = Producer(type, run.Environment);
        Assert.False(typeBody.HasBody);
        Assert.Empty(typeBody.Fragment.Sources);
        Assert.Empty(typeBody.Fragment.CallEdges);
        output.WriteLine("C3_CONSTRUCTOR " + JsonSerializer.Serialize(new {
            implicitConstructor, Constructor = ExceptionFlowFiniteOrbitAdmission.BodyId(constructor),
            Type = ExceptionFlowFiniteOrbitAdmission.BodyId(type), TypeHasBody = typeBody.HasBody }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneMissingFieldRootSymbolProducesOwnerThenScopeThenPublicOutside(bool tagged)
    {
        var fixture = Fixture(("Field", "class Entry {\n" + Doc + (tagged ? Tag : "") + "int value; }"));
        var run = fixture.Session();
        var capture = Capture("field-" + tagged, fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        Assert.Null(capture.Symbol);
        // Variable binding succeeds in exactly the same model: no compiler/world failure is required.
        var variable = Assert.Single(capture.Root.DescendantNodes().OfType<VariableDeclaratorSyntax>());
        Assert.IsAssignableFrom<IFieldSymbol>(fixture.Compilation.GetSemanticModel(capture.Root.SyntaxTree).GetDeclaredSymbol(variable));
        string root = capture.Result.ProfileOutcome!.Root;
        Assert.Equal("unresolved:" + capture.Root.SyntaxTree.FilePath + ":" + capture.Root.SpanStart, root);
        Assert.Equal([
            new ExceptionFlowFiniteOrbitProfile.Reason(Owner, root),
            new ExceptionFlowFiniteOrbitProfile.Reason(Scope, "Root environment is not total and closed: " + root)
        ], capture.Result.ProfileOutcome.Reasons);
        Assert.Equal(0, ExceptionFlowFiniteOrbitAdmission.Discover(capture.Root, run.Environment).DefinitionCount);
        Assert.Equal(["OutsideProfile", "ScopeCoverageIncomplete", "TypeBindingIncomplete"], capture.RootCoverage.ReasonCodes);
        Assert.DoesNotContain(capture.Result.ProfileOutcome.Reasons, reason => reason.Category == Target);
        Assert.Equal(tagged ? 1 : 0, capture.Findings.Length);
        if (tagged) Assert.Equal("DOC631", Assert.Single(capture.Findings).Smell.ID);
    }

    [Theory]
    [InlineData("abstract")]
    [InlineData("interface")]
    [InlineData("metadata")]
    public void MeaningfulCallableQueriesKeepNoBodyReasonsAttributedToTheActualDefinition(string kind)
    {
        AdmissionCharacterization.Fixture fixture;
        if (kind == "metadata")
        {
            var dependency = Fixture(("Api", "public static class Api { public static void Run() {} }"));
            using var image = new MemoryStream();
            Assert.True(dependency.Compilation.Emit(image).Success);
            fixture = new([("/C3/Metadata.cs", "class Entry {\n" + Doc + Tag + "static void Root() { Api.Run(); } }")],
                MetadataReferences.Default.Append(MetadataReference.CreateFromImage(image.ToArray())), assemblyName: "C3.Consumer");
        }
        else fixture = Fixture(("Callable", kind == "abstract"
            ? "abstract class Entry {\n" + Doc + Tag + "public abstract void Root(); }"
            : "interface Entry {\n" + Doc + Tag + "void Root(); }"));
        var run = fixture.Session();
        var capture = Capture("callable-" + kind, fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        ISymbol definition = kind == "metadata" ? fixture.Compilation.GetTypeByMetadataName("Api")!.GetMembers("Run").Single() : capture.Symbol!;
        AssertNoBodyReason(capture, definition);
        Assert.False(Producer(definition, run.Environment).HasBody);
        if (kind == "metadata")
        {
            Assert.True(Producer(capture.Symbol!, run.Environment).HasBody);
            Assert.NotEqual(capture.Result.ProfileOutcome!.Root, ExceptionFlowFiniteOrbitAdmission.BodyId(definition));
            Assert.All(definition.Locations, location => Assert.True(location.IsInMetadata));
        }
        Assert.Equal("DOC631", Assert.Single(capture.Findings).Smell.ID);
        output.WriteLine("C3_CAUSE " + JsonSerializer.Serialize(new {
            kind, QueryRoot = capture.Result.ProfileOutcome!.Root,
            AffectedDefinition = ExceptionFlowFiniteOrbitAdmission.BodyId(definition),
            RuntimeOriginSite = (string?)null, SourceDerivedOrigin = "Admission.InspectDeclaration / executable body unavailable" }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourCauseMixedScopeKeepsExactRootFindingJoinsAndMonotoneGlobalReasons(bool reverse)
    {
        var fixture = Fixture(
            ("Type", Doc + "partial class Entry {}"),
            ("Field", "partial class Entry {\n" + Doc + "int value; }"),
            ("Complete", "partial class Entry {\n" + Doc + Tag + "static void Clean() {} }"),
            ("Incomplete", "partial class Entry {\n" + Doc + Tag + "static void Uncertain() { Bodyless(); } static extern void Bodyless(); }"));
        var run = fixture.Session();
        var global = Started();
        var roots = fixture.Compilation.SyntaxTrees.Select(tree => Assert.Single(Documented(tree))).ToArray();
        var allFindings = new List<Finding>();
        foreach (var root in reverse ? roots.Reverse() : roots)
        {
            var capture = Capture("mixed-" + reverse, fixture, root, run.Context, run.Environment, run.Session, global);
            bool clean = root is MethodDeclarationSyntax method && method.Identifier.ValueText == "Clean";
            Assert.Equal(clean, capture.Result.ProfileOutcome!.Complete);
            if (clean) Assert.Equal("DOC632", Assert.Single(capture.Findings).Smell.ID);
            else if (root is MethodDeclarationSyntax) Assert.Equal("DOC631", Assert.Single(capture.Findings).Smell.ID);
            else Assert.Empty(capture.Findings);
            allFindings.AddRange(capture.Findings);
        }
        Assert.Equal("Incomplete", global.Report().Completion);
        Assert.Equal(["OutsideProfile", "ScopeCoverageIncomplete", "TargetCoverageIncomplete", "TypeBindingIncomplete"], global.Report().ReasonCodes);
        Assert.Single(allFindings, f => f.Smell.ID == "DOC632");
        Assert.Single(allFindings, f => f.Smell.ID == "DOC631");
        var detectorRun = fixture.Session();
        var detectorGlobal = Started();
        var detectorFindings = fixture.Compilation.SyntaxTrees.SelectMany(tree => Detect(fixture, tree, detectorRun.Context,
            detectorRun.Session, detectorGlobal)).ToArray();
        Assert.Equal(JsonSerializer.Serialize(global.Report()), JsonSerializer.Serialize(detectorGlobal.Report()));
        Assert.Equal(global.Report().ReasonCodes, detectorGlobal.Report().ReasonCodes);
        Assert.Equal(allFindings.Select(FindingKey).Order(StringComparer.Ordinal), detectorFindings.Select(FindingKey).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IndependentLowerWitnessSuppressesOnlyItsExactTagWhileTheRootStaysIncomplete()
    {
        var fixture = Fixture(("Lower", "class Entry {\n" + Doc +
            "/// <exception cref=\"System.ArgumentNullException\">Exact.</exception>\n" +
            "/// <exception cref=\"System.ArgumentException\">Base.</exception>\n" +
            "static void Root() { Known(); Bodyless(); } static void Known() { throw new System.ArgumentNullException(); } static extern void Bodyless(); }"));
        var run = fixture.Session();
        var capture = Capture("lower-witness", fixture, OneRoot(fixture), run.Context, run.Environment, run.Session, Started());
        Assert.False(capture.Result.ProfileOutcome!.Complete);
        Assert.Contains(capture.Result.ProfileDiagnostics!.ProvenLowerWitnesses, t => t.Name == "ArgumentNullException");
        Assert.True(capture.Result.ProfileDiagnostics.HasEscapingUncertainty);
        Assert.True(capture.Result.ProfileDiagnostics.NormalFindingAdmissionBlocked);
        var finding = Assert.Single(capture.Findings);
        Assert.Equal("DOC631", finding.Smell.ID);
        Assert.Equal("cref:System.ArgumentException", finding.Context.TargetName);
    }

    [Fact]
    public void SupportingGrowthChangesTheViewAndOutcomeWithoutChangingTheSourceRootOccurrence()
    {
        var dependency = new AdmissionCharacterization.Fixture([("/C3/Support.cs", "public static class Api { public static void Run() {} }")], assemblyName: "C3.Support");
        var fixture = new AdmissionCharacterization.Fixture([("/C3/Consumer.cs", "class Entry {\n" + Doc + Tag + "static void Root() { Api.Run(); } }")],
            MetadataReferences.Default.Append(dependency.Compilation.ToMetadataReference()), assemblyName: "C3.Consumer");
        var run = fixture.Session();
        var root = OneRoot(fixture);
        var view = run.Environment.GetAnalysisScopes();
        var missing = Capture("support-before", fixture, root, run.Context, run.Environment, run.Session, Started());
        Assert.False(missing.Result.ProfileOutcome!.Complete);
        run.Context.RegisterSupportingSource(dependency.Compilation);
        Assert.NotSame(view, run.Environment.GetAnalysisScopes());
        var available = Capture("support-after", fixture, root, run.Context, run.Environment, run.Session, Started());
        Assert.True(available.Result.ProfileOutcome!.Complete);
        Assert.Equal(missing.Result.ProfileOutcome.Root, available.Result.ProfileOutcome.Root);
        Assert.NotEqual(missing.Result.ProfileOutcome.ScopeDigest, available.Result.ProfileOutcome.ScopeDigest);
        Assert.Equal("DOC631", Assert.Single(missing.Findings).Smell.ID);
        Assert.Equal("DOC632", Assert.Single(available.Findings).Smell.ID);
    }

    [Fact]
    public void EqualReasonWitnessesDoNotMergeDifferentConstructedInvocationContexts()
    {
        var fixture = Fixture(
            ("Api", "static class Api<T> { public static extern void Run(); }"),
            ("Int", "partial class Entry {\n" + Doc + Tag + "static void A() { Api<int>.Run(); } }"),
            ("String", "partial class Entry {\n" + Doc + Tag + "static void B() { Api<string>.Run(); } }"));
        var run = fixture.Session();
        var a = Capture("context-int", fixture, fixture.Root("A"), run.Context, run.Environment, run.Session, Started());
        var b = Capture("context-string", fixture, fixture.Root("B"), run.Context, run.Environment, run.Session, Started());
        Assert.Equal(a.Result.ProfileOutcome!.Reasons, b.Result.ProfileOutcome!.Reasons);
        Assert.NotEqual(a.Result.ProfileOutcome.Root, b.Result.ProfileOutcome.Root);
        Assert.NotEqual(a.Result.ProfileOutcome.InventoryDigest, b.Result.ProfileOutcome.InventoryDigest);
        var invocations = new List<string>();
        foreach (var root in new[] { a, b })
        {
            var recorder = new AdmissionObservationRecorder();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(root.Symbol!, run.Environment, recorder,
                new ExceptionFlowSummaryFragment(), new ExceptionFlowCallContext(root.Symbol!)));
            var target = Assert.Single(recorder.Keys);
            invocations.Add(CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(
                target.CallContext!.InvocationSymbol!)));
        }
        Assert.NotEqual(invocations[0], invocations[1]);
        output.WriteLine("C3_CONTEXT " + JsonSerializer.Serialize(new {
            A = a.Result.ProfileOutcome.Root, B = b.Result.ProfileOutcome.Root, Invocations = invocations,
            SharedDefinitionReasons = a.Result.ProfileOutcome.Reasons,
            Evidence = "Separate local-producer observations; no context folding in the query results" }));
    }

    [Fact]
    public void SameDisplayNameOverloadsKeepDifferentCanonicalRootsAndExactFindingAnchors()
    {
        var fixture = Fixture(
            ("Int", "partial class Entry {\n" + Doc + Tag + "static void Root(int value) {} }"),
            ("String", "partial class Entry {\n" + Doc + Tag + "static void Root(string value) {} }"));
        var run = fixture.Session();
        var captures = fixture.Compilation.SyntaxTrees.Select(tree => Capture("overload", fixture,
            Assert.Single(Documented(tree)), run.Context, run.Environment, run.Session, Started())).ToArray();
        Assert.NotEqual(captures[0].Result.ProfileOutcome!.Root, captures[1].Result.ProfileOutcome!.Root);
        Assert.Equal(captures[0].Findings.Single().Context.SymbolName, captures[1].Findings.Single().Context.SymbolName);
        Assert.All(captures, capture => Assert.Equal("DOC632", Assert.Single(capture.Findings).Smell.ID));
        Assert.NotEqual(FindingKey(captures[0].Findings.Single()), FindingKey(captures[1].Findings.Single()));
    }

    private Observation Capture(string scenario, AdmissionCharacterization.Fixture fixture, MemberDeclarationSyntax root,
        ProjectClosureSemanticContext context, ExceptionFlowSemanticEnvironment environment,
        ExceptionFlowSummaryAnalysisSession session, AnalysisCoverage global)
    {
        var view = environment.GetAnalysisScopes();
        Assert.True(environment.TryGetSemanticModel(root.SyntaxTree, out var model));
        Assert.Same(fixture.Compilation, model.Compilation);
        var symbol = model.GetDeclaredSymbol(root);
        var before = session.Analyze(root);
        var local = Started();
        var findings = Detect(fixture, root.SyntaxTree, context, session, local);
        var after = session.Analyze(root);
        Assert.Same(view, environment.GetAnalysisScopes());
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(before)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(after)));
        var expected = Started();
        AnalysisCoverageObserver.Observe(expected, ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(root, environment), summary: false);
        AnalysisCoverageObserver.Observe(expected, after, summary: true);
        Assert.Equal(expected.Report().ReasonCodes, local.Report().ReasonCodes);
        Assert.Equal(expected.Report().Completion, local.Report().Completion);
        global.Merge(local);
        var tags = XmlDocUtils.TryGetDocComment(root)!.DescendantNodes().OfType<XmlCrefAttributeSyntax>().Select(cref => new {
            Cref = cref, Type = model.GetSymbolInfo(cref.Cref).Symbol as INamedTypeSymbol
        }).ToArray();
        foreach (var finding in findings)
        {
            int position = root.SyntaxTree.GetText().Lines.GetPosition(new LinePosition(finding.Line - 1, finding.Column - 1));
            Assert.True(root.FullSpan.Contains(position));
            Assert.Equal(root.SyntaxTree.FilePath, finding.FilePath);
            var tag = Assert.Single(tags, candidate => candidate.Cref.FullSpan.Contains(position));
            Assert.NotNull(tag.Type);
            Assert.Contains(finding.Smell.ID, new[] { "DOC631", "DOC632" });
        }
        output.WriteLine("C3_ROOT " + JsonSerializer.Serialize(new {
            Scenario = scenario, Evidence = "Paired same-view queries around the real detector; not an intercepted single-query trace",
            RootOccurrence = new { File = root.SyntaxTree.FilePath, root.SpanStart, root.Span.Length, Kind = root.Kind().ToString() },
            QueryOwner = symbol is null ? null : new { Identity = ExceptionFlowFiniteOrbitAdmission.BodyId(symbol), Assembly = symbol.ContainingAssembly?.Identity.ToString() },
            View = new { after.ProfileOutcome!.ScopeDigest, CompilationOwner = model.Compilation.Assembly.Identity.ToString(), ExactRootModelChecked = true },
            after.ProfileOutcome, Flow = AdmissionCharacterization.Flow(after), RootCoverage = local.Report(), GlobalAfter = global.Report(),
            Tags = tags.Select(tag => new { Raw = tag.Cref.Cref.ToString(), tag.Cref.SpanStart,
                CanonicalException = tag.Type is null ? null : AdmissionCharacterization.TypeKey(tag.Type) }),
            Findings = findings, RuntimeOriginSite = (string?)null }));
        return new(root, symbol, after, local.Report(), findings);
    }

    private static void AssertNoBodyReason(Observation capture, ISymbol definition)
        => Assert.Contains(new ExceptionFlowFiniteOrbitProfile.Reason(Target,
            ExceptionFlowFiniteOrbitAdmission.BodyId(definition) + " / executable body unavailable"), capture.Result.ProfileOutcome!.Reasons);
    private static (bool HasBody, ExceptionFlowSummaryFragment Fragment) Producer(ISymbol symbol, ExceptionFlowSemanticEnvironment environment)
    {
        // A separate local-producer probe, never injected into a session or substituted for its result.
        var fragment = new ExceptionFlowSummaryFragment();
        return (ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(symbol, environment, new AdmissionObservationRecorder(),
            fragment, new ExceptionFlowCallContext(symbol)), fragment);
    }
    private static AdmissionCharacterization.Fixture Fixture(params (string Name, string Source)[] sources)
        => new(sources.Select(source => ("/C3/" + source.Name + ".cs", source.Source)), assemblyName: "C3.Attribution");
    private static IEnumerable<MemberDeclarationSyntax> Documented(SyntaxTree tree)
        => tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>().Where(member => XmlDocUtils.TryGetDocComment(member) is not null);
    private static MemberDeclarationSyntax OneRoot(AdmissionCharacterization.Fixture fixture)
        => Assert.Single(fixture.Compilation.SyntaxTrees.SelectMany(Documented));
    private static AnalysisCoverage Started()
    {
        var coverage = new AnalysisCoverage(); coverage.Begin("C3", ExceptionAnalysisMode.SolutionTransitive); return coverage;
    }
    private static Finding[] Detect(AdmissionCharacterization.Fixture fixture, SyntaxTree tree,
        ProjectClosureSemanticContext context, ExceptionFlowSummaryAnalysisSession session, AnalysisCoverage coverage)
    {
        Assert.Single(Documented(tree));
        AnalysisCoverageObserver.ObserveCompilation(coverage, fixture.Compilation, session);
        return XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath, fixture.Compilation.GetSemanticModel(tree), context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, session, coverage).ToArray();
    }
    private static string FindingKey(Finding finding)
        => JsonSerializer.Serialize(new { finding.Smell.ID, finding.FilePath, finding.Line, finding.Column, finding.Context, finding.Message });
    private sealed record Observation(MemberDeclarationSyntax Root, ISymbol? Symbol, ExceptionFlowAnalysisResult Result,
        AnalysisCoverageReport RootCoverage, Finding[] Findings);
}
