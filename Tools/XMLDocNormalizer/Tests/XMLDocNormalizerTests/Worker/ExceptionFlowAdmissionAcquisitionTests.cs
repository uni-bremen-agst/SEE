using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Runs today's real supporting-source lifecycle, including its observable mutations (K8/K9).</summary>
public sealed class ExceptionFlowAdmissionAcquisitionTests
{
    [Theory]
    [InlineData("late-plan")]
    [InlineData("failed-terminal")]
    [InlineData("successful-terminal")]
    public void SameCompilationPreservesDistinctNoPlanAndTerminalAcquisitionStates(string scenario)
    {
        using var workspace = new ExternalReconstructionPlanTestWorkspace();
        var dependency = workspace.Prepare("P2B.Acquired", [new SourceInput("/_/External.cs", """
            namespace External { public static class Api { public static void Run() { throw new System.InvalidOperationException(); } } }
            """)]);
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Consumer.cs",
            "class Entry { public static void A() { External.Api.Run(); } }")],
            MetadataReferences.Default.Append(dependency.Reference));
        var run = fixture.Session();
        var assembly = (IAssemblySymbol)fixture.Compilation.GetAssemblyOrModuleSymbol(dependency.Reference)!;
        Assert.True(run.Context.TryGetExternalAssemblyReferenceDescriptor(fixture.Compilation, assembly, out var descriptor));
        var originalView = run.Environment.GetAnalysisScopes();
        string scope = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment);

        if (scenario == "late-plan")
        {
            var without = run.Session.Analyze(fixture.Root("A"));
            Assert.False(without.ProfileOutcome!.Complete);
            Assert.False(run.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
            Assert.Same(originalView, run.Environment.GetAnalysisScopes());
            Assert.True(run.Context.TryRegisterExternalSupportingSourceReconstructionPlan(dependency.CreatePlan(descriptor)));
            var with = run.Session.Analyze(fixture.Root("A"));
            Assert.True(with.ProfileOutcome!.Complete, string.Join("\n", with.ProfileOutcome.Details));
            Assert.Equal("InvalidOperationException", Assert.Single(with.ThrownExceptions).Name);
            Assert.Equal(scope, with.ProfileOutcome.ScopeDigest);
            Assert.NotEqual(without.ProfileOutcome.InventoryDigest, with.ProfileOutcome.InventoryDigest);
            Assert.NotSame(originalView, run.Environment.GetAnalysisScopes());
            Assert.Equal(originalView.Select(item => item.Compilation), run.Environment.GetAnalysisScopes().Select(item => item.Compilation));
            Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(with)),
                JsonSerializer.Serialize(AdmissionCharacterization.Flow(run.Session.Analyze(fixture.Root("A")))));
        }
        else if (scenario == "failed-terminal")
        {
            string initiallyMissing = Path.Combine(workspace.DirectoryPath, "later-target.bin");
            Assert.True(run.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor, targetPath: initiallyMissing)));
            var before = run.Session.Analyze(fixture.Root("A"));
            Assert.False(before.ProfileOutcome!.Complete);
            File.Copy(dependency.TargetPath, initiallyMissing);
            var after = run.Session.Analyze(fixture.Root("A"));
            Assert.False(run.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
            Assert.Same(originalView, run.Environment.GetAnalysisScopes());
            Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(before)),
                JsonSerializer.Serialize(AdmissionCharacterization.Flow(after)));
        }
        else
        {
            Assert.True(run.Context.TryRegisterExternalSupportingSourceReconstructionPlan(dependency.CreatePlan(descriptor)));
            var before = run.Session.Analyze(fixture.Root("A"));
            Assert.True(before.ProfileOutcome!.Complete);
            Assert.True(run.Context.TryGetExternalSupportingSourceScope(descriptor, out var acquired));
            File.Delete(dependency.TargetPath);
            File.Delete(dependency.PdbPath);
            foreach (string path in dependency.SourcePaths) File.Delete(path);
            var after = run.Session.Analyze(fixture.Root("A"));
            Assert.True(run.Context.TryGetExternalSupportingSourceScope(descriptor, out var repeated));
            Assert.Same(acquired, repeated);
            Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(before)),
                JsonSerializer.Serialize(AdmissionCharacterization.Flow(after)));
        }
        Assert.Equal(scope, ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment));
    }
}
