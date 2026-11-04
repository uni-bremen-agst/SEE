using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Acceptance of the finite C3H source family, without relaxing Outside admission.</summary>
public sealed class ExceptionFlowBoundedPositiveLowerWitnessTests(ITestOutputHelper output)
{
    private const string CacheSource = "public sealed class Cache<T> { public T? GetValue() { return default; } } ";
    private static string Source(string type = "Value", string body = "var value = cache.GetValue(); Validate(value);", string cache = CacheSource,
        string entryExtra = "", string validator = "static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); }")
        => "#nullable enable\npublic sealed class Value {} " + cache +
           "public static class Entry { static readonly Cache<" + type + "> cache = new(); " + entryExtra +
           "public static void M() { " + body + " } " + validator + " } " +
           "public static class Safe { public static void Independent() { System.ArgumentNullException.ThrowIfNull((object?)null); } }";

    [Theory]
    [InlineData("Value", true)]
    [InlineData("string", true)]
    [InlineData("int", false)]
    public void FixedDefaultAndActualGenericBindingQualifyOnlyReferenceNull(string type, bool expected)
    {
        var fixture = Fixture(Source(type));
        var run = fixture.Session();
        var flow = run.Session.Analyze(fixture.Root("M"));
        Check(flow, expected);
        if (expected)
        {
            var witness = Assert.Single(flow.ProfileDiagnostics!.QualifiedLowerWitnesses);
            Assert.IsAssignableFrom<ITypeParameterSymbol>(witness.Schema.DefaultReturn);
            Assert.True(witness.ClosedReturn.IsReferenceType);
            Assert.Equal("cache", witness.Schema.ReceiverField.Name);
            Assert.Equal("GetValue", witness.Schema.Getter.Name);
            Assert.Equal("Validate", witness.Schema.Validator.Name);
            Assert.Equal(2, witness.Source.LocalPath.Steps.Count);
            Assert.Equal(ExceptionFlowPathStepKind.MethodCall, witness.Source.LocalPath.Steps[0].Kind);
            Assert.Equal(ExceptionFlowPathStepKind.FrameworkThrowHelper, witness.Source.LocalPath.Steps[1].Kind);
        }
        Capture("default-" + type, fixture, flow);
    }

    [Theory]
    [InlineData("ctor-fails")]
    [InlineData("conditional-init")]
    [InlineData("unknown-init")]
    [InlineData("unknown-return")]
    [InlineData("nonnull-return")]
    [InlineData("boxed-default-return")]
    [InlineData("mutated-local")]
    [InlineData("other-argument")]
    [InlineData("async-validator")]
    [InlineData("name-side-effect")]
    [InlineData("user-conversion")]
    [InlineData("extra-initializer")]
    [InlineData("partial-cache")]
    [InlineData("shadow-framework-guard")]
    public void UnprovedOrIncompatiblePrefixesDoNotPromoteCandidates(string scenario)
    {
        string source = scenario switch
        {
            "ctor-fails" => Source(cache: "public sealed class Cache<T> { public Cache() { throw new System.InvalidOperationException(); } public T? GetValue() => default; } "),
            "conditional-init" => Source(entryExtra: "static bool Fail; static Entry() { if (Fail) throw new System.InvalidOperationException(); } "),
            "unknown-init" => Source().Replace("cache = new();", "cache = Build(); static extern Cache<Value> Build();", StringComparison.Ordinal),
            "unknown-return" => Source(cache: "public sealed class Cache<T> { public T? GetValue() => Unknown(); static extern T? Unknown(); } "),
            "nonnull-return" => Source(cache: "public sealed class Cache<T> { public object GetValue() => new object(); } "),
            "boxed-default-return" => Source(cache: "public sealed class Cache<T> { public object GetValue() => default(int); } "),
            "mutated-local" => Source(body: "var value = cache.GetValue(); value = new Value(); Validate(value);"),
            "other-argument" => Source(body: "var value = cache.GetValue(); Validate(new object());"),
            "async-validator" => Source(validator: "static async void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); }"),
            "name-side-effect" => Source(validator: "static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value, Name()); } static string Name() => throw new System.InvalidOperationException();"),
            "user-conversion" => Source(body: "Wrapper value = cache.GetValue(); Validate(value);") +
                " public sealed class Wrapper { public static implicit operator Wrapper(Value? value) => new(); }",
            "extra-initializer" => Source(entryExtra: "static readonly object other = new(); "),
            "partial-cache" => Source(cache: "public sealed partial class Cache<T> { public T? GetValue() => default; } public sealed partial class Cache<T> {} "),
            "shadow-framework-guard" => Source() + " namespace System { public class ArgumentNullException : Exception { public static void ThrowIfNull(object? value, string? paramName = null) {} } }",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var fixture = Fixture(source);
        var flow = fixture.Session().Session.Analyze(fixture.Root("M"));
        Check(flow, false);
        Capture(scenario, fixture, flow);
    }

    [Theory]
    [InlineData("System.ArgumentNullException", false)]
    [InlineData("System.Exception", false)]
    [InlineData("", false)]
    [InlineData("System.InvalidOperationException", true)]
    public void FinalCatchTransferControlsEscapeAndCannotEraseCoverage(string caught, bool expected)
    {
        string body = "try { var value = cache.GetValue(); Validate(value); } catch " +
            (caught.Length == 0 ? "" : "(" + caught + ")") + " { }";
        var fixture = Fixture(Source(body: body));
        var flow = fixture.Session().Session.Analyze(fixture.Root("M"));
        Check(flow, expected);
        Capture("catch-" + caught, fixture, flow);
    }

    [Fact]
    public void CatchFilterAndFinallyStayOutsideTheBoundedProof()
    {
        foreach (string body in new[]
        {
            "try { var value = cache.GetValue(); Validate(value); } catch (System.ArgumentNullException) when (false) {}",
            "try { var value = cache.GetValue(); Validate(value); } finally { }"
        })
        {
            var fixture = Fixture(Source(body: body));
            var flow = fixture.Session().Session.Analyze(fixture.Root("M"));
            Check(flow, false);
            Capture("unsupported-catch-transfer", fixture, flow);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnlyTheExactTagUsesLowerEvidenceAndMissingFindingPolicyIsUnchanged(bool includeExact)
    {
        string docs = "/// <summary>Read.</summary>\n" +
            (includeExact ? "/// <exception cref=\"System.ArgumentNullException\">Value.</exception>\n/// <exception cref=\"System.ArgumentException\">Base.</exception>\n" : "") +
            "/// <exception cref=\"System.TypeInitializationException\">Init.</exception>\n";
        var fixture = Fixture(Source().Replace("public static void M()", "\n" + docs + "public static void M()", StringComparison.Ordinal));
        var run = fixture.Session();
        var flow = run.Session.Analyze(fixture.Root("M"));
        Check(flow, true);
        var coverage = new AnalysisCoverage(); coverage.Begin("C3H", ExceptionAnalysisMode.SolutionTransitive);
        var root = fixture.Root("M");
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, run.Session, coverage);
        Assert.Equal(includeExact ? 2 : 1, findings.Count);
        Assert.All(findings, finding => Assert.Equal("DOC631", finding.Smell.ID));
        Assert.DoesNotContain(findings, finding => finding.Message.Contains("System.ArgumentNullException", StringComparison.Ordinal));
        Assert.Equal("Incomplete", coverage.Report().Completion);
        output.WriteLine("C3H_CAPTURE " + JsonSerializer.Serialize(new { Case = "finding-" + includeExact,
            Flow = AdmissionCharacterization.Flow(flow), Findings = findings, Coverage = coverage.Report() }));
    }

    [Fact]
    public void ReplaySharesOnlyTheRecipeAndRebuildsTheWitnessForEachRootQuery()
    {
        var fixture = Fixture(Source(entryExtra: "public static void Other() { var value = cache.GetValue(); Validate(new object()); } "));
        var view = fixture.Session();
        int producers = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(view.Environment, symbol => { if (symbol.Name == "M") producers++; });
        var cold = session.Analyze(fixture.Root("M"));
        var other = session.Analyze(fixture.Root("Other"));
        var independent = session.Analyze(fixture.Root("Independent"));
        var warm = session.Analyze(fixture.Root("M"));
        Check(cold, true); Check(warm, true); Check(other, false);
        Assert.Equal(1, producers);
        Assert.True(independent.ProfileOutcome!.Complete);
        Assert.Contains(independent.ThrownExceptions, type => type.ToDisplayString() == "System.ArgumentNullException");
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(cold)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(warm)));
        var first = Assert.Single(cold.ProfileDiagnostics!.QualifiedLowerWitnesses);
        var second = Assert.Single(warm.ProfileDiagnostics!.QualifiedLowerWitnesses);
        Assert.Same(first.Schema, second.Schema);
        Assert.NotSame(first, second);
        Assert.NotSame(first.Source.LocalPath, second.Source.LocalPath);
        Capture("replay", fixture, warm, new { producers, Other = AdmissionCharacterization.Flow(other), Independent = AdmissionCharacterization.Flow(independent) });
    }

    [Fact]
    public void ViewGrowthRetiresTheRecipeEvenWithUnchangedRootSource()
    {
        var fixture = Fixture(Source());
        var view = fixture.Session();
        int producers = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(view.Environment, symbol => { if (symbol.Name == "M") producers++; });
        var before = session.Analyze(fixture.Root("M"));
        var support = new AdmissionCharacterization.Fixture([("/C3H/Support.cs", "public class Support {}")], assemblyName: "C3H.Support");
        view.Context.RegisterSupportingSource(support.Compilation);
        var after = session.Analyze(fixture.Root("M"));
        Check(before, true); Check(after, true);
        Assert.Equal(2, producers);
        Assert.NotSame(before.ProfileDiagnostics!.QualifiedLowerWitnesses[0].Schema, after.ProfileDiagnostics!.QualifiedLowerWitnesses[0].Schema);
        Assert.NotEqual(before.ProfileOutcome!.ScopeDigest, after.ProfileOutcome!.ScopeDigest);
        Capture("view-growth", fixture, after, new { producers });
    }

    [Fact]
    public void ConflictingOwnerIsRejectedBeforeItCanReplaceTheQualifiedView()
    {
        var fixture = Fixture(Source()); var view = fixture.Session();
        var before = view.Session.Analyze(fixture.Root("M")); Check(before, true);
        var conflict = Fixture("public class DuplicateOwner {}");
        Assert.Throws<InvalidOperationException>(() => view.Context.RegisterSupportingSource(conflict.Compilation));
        var after = view.Session.Analyze(fixture.Root("M"));
        Check(after, true);
        Assert.Equal(before.ProfileOutcome!.ScopeDigest, after.ProfileOutcome!.ScopeDigest);
        Capture("owner-conflict-rejected", fixture, after);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossCompilationSourceIsNotSilentlyIncludedInTheBoundedFamily(bool registerSource)
    {
        var support = new AdmissionCharacterization.Fixture([("/C3H/Cache.cs", "#nullable enable\n" + CacheSource)], assemblyName: "C3H.ExternalCache");
        var fixture = new AdmissionCharacterization.Fixture([("InMemory.cs", Source(cache: ""))],
            MetadataReferences.Default.Append(support.Compilation.ToMetadataReference()), assemblyName: "C3H.Reader");
        var view = fixture.Session();
        if (registerSource) view.Context.RegisterSupportingSource(support.Compilation);
        var result = view.Session.Analyze(fixture.Root("M")); Check(result, false);
        Capture("supporting-" + registerSource, fixture, result);
    }

    [Fact]
    public void MetadataGetterWithoutTheRequiredSourceCannotBecomeLowerEvidence()
    {
        var support = new AdmissionCharacterization.Fixture([("/C3H/Cache.cs", "#nullable enable\n" + CacheSource)], assemblyName: "C3H.PECache");
        using var image = new MemoryStream();
        var emitted = support.Compilation.Emit(image); Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        var fixture = new AdmissionCharacterization.Fixture([("InMemory.cs", Source(cache: ""))],
            MetadataReferences.Default.Append(MetadataReference.CreateFromImage(image.ToArray())), assemblyName: "C3H.PEReader");
        var result = fixture.Session().Session.Analyze(fixture.Root("M")); Check(result, false);
        Capture("metadata-no-source", fixture, result);
    }

    private static AdmissionCharacterization.Fixture Fixture(string source) =>
        new([("InMemory.cs", source)], assemblyName: "C3H.Fixture");

    private static void Check(XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult flow, bool lower)
    {
        Assert.False(flow.ProfileOutcome!.Complete);
        Assert.Contains(flow.ProfileOutcome.Reasons, reason => reason.Witness.Contains("StaticInitializationCoverageMissing", StringComparison.Ordinal));
        Assert.True(flow.ProfileDiagnostics!.HasEscapingUncertainty);
        Assert.True(flow.ProfileDiagnostics.NormalFindingAdmissionBlocked);
        Assert.Equal(lower ? 1 : 0, flow.ProfileDiagnostics.QualifiedLowerWitnesses.Length);
        Assert.Equal(lower ? 1 : 0, flow.ThrownExceptions.Count());
        Assert.Equal(lower ? 1 : 0, flow.ProfileDiagnostics.ProvenLowerWitnesses.Length);
    }

    private void Capture(string name, AdmissionCharacterization.Fixture fixture,
        XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult flow, object? extra = null)
        => output.WriteLine("C3H_CAPTURE " + JsonSerializer.Serialize(new { Case = name,
            Source = fixture.Compilation.SyntaxTrees.Single().GetText().ToString(), Flow = AdmissionCharacterization.Flow(flow),
            Qualified = flow.ProfileDiagnostics!.QualifiedLowerWitnesses.Select(witness => new
            {
                Root = ExceptionFlowFiniteOrbitAdmission.BodyId(witness.Schema.Definition),
                Field = witness.Schema.ReceiverField.ToDisplayString(), Getter = witness.Schema.Getter.ToDisplayString(),
                SymbolicReturn = witness.Schema.DefaultReturn.ToDisplayString(), ClosedReturn = witness.ClosedReturn.ToDisplayString(),
                Validator = witness.Schema.Validator.ToDisplayString(), Path = witness.Source.LocalPath.Steps
            }), Extra = extra }));
}
