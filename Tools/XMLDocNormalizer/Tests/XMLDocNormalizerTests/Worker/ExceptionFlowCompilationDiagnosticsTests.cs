using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Protects the exact environment-local compiler status without product counters.</summary>
public sealed class ExceptionFlowCompilationDiagnosticsTests
{
    [Theory]
    [InlineData("class Entry {}", false, false, null)]
    [InlineData("class Entry { void M() { int x = \"bad\"; } }", false, true, "CS0029")]
    [InlineData("class Entry {}", true, false, "CS5001")]
    [InlineData("class Entry { void M() { Missing(); } }", true, true, "CS0103")]
    [InlineData("#warning P1D warning\nclass Entry {}", false, false, "CS1030")]
    [InlineData("class Entry { void M() { int x = ; } }", false, true, "CS1525")]
    [InlineData("class Entry { MissingType value; }", false, true, "CS0246")]
    public void DiagnosticSemanticsMatchTheExistingPredicate(string source, bool executable, bool expected, string? diagnosticId)
    {
        CSharpCompilation compilation = Create(source, executable ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
        Diagnostic[] diagnostics = compilation.GetDiagnostics().ToArray();
        if (diagnosticId != null) Assert.Contains(diagnostics, diagnostic => diagnostic.Id == diagnosticId);
        if (executable) Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CS5001");
        if (diagnosticId == "CS5001")
            Assert.All(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), diagnostic => Assert.Equal("CS5001", diagnostic.Id));
        Assert.Equal(expected, diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS5001"));
        ExceptionFlowSemanticEnvironment environment = Environment(compilation);
        Assert.Equal(expected, environment.HasBlockingCompilationErrors(compilation));
        Assert.Equal(expected, environment.HasBlockingCompilationErrors(compilation));
        Assert.Equal(expected, Assert.Single(Cache(environment)).Value);
    }

    [Fact]
    public void WarningAsErrorUsesEffectiveSeverityAndNewCompilationOptions()
    {
        CSharpCompilation warning = Create("#warning P1D\nclass Entry {}");
        CSharpCompilation error = warning.WithOptions(warning.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
        Assert.Contains(warning.GetDiagnostics(), diagnostic => diagnostic.Id == "CS1030" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(error.GetDiagnostics(), diagnostic => diagnostic.Id == "CS1030" && diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.IsWarningAsError);
        ExceptionFlowSemanticEnvironment environment = Environment(warning);
        Assert.False(environment.HasBlockingCompilationErrors(warning));
        Assert.True(environment.HasBlockingCompilationErrors(error));
        Assert.Equal(2, Cache(environment).Count);
    }

    [Fact]
    public void SameCompilationUsesStoredBooleanWithoutRecomputingDiagnostics()
    {
        CSharpCompilation compilation = Create("class Entry {}");
        ExceptionFlowSemanticEnvironment environment = Environment(compilation);
        Dictionary<Compilation, bool> cache = Cache(environment);
        Assert.Empty(cache);
        Assert.False(environment.HasBlockingCompilationErrors(compilation));
        Assert.Same(compilation, Assert.Single(cache).Key);
        Assert.Same(ReferenceEqualityComparer.Instance, cache.Comparer);

        // A test-only sentinel distinguishes a real cache hit from recomputing
        // and overwriting the same value. No Roslyn or product hook is needed.
        cache[compilation] = true;
        Assert.True(environment.HasBlockingCompilationErrors(compilation));
        Assert.True(environment.HasBlockingCompilationErrors(compilation));
        Assert.True(Assert.Single(cache).Value);
    }

    [Fact]
    public void IdenticalSourceAndAssemblyIdentityStillUseSeparateCompilationReferences()
    {
        CSharpCompilation first = Create("class Entry {}");
        CSharpCompilation second = Create("class Entry {}");
        Assert.NotSame(first, second);
        Assert.Equal(first.Assembly.Identity, second.Assembly.Identity);
        ExceptionFlowSemanticEnvironment environment = Environment(first);
        Assert.False(environment.HasBlockingCompilationErrors(first));
        Cache(environment)[first] = true;
        Assert.False(environment.HasBlockingCompilationErrors(second));
        Assert.True(environment.HasBlockingCompilationErrors(first));
        Assert.Equal(2, Cache(environment).Count);
    }

    [Fact]
    public void ChangedParseOptionsUseSeparateCompilationAndDiagnosticStatus()
    {
        const string source = "class Entry { int M() => 1; }";
        SyntaxTree modern = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), "/P1D/Entry.cs");
        SyntaxTree old = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp5), "/P1D/Entry.cs");
        CSharpCompilation first = Create(source).RemoveAllSyntaxTrees().AddSyntaxTrees(modern);
        CSharpCompilation second = first.ReplaceSyntaxTree(modern, old);
        ExceptionFlowSemanticEnvironment environment = Environment(first);
        Assert.False(environment.HasBlockingCompilationErrors(first));
        Assert.True(environment.HasBlockingCompilationErrors(second));
        Assert.Equal(2, Cache(environment).Count);
    }

    [Fact]
    public void ChangedReferencesUseSeparateCompilationAndDiagnosticStatus()
    {
        CSharpCompilation first = Create("class Entry {}");
        CSharpCompilation second = first.RemoveAllReferences();
        Assert.Contains(second.GetDiagnostics(), diagnostic => diagnostic.Id == "CS0518");
        ExceptionFlowSemanticEnvironment environment = Environment(first);
        Assert.False(environment.HasBlockingCompilationErrors(first));
        Assert.True(environment.HasBlockingCompilationErrors(second));
        Assert.Equal(2, Cache(environment).Count);
    }

    [Fact]
    public void DifferentEnvironmentsNeverShareMaterializedStatus()
    {
        CSharpCompilation compilation = Create("class Entry {}");
        ExceptionFlowSemanticEnvironment first = Environment(compilation);
        ExceptionFlowSemanticEnvironment second = Environment(compilation);
        first.HasBlockingCompilationErrors(compilation);
        Cache(first)[compilation] = true;
        Assert.False(second.HasBlockingCompilationErrors(compilation));
        Assert.NotSame(Cache(first), Cache(second));
    }

    [Fact]
    public void SupportingCompilationsAddedAfterMaterializationHaveIndependentPartitions()
    {
        CSharpCompilation main = Create("class Entry {}");
        ProjectClosureSemanticContext context = ProjectClosureSemanticContext.CreateSingleCompilationContext(main.SyntaxTrees.Single(), main);
        ExceptionFlowSemanticEnvironment environment = new(context);
        Assert.False(environment.HasBlockingCompilationErrors(main));
        CSharpCompilation valid = Create("class SupportingOne {}", name: "P1D.SupportingOne");
        CSharpCompilation invalid = Create("class SupportingTwo { MissingType value; }", name: "P1D.SupportingTwo");
        context.RegisterSupportingSource(valid);
        context.RegisterSupportingSource(invalid);
        foreach (var scope in environment.GetAnalysisScopes())
            Assert.Equal(ReferenceEquals(scope.Compilation, invalid), environment.HasBlockingCompilationErrors(scope.Compilation));
        Assert.Equal(3, Cache(environment).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleRootsAndDefinitionsReuseOneStatusAndKeepDefinitionWitnesses(bool invalid)
    {
        string source = "class Entry { public static void First() { Helper(); } public static void Second() { Helper(); } static void Helper() {} "
            + (invalid ? "static void Broken() { Missing(); }" : "") + " }";
        CSharpCompilation compilation = Create(source);
        ExceptionFlowSemanticEnvironment environment = Environment(compilation);
        ExceptionFlowSummaryAnalysisSession session = new(environment);
        MethodDeclarationSyntax[] roots = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "First" or "Second").ToArray();
        foreach (MethodDeclarationSyntax root in roots)
        {
            var result = session.Analyze(root);
            Assert.Equal(!invalid, result.ProfileOutcome!.Complete);
            Assert.Equal(invalid, Assert.Single(Cache(environment)).Value);
            if (invalid)
            {
                var reasons = result.ProfileOutcome.Reasons.Where(reason => reason.Category == "RepresentationOutsideGrammar").ToArray();
                Assert.Equal(2, reasons.Length);
                foreach (string name in new[] { root.Identifier.ValueText, "Helper" })
                {
                    ISymbol symbol = compilation.GetTypeByMetadataName("Entry")!.GetMembers(name).Single();
                    Assert.Contains(reasons, reason => reason.Witness == ExceptionFlowFiniteOrbitAdmission.BodyId(symbol) + " / source compilation has errors");
                }
                Assert.Empty(result.ProfileDiagnostics!.ProvenLowerWitnesses);
                Assert.True(result.ProfileDiagnostics.HasEscapingUncertainty);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoverageAndAdmissionShareTheSessionStatus(bool invalid)
    {
        CSharpCompilation compilation = Create("class Entry { public static void Root() {} "
            + (invalid ? "static void Broken() { Missing(); }" : "") + " }");
        ExceptionFlowSemanticEnvironment environment = Environment(compilation);
        ExceptionFlowSummaryAnalysisSession session = new(environment);
        AnalysisCoverage coverage = new();
        coverage.Begin("P1D", ExceptionAnalysisMode.SolutionTransitive);
        AnalysisCoverageObserver.ObserveCompilation(coverage, compilation, session);
        Assert.Equal(invalid, Assert.Single(Cache(environment)).Value);
        MethodDeclarationSyntax root = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        Assert.Equal(!invalid, session.Analyze(root).ProfileOutcome!.Complete);
        Assert.Equal(invalid, Assert.Single(Cache(environment)).Value);
        AnalysisCoverage direct = new();
        direct.Begin("P1D", ExceptionAnalysisMode.SolutionTransitive);
        AnalysisCoverageObserver.ObserveCompilation(direct, compilation);
        Assert.Equal(direct.Report().ReasonCodes, coverage.Report().ReasonCodes);
        Assert.Equal(direct.Report().Completion, coverage.Report().Completion);
    }

    [Fact]
    public void NullCompilationIsRejectedWithoutPublishingStatus()
    {
        ExceptionFlowSemanticEnvironment environment = Environment(Create("class Entry {}"));
        Assert.Throws<ArgumentNullException>(() => environment.HasBlockingCompilationErrors(null!));
        Assert.Empty(Cache(environment));
    }

    private static Dictionary<Compilation, bool> Cache(ExceptionFlowSemanticEnvironment environment)
        => (Dictionary<Compilation, bool>)typeof(ExceptionFlowSemanticEnvironment)
            .GetField("blockingCompilationErrors", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(environment)!;

    private static ExceptionFlowSemanticEnvironment Environment(CSharpCompilation compilation)
        => new(ProjectClosureSemanticContext.CreateSingleCompilationContext(compilation.SyntaxTrees.First(), compilation));

    private static CSharpCompilation Create(string source, OutputKind output = OutputKind.DynamicallyLinkedLibrary, string name = "P1D.Diagnostics")
        => CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, path: "/P1D/" + name + ".cs")], MetadataReferences.Default,
            new CSharpCompilationOptions(output));
}
