using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Emits complete legacy/candidate fixture products; no timing or benchmark instrumentation.</summary>
public sealed class ExceptionFlowMetadataNoBodyEvidenceParityTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("plain")]
    [InlineData("caught")]
    [InlineData("constructor")]
    [InlineData("accessors")]
    [InlineData("dispatch")]
    [InlineData("documentation")]
    [InlineData("source")]
    public void CompleteFixtureProductsAndEventsAreDeterministicAcrossRepeatedRoots(string scenario)
    {
        var dependency = new AdmissionCharacterization.Fixture([("/P5C/Api.cs", """
            namespace External { public class Api<T> {
                public Api() {}
                public int Value { get; set; }
                public static void Run<U>(object? value) {}
            } }
            """)], options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable, deterministic: true), assemblyName: "P5C.Parity.Api");
        using var image = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(image).Success);
        MetadataReference reference = scenario == "source" ? dependency.Compilation.ToMetadataReference()
            : MetadataReference.CreateFromImage(image.ToArray(), documentation: scenario == "documentation" ? new Documentation() : null);
        string a = "Shared(); External.Api<int>.Run<string>(\"value\");";
        string b = "Shared(); External.Api<string>.Run<int>(null);";
        if (scenario == "caught") a = "try { " + a + " } catch {}";
        if (scenario == "constructor") { a = "Shared(); _ = new External.Api<int>();"; b = "Shared(); _ = new External.Api<string>();"; }
        if (scenario == "accessors") { a = "var x = new External.Api<int>(); x.Value = 1; _ = x.Value;"; b = "var x = new External.Api<string>(); x.Value = 2; _ = x.Value;"; }
        if (scenario == "dispatch") { a = "IWork worker = new Worker(); worker.Run();"; b = a + " worker.Run();"; }
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", a), Root("B", b), ("/P5C/Shared.cs", """
            public static partial class Entry { static void Shared() { throw new System.ArgumentException(); } }
            interface IWork { void Run(); }
            sealed class Worker : IWork { public void Run() { External.Api<int>.Run<string>(null); } }
            """)
        ], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        if (scenario == "source") run.Context.RegisterSupportingSource(dependency.Compilation);
        var products = new List<object>();
        string? first = null;
        foreach (string name in new[] { "A", "B", "A" })
        {
            string full = fixture.Capture(name, run.Context, run.Session);
            if (name == "A") { if (first != null) Assert.Equal(first, full); first = full; }
            var sink = new AdmissionObservationRecorder();
            var fragment = new ExceptionFlowSummaryFragment();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol(name), run.Environment,
                sink, fragment, new(fixture.Symbol(name))));
            var facts = AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(run.Session, "admissionFacts");
            var admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(name), run.Environment, facts);
            var edges = AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
            var arrays = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            int Alias(object value) { if (!arrays.TryGetValue(value, out int ordinal)) arrays.Add(value, ordinal = arrays.Count); return ordinal; }
            var service = fixture.Compilation.GetTypeByMetadataName("IWork")!;
            var targets = ExceptionFlowAnalyzer.ResolveSummaryRuntimeTargets(service.GetMembers("Run").OfType<IMethodSymbol>().Single(),
                service, null, run.Environment).Select(ExceptionFlowFiniteOrbitAdmission.BodyId).Order(StringComparer.Ordinal).ToArray();
            var ground = admission.DiagnoseGroundFacts();
            products.Add(new {
                Root = name, FullProduct = JsonSerializer.Deserialize<JsonElement>(full),
                Events = sink.Events.ToArray(), FinalFragment = AdmissionCharacterization.Fragment(fragment),
                Registrations = edges.ToArray(), AssignmentAliases = edges.Select(edge => Alias(edge.Assignments)).ToArray(),
                RuntimeTargets = targets,
                Certificate = admission.Outcome,
                Ground = new { Proven = ground.ProvenLowerWitnesses.Select(AdmissionCharacterization.TypeKey).ToArray(),
                    External = ground.ExternalDocumentation.Select(AdmissionCharacterization.TypeKey).ToArray(),
                    ground.HasEscapingUncertainty, ground.NormalFindingAdmissionBlocked, ground.Proof }
            });
        }
        output.WriteLine("P5C_PARITY " + JsonSerializer.Serialize(new { Scenario = scenario, Products = products }));
    }

    private static (string Path, string Text) Root(string name, string body) => ($"/P5C/{name}.cs", $$"""
        public static partial class Entry {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);
    private sealed class Documentation : DocumentationProvider
    {
        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture,
            CancellationToken cancellationToken = default) => "<member><exception cref=\"T:System.InvalidOperationException\">Failure.</exception></member>";
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
