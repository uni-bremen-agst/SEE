using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Compares the original ground-equation sweeps with production on real discovery products.</summary>
public sealed class ExceptionFlowGroundDiagnosticsScalingTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(8, "chain")]
    [InlineData(16, "chain")]
    [InlineData(32, "chain")]
    [InlineData(8, "cycle")]
    [InlineData(16, "cycle")]
    [InlineData(32, "cycle")]
    [InlineData(8, "fanout")]
    [InlineData(16, "fanout")]
    [InlineData(32, "fanout")]
    public void GrowingGraphsPreserveWitnessesAndBoundEquationWork(int size, string shape)
    {
        string methods = string.Join(" ", Enumerable.Range(0, size).Select(index =>
            "static void H" + index + "() { " + (shape != "fanout" && index + 1 < size ? "H" + (index + 1) + "();" :
                (shape == "cycle" ? "H0(); " : "") + "throw new System.InvalidOperationException();") + " }"));
        string calls = shape == "fanout" ? string.Join(" ", Enumerable.Range(0, size).Select(index => "H" + index + "();")) : "H0();";
        Compare("class Entry { static void Root() { " + calls + " Missing(); } static extern void Missing(); " + methods + " }",
            ["InvalidOperationException"], true, size, shape);
    }

    [Theory]
    [InlineData("Leaf(); Leaf();", "", "", "InvalidOperationException")]
    [InlineData("Left(); Right();", "static void Left() { Leaf(); } static void Right() { Leaf(); }", "", "InvalidOperationException")]
    [InlineData("Cycle();", "static void Cycle() { Root(); Leaf(); }", "", "InvalidOperationException")]
    [InlineData("Root(); Leaf();", "", "", "InvalidOperationException")]
    [InlineData("try { Leaf(); } catch (System.Exception) {}", "", "", "")]
    [InlineData("try { Leaf(); } catch (System.ArgumentException) {}", "", "", "InvalidOperationException")]
    [InlineData("try { Leaf(); } catch (System.Exception) when (false) {}", "", "", "InvalidOperationException")]
    [InlineData("WithArgument(0); Leaf();", "static void WithArgument(int value) { throw new System.ArgumentException(); }", "", "InvalidOperationException")]
    [InlineData("Generic<int>(); Leaf();", "static void Generic<T>() { throw new System.ArgumentException(); }", "", "InvalidOperationException")]
    [InlineData("Leaf();", "", "static Entry() { System.Action action = null!; action(); }", "")]
    public void ClosedDiscoveryParityIncludesCyclesCatchesMultiplicityAndInitialization(
        string body, string extra, string initializer, string expected)
    {
        Compare("class Entry { " + initializer + " static void Root() { " + body +
            " Missing(); } static extern void Missing(); " + extra +
            " static void Leaf() { throw new System.InvalidOperationException(); } }",
            expected.Length == 0 ? [] : [expected], true);
    }

    [Fact]
    public void CompletedAdmissionDoesNotAcquireUnknownRemainder()
    {
        Compare("class Entry { static void Root() { Leaf(); } static void Leaf() {} }", [], false);
    }

    [Fact]
    public void SeededExternalEvidenceRemainsSeparateAndOrdered()
    {
        Compare("""
            class Entry {
                static void Root() { Leaf(); Missing(); }
                /// <exception cref="System.ArgumentException">External contract.</exception>
                static extern void Leaf();
                static extern void Missing();
            }
            """, [], true, expectedExternal: ["ArgumentException"], seedExternal: true);
    }

    private void Compare(string source, string[] expectedKnown, bool expectedUnknown, int? size = null, string? shape = null,
        string[]? expectedExternal = null, bool seedExternal = false)
    {
        AdmissionCharacterization.Fixture fixture = new([("/PERF05B/Ground.cs", source)]);
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        ExceptionFlowFiniteOrbitAdmission admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("Root"), environment);
        string certificate = JsonSerializer.Serialize(admission.Outcome);
        Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> snapshots =
            AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
        if (seedExternal)
        {
            // Exercise the diagnostic external channel directly; source documentation is not executable proof.
            string leaf = ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("Leaf"));
            snapshots[leaf].Fragment.AddSource(new(fixture.Compilation.GetTypeByMetadataName("System.ArgumentException")!,
                new XMLDocNormalizer.Models.ExceptionFlowPath(new(XMLDocNormalizer.Models.ExceptionFlowPathStepKind.ExternalDocumentationEvidence,
                    "seed", null, null, null)), XMLDocNormalizer.Models.ExceptionFlowSourceKind.ExternalDocumentationEvidence));
        }
        Counts counts = new();
        FieldInfo field = typeof(ExceptionFlowSummaryFragment).GetField("callEdges", BindingFlags.Instance | BindingFlags.NonPublic)!;
        int rawEdges = snapshots.Values.Sum(snapshot => snapshot.Fragment.CallEdges.Count);
        foreach ((ExceptionFlowSummaryFragment fragment, bool _) in snapshots.Values)
        {
            // Only enumeration is observed; the actual list entries, multiplicity and order are retained.
            field.SetValue(fragment, new CountingEdges(fragment.CallEdges, counts));
        }
        ExceptionFlowProfileDiagnostics expected = LegacySweeps(admission);
        int beforeEquations = counts.Equations;
        int beforeEdges = counts.Edges;
        counts.Reset();
        ExceptionFlowProfileDiagnostics actual = admission.DiagnoseGroundFacts();
        int afterEquations = counts.Equations;
        int afterEdges = counts.Edges;
        AssertParity(expected, actual);
        Assert.Equal(expectedKnown.Order(), actual.ProvenLowerWitnesses.Select(type => type.Name).Order());
        Assert.Equal((expectedExternal ?? []).Order(), actual.ExternalDocumentation.Select(type => type.Name).Order());
        Assert.Equal(expectedUnknown, actual.HasEscapingUncertainty);
        Assert.Equal(certificate, JsonSerializer.Serialize(admission.Outcome));
        Assert.Equal(rawEdges, snapshots.Values.Sum(snapshot => snapshot.Fragment.CallEdges.Count));
        AssertParity(actual, admission.DiagnoseGroundFacts()); // No retained solver state between invocations.
        if (size != null)
        {
            Assert.True(afterEquations <= beforeEquations);
            Assert.True(afterEdges <= beforeEdges);
            Assert.InRange(afterEquations, 1, 4 * snapshots.Count);
            output.WriteLine("PERF05B_SCALE " + JsonSerializer.Serialize(new
            {
                size, shape, definitions = snapshots.Count, rawEdges, beforeEquations, beforeEdges, afterEquations, afterEdges,
                parity = true
            }));
        }
    }

    private static void AssertParity(ExceptionFlowProfileDiagnostics expected, ExceptionFlowProfileDiagnostics actual)
    {
        Assert.Equal(expected.ProvenLowerWitnesses.Select(AdmissionCharacterization.TypeKey),
            actual.ProvenLowerWitnesses.Select(AdmissionCharacterization.TypeKey));
        Assert.Equal(expected.ExternalDocumentation.Select(AdmissionCharacterization.TypeKey),
            actual.ExternalDocumentation.Select(AdmissionCharacterization.TypeKey));
        for (int index = 0; index < expected.ProvenLowerWitnesses.Length; index++)
        {
            Assert.Same(expected.ProvenLowerWitnesses[index], actual.ProvenLowerWitnesses[index]);
        }
        for (int index = 0; index < expected.ExternalDocumentation.Length; index++)
        {
            Assert.Same(expected.ExternalDocumentation[index], actual.ExternalDocumentation[index]);
        }
        Assert.Equal(expected.HasEscapingUncertainty, actual.HasEscapingUncertainty);
        Assert.Equal(expected.NormalFindingAdmissionBlocked, actual.NormalFindingAdmissionBlocked);
        Assert.Equal(expected.Proof, actual.Proof);
        Assert.Equal(expected.QualifiedLowerWitnesses, actual.QualifiedLowerWitnesses);
    }

    /// <summary>The pre-PERF-05B full-sweep scheduler, using unchanged production term/catch rules.</summary>
    private static ExceptionFlowProfileDiagnostics LegacySweeps(ExceptionFlowFiniteOrbitAdmission admission)
    {
        Dictionary<string, ISymbol> declarations = AdmissionCharacterization.Field<Dictionary<string, ISymbol>>(admission, "declarations");
        Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> snapshots =
            AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
        HashSet<string> gaps = AdmissionCharacterization.Field<HashSet<string>>(admission, "staticInitializationGaps");
        Func<ITypeSymbol, Term> toTerm = typeof(ExceptionFlowFiniteOrbitAdmission)
            .GetMethod("ToTerm", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<ITypeSymbol, Term>>(admission);
        Func<INamedTypeSymbol, INamedTypeSymbol, bool> caughtBy = typeof(ExceptionFlowFiniteOrbitAdmission)
            .GetMethod("ExactGroundCatch", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<INamedTypeSymbol, INamedTypeSymbol, bool>>();
        string root = admission.Outcome.Root;
        if (!declarations.ContainsKey(root) || admission.Outcome.Reasons.Any(reason =>
                reason.Category is "OwnerBindingFailure" or "RepresentationOutsideGrammar" or "ScopeMapNotClosed"))
        {
            return new([], [], true, ["No ground diagnostic claim: binding/representation is not established."]);
        }
        bool Eligible(string body) => !ExceptionFlowInvocationSubstitution.Bindings(declarations[body]).Any()
            && declarations[body] is IMethodSymbol { Parameters.Length: 0 };
        Dictionary<string, Dictionary<string, INamedTypeSymbol>> known = new(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, INamedTypeSymbol>> external = new(StringComparer.Ordinal);
        Dictionary<string, bool> unknown = new(StringComparer.Ordinal);
        foreach (string body in declarations.Keys)
        {
            known.Add(body, new(StringComparer.Ordinal));
            external.Add(body, new(StringComparer.Ordinal));
            (ExceptionFlowSummaryFragment fragment, bool hasBody) = snapshots[body];
            unknown.Add(body, !Eligible(body) || !hasBody || fragment.UncertainTargets.Count != 0);
            if (!Eligible(body))
            {
                continue;
            }
            foreach (ExceptionFlowSummarySource source in fragment.Sources)
            {
                if (!toTerm(source.ExceptionType).IsGround)
                {
                    unknown[body] = true;
                    continue;
                }
                (source.Kind == XMLDocNormalizer.Models.ExceptionFlowSourceKind.ProvenException ? known[body] : external[body])
                    .TryAdd(AdmissionCharacterization.TypeKey(source.ExceptionType), source.ExceptionType);
            }
        }
        bool changed;
        do
        {
            changed = false;
            foreach (string body in declarations.Keys)
            {
                if (!Eligible(body))
                {
                    continue;
                }
                foreach (ExceptionFlowSummaryCallEdge edge in snapshots[body].Fragment.CallEdges)
                {
                    string target = ExceptionFlowFiniteOrbitAdmission.BodyId(edge.Target.Symbol);
                    if (!unknown.ContainsKey(target))
                    {
                        if (!unknown[body])
                        {
                            unknown[body] = true;
                            changed = true;
                        }
                        continue;
                    }
                    if (unknown[target] && !unknown[body])
                    {
                        unknown[body] = true;
                        changed = true;
                    }
                    foreach (KeyValuePair<string, INamedTypeSymbol> pair in known[target])
                    {
                        if (!edge.CaughtExceptionTypes.Any(caught => caughtBy(pair.Value, caught)) && known[body].TryAdd(pair.Key, pair.Value))
                        {
                            changed = true;
                        }
                    }
                    foreach (KeyValuePair<string, INamedTypeSymbol> pair in external[target])
                    {
                        if (!edge.CaughtExceptionTypes.Any(caught => caughtBy(pair.Value, caught)) && external[body].TryAdd(pair.Key, pair.Value))
                        {
                            changed = true;
                        }
                    }
                }
            }
        } while (changed);
        if (gaps.Count != 0)
        {
            return new([], external[root].OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(), true,
                ["Uncovered static initialization; local candidates are not independent lower witnesses.", "Inventory=" + admission.Outcome.InventoryDigest]);
        }
        return new(known[root].OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
            external[root].OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(), unknown[root],
            ["Ground parameterless declaration equations; whole-result admission remains blocked.", "Inventory=" + admission.Outcome.InventoryDigest]);
    }

    private sealed class Counts
    {
        internal int Equations { get; set; }
        internal int Edges { get; set; }
        internal void Reset()
        {
            Equations = 0;
            Edges = 0;
        }
    }

    private sealed class CountingEdges(IEnumerable<ExceptionFlowSummaryCallEdge> edges, Counts counts)
        : List<ExceptionFlowSummaryCallEdge>(edges), IReadOnlyList<ExceptionFlowSummaryCallEdge>
    {
        IEnumerator<ExceptionFlowSummaryCallEdge> IEnumerable<ExceptionFlowSummaryCallEdge>.GetEnumerator()
        {
            counts.Equations++;
            for (int index = 0; index < Count; index++)
            {
                counts.Edges++;
                yield return this[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<ExceptionFlowSummaryCallEdge>)this).GetEnumerator();
    }
}
