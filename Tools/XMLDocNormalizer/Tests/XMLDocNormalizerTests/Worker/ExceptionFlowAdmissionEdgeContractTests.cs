using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Characterizes existing registration handles and record/array equality (P2A K3/K4).</summary>
public sealed class ExceptionFlowAdmissionEdgeContractTests
{
    [Theory]
    [InlineData("shared", 1)]
    [InlineData("equal-separate", 2)]
    [InlineData("different", 2)]
    [InlineData("empty-shared", 1)]
    [InlineData("empty-separate", 2)]
    public void EdgeDistinctUsesArrayReferenceAndFullWitness(string scenario, int count)
    {
        bool empty = scenario.StartsWith("empty", StringComparison.Ordinal);
        Term[] first = empty ? new Term[0] : [Term.Named("int")];
        Term[] second = scenario is "shared" or "empty-shared" ? first
            : empty ? new Term[0] : [Term.Named(scenario == "different" ? "string" : "int")];
        Edge[] edges = [new("A", "B", first, "same site"), new("A", "B", second, "same site")];
        Assert.Equal(count, edges.Distinct().Count());
        var bodies = new[] { new Body("A", [], []), new Body("B", empty ? [] : ["T"], []) };
        var single = ExceptionFlowFiniteOrbitProfile.Check(bodies, [edges[0]], "A", [], "scope", []);
        var actual = ExceptionFlowFiniteOrbitProfile.Check(bodies, edges.Distinct().ToArray(), "A", [], "scope", []);
        Assert.True(actual.Complete);
        Assert.Empty(actual.Reasons);
        using var inventory = JsonDocument.Parse(actual.InventoryWitness);
        Assert.Equal(count, inventory.RootElement.GetProperty("Edges").GetArrayLength());
        Assert.Equal(count == 1, single.InventoryDigest == actual.InventoryDigest);
        Assert.Equal(2, new[] { edges[0], edges[0] with { Witness = "other site" } }.Distinct().Count());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void ActualRegistrationsHaveFreshHandlesAndPreserveAssignmentArrayPartition(bool generic, int registrations)
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Edges.cs",
            "class Entry { static void A() {} static void Target<T>() {} static void Plain() {} }")]);
        var run = fixture.Session();
        // Exercise the existing sink in its pre-Check frame. No new production seam or replay is introduced.
        var admission = (ExceptionFlowFiniteOrbitAdmission)Activator.CreateInstance(
            typeof(ExceptionFlowFiniteOrbitAdmission), BindingFlags.Instance | BindingFlags.NonPublic,
            null, [run.Environment], null)!;
        string caller = (string)typeof(ExceptionFlowFiniteOrbitAdmission).GetMethod("AddDefinition",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(admission, [fixture.Symbol("A")])!;
        typeof(ExceptionFlowFiniteOrbitAdmission).GetField("currentDefinition",
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(admission, caller);
        IMethodSymbol target = generic ? fixture.Symbol("Target").Construct(
            fixture.Compilation.GetSpecialType(SpecialType.System_Int32)) : fixture.Symbol("Plain");
        var keys = Enumerable.Range(0, registrations)
            .Select(_ => admission.RegisterTarget(target, new(target))).ToArray();
        for (int index = 1; index < keys.Length; index++)
        {
            Assert.Equal(keys[0], keys[index]);
            Assert.NotSame(keys[0], keys[index]);
        }

        static ExceptionFlowSummaryFragment At(ExceptionFlowCallableKey key, int line)
        {
            var fragment = new ExceptionFlowSummaryFragment();
            fragment.AddCallEdge(new(key, new(ExceptionFlowPathStepKind.MethodCall, "Target", "/P2B/site.cs", line, 1)));
            return fragment;
        }
        foreach (var key in keys) admission.ObserveLocalFragment(At(key, 10));
        admission.ObserveLocalFragment(At(keys[0], 99));
        var raw = AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
        Assert.Equal(registrations, raw.Count);
        Assert.All(raw, edge =>
        {
            Assert.Contains("\"Line\":10", edge.Witness, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Line\":99", edge.Witness, StringComparison.Ordinal);
        });
        for (int index = 1; index < raw.Count; index++)
        {
            if (generic) Assert.NotSame(raw[0].Assignments, raw[index].Assignments);
            else Assert.Same(raw[0].Assignments, raw[index].Assignments);
        }
        Assert.Equal(generic ? registrations : 1, raw.Distinct().Count());
        Assert.Single(AdmissionCharacterization.Field<HashSet<string>>(admission, "edgeTemplates"));
        Assert.Equal(2, admission.DefinitionCount);
    }
}
