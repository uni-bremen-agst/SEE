using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Proves actual environment/view-local materialization without a product counter or hook.</summary>
public sealed class ExceptionFlowScopeDigestMaterializationTests
{
    [Fact]
    public void SameViewComputesOnceAndReturnsTheFinishedString()
    {
        using var input = new Input();
        var run = input.Fixture.Session();
        string expected = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment);
        string control = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment);
        Assert.Equal(expected, control);
        Assert.NotSame(expected, control); // An uncached complete calculation creates a new string.
        input.Reference.Reads = 0;
        string first = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Equal(expected, first);
        Assert.Equal(1, input.Reference.Reads);
        input.Reference.Fail = true; // Any further metadata read by the complete calculation would fail.
        Assert.Same(first, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
        Assert.Same(first, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
        Assert.Equal(1, input.Reference.Reads);
        Assert.Same(run.Environment.GetAnalysisScopes(), Field(run.Environment, "scopeDigestView"));
        Assert.Same(first, Field(run.Environment, "scopeDigest"));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    public void MultipleColdAndWarmRootsShareOneDigestWhileConflictChecksStayFresh(int count, bool reverse)
    {
        using var input = new Input(count);
        var fixture = input.Fixture;
        var cold = Enumerable.Range(0, count).ToDictionary(index => index,
            index => Flow(fixture.Session().Session.Analyze(fixture.Root("Root" + index))));
        var run = fixture.Session();
        int shared = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        {
            if (symbol.Name == "Shared" && ++shared > 1) throw new InvalidOperationException("P2C did not reuse the shared schema.");
        });
        int[] order = Enumerable.Range(0, count).ToArray();
        if (reverse) Array.Reverse(order);
        input.Reference.Reads = 0;
        string? first = null;
        int roots = 0;
        foreach (int index in order.Concat(order.Reverse()).Concat([order[0], order[0]]))
        {
            var result = session.Analyze(fixture.Root("Root" + index));
            Assert.True(result.ProfileOutcome!.Complete);
            Assert.Equal(cold[index], Flow(result));
            first ??= result.ProfileOutcome.ScopeDigest;
            Assert.Same(first, result.ProfileOutcome.ScopeDigest);
            Assert.Same(first, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
            // Each Discover still reads this PE once in its separate owner-conflict
            // check. Only the first Discover adds the complete digest read.
            Assert.Equal(1 + ++roots, input.Reference.Reads);
        }
        Assert.Equal(1, shared);
    }

    [Fact]
    public void SupportingGrowthRetiresTheOldDigestAndKeepsOnlyTheCurrentSnapshot()
    {
        using var input = new Input();
        var run = input.Fixture.Session();
        string first = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        var before = run.Environment.GetAnalysisScopes();
        var support = Compilation("P3B.Support", "class Support {}", "/P3B/Support.cs");
        run.Context.RegisterSupportingSource(support);
        var after = run.Environment.GetAnalysisScopes();
        Assert.NotSame(before, after);
        int reads = input.Reference.Reads;
        string second = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.NotEqual(first, second);
        Assert.Equal(reads + 1, input.Reference.Reads);
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment), second);
        Assert.Same(after, Field(run.Environment, "scopeDigestView"));
        Assert.Same(second, Field(run.Environment, "scopeDigest"));
        run.Context.RegisterSupportingSource(support);
        reads = input.Reference.Reads;
        Assert.Same(second, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
        Assert.Equal(reads, input.Reference.Reads);
    }

    [Fact]
    public void ReplacedSnapshotWithIdenticalCompilationsStillRecomputes()
    {
        using var input = new Input();
        var run = input.Fixture.Session();
        string first = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        var before = run.Environment.GetAnalysisScopes();
        // Test-only replacement of the existing host snapshot models conservative
        // view replacement without changing any scope or compilation member.
        var replacement = before.Cast<SemanticCompilationScope>().ToArray();
        typeof(ProjectClosureSemanticContext).GetField("analysisCompilationScopes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(run.Context, replacement);
        Assert.Same(replacement, run.Environment.GetAnalysisScopes());
        int reads = input.Reference.Reads;
        string second = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Equal(first, second);
        Assert.NotSame(first, second);
        Assert.Equal(reads + 1, input.Reference.Reads);
        Assert.Same(replacement, Field(run.Environment, "scopeDigestView"));
        Assert.Same(second, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailurePublishesNothingAndAnExplicitRetryRecomputes(bool previousViewSucceeded)
    {
        using var input = new Input();
        var run = input.Fixture.Session();
        if (previousViewSucceeded)
        {
            run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
            run.Context.RegisterSupportingSource(Compilation("P3B.Support", "class Support {}", "/P3B/Support.cs"));
        }
        input.Reference.Fail = true;
        input.Reference.Reads = 0;
        Assert.Throws<IOException>(() => run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
        Assert.Equal(1, input.Reference.Reads);
        Assert.Null(Field(run.Environment, "scopeDigest"));
        Assert.Null(Field(run.Environment, "scopeDigestView"));
        input.Reference.Fail = false;
        string value = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Equal(2, input.Reference.Reads);
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment), value);
        int reads = input.Reference.Reads;
        input.Reference.Fail = true;
        Assert.Same(value, run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest));
        Assert.Equal(reads, input.Reference.Reads);
    }

    [Fact]
    public void NewEnvironmentAndItsSessionCannotInheritAnotherEnvironmentsValue()
    {
        using var input = new Input();
        var first = input.Fixture.Session();
        var second = new ExceptionFlowSemanticEnvironment(first.Context); // Even the exact host snapshot is shared.
        var secondSession = new ExceptionFlowSummaryAnalysisSession(second);
        string value = first.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Same(first.Environment.GetAnalysisScopes(), second.GetAnalysisScopes());
        Assert.Null(Field(second, "scopeDigest"));
        Assert.Null(Field(second, "scopeDigestView"));
        int reads = input.Reference.Reads;
        string other = second.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Equal(value, other);
        Assert.NotSame(value, other);
        Assert.Equal(reads + 1, input.Reference.Reads);
        Assert.Same(other, secondSession.Analyze(input.Fixture.Root("Root0")).ProfileOutcome!.ScopeDigest);
    }

    [Fact]
    public void SameEnvironmentCanShareDigestAcrossSessionsWhileP2CFactsRemainSeparate()
    {
        using var input = new Input();
        var run = input.Fixture.Session();
        var first = run.Session.Analyze(input.Fixture.Root("Root0"));
        int shared = 0;
        var second = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol => { if (symbol.Name == "Shared") shared++; });
        var result = second.Analyze(input.Fixture.Root("Root1"));
        Assert.Same(first.ProfileOutcome!.ScopeDigest, result.ProfileOutcome!.ScopeDigest);
        Assert.Equal(1, shared); // P2C still has fresh session ownership.
    }

    [Fact]
    public void NewEqualDigestCompilationDoesNotShareMaterialization()
    {
        using var input = new Input();
        var first = input.Fixture.Session();
        string value = first.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        var clone = input.Fixture.Compilation.Clone();
        var second = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(clone.SyntaxTrees.First(), clone));
        int reads = input.Reference.Reads;
        Assert.Null(Field(second, "scopeDigest"));
        string other = second.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        Assert.Equal(value, other);
        Assert.NotSame(value, other);
        Assert.True(input.Reference.Reads > reads);
    }

    [Fact]
    public void EnvironmentAndDigestAreCollectibleEvenWhileTheContextRemainsAlive()
    {
        using var input = new Input();
        var released = ReleasedEnvironment(input.Fixture);
        for (int index = 0; index < 3 && (released.Environment.IsAlive || released.Digest.IsAlive); index++)
        { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.False(released.Environment.IsAlive);
        Assert.False(released.Digest.IsAlive);
        GC.KeepAlive(released.Context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ProjectClosureSemanticContext Context, WeakReference Environment, WeakReference Digest) ReleasedEnvironment(AdmissionCharacterization.Fixture fixture)
    {
        var run = fixture.Session();
        string value = run.Environment.GetScopeDigest(ExceptionFlowFiniteOrbitAdmission.ScopeDigest);
        return (run.Context, new WeakReference(run.Environment), new WeakReference(value));
    }

    private static object? Field(object owner, string name) => owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);

    private static string Flow(XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult result)
        => JsonSerializer.Serialize(AdmissionCharacterization.Flow(result));

    private static CSharpCompilation Compilation(string name, string source, string path)
        => CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path)],
            MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private sealed class Input : IDisposable
    {
        private readonly AssemblyMetadata metadata;
        internal CountingReference Reference { get; }
        internal AdmissionCharacterization.Fixture Fixture { get; }

        internal Input(int roots = 2)
        {
            using var image = new MemoryStream();
            Assert.True(Compilation("P3B.Dependency", "public class Dependency {}", "/P3B/Dependency.cs").Emit(image).Success);
            metadata = AssemblyMetadata.CreateFromImage(image.ToArray());
            Reference = new CountingReference(metadata);
            Fixture = new AdmissionCharacterization.Fixture(Enumerable.Range(0, roots).Select(index =>
                ($"/P3B/Root{index}.cs", $"public static partial class Entry {{ public static void Root{index}() {{ Shared(); }} }}"))
                .Append(("/P3B/Shared.cs", "public static partial class Entry { static void Shared() { throw null; } }")),
                MetadataReferences.Default.Append(Reference));
        }

        public void Dispose() => metadata.Dispose();
    }

    // Both the counter and throwing sentinel belong solely to a test input.
    private sealed class CountingReference(AssemblyMetadata metadata) : PortableExecutableReference(MetadataReferenceProperties.Assembly)
    {
        internal int Reads { get; set; }
        internal bool Fail { get; set; }
        protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;
        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties) => throw new NotSupportedException();
        protected override Metadata GetMetadataImpl()
        {
            Reads++;
            if (Fail) throw new IOException("P3B digest metadata sentinel");
            return metadata;
        }
    }
}
