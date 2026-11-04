using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Independent tag/query contracts, including native type-root eligibility and constructor controls.</summary>
public sealed class ExceptionFlowTypeRootApplicabilityContractTests(ITestOutputHelper output)
{
    private const string Doc = "/// <summary>Root.</summary>\n";
    private const string Tag = "/// <exception cref=\"System.InvalidOperationException\">Failure.</exception>\n";
    private const string Target = "TargetOrAnalysisCoverageIncomplete";

    public static IEnumerable<object[]> DeclarationCases()
    {
        var shapes = new (string Name, string Declaration, string Placement, string Query)[] {
            ("class", "class Entry {}", "DOC680", "target"),
            ("generic-class", "class Entry<T> {}", "DOC680", "open"),
            ("nested-generic-class", "class Outer<T> { @DOC@ class Entry {} }", "DOC680", "open"),
            ("struct", "struct Entry {}", "DOC680", "target"),
            ("record-class", "record class Entry;", "DOC680", "target"),
            ("record-struct", "record struct Entry;", "DOC680", "target"),
            ("interface", "interface Entry {}", "DOC680", "target"),
            ("enum", "enum Entry { Value }", "DOC680", "target"),
            ("delegate", "delegate void Entry();", "DOC680", "target"),
            ("namespace", "namespace Entry { class Item {} }", "DOC680", "target"),
            ("enum-member", "enum Entry { @DOC@ Value }", "DOC680", "target"),
            ("primary-class", "class Entry(int value) {}", "DOC680", "target"),
            ("primary-struct", "struct Entry(int value) {}", "DOC680", "target"),
            ("primary-record-class", "record class Entry(int value);", "DOC680", "target"),
            ("primary-record-struct", "record struct Entry(int value);", "DOC680", "target"),
            ("field", "class Host { @DOC@ int value; }", "DOC680", "binding"),
            ("method", "class Host { @DOC@ void Run() {} }", "", "complete"),
            ("constructor", "class Host { @DOC@ public Host() {} }", "", "complete"),
            ("static-constructor", "class Host { @DOC@ static Host() {} }", "", "complete"),
            ("property-body", "class Host { @DOC@ int Value { get => 0; set {} } }", "DOC143", "complete"),
            ("property-auto", "class Host { @DOC@ int Value { get; set; } }", "DOC680", "target"),
            ("indexer", "class Host { @DOC@ int this[int index] => 0; }", "DOC143", "complete"),
            ("event", "class Host { @DOC@ event System.Action Changed { add {} remove {} } }", "DOC143", "complete"),
            ("event-field", "class Host { @DOC@ event System.Action? Changed; }", "DOC680", "binding"),
            ("operator", "class Host { @DOC@ public static int operator +(Host a, Host b) => 0; }", "", "complete"),
            ("conversion", "class Host { @DOC@ public static explicit operator int(Host value) => 0; }", "", "complete")
        };
        foreach (var shape in shapes)
        foreach (bool tagged in new[] { false, true })
            yield return [shape.Name, shape.Declaration, shape.Placement, shape.Query, tagged];
    }

    [Theory]
    [MemberData(nameof(DeclarationCases))]
    public void DeclarationMatrixKeepsPlacementCrefFlowAndCoverageAsSeparateProducts(
        string name, string declaration, string placement, string query, bool tagged)
    {
        string prefix = Doc + (name == "generic-class" ? "/// <typeparam name=\"T\">Type.</typeparam>\n" : "") + (name.StartsWith("primary-", StringComparison.Ordinal)
            ? "/// <param name=\"value\">Value.</param>\n" : "") + (tagged ? Tag : "");
        var fixture = Fixture(declaration.Contains("@DOC@", StringComparison.Ordinal)
            ? declaration.Replace("@DOC@", "\n" + prefix, StringComparison.Ordinal) : prefix + declaration);
        var root = Assert.Single(Documented(fixture));
        var run = fixture.Session();
        var flow = run.Session.Analyze(root);
        bool complete = query == "complete";
        Assert.Equal(complete, flow.ProfileOutcome!.Complete);
        string[] categories = flow.ProfileOutcome.Reasons.Select(r => r.Category).Distinct().Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(query switch {
            "target" => new[] { Target },
            "open" => new[] { "ScopeMapNotClosed", Target },
            "binding" => new[] { "OwnerBindingFailure", "ScopeMapNotClosed" },
            _ => Array.Empty<string>()
        }, categories);
        var products = Pipeline(fixture, run.Context, run.Session);
        var placementFindings = products.Syntax.SelectMany(p => p.Findings).Where(f => f.Smell.ID is "DOC143" or "DOC680").ToArray();
        if (tagged && placement.Length != 0) Assert.Equal(placement, Assert.Single(placementFindings).Smell.ID);
        else Assert.Empty(placementFindings);
        bool typeQuerySkipped = name is "class" or "generic-class" or "nested-generic-class" or "struct"
            or "record-class" or "record-struct" or "interface" or "enum" or "delegate";
        if (tagged && !typeQuerySkipped) Assert.Equal(complete ? "DOC632" : "DOC631", Assert.Single(products.Semantic).Smell.ID);
        else Assert.Empty(products.Semantic);
        Assert.Equal(complete || typeQuerySkipped ? "Complete" : "Incomplete", products.Coverage.Completion);
        Assert.Equal("Succeeded", products.Coverage.Execution);
        Assert.DoesNotContain(products.Syntax.SelectMany(p => p.Findings), f => f.Smell.ID == "DOC600");
        if (name.StartsWith("primary-", StringComparison.Ordinal))
            Assert.DoesNotContain(products.Syntax.SelectMany(p => p.Findings), f => f.TagName == "param");
        Write("matrix-" + name + "-" + tagged, root, flow, products, fixture);
    }

    [Theory]
    [InlineData("valid", "System.InvalidOperationException", "")]
    [InlineData("unresolved", "MissingExceptionType", "DOC660")]
    [InlineData("not-exception", "System.String", "DOC670")]
    [InlineData("missing", "", "")]
    public void InvalidTypeTagsStillReceiveTheIndependentSyntaxAndSemanticDiagnostics(
        string name, string cref, string semanticRule)
    {
        string tag = cref.Length == 0 ? "/// <exception>Failure.</exception>\n"
            : "/// <exception cref=\"" + cref + "\">Failure.</exception>\n";
        var fixture = Fixture(Doc + tag + "class Entry {}");
        var run = fixture.Session();
        var products = Pipeline(fixture, run.Context, run.Session);
        var syntaxException = products.Syntax.Single(p => p.Detector == nameof(XmlDocExceptionDetector));
        Assert.Equal("DOC680", Assert.Single(syntaxException.Findings).Smell.ID);
        Assert.DoesNotContain(products.Syntax.SelectMany(p => p.Findings), f => f.Smell.ID == "DOC143");
        if (name == "missing") Assert.Contains(products.Syntax.SelectMany(p => p.Findings), f => f.Smell.ID == "DOC600");
        if (semanticRule.Length != 0) Assert.Equal(semanticRule, Assert.Single(products.Semantic).Smell.ID);
        else Assert.Empty(products.Semantic);
        Assert.DoesNotContain(products.Semantic, f => f.Smell.ID == "DOC632");
        Assert.Empty(products.Coverage.ReasonCodes);
        Assert.Equal("Complete", products.Coverage.Completion);
        Write("cref-" + name, Assert.Single(Documented(fixture)), run.Session.Analyze(Assert.Single(Documented(fixture))), products, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MethodsWithoutExceptionTagsRetainDirectAndTransitiveMissingDocumentation(bool transitive)
    {
        var fixture = Fixture("class Entry {\n" + Doc + "public static void Run() { " +
            (transitive ? "Fail();" : "throw new System.InvalidOperationException();") +
            " } static void Fail() { throw new System.InvalidOperationException(); } }");
        var run = fixture.Session();
        var products = Pipeline(fixture, run.Context, run.Session);
        Assert.True(run.Session.Analyze(fixture.Root("Run")).ProfileOutcome!.Complete);
        Assert.Equal(transitive ? "DOC611" : "DOC610", Assert.Single(products.Semantic).Smell.ID);
        Assert.Equal("Complete", products.Coverage.Completion);
        Write("missing-doc-" + transitive, fixture.Root("Run"), run.Session.Analyze(fixture.Root("Run")), products, fixture);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExplicitAndImplicitInstanceConstructorFlowsDoNotDependOnASeparateTypeQuery(bool implicitConstructor, bool property)
    {
        string initializer = property ? "public int Value { get; } = Fail();" : "int value = Fail();";
        var fixture = Fixture(Doc + "class Item { " + initializer +
            " static int Fail() => throw new System.InvalidOperationException();\n" +
            (implicitConstructor ? "" : Doc + Tag + "public Item() {}\n") +
            "}\nclass Entry {\n" + Doc + "public static void Run() { _ = new Item(); } }");
        var run = fixture.Session();
        var caller = fixture.Root("Run");
        var type = Documented(fixture).OfType<ClassDeclarationSyntax>().Single();
        var before = run.Session.Analyze(caller);
        Assert.True(before.ProfileOutcome!.Complete);
        Assert.Equal("InvalidOperationException", Assert.Single(before.ThrownExceptions).Name);
        Assert.False(run.Session.Analyze(type).ProfileOutcome!.Complete);
        AssertFullFlowEqual(before, run.Session.Analyze(caller));
        var reverse = fixture.Session();
        reverse.Session.Analyze(type);
        AssertFullFlowEqual(before, reverse.Session.Analyze(caller));
        var constructor = Assert.Single(fixture.Compilation.GetTypeByMetadataName("Item")!.InstanceConstructors);
        Assert.Equal(implicitConstructor, constructor.IsImplicitlyDeclared);
        var body = Probe(constructor, run.Environment);
        Assert.True(body.HasBody);
        Assert.Single(body.Fragment.CallEdges, edge => edge.Target.Symbol.Name == "Fail");
        var products = Pipeline(fixture, run.Context, run.Session);
        Assert.Equal("DOC611", Assert.Single(products.Semantic).Smell.ID);
        Assert.Equal("Run", products.Semantic.Single().Context.SymbolName);
        Assert.Empty(products.Coverage.ReasonCodes);
        Assert.Equal("Complete", products.Coverage.Completion);
        if (!implicitConstructor)
        {
            var root = Assert.Single(Documented(fixture).OfType<ConstructorDeclarationSyntax>());
            AssertFullFlowEqual(run.Session.Analyze(root), reverse.Session.Analyze(root));
        }
        Write("instance-" + implicitConstructor + "-property-" + property, caller, before, products, fixture);
        WriteConstructor(constructor, body);
    }

    [Theory]
    [InlineData("class", false)]
    [InlineData("class", true)]
    [InlineData("struct", false)]
    [InlineData("struct", true)]
    [InlineData("record class", false)]
    [InlineData("record class", true)]
    [InlineData("record struct", false)]
    [InlineData("record struct", true)]
    public void PrimaryConstructorSymbolsAndCurrentOutcomesRemainDistinctFromTheirTypeRoot(string kind, bool property)
    {
        var fixture = Fixture(Doc + "/// <param name=\"value\">Value.</param>\n" + kind +
            " Item(int value) { " + (property ? "public int Value { get; } = Fail(value);" : "int field = Fail(value);") +
            " static int Fail(int value) => throw new System.InvalidOperationException(); }\n" +
            "class Entry {\n" + Doc + Tag + "public static void Run() { _ = new Item(1); } }");
        var run = fixture.Session();
        var typeRoot = Assert.Single(Documented(fixture).OfType<TypeDeclarationSyntax>());
        var type = fixture.Compilation.GetTypeByMetadataName("Item")!;
        var constructor = Assert.Single(type.InstanceConstructors, c => c.Parameters.Length == 1 &&
            c.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        Assert.IsAssignableFrom<INamedTypeSymbol>(fixture.Compilation.GetSemanticModel(typeRoot.SyntaxTree).GetDeclaredSymbol(typeRoot));
        Assert.NotEqual(ExceptionFlowFiniteOrbitAdmission.BodyId(type), ExceptionFlowFiniteOrbitAdmission.BodyId(constructor));
        var before = run.Session.Analyze(fixture.Root("Run"));
        Assert.False(run.Session.Analyze(typeRoot).ProfileOutcome!.Complete);
        AssertFullFlowEqual(before, run.Session.Analyze(fixture.Root("Run")));
        var reverse = fixture.Session();
        reverse.Session.Analyze(typeRoot);
        AssertFullFlowEqual(before, reverse.Session.Analyze(fixture.Root("Run")));
        var body = Probe(constructor, run.Environment);
        // Characterize the present constructor branch, not the C# execution promise.
        Assert.False(constructor.IsImplicitlyDeclared);
        Assert.False(body.HasBody);
        Assert.False(before.ProfileOutcome!.Complete);
        Assert.Contains(before.ProfileOutcome.Reasons, r => r.Category == Target && r.Witness ==
            ExceptionFlowFiniteOrbitAdmission.BodyId(constructor) + " / executable body unavailable");
        var products = Pipeline(fixture, run.Context, run.Session);
        Assert.Equal("DOC631", Assert.Single(products.Semantic).Smell.ID);
        Assert.DoesNotContain(products.Syntax.SelectMany(p => p.Findings), f => f.TagName == "param");
        Write("primary-" + kind + "-property-" + property, fixture.Root("Run"), before, products, fixture);
        WriteConstructor(constructor, body);
    }

    [Theory]
    [InlineData("body", true, true)]
    [InlineData("field-only", true, false)]
    [InlineData("field-and-body", true, true)]
    [InlineData("implicit-field", false, false)]
    public void StaticConstructorAndStaticInitializerEvidenceAreRecordedWithoutInventingSupport(string kind, bool explicitConstructor, bool bodyThrows)
    {
        bool field = kind != "body";
        var fixture = Fixture(Doc + "class Item { " + (field ? "public static int Value = Fail();" : "public static int Value;") +
            " static int Fail() => throw new System.InvalidOperationException();\n" +
            (explicitConstructor ? Doc + Tag + "static Item() { " + (bodyThrows ? "throw new System.InvalidOperationException();" : "") + " }\n" : "") +
            "}\nclass Entry {\n" + Doc + "public static void Run() { _ = Item.Value; } }");
        var run = fixture.Session();
        var typeRoot = Assert.Single(Documented(fixture).OfType<ClassDeclarationSyntax>());
        var caller = fixture.Root("Run");
        var before = run.Session.Analyze(caller);
        run.Session.Analyze(typeRoot);
        AssertFullFlowEqual(before, run.Session.Analyze(caller));
        var reverse = fixture.Session();
        reverse.Session.Analyze(typeRoot);
        AssertFullFlowEqual(before, reverse.Session.Analyze(caller));
        var constructor = Assert.Single(fixture.Compilation.GetTypeByMetadataName("Item")!.StaticConstructors);
        var body = Probe(constructor, run.Environment);
        Assert.Equal(explicitConstructor, body.HasBody);
        if (explicitConstructor)
        {
            var root = Assert.Single(Documented(fixture).OfType<ConstructorDeclarationSyntax>());
            var flow = run.Session.Analyze(root);
            Assert.Equal(!field, flow.ProfileOutcome!.Complete);
            Assert.Equal(bodyThrows && !field, flow.ThrownExceptions.Any(t => t.Name == "InvalidOperationException"));
            Assert.Equal(bodyThrows, body.Fragment.Sources.Any(s => s.ExceptionType.Name == "InvalidOperationException"));
            AssertFullFlowEqual(flow, reverse.Session.Analyze(root));
            Write("static-root-" + kind, root, flow, Pipeline(fixture, run.Context, run.Session), fixture);
        }
        Assert.False(before.ProfileOutcome!.Complete);
        Assert.Empty(before.ThrownExceptions);
        var products = Pipeline(fixture, run.Context, run.Session);
        if (explicitConstructor && field) Assert.Equal("DOC631", Assert.Single(products.Semantic).Smell.ID);
        else Assert.Empty(products.Semantic);
        Write("static-caller-" + kind, caller, before, products, fixture);
        WriteConstructor(constructor, body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedTypeConstructorAndMethodRootsKeepTheirOwnDiagnostics(bool reverse)
    {
        var fixture = Fixture(Doc + "class Item {\n" + Doc + Tag + "public Item() { throw new System.InvalidOperationException(); } }\n" +
            "class Entry {\n" + Doc + "public static void Run() { _ = new Item(); }\n" + Doc + Tag + "public static void Empty() {} }");
        var run = fixture.Session();
        var roots = Documented(fixture).ToArray();
        foreach (var root in reverse ? roots.Reverse() : roots)
        {
            var result = run.Session.Analyze(root);
            Assert.Equal(root is not TypeDeclarationSyntax, result.ProfileOutcome!.Complete);
        }
        var products = Pipeline(fixture, run.Context, run.Session);
        Assert.Equal(["DOC611", "DOC632"], products.Semantic.Select(f => f.Smell.ID).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(products.Semantic, f => f.Smell.ID == "DOC631");
        Assert.Empty(products.Coverage.ReasonCodes);
        Assert.Equal("Complete", products.Coverage.Completion);
        foreach (var root in roots) Write("mixed-" + reverse, root, run.Session.Analyze(root), products, fixture);
    }

    [Fact]
    public void IndependentTypeDocumentationRulesAreNotExceptionFlowResults()
    {
        var fixture = Fixture("/// <summary></summary>\n/// <param name=\"ghost\">Unknown.</param>\nclass Entry(int value) {}");
        var run = fixture.Session();
        var products = Pipeline(fixture, run.Context, run.Session);
        Assert.Contains(products.Syntax.SelectMany(p => p.Findings), f => f.Smell.ID == "DOC210");
        Assert.Contains(products.Syntax.SelectMany(p => p.Findings), f => f.Smell.ID == "DOC310");
        Assert.Contains(products.Syntax.SelectMany(p => p.Findings), f => f.Context.TargetName == "ghost");
        Assert.Empty(products.Semantic);
        Assert.Equal("Incomplete", products.Coverage.Completion);
        Write("independent-xml", Assert.Single(Documented(fixture)), run.Session.Analyze(Assert.Single(Documented(fixture))), products, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualCallableDetectorFindingsAreIdenticalBeforeAndAfterAnIndependentTypeQuery(bool implicitConstructor)
    {
        var sources = new List<(string Path, string Text)> {
            ("/C3B/Type.cs", Doc + "partial class Item { int field = Fail(); static int Fail() => throw new System.InvalidOperationException(); }"),
            ("/C3B/Caller.cs", "class Entry {\n" + Doc + "public static void Run() { _ = new Item(); } }")
        };
        if (!implicitConstructor)
            sources.Add(("/C3B/Constructor.cs", "partial class Item {\n" + Doc + Tag + "public Item() {} }"));
        var fixture = new AdmissionCharacterization.Fixture(sources, assemblyName: "C3B.TreeParity");
        var run = fixture.Session();
        var caller = fixture.Root("Run");
        var type = Assert.Single(Documented(fixture).OfType<ClassDeclarationSyntax>());
        (Finding[] Findings, AnalysisCoverageReport Coverage) DetectTree(MemberDeclarationSyntax root,
            ProjectClosureSemanticContext context, ExceptionFlowSummaryAnalysisSession session)
        {
            var coverage = new AnalysisCoverage(); coverage.Begin("C3B.TreeParity", ExceptionAnalysisMode.SolutionTransitive);
            var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
                fixture.Compilation.GetSemanticModel(root.SyntaxTree), context,
                new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, session, coverage);
            return (findings.ToArray(), coverage.Report());
        }
        var before = DetectTree(caller, run.Context, run.Session);
        Assert.Equal("DOC611", Assert.Single(before.Findings).Smell.ID);
        var typeFlow = run.Session.Analyze(type);
        Assert.False(typeFlow.ProfileOutcome!.Complete);
        var after = DetectTree(caller, run.Context, run.Session);
        Assert.Equal(JsonSerializer.Serialize(before.Coverage), JsonSerializer.Serialize(after.Coverage));
        Assert.Equal(JsonSerializer.Serialize(before.Findings), JsonSerializer.Serialize(after.Findings));
        Assert.Equal("Complete", before.Coverage.Completion);
        Assert.Equal("Complete", after.Coverage.Completion);
        var reverse = fixture.Session();
        reverse.Session.Analyze(type);
        var otherOrder = DetectTree(caller, reverse.Context, reverse.Session);
        Assert.Equal(JsonSerializer.Serialize(before.Findings), JsonSerializer.Serialize(otherOrder.Findings));
        if (!implicitConstructor)
        {
            var constructor = Assert.Single(Documented(fixture).OfType<ConstructorDeclarationSyntax>());
            Assert.Empty(DetectTree(constructor, run.Context, run.Session).Findings);
            Assert.Empty(DetectTree(constructor, reverse.Context, reverse.Session).Findings);
        }
        output.WriteLine("C3B_TREE_PARITY " + JsonSerializer.Serialize(new {
            implicitConstructor, Before = before.Findings, After = after.Findings, OtherOrder = otherOrder.Findings,
            BeforeCoverage = before.Coverage, AfterCoverage = after.Coverage,
            TypeFlow = AdmissionCharacterization.Flow(typeFlow),
            Evidence = "Actual single-documented-root detector calls in the same compilation and view" }));
    }

    private static AdmissionCharacterization.Fixture Fixture(string source)
        => new([("/C3B/Fixture.cs", source)], assemblyName: "C3B.Contract");
    private static IEnumerable<MemberDeclarationSyntax> Documented(AdmissionCharacterization.Fixture fixture)
        => fixture.Compilation.SyntaxTrees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            .Where(m => XmlDocUtils.TryGetDocComment(m) is not null);
    private static Products Pipeline(AdmissionCharacterization.Fixture fixture, ProjectClosureSemanticContext context,
        ExceptionFlowSummaryAnalysisSession session)
    {
        var tree = fixture.Compilation.SyntaxTrees.Single();
        var options = new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive };
        var syntax = new List<DetectorProduct> { new(nameof(XmlDocBasicDetector), XmlDocBasicDetector.FindBasicSmells(tree, tree.FilePath, options).ToArray()) };
        foreach (var detector in XmlDocDetectorCatalog.SyntaxDetectors)
            syntax.Add(new(detector.Method.DeclaringType!.Name, detector(tree, tree.FilePath).ToArray()));
        syntax.Add(new(nameof(XmlDocValueDetector), XmlDocValueDetector.FindValueSmells(tree, tree.FilePath, options).ToArray()));
        var coverage = new AnalysisCoverage(); coverage.Begin("C3B", ExceptionAnalysisMode.SolutionTransitive);
        AnalysisCoverageObserver.ObserveCompilation(coverage, fixture.Compilation, session);
        var semantic = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath,
            fixture.Compilation.GetSemanticModel(tree), context, options, session, coverage).ToArray();
        return new(syntax.ToArray(), semantic, coverage.Report());
    }
    private static void AssertFullFlowEqual(ExceptionFlowAnalysisResult expected, ExceptionFlowAnalysisResult actual)
        => Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(expected)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(actual)));
    private static (bool HasBody, ExceptionFlowSummaryFragment Fragment) Probe(ISymbol symbol, ExceptionFlowSemanticEnvironment environment)
    {
        var fragment = new ExceptionFlowSummaryFragment();
        return (ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(symbol, environment, new AdmissionObservationRecorder(), fragment,
            new ExceptionFlowCallContext(symbol)), fragment);
    }
    private void Write(string scenario, MemberDeclarationSyntax root, ExceptionFlowAnalysisResult flow, Products products,
        AdmissionCharacterization.Fixture fixture)
    {
        var symbol = fixture.Compilation.GetSemanticModel(root.SyntaxTree).GetDeclaredSymbol(root);
        output.WriteLine("C3B_ROOT " + JsonSerializer.Serialize(new {
            Scenario = scenario, Kind = root.Kind().ToString(), root.SpanStart, root.Span.Length,
            SymbolKind = symbol?.Kind.ToString(), Owner = symbol?.ContainingAssembly?.Identity.ToString(),
            DocumentedRootCount = Documented(fixture).Count(),
            DiagnosticScope = "Syntax/Semantic/Coverage are whole-fixture products; Flow belongs only to this displayed root",
            TagAllowedByMatrix = AllowedTagMatrix.IsTagAllowed(root, "exception"), SyntaxHasBody = SyntaxUtils.HasExecutableBody(root),
            Flow = AdmissionCharacterization.Flow(flow), products.Syntax, products.Semantic, products.Coverage,
            Evidence = "Current behavior; complete labels are not a new claim for unsupported C# initializer semantics" }));
    }
    private void WriteConstructor(IMethodSymbol constructor, (bool HasBody, ExceptionFlowSummaryFragment Fragment) probe)
        => output.WriteLine("C3B_CONSTRUCTOR " + JsonSerializer.Serialize(new {
            Identity = ExceptionFlowFiniteOrbitAdmission.BodyId(constructor), constructor.IsImplicitlyDeclared,
            MethodKind = constructor.MethodKind.ToString(), Symbol = constructor.ToDisplayString(),
            Declarations = constructor.DeclaringSyntaxReferences.Select(s => s.GetSyntax().Kind().ToString()).ToArray(),
            probe.HasBody, Fragment = JsonSerializer.Deserialize<JsonElement>(AdmissionCharacterization.Fragment(probe.Fragment)),
            Evidence = "Separate local-producer probe; not a replacement for session results" }));
    private sealed record DetectorProduct(string Detector, Finding[] Findings);
    private sealed record Products(DetectorProduct[] Syntax, Finding[] Semantic, AnalysisCoverageReport Coverage);
}
