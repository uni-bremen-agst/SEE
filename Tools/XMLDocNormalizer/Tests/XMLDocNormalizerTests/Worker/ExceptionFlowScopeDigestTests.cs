using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Characterizes the existing digest function, without materialization or product instrumentation.</summary>
public sealed class ExceptionFlowScopeDigestTests
{
    private const string Source = "public static class Entry { public static void A() {} }";

    [Theory]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    public void FixedViewManyRootsAndRepeatedWarmRootsHaveIdenticalDigests(int count, bool reverse)
    {
        var fixture = new AdmissionCharacterization.Fixture(Enumerable.Range(0, count).Select(index =>
            ($"/P3A/Root{index}.cs", $"public static partial class Entry {{ public static void Root{index}() {{ Shared(); }} }}"))
            .Append(("/P3A/Shared.cs", "public static partial class Entry { static void Shared() {} }")));
        var run = fixture.Session();
        var view = run.Environment.GetAnalysisScopes();
        string digest = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment);
        int sharedInspections = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol =>
        {
            if (symbol.Name == "Shared" && ++sharedInspections > 1)
                throw new InvalidOperationException("The cold/warm characterization did not reuse the shared fact.");
        });
        var order = Enumerable.Range(0, count).ToArray();
        if (reverse) Array.Reverse(order);
        var certificates = new Dictionary<int, string>();
        foreach (int index in order.Concat(order.Reverse()).Concat([order[0], order[0]]))
        {
            var outcome = session.Analyze(fixture.Root("Root" + index)).ProfileOutcome!;
            Assert.True(outcome.Complete);
            Assert.Equal(digest, outcome.ScopeDigest);
            Assert.Equal(digest, ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment));
            Assert.Same(view, run.Environment.GetAnalysisScopes());
            string json = JsonSerializer.Serialize(outcome);
            if (certificates.TryGetValue(index, out var previous)) Assert.Equal(previous, json);
            else
            {
                var cold = fixture.Session();
                Assert.Equal(JsonSerializer.Serialize(cold.Session.Analyze(fixture.Root("Root" + index)).ProfileOutcome), json);
                certificates.Add(index, json);
            }
        }
        Assert.Equal(count, certificates.Count);
        Assert.Equal(1, sharedInspections);
        Assert.Equal(count, certificates.Values.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceOrderIsCanonicalOnlyWhenPathsDiffer(bool equalPaths)
    {
        var first = Tree("class First {}", "/P3A/A.cs");
        var second = Tree("class Second {}", equalPaths ? first.FilePath : "/P3A/B.cs");
        var forward = Compilation([first, second]);
        var reversed = forward.RemoveAllSyntaxTrees().AddSyntaxTrees(second, first);
        Assert.Equal(!equalPaths, Digest(forward) == Digest(reversed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopeOrderIsCanonicalOnlyWhenAssemblyKeysDiffer(bool equalKeys)
    {
        var first = Compilation([Tree("class First {}", "/P3A/A.cs")], "P3A.First");
        var second = Compilation([Tree("class Second {}", "/P3A/B.cs")], equalKeys ? "P3A.First" : "P3A.Second");
        Assert.Equal(!equalKeys, Digest(first, second) == Digest(second, first));
    }

    [Fact]
    public void ReferenceAndAliasInsertionOrderIsCanonical()
    {
        var references = MetadataReferences.Default.ToArray();
        references[0] = references[0].WithAliases(["global", "Z", "A"]);
        var compilation = Compilation([Tree(Source, "/P3A/A.cs")]).WithReferences(references);
        var reordered = references.Reverse().Select(reference => reference == references[0]
            ? reference.WithAliases(["A", "global", "Z"]) : reference);
        Assert.Equal(Digest(compilation), Digest(compilation.WithReferences(reordered)));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("assembly")]
    [InlineData("output-kind")]
    [InlineData("embed-interop")]
    public void AdditionalDigestInputsAreObserved(string change)
    {
        var compilation = Compilation([Tree(Source, "/P3A/A.cs"), Tree("class Other {}", "/P3A/B.cs")]);
        var different = change switch
        {
            "add" => compilation.AddSyntaxTrees(Tree("class Added {}", "/P3A/C.cs")),
            "remove" => compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last()),
            "assembly" => compilation.WithAssemblyName("P3A.Other"),
            "output-kind" => compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.ConsoleApplication)),
            _ => compilation.ReplaceReference(compilation.References.Last(),
                compilation.References.Last().WithProperties(compilation.References.Last().Properties.WithEmbedInteropTypes(true)))
        };
        Assert.NotEqual(Digest(compilation), Digest(different));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefinesAndFeaturesUseOrdinalOrdering(bool changeFeature)
    {
        var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12)
            .WithPreprocessorSymbols("Z", "A").WithFeatures(new Dictionary<string, string> { ["z"] = "1", ["a"] = "2" });
        var other = parse.WithPreprocessorSymbols("A", "Z").WithFeatures(
            new Dictionary<string, string> { ["a"] = "2", ["z"] = changeFeature ? "3" : "1" });
        Assert.Equal(!changeFeature, Digest(Compilation([CSharpSyntaxTree.ParseText(Source, parse, "/P3A/A.cs")]))
            == Digest(Compilation([CSharpSyntaxTree.ParseText(Source, other, "/P3A/A.cs")])));
    }

    [Fact]
    public void SpecifiedLanguageVersionIsObservedEvenWhenEffectiveLanguageAgrees()
    {
        var defaults = CSharpParseOptions.Default;
        var explicitVersion = defaults.WithLanguageVersion(defaults.LanguageVersion);
        Assert.Equal(defaults.LanguageVersion, explicitVersion.LanguageVersion);
        Assert.NotEqual(defaults.SpecifiedLanguageVersion, explicitVersion.SpecifiedLanguageVersion);
        Assert.NotEqual(Digest(Compilation([CSharpSyntaxTree.ParseText(Source, defaults, "/P3A/A.cs")])),
            Digest(Compilation([CSharpSyntaxTree.ParseText(Source, explicitVersion, "/P3A/A.cs")])));
    }

    [Theory]
    [InlineData("order")]
    [InlineData("text")]
    [InlineData("options")]
    [InlineData("parse")]
    [InlineData("path")]
    [InlineData("references")]
    public void CompilationReferenceUsesItsOwnSortedSourceHashes(string change)
    {
        var dependency = Compilation([Tree("public class First {}", "/P3A/DepA.cs"),
            Tree("public class Second {}", "/P3A/DepB.cs")], "P3A.Dependency");
        var different = change switch
        {
            "order" => dependency.RemoveAllSyntaxTrees().AddSyntaxTrees(dependency.SyntaxTrees.Reverse()),
            "text" => dependency.ReplaceSyntaxTree(dependency.SyntaxTrees.Last(), Tree("public class Second { } // changed", "/P3A/DepB.cs")),
            "path" => dependency.ReplaceSyntaxTree(dependency.SyntaxTrees.Last(), Tree("public class Second {}", "/P3A/Other.cs")),
            "parse" => dependency.RemoveAllSyntaxTrees().AddSyntaxTrees(dependency.SyntaxTrees.Select(tree =>
                CSharpSyntaxTree.ParseText(tree.GetText(), ((CSharpParseOptions)tree.Options).WithPreprocessorSymbols("FEATURE"), tree.FilePath))),
            "references" => dependency.WithReferences(dependency.References.Reverse()),
            _ => dependency.WithOptions(dependency.Options.WithOverflowChecks(true))
        };
        var primary = Compilation([Tree(Source, "/P3A/A.cs")]);
        Assert.Equal(change is not ("text" or "path"), Digest(primary.AddReferences(dependency.ToMetadataReference()))
            == Digest(primary.AddReferences(different.ToMetadataReference())));
        // The options of a dependency are observed when it is an actual analysis scope.
        if (change == "options") Assert.NotEqual(Digest(primary, dependency), Digest(primary, different));
    }

    [Fact]
    public void ScopeRolesProjectIdsAndUnserializedOptionsAreNotDigestInputs()
    {
        var compilation = Compilation([Tree(Source, "/P3A/A.cs")]);
        var target = SemanticCompilationScope.CreateAnalysisTarget(compilation, ProjectId.CreateNewId());
        var referenced = SemanticCompilationScope.CreateReferencedProject(compilation, ProjectId.CreateNewId());
        Assert.Equal(DigestScopes([target]), DigestScopes([referenced]));
        Assert.Equal(Digest(compilation), Digest(compilation.WithOptions(compilation.Options
            .WithConcurrentBuild(!compilation.Options.ConcurrentBuild).WithDeterministic(!compilation.Options.Deterministic))));
        Assert.Equal(Digest(compilation), Digest(compilation.Clone()));
    }

    [Fact]
    public void SupportingRegistrationCreatesANewSnapshotAndChangesTheNextRootDigest()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P3A/A.cs", Source)]);
        var run = fixture.Session();
        var before = run.Environment.GetAnalysisScopes();
        var original = run.Session.Analyze(fixture.Root("A")).ProfileOutcome!;
        var support = Compilation([Tree("class Support {}", "/P3A/Support.cs")], "P3A.Support");
        run.Context.RegisterSupportingSource(support);
        var after = run.Environment.GetAnalysisScopes();
        Assert.NotSame(before, after);
        Assert.Single(before);
        Assert.Equal(2, after.Count);
        Assert.Same(after, run.Environment.GetAnalysisScopes());
        var expanded = run.Session.Analyze(fixture.Root("A")).ProfileOutcome!;
        Assert.True(expanded.Complete);
        Assert.NotEqual(original.ScopeDigest, expanded.ScopeDigest);
        Assert.Equal(Digest(fixture.Compilation, support), expanded.ScopeDigest);
        Assert.Equal(original.ScopeDigest, DigestScopes(before.Cast<SemanticCompilationScope>().ToArray()));
        run.Context.RegisterSupportingSource(support);
        Assert.Same(after, run.Environment.GetAnalysisScopes());
    }

    [Fact]
    public void SupportingDiscoveryOrderDoesNotMatterForDistinctAssemblyKeys()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P3A/A.cs", Source)]);
        var first = fixture.Session();
        var second = fixture.Session();
        var a = Compilation([Tree("class A {}", "/P3A/SupportA.cs")], "P3A.SupportA");
        var b = Compilation([Tree("class B {}", "/P3A/SupportB.cs")], "P3A.SupportB");
        first.Context.RegisterSupportingSource(a); first.Context.RegisterSupportingSource(b);
        second.Context.RegisterSupportingSource(b); second.Context.RegisterSupportingSource(a);
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(first.Environment),
            ExceptionFlowFiniteOrbitAdmission.ScopeDigest(second.Environment));
    }

    [Fact]
    public void MetadataFailureReturnsNoDigestAndAnExplicitLaterCallCanSucceed()
    {
        var dependency = Compilation([Tree("public class Dependency {}", "/P3A/Dependency.cs")], "P3A.Dependency");
        using var image = new MemoryStream();
        Assert.True(dependency.Emit(image).Success);
        using var metadata = AssemblyMetadata.CreateFromImage(image.ToArray());
        var reference = new FailingReference(metadata);
        var compilation = Compilation([Tree(Source, "/P3A/A.cs")]).AddReferences(reference);
        var environment = Environment(compilation);
        string? published = null;
        Assert.Throws<IOException>(() => published = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(environment));
        Assert.Null(published);
        reference.Fail = false;
        published = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(environment);
        Assert.Matches("^[0-9A-F]{64}$", published);
        Assert.Equal(published, ExceptionFlowFiniteOrbitAdmission.ScopeDigest(environment));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeldPeImageUsesBoundOwnerAndModuleIdentityWithoutOpeningDisplayPath(bool module)
    {
        var dependency = Compilation([Tree("public class Dependency {}", "/P3A/Dependency.cs")], "P3A.Dependency");
        if (module) dependency = dependency.WithOptions(dependency.Options.WithOutputKind(OutputKind.NetModule));
        using var image = new MemoryStream();
        Assert.True(dependency.Emit(image).Success);
        using Metadata metadata = module ? ModuleMetadata.CreateFromImage(image.ToArray()) : AssemblyMetadata.CreateFromImage(image.ToArray());
        string directory = Path.Combine(Path.GetTempPath(), "P3A-absent-" + Guid.NewGuid().ToString("N"));
        string first = Path.Combine(directory, "first.dll"), second = Path.Combine(directory, "second.dll");
        Assert.False(Directory.Exists(directory));
        var compilation = Compilation([Tree(Source, "/P3A/A.cs")]);
        PortableExecutableReference Reference(string path) => metadata is ModuleMetadata netmodule
            ? netmodule.GetReference(filePath: path) : ((AssemblyMetadata)metadata).GetReference(filePath: path);
        // A bound assembly uses its canonical owner; a netmodule takes the existing module:Display branch.
        Assert.Equal(!module, Digest(compilation.AddReferences(Reference(first)))
            == Digest(compilation.AddReferences(Reference(second))));
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task HistoricalFailureDetailsExposeTheSameDigestAcrossRootsAndCurrentCompiler()
    {
        const string source = "public static class Entry { public static void Root() { Missing(); } public static void A() { Missing(); } public static void B() { Missing(); } static extern void Missing(); }";
        string expected = ExceptionFlowFiniteOrbitAdmissionTests.WorkerSourceOutcome(source).ProfileOutcome!.ScopeDigest;
        string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(),
            "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin",
            new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
        foreach (string root in new[] { "A", "B", "A" })
        {
            var input = new WorkerAnalysisInput(source, "Entry", root, new());
            var start = new ProcessStartInfo("dotnet")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(worker);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new WorkerRequest(WorkerProtocol.Version, "analyze", input), WorkerProtocol.JsonOptions));
                process.StandardInput.Close();
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                var response = JsonSerializer.Deserialize<WorkerResponse>(await output, WorkerProtocol.JsonOptions)!;
                Assert.Equal(1, process.ExitCode);
                Assert.False(response.Success);
                Assert.Null(response.Result);
                Assert.Equal(WorkerFailureCode.UnsupportedContext, response.Failure!.Code);
                Assert.Equal("Scope=" + expected, Assert.Single(response.Failure.Details, detail => detail.StartsWith("Scope=", StringComparison.Ordinal)));
                Assert.All(response.Identity!.LoadedRoslyn, compiler => Assert.StartsWith("5.0.0-2.25451.107+", compiler.InformationalVersion));
                var validated = XMLDocNormalizer.Execution.Historical.HistoricalWorkerResponseValidation.Validate(
                    await output, await error, process.ExitCode, input, process.Id);
                Assert.False(validated.Success);
                Assert.NotNull(validated.Identity);
            }
            finally
            {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            }
        }
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical");
    }

    private static SyntaxTree Tree(string text, string path) => CSharpSyntaxTree.ParseText(text,
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12), path);

    private static CSharpCompilation Compilation(IEnumerable<SyntaxTree> trees, string assembly = "P3A.Input")
        => CSharpCompilation.Create(assembly, trees, MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static ExceptionFlowSemanticEnvironment Environment(params CSharpCompilation[] compilations)
        => new(new ProjectClosureSemanticContext(compilations.Select(SemanticCompilationScope.CreateSupportingSourceDependency).ToArray(), []));

    private static string Digest(params CSharpCompilation[] compilations)
        => ExceptionFlowFiniteOrbitAdmission.ScopeDigest(Environment(compilations));

    private static string DigestScopes(SemanticCompilationScope[] scopes)
        => ExceptionFlowFiniteOrbitAdmission.ScopeDigest(new(new ProjectClosureSemanticContext(scopes, [])));

    // A test-owned PE input models a transient metadata read failure; no product seam or counter.
    private sealed class FailingReference(AssemblyMetadata metadata) : PortableExecutableReference(MetadataReferenceProperties.Assembly)
    {
        internal bool Fail { get; set; } = true;
        protected override DocumentationProvider CreateDocumentationProvider() => DocumentationProvider.Default;
        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => throw new NotSupportedException();
        protected override Metadata GetMetadataImpl() => Fail ? throw new IOException("P3A metadata read failure") : metadata;
    }
}
