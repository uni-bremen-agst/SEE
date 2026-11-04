using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Native query eligibility without changing the solver's root or finding contracts.</summary>
public sealed class ExceptionFlowTypeRootQueryEligibilityTests(ITestOutputHelper output)
{
    private const string Doc = "/// <summary>Documented.</summary>\n";
    private const string Tag = "/// <exception cref=\"System.InvalidOperationException\">Failure.</exception>\n";

    [Theory]
    [InlineData("class", "class Entry {}")]
    [InlineData("struct", "struct Entry {}")]
    [InlineData("record-class", "record class Entry;")]
    [InlineData("record-struct", "record struct Entry;")]
    [InlineData("interface", "interface Entry {}")]
    [InlineData("enum", "enum Entry { Value }")]
    [InlineData("delegate", "delegate void Entry();")]
    [InlineData("generic", "class Entry<T> {}")]
    [InlineData("nested-generic", "class Outer<T> { @DOC@ class Entry {} }")]
    [InlineData("members", "class Entry { public void Fail() { throw new System.InvalidOperationException(); } }")]
    public void PureTypeWithoutTagsDoesNotIssueAnExceptionQuery(string name, string declaration)
    {
        string docs = Doc + (name == "generic" ? "/// <typeparam name=\"T\">Type.</typeparam>\n" : "");
        var run = Run("pure-" + name, declaration.Contains("@DOC@", StringComparison.Ordinal)
            ? declaration.Replace("@DOC@", "\n" + docs, StringComparison.Ordinal) : docs + declaration);
        Assert.Empty(run.Produced);
        Assert.Empty(run.Semantic);
        Assert.Equal("Complete", run.Coverage.Completion);
        Assert.Empty(run.Coverage.ReasonCodes);
    }

    [Theory]
    [InlineData("Direct")]
    [InlineData("ProjectTransitive")]
    [InlineData("ProjectTransitiveDeclaredExceptions")]
    [InlineData("SolutionTransitive")]
    public void InvalidTypeExceptionTagKeepsDoc680WithoutAFlowClaim(string mode)
    {
        var run = Run("tag-mode-" + mode, Doc + Tag + "class Entry {}", Enum.Parse<ExceptionAnalysisMode>(mode));
        Assert.Contains(run.Syntax, finding => finding.Smell.ID == "DOC680");
        Assert.Empty(run.Semantic);
        Assert.Empty(run.Produced);
        Assert.Equal("Complete", run.Coverage.Completion);
        Assert.DoesNotContain(run.All, finding => finding.Smell.ID is "DOC630" or "DOC631" or "DOC632");
    }

    [Theory]
    [InlineData("unresolved", "MissingExceptionType", "DOC660")]
    [InlineData("non-exception", "System.String", "DOC670")]
    [InlineData("malformed", "System.", "DOC660")]
    [InlineData("missing", "", "DOC600")]
    public void CrefValidationSurvivesSkippedTypeQueries(string name, string cref, string expected)
    {
        string tag = cref.Length == 0 ? "/// <exception>Failure.</exception>\n"
            : "/// <exception cref=\"" + cref + "\">Failure.</exception>\n";
        var run = Run("cref-" + name, Doc + tag + "class Entry {}");
        Assert.Contains(run.All, finding => finding.Smell.ID == expected);
        Assert.Contains(run.Syntax, finding => finding.Smell.ID == "DOC680");
        Assert.DoesNotContain(run.All, finding => finding.Smell.ID is "DOC631" or "DOC632");
        Assert.Empty(run.Produced);
        Assert.Empty(run.Coverage.ReasonCodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DocumentedMethodsWithoutExceptionTagsKeepTheirOwnQueries(bool transitive)
    {
        var run = Run("method-" + transitive, Doc + "class Entry {\n" + Doc +
            "public static void Run() { " + (transitive ? "Fail();" : "throw new System.InvalidOperationException();") +
            " } static void Fail() { throw new System.InvalidOperationException(); } }");
        Assert.Equal(transitive ? "DOC611" : "DOC610", Assert.Single(run.Semantic).Smell.ID);
        Assert.Contains(run.Produced, symbol => symbol is IMethodSymbol { Name: "Run" });
        Assert.DoesNotContain(run.Produced, symbol => symbol is INamedTypeSymbol);
        Assert.Equal("Complete", run.Coverage.Completion);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExplicitAndImplicitConstructorsStillReachInstanceInitializers(bool implicitConstructor, bool property)
    {
        string member = property ? "public int Value { get; } = Fail();" : "int value = Fail();";
        var run = Run("instance-" + implicitConstructor + "-" + property, Doc + "class Item { " + member +
            " static int Fail() => throw new System.InvalidOperationException();\n" +
            (implicitConstructor ? "" : Doc + Tag + "public Item() {}\n") + "}\nclass Entry {\n" + Doc +
            "public static void Run() { _ = new Item(); } }");
        Assert.Equal("DOC611", Assert.Single(run.Semantic).Smell.ID);
        Assert.Contains(run.Produced, symbol => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
            && constructor.ContainingType.Name == "Item" && constructor.IsImplicitlyDeclared == implicitConstructor);
        Assert.DoesNotContain(run.Produced, symbol => symbol is INamedTypeSymbol);
        Assert.Equal("Complete", run.Coverage.Completion);
    }

    [Theory]
    [InlineData("explicit")]
    [InlineData("field")]
    [InlineData("property")]
    public void StaticConstructorAndInitializerCoverageIsNotDischargedBySkippingTheType(string kind)
    {
        string member = kind switch
        {
            "explicit" => "public static int Value;\n" + Doc + Tag + "static Item() { throw new System.InvalidOperationException(); }",
            "field" => "public static readonly int Value = Fail();",
            _ => "public static int Value { get; } = Fail();"
        };
        var run = Run("static-" + kind, Doc + "class Item { " + member +
            " static int Fail() => throw new System.InvalidOperationException(); }\nclass Entry {\n" + Doc +
            "/// <exception cref=\"System.TypeInitializationException\">Init.</exception>\n" +
            "public static void Run() { _ = Item.Value; } }");
        Assert.Contains(run.Semantic, finding => finding.Smell.ID == "DOC631" && finding.Context.SymbolName == "Run");
        Assert.DoesNotContain(run.Semantic, finding => finding.Smell.ID == "DOC632");
        Assert.Equal("Incomplete", run.Coverage.Completion);
        Assert.Contains("TargetCoverageIncomplete", run.Coverage.ReasonCodes);
        Assert.DoesNotContain(run.Produced, symbol => symbol is INamedTypeSymbol);
        if (kind == "explicit")
            Assert.Contains(run.Produced, symbol => symbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor });
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public void PrimaryConstructorDeclarationsKeepTheExistingUnresolvedContract(string kind)
    {
        var run = Run("primary-" + kind, Doc + "/// <param name=\"value\">Value.</param>\n" + Tag + kind +
            " Item(int value) { int field = Fail(value); static int Fail(int value) => throw new System.InvalidOperationException(); }\n" +
            "class Entry {\n" + Doc + Tag + "public static void Run() { _ = new Item(1); } }");
        Assert.Contains(run.Produced, symbol => symbol is INamedTypeSymbol { Name: "Item" });
        Assert.Contains(run.Produced, symbol => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
            && constructor.ContainingType.Name == "Item" && !constructor.IsImplicitlyDeclared);
        Assert.Contains(run.Syntax, finding => finding.Smell.ID == "DOC680");
        Assert.Contains(run.Semantic, finding => finding.Smell.ID == "DOC631");
        Assert.DoesNotContain(run.Semantic, finding => finding.Smell.ID == "DOC632");
        Assert.Equal("Incomplete", run.Coverage.Completion);
    }

    [Theory]
    [InlineData("abstract", "abstract class Entry { @DOC@ public abstract void Run(); }", "DOC680", "DOC631")]
    [InlineData("extern", "class Entry { @DOC@ public static extern void Run(); }", "DOC680", "DOC631")]
    [InlineData("interface-method", "interface Entry { @DOC@ void Run(); }", "DOC680", "DOC631")]
    [InlineData("auto-property", "class Entry { @DOC@ public int Value { get; } }", "DOC680", "DOC631")]
    [InlineData("property", "class Entry { @DOC@ public int Value => 0; }", "DOC143", "DOC632")]
    [InlineData("indexer", "class Entry { @DOC@ public int this[int index] => 0; }", "DOC143", "DOC632")]
    [InlineData("event", "class Entry { @DOC@ public event System.Action Changed { add {} remove {} } }", "DOC143", "DOC632")]
    public void OtherMemberKindsRetainTheirQueriesAndTagPolicy(string name, string declaration, string syntaxRule, string semanticRule)
    {
        var run = Run("member-" + name, Doc + declaration.Replace("@DOC@", "\n" + Doc + Tag, StringComparison.Ordinal));
        Assert.Contains(run.Syntax, finding => finding.Smell.ID == syntaxRule);
        Assert.Equal(semanticRule, Assert.Single(run.Semantic).Smell.ID);
        Assert.NotEmpty(run.Produced);
        Assert.DoesNotContain(run.Produced, symbol => symbol is INamedTypeSymbol);
    }

    [Fact]
    public void IndependentCompleteAndIncompleteMembersKeepTheirOwnFindings()
    {
        var run = Run("independent", Doc + "class Container {}\nclass Entry {\n" + Doc + Tag +
            "public static void Empty() {}\n" + Doc + Tag + "public static extern void Unknown(); }");
        Assert.Contains(run.Semantic, finding => finding.Smell.ID == "DOC632" && finding.Context.SymbolName == "Empty");
        Assert.Contains(run.Semantic, finding => finding.Smell.ID == "DOC631" && finding.Context.SymbolName == "Unknown");
        Assert.DoesNotContain(run.Produced, symbol => symbol is INamedTypeSymbol);
        Assert.Equal("Incomplete", run.Coverage.Completion);
    }

    private Observation Run(string name, string source, ExceptionAnalysisMode mode = ExceptionAnalysisMode.SolutionTransitive)
    {
        var fixture = new AdmissionCharacterization.Fixture([("/TR1/Root.cs", source)], assemblyName: "TR1.Fixture");
        var created = fixture.Session();
        var produced = new List<ISymbol>();
        var session = new ExceptionFlowSummaryAnalysisSession(created.Environment, produced.Add);
        var tree = fixture.Compilation.SyntaxTrees.Single();
        var model = fixture.Compilation.GetSemanticModel(tree);
        var coverage = new AnalysisCoverage(); coverage.Begin("TR1", mode);
        AnalysisCoverageObserver.ObserveCompilation(coverage, fixture.Compilation, session);
        var syntax = XmlDocDetectorCatalog.SyntaxDetectors.SelectMany(detector => detector(tree, tree.FilePath))
            .Concat(XmlDocBasicDetector.FindBasicSmells(tree, tree.FilePath))
            .Concat(XmlDocDetectorCatalog.SemanticDetectors.SelectMany(detector => detector(tree, tree.FilePath, model))).ToArray();
        var semantic = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath, model, created.Context,
            new XmlDocOptions { ExceptionAnalysisMode = mode }, session, coverage).ToArray();
        var report = coverage.Report();
        output.WriteLine("TR1_CAPTURE " + JsonSerializer.Serialize(new { Case = name, Source = source, Mode = mode.ToString(),
            Produced = produced.Select(symbol => new { Kind = symbol.Kind.ToString(), Name = symbol.ToDisplayString(),
                MethodKind = (symbol as IMethodSymbol)?.MethodKind.ToString() }).ToArray(),
            Coverage = report, Syntax = syntax, Semantic = semantic }));
        return new(produced.ToArray(), syntax, semantic, report);
    }

    private sealed record Observation(ISymbol[] Produced, Finding[] Syntax, Finding[] Semantic, AnalysisCoverageReport Coverage)
    {
        internal IEnumerable<Finding> All => Syntax.Concat(Semantic);
    }
}
