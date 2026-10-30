using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Observes the current declaration analyzer and admission before/after source catch suppression.</summary>
public sealed class ExceptionFlowAdmissionObservationTests
{
    [Theory]
    [InlineData("catch {}", false, false)]
    [InlineData("catch (System.InvalidOperationException) {}", true, false)]
    [InlineData("catch (System.InvalidOperationException) when (true) {}", true, true)]
    [InlineData("catch { throw; }", true, true)]
    public void PreCatchObservationsSurviveSuppressedFinalFragments(string handler, bool finalCalls, bool finalSources)
    {
        var fixture = Create(handler, unknown: true);
        var run = fixture.Session();
        var first = Inspect(fixture, run.Environment);
        var second = Inspect(fixture, run.Environment);
        Assert.Equal(first.Recorder.Events, second.Recorder.Events);
        Assert.Equal(AdmissionCharacterization.Fragment(first.Fragment), AdmissionCharacterization.Fragment(second.Fragment));
        Assert.Contains(true, first.Recorder.ObservedUncertainty);
        Assert.Equal(finalCalls, first.Fragment.CallEdges.Count != 0);
        Assert.Equal(finalSources, first.Fragment.Sources.Count != 0);
        for (int index = 0; index < first.Recorder.Keys.Count; index++)
            Assert.NotSame(first.Recorder.Keys[index], second.Recorder.Keys[index]);

        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment);
        Assert.False(admission.Outcome.Complete);
        string bodyId = ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("A"));
        Assert.Contains(admission.Outcome.Reasons, reason => reason.Category == "TargetOrAnalysisCoverageIncomplete"
            && reason.Witness == bodyId + " / local analysis coverage is incomplete");
        var raw = AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
        Assert.Contains(raw, edge => edge.From == bodyId && edge.To == ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("Known"))
            && edge.Witness.Contains(" / site=", StringComparison.Ordinal));
        using var inventory = JsonDocument.Parse(admission.Outcome.InventoryWitness);
        var body = inventory.RootElement.GetProperty("Bodies").EnumerateArray()
            .Single(value => value.GetProperty("Id").GetString() == bodyId);
        Assert.Contains("InvalidOperationException", body.GetProperty("Templates").GetRawText(), StringComparison.Ordinal);
        var ground = admission.DiagnoseGroundFacts();
        Assert.Equal(finalCalls, ground.HasEscapingUncertainty);
        Assert.Equal(finalCalls ? (finalSources ? 2 : 1) : 0, ground.ProvenLowerWitnesses.Count());
        var result = run.Session.Analyze(fixture.Root("A"));
        Assert.Equal(JsonSerializer.Serialize(admission.Outcome), JsonSerializer.Serialize(result.ProfileOutcome));
        Assert.True(result.ProfileOutcome!.NormalFindingAdmissionBlocked);
        Assert.Empty(result.ThrownExceptions);

        var success = Create(handler, unknown: false);
        var successRun = success.Session();
        Assert.True(successRun.Session.Analyze(success.Root("A")).ProfileOutcome!.Complete);
    }

    [Fact]
    public void RealSinkExceptionStopsAtObservedPrefixWithoutCompletingTheDefinition()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Stop.cs",
            "class Entry { static void A() { First(); Second(); } static void First() {} static void Second() {} }")]);
        var run = fixture.Session();
        var recorder = new AdmissionObservationRecorder(stopAfterEvent: 2);
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.Throws<AdmissionObservationRecorder.ObservationStoppedException>(() =>
            ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("A"), run.Environment,
                recorder, fragment, new(fixture.Symbol("A"))));
        Assert.Equal(2, recorder.Events.Count);
        Assert.All(recorder.Events, value => Assert.Contains("\"Kind\":\"Register\"", value, StringComparison.Ordinal));
        Assert.Single(fragment.CallEdges); // Second registration throws before its edge can be appended.
        var complete = Inspect(fixture, run.Environment);
        Assert.Equal(recorder.Events, complete.Recorder.Events.Take(2));
        Assert.Equal(2, complete.Fragment.CallEdges.Count);
    }

    [Fact]
    public void CatchAndFinallyCallsFollowTheProtectedPreCatchObservation()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Finally.cs", """
            class Entry
            {
                static void A() { try { First(); } catch { Second(); } finally { Third(); } }
                static void First() {} static void Second() {} static void Third() {}
            }
            """)]);
        var run = fixture.Session();
        var (recorder, fragment) = Inspect(fixture, run.Environment);
        Assert.Equal(new[] { "First", "Second", "Third" }, recorder.Keys.Select(key => key.Symbol.Name));
        Assert.Equal(new[] { "Second", "Third" }, fragment.CallEdges.Select(edge => edge.Target.Symbol.Name));
        int firstRegister = recorder.Events.FindIndex(value => value.Contains("\"Kind\":\"Register\"", StringComparison.Ordinal));
        using var observation = JsonDocument.Parse(recorder.Events[firstRegister + 1]);
        Assert.Equal("Observe", observation.RootElement.GetProperty("Kind").GetString());
        Assert.Equal(0, Assert.Single(observation.RootElement.GetProperty("Calls").EnumerateArray()).GetProperty("Ordinal").GetInt32());
    }

    private static (AdmissionObservationRecorder Recorder, ExceptionFlowSummaryFragment Fragment) Inspect(
        AdmissionCharacterization.Fixture fixture, ExceptionFlowSemanticEnvironment environment)
    {
        var recorder = new AdmissionObservationRecorder();
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("A"), environment,
            recorder, fragment, new(fixture.Symbol("A"))));
        return (recorder, fragment);
    }

    private static AdmissionCharacterization.Fixture Create(string handler, bool unknown) => new([
        ("/P2B/Catch.cs", $$"""
        class Entry
        {
            public static void A()
            {
                System.Action action = null!;
                try { Known(); {{(unknown ? "action();" : "")}} throw new System.InvalidOperationException(); }
                {{handler}}
            }
            static void Known() { throw new System.ArgumentException(); }
        }
        """)
    ]);
}
