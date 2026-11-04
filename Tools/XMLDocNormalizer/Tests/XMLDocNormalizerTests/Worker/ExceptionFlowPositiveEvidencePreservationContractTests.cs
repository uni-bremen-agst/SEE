using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Isolated C3F observations of the stopped prototype, not a corrected production contract.</summary>
public sealed class ExceptionFlowPositiveEvidencePreservationContractTests(ITestOutputHelper output)
{
    [Fact]
    public void OriginalSourceSeparatesRecursiveWitnessFromSummaryAdmissionAndGround()
    {
        string source = OriginalSource();
        var recursive = ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "M");
        var summary = ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");
        Assert.NotEmpty(recursive.Result.GetExceptionPaths(recursive.GetRequiredType("System.ArgumentNullException")));
        Assert.NotEmpty(summary.Result.GetExceptionPaths(summary.GetRequiredType("System.ArgumentNullException")));
        Assert.False(summary.Result.ProfileOutcome!.Complete);
        Assert.Contains(summary.Result.ProfileOutcome.Reasons, reason => reason.Witness.Contains("StaticInitializationCoverageMissing", StringComparison.Ordinal));
        Assert.Contains(summary.Result.ProfileDiagnostics!.ProvenLowerWitnesses,
            type => SymbolEqualityComparer.Default.Equals(type, summary.GetRequiredType("System.ArgumentNullException")));

        var fixture = Fixture(source);
        var session = fixture.Session();
        var locals = new List<object>();
        AdmissionObservationRecorder? rootRecorder = null;
        foreach (string name in new[] { "M", "Validate", "GetValue" })
        {
            var fragment = new ExceptionFlowSummaryFragment();
            var recorder = new AdmissionObservationRecorder();
            var symbol = fixture.Symbol(name);
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(symbol, session.Environment,
                recorder, fragment, new ExceptionFlowCallContext(symbol)));
            if (name == "M") rootRecorder = recorder;
            if (name == "Validate") Assert.Contains(fragment.Sources, s => s.ExceptionType.ToDisplayString() == "System.ArgumentNullException");
            locals.Add(new { Name = name, Body = ExceptionFlowFiniteOrbitAdmission.BodyId(symbol),
                Parameters = symbol.Parameters.Length,
                BindingCount = ExceptionFlowInvocationSubstitution.Bindings(symbol).Count(),
                Fragment = AdmissionCharacterization.Fragment(fragment),
                Gaps = fragment.StaticInitializationGaps.Order(StringComparer.Ordinal).ToArray(), Events = recorder.Events });
        }
        var validateKey = Assert.Single(rootRecorder!.Keys, key => key.Symbol.Name == "Validate");
        Assert.NotNull(validateKey.CallContext);
        var actual = new ExceptionFlowSummaryFragment();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(validateKey.Symbol, session.Environment,
            new AdmissionObservationRecorder(), actual, validateKey.CallContext!));
        Assert.Contains(actual.Sources, s => s.ExceptionType.ToDisplayString() == "System.ArgumentNullException");
        var nonNull = new ExceptionFlowSummaryFragment();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(validateKey.Symbol, session.Environment,
            new AdmissionObservationRecorder(), nonNull, new ExceptionFlowCallContext(validateKey.Symbol,
                [new KeyValuePair<int, ExceptionFlowValueFacts>(0, ExceptionFlowValueFacts.NonNull)])));
        Assert.DoesNotContain(nonNull.Sources, s => s.ExceptionType.ToDisplayString() == "System.ArgumentNullException");

        var model = fixture.Compilation.GetSemanticModel(fixture.Root("M").SyntaxTree);
        var getValue = Assert.Single(fixture.Root("M").DescendantNodes().OfType<InvocationExpressionSyntax>(),
            call => call.Expression is MemberAccessExpressionSyntax access && access.Name.Identifier.ValueText == "GetValue");
        Assert.False(ExceptionFlowConditionalWeakTableValueFactsProvider.TryGetConditionalWeakTableGetValueParts(getValue,
            model, out _, out _));
        var selected = (IMethodSymbol)model.GetSymbolInfo(getValue).Symbol!;
        Assert.True(SymbolEqualityComparer.Default.Equals(selected.ContainingAssembly, fixture.Compilation.Assembly));
        Capture("original", new { Source = source, SourceSha256 = Hash(source),
            Recursive = AdmissionCharacterization.Flow(recursive.Result), Summary = AdmissionCharacterization.Flow(summary.Result),
            Locals = locals, ActualValidateContext = validateKey.CanonicalContext,
            ActualValidateFragment = AdmissionCharacterization.Fragment(actual),
            NonNullValidateFragment = AdmissionCharacterization.Fragment(nonNull),
            GetValueOwner = selected.ContainingAssembly.Identity.ToString(), GetValueType = selected.ContainingType.ToDisplayString() });
    }

    [Fact]
    public void ExactOriginalExecutesTheGuardInTwoFreshRuntimeWorlds()
    {
        string source = OriginalSource();
        for (int repeat = 0; repeat < 2; repeat++)
        {
            var observed = Execute(source, "EntryPoint", "M");
            Assert.Equal("System.ArgumentNullException", observed.Outer);
            Assert.Equal("value", observed.Parameter);
            Capture("original-runtime-" + repeat, new { SourceSha256 = Hash(source), observed });
        }
    }

    [Theory]
    [InlineData("always-fails", "System.TypeInitializationException")]
    [InlineData("conditional-success", "System.ArgumentNullException")]
    [InlineData("conditional-failure", "System.TypeInitializationException")]
    [InlineData("successful-incompatible-effect", null)]
    public void SuccessfulInitializationNeedsWitnessCompatibleEffects(string scenario, string? expected)
    {
        string source = RuntimeCounterexample(scenario);
        var observed = Execute(source, "Harness", "Run");
        Assert.Equal(expected, observed.Outer);
        if (expected == "System.TypeInitializationException") Assert.Equal("System.InvalidOperationException", observed.Inner);
        var recursive = ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "Run");
        var summary = ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "Run");
        Capture(scenario, new { Source = source, SourceSha256 = Hash(source), AnalyzedRoot = "Harness.Run", observed,
            Recursive = AdmissionCharacterization.Flow(recursive.Result), Summary = AdmissionCharacterization.Flow(summary.Result) });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CatchAndRepeatedAdmissionPreserveCoverageWithoutPromotingCandidates(bool catches)
    {
        string body = "_ = Risk.Value; throw new Fault();";
        if (catches) body = "try { " + body + " } catch (Fault) { }";
        string source = "class Fault : System.Exception {} class Risk { public static readonly object Value = new(); } " +
            "public static class EntryPoint { public static void M() { " + body + " } public static void Independent() {} }";
        var fixture = Fixture(source);
        var view = fixture.Session();
        int inspected = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(view.Environment, symbol => { if (symbol.Name == "M") inspected++; });
        var cold = session.Analyze(fixture.Root("M"));
        int afterCold = inspected;
        var independent = session.Analyze(fixture.Root("Independent"));
        int beforeWarm = inspected;
        var warm = session.Analyze(fixture.Root("M"));
        Assert.False(cold.ProfileOutcome!.Complete);
        Assert.True(independent.ProfileOutcome!.Complete);
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(cold)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(warm)));
        Assert.Equal(beforeWarm, inspected);
        var local = new ExceptionFlowSummaryFragment();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("M"), view.Environment,
            new AdmissionObservationRecorder(), local, new ExceptionFlowCallContext(fixture.Symbol("M"))));
        Assert.NotEmpty(local.StaticInitializationGaps);
        if (catches) Assert.DoesNotContain(local.Sources, s => s.ExceptionType.Name == "Fault");
        else Assert.Contains(local.Sources, s => s.ExceptionType.Name == "Fault");
        Capture("catch-replay-" + catches, new { Source = source, Cold = AdmissionCharacterization.Flow(cold),
            Warm = AdmissionCharacterization.Flow(warm), Independent = AdmissionCharacterization.Flow(independent),
            afterCold, beforeWarm, AfterWarm = inspected,
            Fragment = AdmissionCharacterization.Fragment(local), Gaps = local.StaticInitializationGaps.Order(StringComparer.Ordinal).ToArray() });
    }

    [Fact]
    public void FutureMatrixSourcesCompileWithoutClaimingFutureAnalyzerExpectations()
    {
        foreach (var (name, source) in Matrix())
        {
            var fixture = Fixture(source);
            Capture("matrix-" + name, new { Source = source, SourceSha256 = Hash(source),
                Errors = fixture.Compilation.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error),
                Purpose = "Compilation only; future witness, findings, supporting and historical contracts are not validated here." });
        }
    }

    private static IEnumerable<(string Name, string Source)> Matrix()
    {
        yield return ("01-original", OriginalSource());
        yield return ("02-valid-positive", "public static class EntryPoint { public static void M() { System.ArgumentNullException.ThrowIfNull((object?)null); } }");
        yield return ("03-unknown-init", "class Risk { public static readonly object Value = new(); } public static class EntryPoint { public static void M() { _ = Risk.Value; System.ArgumentNullException.ThrowIfNull((object?)null); } }");
        yield return ("04-literal-init", "public static class EntryPoint { static readonly int Value = 42; static EntryPoint() {} public static void M() { _ = Value; System.ArgumentNullException.ThrowIfNull((object?)null); } }");
        yield return ("05-always-fails", RuntimeCounterexample("always-fails"));
        yield return ("06-conditional-init", RuntimeCounterexample("conditional-success"));
        yield return ("07-independent-root", "class Risk { public static readonly object Value = new(); } public static class EntryPoint { public static void M() { _ = Risk.Value; } public static void Independent() {} }");
        yield return ("08-other-exception", "class Risk { public static readonly object Value = new(); } class Fault : System.Exception {} public static class EntryPoint { public static void M() { _ = Risk.Value; throw new Fault(); } }");
        yield return ("09-incompatible-candidate", RuntimeCounterexample("successful-incompatible-effect"));
        yield return ("10-two-contexts", "public static class EntryPoint { public static void M() { Validate(null); } public static void NonNull() { Validate(new object()); } static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); } }");
        yield return ("11-catch", "class Risk { public static readonly object Value = new(); } public static class EntryPoint { public static void M() { try { _ = Risk.Value; System.ArgumentNullException.ThrowIfNull((object?)null); } catch (System.ArgumentNullException) {} } }");
        yield return ("12-replay", "class Risk { public static readonly object Value = new(); } public static class EntryPoint { public static void M() { _ = Risk.Value; System.ArgumentNullException.ThrowIfNull((object?)null); } }");
        yield return ("13-c3c-negative", "class Risk { static Risk() { throw new System.InvalidOperationException(); } public static int Method() => 7; } public static class EntryPoint { /// <exception cref=\"System.TypeInitializationException\">Initialization.</exception>\n public static int M() => Risk.Method(); }");
        yield return ("14-generic-contexts", "class Risk<T> { public static readonly object Value = new(); public static void Validate(object? value) { _ = Value; System.ArgumentNullException.ThrowIfNull(value); } } public static class EntryPoint { public static void M() { Risk<int>.Validate(null); } public static void Other() { Risk<string>.Validate(new object()); } }");
        yield return ("15-boundary-payload", "public class Dependency { public static readonly object Value = new(); } public static class EntryPoint { public static void M() { _ = Dependency.Value; System.ArgumentNullException.ThrowIfNull((object?)null); } }");
        yield return ("16-framework-name", OriginalSource());
    }

    private static string RuntimeCounterexample(string scenario)
    {
        string initializer = scenario switch
        {
            "always-fails" => "throw new System.InvalidOperationException();",
            "conditional-success" or "conditional-failure" => "if (Switch.Fail) throw new System.InvalidOperationException();",
            "successful-incompatible-effect" => "allow = false;",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        string body = scenario == "successful-incompatible-effect"
            ? "if (allow) System.ArgumentNullException.ThrowIfNull((object?)null);"
            : "System.ArgumentNullException.ThrowIfNull((object?)null);";
        return "public static class Switch { public static bool Fail; } " +
            "public static class EntryPoint { " + (scenario == "successful-incompatible-effect" ? "private static bool allow = true; " : "") +
            "static EntryPoint() { " + initializer + " } public static void M() { " + body + " } } " +
            "public static class Harness { public static void Run() { Switch.Fail = " + (scenario == "conditional-failure" ? "true" : "false") + "; EntryPoint.M(); } }";
    }

    private static AdmissionCharacterization.Fixture Fixture(string source) =>
        new([("InMemory.cs", source)], parse: CSharpParseOptions.Default, assemblyName: "ExceptionFlowAnalyzerTests");

    private static string OriginalSource()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string path = Path.Combine(directory!.FullName, "Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs");
        var method = Assert.Single(CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(),
            m => m.Identifier.ValueText == "UserDefinedConditionalWeakTable_DoesNotReceiveFrameworkFact");
        var variable = Assert.Single(method.DescendantNodes().OfType<VariableDeclaratorSyntax>(), v => v.Identifier.ValueText == "source");
        return Assert.IsType<LiteralExpressionSyntax>(variable.Initializer!.Value).Token.ValueText;
    }

    private sealed record RuntimeObservation(string? Outer, string? Inner, string? Parameter);

    private static RuntimeObservation Execute(string source, string type, string method)
    {
        using var image = new MemoryStream();
        var emitted = Fixture(source).Compilation.Emit(image);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext("C3F-" + Guid.NewGuid(), isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(image);
            var entry = assembly.GetType(type, throwOnError: true)!.GetMethod(method, BindingFlags.Public | BindingFlags.Static)!;
            var action = entry.CreateDelegate<Action>();
            try { action(); return new(null, null, null); }
            catch (System.Exception exception) { return new(exception.GetType().FullName, exception.InnerException?.GetType().FullName,
                (exception as ArgumentNullException)?.ParamName); }
        }
        finally { context.Unload(); }
    }

    private static string Hash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    private void Capture(string name, object value) => output.WriteLine("C3F_CAPTURE " + JsonSerializer.Serialize(new { Case = name, Evidence = value }));
}
