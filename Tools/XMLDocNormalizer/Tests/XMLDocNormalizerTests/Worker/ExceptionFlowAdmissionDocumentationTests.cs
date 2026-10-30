using System.Text.Json;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Freezes live sidecar acquisition, including equal final fragments with different admission evidence (K5/K10).</summary>
public sealed class ExceptionFlowAdmissionDocumentationTests
{
    [Fact]
    public void EqualCaughtFinalFragmentsCanHaveDifferentInventoriesWhenASidecarAppears()
    {
        using var workspace = new ExternalReconstructionPlanTestWorkspace();
        var dependency = workspace.Prepare("P2B.Documented", [new SourceInput("/_/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")]);
        string sidecar = Path.ChangeExtension(dependency.Reference.FilePath!, ".xml");
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Consumer.cs",
            "class Entry { public static void A() { try { External.Api.Run(); } catch {} } }")],
            MetadataReferences.Default.Append(dependency.Reference));
        var run = fixture.Session();
        (string Final, string Trace, ExceptionFlowFiniteOrbitProfile.Certificate Certificate) Capture()
        {
            var recorder = new AdmissionObservationRecorder();
            var fragment = new ExceptionFlowSummaryFragment();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("A"), run.Environment,
                recorder, fragment, new(fixture.Symbol("A"))));
            Assert.Empty(fragment.Sources);
            Assert.Empty(fragment.CallEdges);
            Assert.Empty(fragment.UncertainTargets);
            return (AdmissionCharacterization.Fragment(fragment), string.Join("\n", recorder.Events),
                ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment).Outcome);
        }
        var absent = Capture();
        void AssertSame((string Final, string Trace, ExceptionFlowFiniteOrbitProfile.Certificate Certificate) expected)
        {
            var actual = Capture();
            Assert.Equal(expected.Final, actual.Final);
            Assert.Equal(expected.Trace, actual.Trace);
            Assert.Equal(JsonSerializer.Serialize(expected.Certificate), JsonSerializer.Serialize(actual.Certificate));
        }
        File.WriteAllText(sidecar, Xml("System.InvalidOperationException"));
        var loaded = Capture();
        Assert.Equal(absent.Final, loaded.Final);
        Assert.NotEqual(absent.Trace, loaded.Trace);
        Assert.Equal(absent.Certificate.ScopeDigest, loaded.Certificate.ScopeDigest);
        Assert.NotEqual(absent.Certificate.InventoryDigest, loaded.Certificate.InventoryDigest);
        Assert.Contains("InvalidOperationException", loaded.Certificate.InventoryWitness, StringComparison.Ordinal);
        File.WriteAllText(sidecar, Xml("System.ArgumentException"));
        AssertSame(loaded); // Existing path-keyed index is retained while the file exists.
        File.Delete(sidecar);
        AssertSame(absent); // File.Exists is checked before consulting the existing index.
        File.WriteAllText(sidecar, Xml("System.ArgumentException"));
        AssertSame(loaded);
    }

    [Fact]
    public void CaughtMalformedSidecarFailurePreservesEarlierEvidenceAndRetainsEmptyIndex()
    {
        using var workspace = new ExternalReconstructionPlanTestWorkspace();
        var dependency = workspace.Prepare("P2B.Malformed", [new SourceInput("/_/Api.cs",
            "namespace External { public static class Api { public static void Run() {} } }")]);
        string sidecar = Path.ChangeExtension(dependency.Reference.FilePath!, ".xml");
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Consumer.cs", """
            class Entry
            {
                public static void A() { Known(); External.Api.Run(); }
                static void Known() { throw new System.InvalidOperationException(); }
            }
            """)], MetadataReferences.Default.Append(dependency.Reference));
        var run = fixture.Session();
        string Capture()
        {
            var result = run.Session.Analyze(fixture.Root("A"));
            Assert.False(result.ProfileOutcome!.Complete);
            Assert.Equal("InvalidOperationException", Assert.Single(result.ProfileDiagnostics!.ProvenLowerWitnesses).Name);
            Assert.Contains("Known", result.ProfileOutcome.InventoryWitness, StringComparison.Ordinal);
            return JsonSerializer.Serialize(AdmissionCharacterization.Flow(result));
        }
        string missing = Capture();
        File.WriteAllText(sidecar, "<doc><broken>");
        Assert.Equal(missing, Capture()); // Existing XmlException -> empty contract behavior; no analyzer exception.
        File.WriteAllText(sidecar, Xml("System.ArgumentException"));
        Assert.Equal(missing, Capture()); // The failed load was published as the existing empty path index.
    }

    private static string Xml(string type) => $$"""
        <doc><members><member name="M:External.Api.Run">
        <exception cref="T:{{type}}">Failure.</exception>
        </member></members></doc>
        """;
}
