using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

public sealed class ExceptionFlowStaticInitializationCompletionSafetyTests(ITestOutputHelper output)
{
    private const string Docs = "/// <summary>Read.</summary>\n" +
        "/// <exception cref=\"System.TypeInitializationException\">Initialization.</exception>\n" +
        "/// <exception cref=\"System.InvalidOperationException\">Inner control.</exception>\n";
    private const string Risk = "public class Target { public static int Value; static Target() { throw new System.InvalidOperationException(); } " +
        "public Target() {} public static int Method() => 7; public static int Property { get => 7; set {} } " +
        "public static event System.Action Changed { add {} remove {} } }\n";

    [Theory]
    [InlineData("return Target.Value;")]
    [InlineData("Target.Value = 7; return 0;")]
    [InlineData("ref int value = ref Target.Value; return value;")]
    [InlineData("return Target.Method();")]
    [InlineData("return Target.Property;")]
    [InlineData("Target.Property = 7; return 0;")]
    [InlineData("Target.Changed += Handler; return 0;")]
    [InlineData("_ = new Target(); return 0;")]
    [InlineData("return Relay();")]
    [InlineData("return new Other().Invoke();")]
    public void BoundExecutableTriggersBlockExactNegativeEvidence(string body)
    {
        var fixture = Fixture(Risk, body);
        Check(fixture, expectedComplete: false);
    }

    [Theory]
    [InlineData("static Target() {}", "public static readonly int Value = Fail(); static int Fail() => throw new System.InvalidOperationException();")]
    [InlineData("", "public static readonly int Value = Fail(); static int Fail() => throw new System.InvalidOperationException();")]
    [InlineData("", "public static int Value = 7; public static int Other { get; } = Fail(); static int Fail() => throw new System.InvalidOperationException();")]
    public void AllStaticInitializersCountEvenWithEmptyOrSynthesizedConstructor(string constructor, string fields)
        => Check(Fixture("class Target { " + constructor + fields + " }", "return Target.Value;"), false);

    [Theory]
    [InlineData("class Target { public static int Value; }")]
    [InlineData("class Target { public static readonly int Value = 42; }")]
    [InlineData("class Target { public static readonly int Value = 42; static Target() {} }")]
    [InlineData("class Target { public static readonly int Value = default(int); static Target() { ; } }")]
    [InlineData("class Target { public static int Value; static event System.Action? Changed = null!; }")]
    [InlineData("class Target { public static int Value; static readonly object Empty = (default(object))!; }")]
    public void ProvenAbsentOrLiteralOnlyInitializationRetainsCompleteNegatives(string type)
        => Check(Fixture(type, "return Target.Value;"), true);

    [Theory]
    [InlineData("_ = typeof(Target); return 0;")]
    [InlineData("_ = nameof(Target.Value); return 0;")]
    [InlineData("System.Func<int> unused = () => Target.Value; return 0;")]
    [InlineData("int Unused() => Target.Value; return 0;")]
    public void UnexecutedReferencesDoNotBecomeInitializationTriggers(string body)
        => Check(Fixture(Risk, body), true);

    [Theory]
    [InlineData("System.TypeInitializationException")]
    [InlineData("System.InvalidOperationException")]
    [InlineData("System.Exception")]
    [InlineData("")]
    public void CatchCannotEraseTheIndependentCoverageGap(string caught)
        => Check(Fixture(Risk, "try { return Target.Value; } catch " + (caught.Length == 0 ? "" : "(" + caught + ")") + " { return 9; }"), false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BfiMethodAndPropertyArePossibleRatherThanGuaranteedInitializationCandidates(bool property)
        => Check(Fixture("class Target { static int Value = Fail(); static int Fail() => throw new System.InvalidOperationException(); " +
            "public static int Method() => 7; public static int Property => 7; }", "return Target." + (property ? "Property;" : "Method();")), false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryCoverageDoesNotRequireAnExplicitConsumerOperation(bool constructor)
    {
        string source = "class Target { static Target() { throw new System.InvalidOperationException(); }\n" + Docs +
            (constructor ? "public Target() {}" : "public static int Read() => 7;") + " }";
        var fixture = new AdmissionCharacterization.Fixture([("/C3E/Entry.cs", source)], assemblyName: "C3E.Entry");
        var run = fixture.Session();
        var root = constructor ? fixture.Compilation.SyntaxTrees.Single().GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax>().Single(c => c.Modifiers.Any(m => m.Text == "public"))
            : (Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax)fixture.Root("Read");
        Assert.False(run.Session.Analyze(root).ProfileOutcome!.Complete);
    }

    [Fact]
    public void ColdWarmReplayAndIndependentRootRemainSeparated()
    {
        var fixture = Fixture(Risk, "try { return Target.Method(); } catch { return 9; }");
        var run = fixture.Session();
        int inspections = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, _ => inspections++);
        string first = JsonSerializer.Serialize(AdmissionCharacterization.Flow(session.Analyze(fixture.Root("Read"))));
        int afterCold = inspections;
        Assert.True(afterCold > 0);
        Assert.Equal(first, JsonSerializer.Serialize(AdmissionCharacterization.Flow(session.Analyze(fixture.Root("Read")))));
        Assert.Equal(afterCold, inspections);
        var independent = session.Analyze(fixture.Root("Independent"));
        Assert.True(independent.ProfileOutcome!.Complete);
        Assert.Empty(independent.ThrownExceptions);
        Assert.Equal(first, JsonSerializer.Serialize(AdmissionCharacterization.Flow(session.Analyze(fixture.Root("Read")))));
        output.WriteLine("C3E_REPLAY " + JsonSerializer.Serialize(new { first, afterCold, inspections, Independent = AdmissionCharacterization.Flow(independent) }));
    }

    [Fact]
    public void SourcePositiveEvidenceWithoutInitializationGapRemainsExact()
    {
        var fixture = Fixture("class Fault : System.Exception {} class Target { public static int Method() => throw new Fault(); }", "return Target.Method();");
        var flow = fixture.Session().Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Equal("Fault", Assert.Single(flow.ThrownExceptions).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PositiveAndNegativeFindingsRetainTheirExactSeparateTypesOnAnIndependentCompleteRoot(bool match)
    {
        var original = Fixture("class Fault : System.Exception {} class Target { public static int Method() => throw new Fault(); }", "return Target.Method();");
        string source = original.Compilation.SyntaxTrees.Single().GetText().ToString();
        if (match) source = source.Replace("cref=\"System.InvalidOperationException\"", "cref=\"Fault\"", StringComparison.Ordinal);
        var fixture = new AdmissionCharacterization.Fixture([("/C3E/Positive.cs", source)]);
        var run = fixture.Session();
        var root = fixture.Root("Read");
        var coverage = new AnalysisCoverage(); coverage.Begin("C3E-positive", ExceptionAnalysisMode.SolutionTransitive);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, run.Session, coverage);
        Assert.Equal("Complete", coverage.Report().Completion);
        Assert.Equal(match ? 1 : 2, findings.Count(f => f.Smell.ID == "DOC632"));
        Assert.Equal(match ? 0 : 1, findings.Count(f => f.Smell.ID == "DOC611"));
        Assert.DoesNotContain(findings, f => f.Smell.ID == "DOC631");
    }

    [Fact]
    public void NewGapRetainsRawBodySourcesWithoutPromotingThemAcrossTheInitializationBoundary()
    {
        var fixture = Fixture(Risk + "class Fault : System.Exception {}", "_ = Target.Value; throw new Fault();");
        var run = fixture.Session();
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("Read"), run.Environment,
            new AdmissionObservationRecorder(), fragment, new(fixture.Symbol("Read"))));
        Assert.Equal("Fault", Assert.Single(fragment.Sources).ExceptionType.Name);
        Assert.NotEmpty(fragment.StaticInitializationGaps);
        var flow = run.Session.Analyze(fixture.Root("Read"));
        Assert.False(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ProfileDiagnostics!.ProvenLowerWitnesses);
        Assert.True(flow.ProfileDiagnostics.HasEscapingUncertainty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegisteredCompilationReferenceUsesExactSourceAndRetiresAnEarlierUnknownView(bool unsafeInitializer)
    {
        string target = "public class Target<T> { public static int Value = " +
            (unsafeInitializer ? "Initialize(); static int Initialize() => throw new System.InvalidOperationException();" : "42;") + " }";
        var dependency = new AdmissionCharacterization.Fixture([("/C3E/Supporting.cs", target)], assemblyName: "C3E.Support");
        var fixture = new AdmissionCharacterization.Fixture([("/C3E/Reader.cs", "class Consumer {\n" + Docs +
            "public static int Read() => Target<int>.Value; }")], MetadataReferences.Default.Append(dependency.Compilation.ToMetadataReference()));
        var run = fixture.Session();
        var before = run.Session.Analyze(fixture.Root("Read"));
        Assert.False(before.ProfileOutcome!.Complete);
        run.Context.RegisterSupportingSource(dependency.Compilation);
        var after = run.Session.Analyze(fixture.Root("Read"));
        Assert.Equal(!unsafeInitializer, after.ProfileOutcome!.Complete);
        Assert.NotEqual(before.ProfileOutcome.ScopeDigest, after.ProfileOutcome.ScopeDigest);
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(after)),
            JsonSerializer.Serialize(AdmissionCharacterization.Flow(run.Session.Analyze(fixture.Root("Read")))));
    }

    [Fact]
    public void ExistingReferenceReceiverHasNoNewTypeEntryTriggerButValueTypeEntryRemainsConservative()
    {
        var reference = new AdmissionCharacterization.Fixture([("/C3E/Instance.cs",
            "class Target { static Target() { throw new System.InvalidOperationException(); } public int Read() => 7; }")]);
        Assert.True(reference.Session().Session.Analyze(reference.Root("Read")).ProfileOutcome!.Complete);
        Check(Fixture("struct Target { static Target() { throw new System.InvalidOperationException(); } public int Method() => 7; }",
            "return default(Target).Method();"), false);
    }

    [Theory]
    [InlineData("return Target.Method();", true)]
    [InlineData("return Target.Value;", false)]
    [InlineData("_ = new Target(); return 0;", false)]
    public void BoundDeclaringOwnerAndActualBaseConstructionDecideRelevance(string body, bool complete)
        => Check(Fixture("class Base { public static int Value; static Base() { throw new System.InvalidOperationException(); } public Base() {} } " +
            "class Target : Base { static Target() {} public Target() {} public static int Method() => 7; }", body), complete);

    [Fact]
    public void DirectModeKeepsItsExplicitBodyScopeAndDoesNotClaimTransitiveInitializationCoverage()
    {
        var fixture = Fixture(Risk, "return Target.Method();");
        var run = fixture.Session(); var root = fixture.Root("Read");
        var coverage = new AnalysisCoverage(); coverage.Begin("C3E-direct", ExceptionAnalysisMode.Direct);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.Direct }, run.Session, coverage);
        Assert.Equal("Complete", coverage.Report().Completion);
        Assert.All(findings, f => Assert.Equal("DOC630", f.Smell.ID));
        Assert.Equal(2, findings.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HistoricalWorkerUsesTheSameSafetyBoundaryWithoutAChangedPayload(bool unsafeInitialization)
    {
        string source = "public class Target { " + (unsafeInitialization ? "static Target() { throw new System.InvalidOperationException(); }" : "") +
            "public static int Method() => 7; } public static class Consumer { public static int Read() => Target.Method(); }";
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string worker = Path.Combine(directory!.FullName, "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin", "Debug", "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
        var result = await new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(30))
            .AnalyzeAsync(new WorkerAnalysisInput(source, "Consumer", "Read", new()));
        if (unsafeInitialization)
        {
            Assert.False(result.Success);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code);
            Assert.Contains(result.Failure.WorkerFailure.Details, d => d.Contains("StaticInitializationCoverageMissing", StringComparison.Ordinal));
            Assert.Null(result.Result);
        }
        else { Assert.True(result.Success, result.Failure?.Message); Assert.Empty(result.Result!.Entries); }
        output.WriteLine("C3E_HISTORICAL " + JsonSerializer.Serialize(new { unsafeInitialization, result.Success, result.Failure, result.Identity }));
    }

    [Theory]
    [InlineData("int")]
    [InlineData("string")]
    public void GenericDefinitionCannotBorrowAClosedTypesSuccess(string argument)
        => Check(Fixture("class Target<T> { public static int Value; static Target() { if (typeof(T) == typeof(string)) throw new System.InvalidOperationException(); } }",
            "return Target<" + argument + ">.Value;"), false);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void MetadataAbsenceRequiresFullImplementationRatherThanReferenceOnlySurface(bool initialization, bool referenceOnly)
    {
        var dependency = new AdmissionCharacterization.Fixture([("/C3E/Dependency.cs", "public class Target { public static int Value; " +
            (initialization ? "static Target() { throw new System.InvalidOperationException(); }" : "") + " }")], assemblyName: "C3E.Dependency");
        using var image = new MemoryStream();
        var emitted = dependency.Compilation.Emit(image, options: new EmitOptions(metadataOnly: referenceOnly, includePrivateMembers: !referenceOnly));
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var fixture = new AdmissionCharacterization.Fixture([("/C3E/Read.cs", "class Consumer {\n" + Docs + "public static int Read() => Target.Value; }")],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromImage(image.ToArray())]), assemblyName: "C3E.Metadata");
        Check(fixture, !initialization && !referenceOnly);
    }

    private static AdmissionCharacterization.Fixture Fixture(string target, string body)
        => new([("/C3E/Fixture.cs", target + "\nclass Consumer {\n" + Docs + "public static int Read() { " + body +
            " } static int Relay() => " + (target.Contains("public static int Method", StringComparison.Ordinal) ? "Target.Method()" : "0") +
            "; static void Handler() {} public static void Independent() {} }\nclass Other { public int Invoke() => " +
            (target.Contains("public static int Method", StringComparison.Ordinal) ? "Target.Method()" : "0") + "; }")], assemblyName: "C3E.Safety");

    private void Check(AdmissionCharacterization.Fixture fixture, bool expectedComplete)
    {
        var root = fixture.Root("Read");
        var run = fixture.Session();
        var flow = run.Session.Analyze(root);
        Assert.Equal(expectedComplete, flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        if (!expectedComplete) Assert.Contains(flow.ProfileOutcome.Reasons, r => r.Category == "TargetOrAnalysisCoverageIncomplete"
            && r.Witness.Contains("StaticInitializationCoverageMissing", StringComparison.Ordinal));
        var coverage = new AnalysisCoverage(); coverage.Begin("C3E", ExceptionAnalysisMode.SolutionTransitive);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, run.Session, coverage);
        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal(expectedComplete ? "DOC632" : "DOC631", f.Smell.ID));
        Assert.Equal(expectedComplete ? "Complete" : "Incomplete", coverage.Report().Completion);
        output.WriteLine("C3E_CAPTURE " + JsonSerializer.Serialize(new { Source = root.SyntaxTree.GetText().ToString(),
            Flow = AdmissionCharacterization.Flow(flow), Findings = findings, Coverage = coverage.Report() }));
    }
}
