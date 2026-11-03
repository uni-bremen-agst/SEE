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

/// <summary>Current root selection and body evidence, without an applicability filter.</summary>
public sealed class ExceptionFlowRootQueryApplicabilityTests(ITestOutputHelper output)
{
    private const string Doc = "/// <summary>Root.</summary>\n";
    private const string Tag = "/// <exception cref=\"System.InvalidOperationException\">Documented.</exception>\n";
    private const string Target = "TargetOrAnalysisCoverageIncomplete";
    private const string Scope = "ScopeMapNotClosed";
    private const string Owner = "OwnerBindingFailure";

    public static IEnumerable<object?[]> RootKinds()
    {
        yield return Row("class", "class Entry {}", false, Target);
        yield return Row("struct", "struct Entry {}", false, Target);
        yield return Row("record", "record Entry;", false, Target);
        yield return Row("record-struct", "record struct Entry;", false, Target);
        yield return Row("interface", "interface Entry {}", false, Target);
        yield return Row("enum", "enum Entry { Value }", false, Target);
        yield return Row("delegate", "delegate void Entry();", false, Target);
        yield return Row("generic-type", "class Entry<T> {}", false, Scope, Target);
        yield return Row("primary-record", "record Entry(int Value);", false, Target);
        yield return Row("class-with-initializers", "class Entry { int value = Fail(); static int Fail() => throw new System.InvalidOperationException(); }", false, Target);
        yield return Row("nested-type", "class Outer { @DOC@ class Entry {} }", false, Target);
        yield return Row("namespace", "namespace Entry { class Item {} }", false, Target);
        yield return Row("file-namespace", "namespace Entry; class Item {}", false, Target);
        yield return Row("enum-value", "enum Entry { @DOC@ Value }", false, Target);
        yield return Row("field", "class Entry { @DOC@ int value; }", null, Owner, Scope);
        yield return Row("multi-field", "class Entry { @DOC@ int first, second; }", null, Owner, Scope);
        yield return Row("constant", "class Entry { @DOC@ const int Value = 1; }", null, Owner, Scope);
        yield return Row("event-field", "class Entry { @DOC@ event System.Action? Changed; }", null, Owner, Scope);
        yield return Row("method", "class Entry { @DOC@ void Run() { throw new System.InvalidOperationException(); } }", true);
        yield return Row("constructor", "class Entry { @DOC@ public Entry() { throw new System.InvalidOperationException(); } }", true);
        yield return Row("static-constructor", "class Entry { @DOC@ static Entry() { throw new System.InvalidOperationException(); } }", true);
        yield return Row("destructor", "class Entry { @DOC@ ~Entry() { throw new System.InvalidOperationException(); } }", false, Target);
        yield return Row("property", "class Entry { @DOC@ int Value { get { throw new System.InvalidOperationException(); } set { throw new System.ArgumentException(); } } }", true);
        yield return Row("expression-property", "class Entry { @DOC@ int Value => throw new System.InvalidOperationException(); }", true);
        yield return Row("auto-property", "class Entry { @DOC@ int Value { get; set; } }", false, Target);
        yield return Row("init-property", "class Entry { @DOC@ int Value { get => 0; init {} } }", true);
        yield return Row("indexer", "class Entry { @DOC@ int this[int index] { get { throw new System.InvalidOperationException(); } set { throw new System.ArgumentException(); } } }", true);
        yield return Row("event", "class Entry { @DOC@ event System.Action Changed { add { throw new System.InvalidOperationException(); } remove { throw new System.ArgumentException(); } } }", true);
        yield return Row("operator", "class Entry { @DOC@ public static Entry operator +(Entry a, Entry b) => throw new System.InvalidOperationException(); }", true);
        yield return Row("conversion", "class Entry { @DOC@ public static explicit operator int(Entry value) => throw new System.InvalidOperationException(); }", true);
        yield return Row("generic-method", "class Entry { @DOC@ void Run<T>() {} }", true, Scope);
        yield return Row("generic-containing-type-method", "class Entry<T> { @DOC@ void Run() {} }", true, Scope);
        yield return Row("abstract-method", "abstract class Entry { @DOC@ public abstract void Run(); }", false, Target);
        yield return Row("interface-method", "interface Entry { @DOC@ void Run(); }", false, Target);
        yield return Row("default-interface-method", "interface Entry { @DOC@ void Run() { throw new System.InvalidOperationException(); } }", true);
        yield return Row("extern-method", "class Entry { @DOC@ public static extern void Run(); }", false, Target);
        yield return Row("partial-method", "partial class Entry { @DOC@ partial void Run(); partial void Run() {} }", false, Target);
    }

    private static object?[] Row(string name, string declaration, bool? hasBody, params string[] reasons)
        => [name, declaration, hasBody, reasons.Order(StringComparer.Ordinal).ToArray()];

    [Theory]
    [MemberData(nameof(RootKinds))]
    public void DocumentedRootKindsKeepTheirCurrentIdentityBodyAndCoverage(
        string name, string declaration, bool? hasBody, string[] reasons)
    {
        string source = declaration.Contains("@DOC@", StringComparison.Ordinal)
            ? declaration.Replace("@DOC@", "\n" + Doc, StringComparison.Ordinal) : Doc + declaration;
        var fixture = Fixture(source);
        var root = Assert.Single(Documented(fixture.Compilation.SyntaxTrees.Single()));
        var run = fixture.Session();
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(root, run.Environment);
        var symbol = fixture.Compilation.GetSemanticModel(root.SyntaxTree).GetDeclaredSymbol(root);
        if (hasBody is null)
        {
            Assert.Null(symbol);
            Assert.StartsWith("unresolved:/C2/Root.cs:", admission.Outcome.Root);
            Assert.Empty(Definitions(admission));
        }
        else
        {
            Assert.NotNull(symbol);
            Assert.Equal(ExceptionFlowFiniteOrbitAdmission.BodyId(symbol!), admission.Outcome.Root);
            var definition = Definitions(admission)[admission.Outcome.Root];
            Assert.Equal(hasBody.Value, definition.HasBody);
            Assert.Equal(!hasBody.Value, admission.Outcome.Reasons.Any(reason =>
                reason.Witness == admission.Outcome.Root + " / executable body unavailable"));
            if (!hasBody.Value)
            {
                Assert.Empty(definition.Fragment.Sources);
                Assert.Empty(definition.Fragment.CallEdges);
            }
        }
        Assert.Equal(reasons, admission.Outcome.Reasons.Select(reason => reason.Category)
            .Distinct().Order(StringComparer.Ordinal));
        var result = run.Session.Analyze(root);
        Assert.Equal(reasons.Length == 0, result.ProfileOutcome!.Complete);
        Assert.Equal(admission.Outcome.Details, result.ProfileOutcome.Details);
        Assert.Equal(reasons.Length != 0, result.HasUncertainPaths);
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Equal("Succeeded", detected.Coverage.Execution);
        Assert.Equal(reasons.Length == 0 ? "Complete" : "Incomplete", detected.Coverage.Completion);
        Assert.Equal(PublicReasons(reasons), detected.Coverage.ReasonCodes);
        Assert.DoesNotContain(detected.Findings, finding => finding.Smell.ID is "DOC631" or "DOC632");
        Write(name, root, symbol, admission, result, detected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypeNoBodyCanContributeCoverageWithOrWithoutAnExceptionTag(bool withTag)
    {
        var fixture = Fixture("/// <summary></summary>\n" + (withTag ? Tag : "") + "class Entry {}");
        var run = fixture.Session();
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Equal(["OutsideProfile", "TargetCoverageIncomplete"], detected.Coverage.ReasonCodes);
        Assert.Equal(withTag ? 1 : 0, detected.Findings.Count(finding => finding.Smell.ID == "DOC631"));
        Assert.DoesNotContain(detected.Findings, finding => finding.Smell.ID == "DOC632");
        // Ordinary XML rules still process the documented type independently of exception flow.
        Assert.Contains(XmlDocBasicDetector.FindBasicSmells(fixture.Compilation.SyntaxTrees.Single(), "/C2/Root.cs"),
            finding => finding.Smell.ID == "DOC210");
        output.WriteLine("C2_TAG " + JsonSerializer.Serialize(new { withTag, detected.Coverage, detected.Findings }));
    }

    [Fact]
    public void AccessorDocumentationDoesNotCreateIndependentMemberRoots()
    {
        var fixture = Fixture("""
            class Entry {
                int Value {
                    /// <summary>Getter.</summary>
                    get => 0;
                    /// <summary>Setter.</summary>
                    set {}
                }
                event System.Action Changed {
                    /// <summary>Add.</summary>
                    add {}
                    /// <summary>Remove.</summary>
                    remove {}
                }
            }
            """);
        var tree = fixture.Compilation.SyntaxTrees.Single();
        Assert.Empty(Documented(tree));
        Assert.Equal(4, tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>()
            .Count(accessor => XmlDocUtils.TryGetDocComment(accessor) is not null));
        var run = fixture.Session();
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Empty(detected.Findings);
        Assert.Empty(detected.Coverage.ReasonCodes);
        Assert.Equal(0, AdmissionCharacterization.Field<ExceptionFlowSummaryGraph>(run.Session, "graph").Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedTypeAndMethodRootsPreserveLocalFindingsInEitherOrder(bool typeFirst)
    {
        const string type = "/// <summary>Container.</summary>\npartial class Entry {}\n";
        const string method = """
            partial class Entry {
                /// <summary>Empty executable body.</summary>
                /// <exception cref="System.InvalidOperationException">Unused.</exception>
                public static void Run() {}
            }
            """;
        var fixture = Fixture(typeFirst ? type + method : method + type);
        var run = fixture.Session();
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Equal("Incomplete", detected.Coverage.Completion);
        Assert.Equal(["OutsideProfile", "TargetCoverageIncomplete"], detected.Coverage.ReasonCodes);
        Assert.Single(detected.Findings, finding => finding.Smell.ID == "DOC632");
        Assert.DoesNotContain(detected.Findings, finding => finding.Smell.ID == "DOC631");
        Assert.True(run.Session.Analyze(fixture.Root("Run")).ProfileOutcome!.Complete);
        var onlyMethod = Fixture(method);
        var onlyRun = onlyMethod.Session();
        var baseline = Detect(onlyMethod, onlyRun.Context, onlyRun.Session);
        Assert.Equal("Complete", baseline.Coverage.Completion);
        Assert.Equal(baseline.Findings.Select(f => (f.Smell.ID, f.TagName, f.Context.SymbolName)),
            detected.Findings.Select(f => (f.Smell.ID, f.TagName, f.Context.SymbolName)));
        output.WriteLine("C2_MIXED " + JsonSerializer.Serialize(new { typeFirst, detected.Coverage, detected.Findings }));
    }

    [Fact]
    public void MissingRootOwnerAndSupportingRegistrationDoNotBecomeNotApplicable()
    {
        var owner = Fixture("class Owner { void Start() {} }");
        var supporting = new AdmissionCharacterization.Fixture([("/C2/Supporting.cs", Doc + "class Other {}")],
            assemblyName: "C2.Supporting");
        var root = Assert.Single(Documented(supporting.Compilation.SyntaxTrees.Single()));
        var run = owner.Session();
        var missing = run.Session.Analyze(root);
        Assert.Equal([Owner, Scope], missing.ProfileOutcome!.Reasons.Select(r => r.Category).Distinct().Order(StringComparer.Ordinal));
        var coverage = NewCoverage();
        AnalysisCoverageObserver.Observe(coverage, missing, summary: true);
        Assert.Equal(["OutsideProfile", "ScopeCoverageIncomplete", "TypeBindingIncomplete"], coverage.Report().ReasonCodes);
        run.Context.RegisterSupportingSource(supporting.Compilation);
        Assert.True(run.Environment.TryGetSemanticModel(root.SyntaxTree, out var model));
        Assert.Same(supporting.Compilation, model.Compilation);
        var available = run.Session.Analyze(root);
        Assert.Equal([Target], available.ProfileOutcome!.Reasons.Select(r => r.Category).Distinct());
        Assert.False(available.ProfileOutcome.Complete);
        Assert.NotEqual(missing.ProfileOutcome.ScopeDigest, available.ProfileOutcome.ScopeDigest);
        var direct = supporting.Session().Session.Analyze(root);
        Assert.Equal(direct.ProfileOutcome!.Root, available.ProfileOutcome.Root);
        Assert.NotEqual(missing.ProfileOutcome.Root, available.ProfileOutcome.Root);
        output.WriteLine("C2_BINDING " + JsonSerializer.Serialize(new {
            Missing = AdmissionCharacterization.Flow(missing), Registered = AdmissionCharacterization.Flow(available) }));
    }

    [Fact]
    public void SameNamedTypesInDifferentCompilationsKeepExactOwners()
    {
        var first = new AdmissionCharacterization.Fixture([("/C2/First.cs", Doc + "class Entry {}")], assemblyName: "C2.First");
        var second = new AdmissionCharacterization.Fixture([("/C2/Second.cs", Doc + "class Entry {}")], assemblyName: "C2.Second");
        var run = first.Session();
        run.Context.RegisterSupportingSource(second.Compilation);
        var a = run.Session.Analyze(Assert.Single(Documented(first.Compilation.SyntaxTrees.Single())));
        var b = run.Session.Analyze(Assert.Single(Documented(second.Compilation.SyntaxTrees.Single())));
        Assert.NotEqual(a.ProfileOutcome!.Root, b.ProfileOutcome!.Root);
        Assert.Equal(a.ProfileOutcome.ScopeDigest, b.ProfileOutcome.ScopeDigest);
        Assert.Equal([Target], a.ProfileOutcome.Reasons.Select(r => r.Category).Distinct());
        Assert.Equal([Target], b.ProfileOutcome.Reasons.Select(r => r.Category).Distinct());
    }

    [Fact]
    public void PeNoBodyRemainsAnIncompleteCallableAndFreshReasonOnRepeatedRoots()
    {
        var dependency = new AdmissionCharacterization.Fixture([("/C2/Api.cs", "public static class Api { public static void Run() {} }")],
            assemblyName: "C2.Metadata");
        using var image = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(image).Success);
        var fixture = new AdmissionCharacterization.Fixture([("/C2/Root.cs", "class Entry {\n" + Doc + Tag + "public static void Root() { Api.Run(); } }")],
            MetadataReferences.Default.Append(MetadataReference.CreateFromImage(image.ToArray())), assemblyName: "C2.Consumer");
        var run = fixture.Session();
        var root = fixture.Root("Root");
        var first = run.Session.Analyze(root);
        var second = run.Session.Analyze(root);
        Assert.False(first.ProfileOutcome!.Complete);
        Assert.Equal(first.ProfileOutcome.Details, second.ProfileOutcome!.Details);
        Assert.Contains(first.ProfileOutcome.Reasons, r => r.Category == Target && r.Witness.EndsWith(" / executable body unavailable", StringComparison.Ordinal));
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(root, run.Environment);
        var definition = Assert.Single(Definitions(admission), item => item.Key.Contains("Run", StringComparison.Ordinal));
        Assert.False(definition.Value.HasBody);
        Assert.True(Definitions(admission)[admission.Outcome.Root].HasBody);
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Single(detected.Findings, finding => finding.Smell.ID == "DOC631");
        Assert.DoesNotContain(detected.Findings, finding => finding.Smell.ID == "DOC632");
        Write("PE-call", root, fixture.Symbol("Root"), admission, second, detected);
    }

    [Theory]
    [InlineData("implicit-constructor", "class Item { int value = Fail(); static int Fail() => throw new System.InvalidOperationException(); }", "_ = new Item();", true)]
    [InlineData("expression-getter-fallback", "class Item { public int Value => throw new System.InvalidOperationException(); }", "_ = new Item().Value;", true)]
    [InlineData("auto-getter", "class Item { public int Value { get; set; } }", "_ = new Item().Value;", false)]
    [InlineData("auto-setter", "class Item { public int Value { get; set; } }", "new Item().Value = 1;", false)]
    [InlineData("local-function", "", "void Local() { throw new System.InvalidOperationException(); } Local();", true)]
    [InlineData("lambda", "", "System.Action action = () => throw new System.InvalidOperationException(); action();", true)]
    public void SupportedTargetsAreDistinctFromIndependentDetectorRoots(string name, string declarations, string body, bool complete)
    {
        var fixture = Fixture(declarations + "\nclass Entry {\n" + Doc + Tag + "static void Root() { " + body + " } }");
        var run = fixture.Session();
        var root = fixture.Root("Root");
        Assert.Single(Documented(root.SyntaxTree));
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(root, run.Environment);
        var result = run.Session.Analyze(root);
        Assert.Equal(complete, result.ProfileOutcome!.Complete);
        Assert.Equal(complete ? Array.Empty<string>() : [Target], result.ProfileOutcome.Reasons.Select(r => r.Category).Distinct());
        Assert.Equal(complete, result.ThrownExceptions.Any(t => t.ToDisplayString() == "System.InvalidOperationException"));
        if (!complete)
            Assert.Contains(Definitions(admission), item => !item.Value.HasBody && item.Key.Contains("Value", StringComparison.Ordinal));
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Equal(complete ? 0 : 1, detected.Findings.Count(f => f.Smell.ID == "DOC631"));
        Assert.DoesNotContain(detected.Findings, f => f.Smell.ID == "DOC632");
        Write(name, root, fixture.Symbol("Root"), admission, result, detected);
    }

    [Fact]
    public void FourDifferentKindsInOneScopeKeepIndependentBodyEvidence()
    {
        var fixture = Fixture("""
            /// <summary>Container.</summary>
            class Entry {
                /// <summary>Constructor.</summary>
                public Entry() {}
                /// <summary>Property.</summary>
                public int Value => 0;
                /// <summary>Method.</summary>
                public void Run() {}
            }
            """);
        var run = fixture.Session();
        var roots = Documented(fixture.Compilation.SyntaxTrees.Single()).ToArray();
        Assert.Equal(4, roots.Length);
        foreach (var root in roots)
        {
            var result = run.Session.Analyze(root);
            Assert.Equal(root is not TypeDeclarationSyntax, result.ProfileOutcome!.Complete);
        }
        var detected = Detect(fixture, run.Context, run.Session);
        Assert.Empty(detected.Findings);
        Assert.Equal(["OutsideProfile", "TargetCoverageIncomplete"], detected.Coverage.ReasonCodes);
    }

    private static AdmissionCharacterization.Fixture Fixture(string source)
        => new([("/C2/Root.cs", source)], assemblyName: "C2.RootKinds");
    private static IEnumerable<MemberDeclarationSyntax> Documented(SyntaxTree tree)
        => tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(member => XmlDocUtils.TryGetDocComment(member) is not null);
    private static Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> Definitions(ExceptionFlowFiniteOrbitAdmission admission)
        => AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
    private static AnalysisCoverage NewCoverage()
    {
        var coverage = new AnalysisCoverage();
        coverage.Begin("C2", ExceptionAnalysisMode.SolutionTransitive);
        return coverage;
    }
    private static string[] PublicReasons(string[] reasons) => reasons.Length == 0 ? [] :
        new[] { "OutsideProfile" }.Concat(reasons.Select(reason => reason switch {
            Target => "TargetCoverageIncomplete", Scope => "ScopeCoverageIncomplete", Owner => "TypeBindingIncomplete",
            _ => throw new InvalidOperationException(reason)
        })).Order(StringComparer.Ordinal).ToArray();
    private static (Finding[] Findings, AnalysisCoverageReport Coverage) Detect(AdmissionCharacterization.Fixture fixture,
        ProjectClosureSemanticContext context, ExceptionFlowSummaryAnalysisSession session)
    {
        var coverage = NewCoverage();
        AnalysisCoverageObserver.ObserveCompilation(coverage, fixture.Compilation, session);
        var findings = fixture.Compilation.SyntaxTrees.SelectMany(tree => XmlDocExceptionSemanticDetector.FindExceptionSmells(
            tree, tree.FilePath, fixture.Compilation.GetSemanticModel(tree), context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, session, coverage)).ToArray();
        return (findings, coverage.Report());
    }
    private void Write(string name, MemberDeclarationSyntax root, ISymbol? symbol,
        ExceptionFlowFiniteOrbitAdmission admission, ExceptionFlowAnalysisResult result,
        (Finding[] Findings, AnalysisCoverageReport Coverage) detected)
        => output.WriteLine("C2_ROOT " + JsonSerializer.Serialize(new {
            Name = name, Kind = root.Kind().ToString(), SymbolKind = symbol?.Kind.ToString(),
            Symbol = symbol?.ToDisplayString(), Owner = symbol?.ContainingAssembly?.Identity.ToString(),
            root.SpanStart, root.Span.Length, Admission = admission.Outcome,
            Definitions = Definitions(admission).Select(item => new { Id = item.Key, item.Value.HasBody,
                Fragment = JsonSerializer.Deserialize<JsonElement>(AdmissionCharacterization.Fragment(item.Value.Fragment)) }).ToArray(),
            Flow = AdmissionCharacterization.Flow(result), detected.Coverage, detected.Findings }));
}
