using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizerTests.Execution.Semantic;
using static XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalyzerSelectionPolicy;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Proves exact, pure decisions and composition through the unchanged productive B5 router.</summary>
    public sealed class ExceptionFlowAnalyzerSelectionPolicyTests
    {
        private const string Source = "public static class Fixture { public static void Root() { Thrower(); } static void Thrower() { throw null; } }";

        [Theory]
        [InlineData((int)ExceptionFlowCompilerProvenance.CurrentCompilation, CurrentCompilerVersion, (int)ExceptionFlowAnalyzerSelection.Current)]
        [InlineData((int)ExceptionFlowCompilerProvenance.ValidatedPortablePdb, CurrentCompilerVersion, (int)ExceptionFlowAnalyzerSelection.Current)]
        [InlineData((int)ExceptionFlowCompilerProvenance.ValidatedPortablePdb, HistoricalCompilerVersion, (int)ExceptionFlowAnalyzerSelection.Historical)]
        public void ExactSupportedContexts_SelectOnlyTheRequiredEngine(int provenance, string version, int expected)
        {
            ExceptionFlowAnalyzerSelectionDecision decision = Select(new((ExceptionFlowCompilerProvenance)provenance, version));
            Assert.True(decision.Succeeded);
            Assert.Null(decision.Failure);
            Assert.Equal((ExceptionFlowAnalyzerSelection)expected, decision.Selection);
        }

        public static IEnumerable<object[]> UnsupportedVersions()
        {
            foreach (string version in new[]
            {
                "5.0.0.0", "5.0.0", "5.0.0-2.25567.12", "5.0.0-2.25451.107", "4.8.0-3.23524.11+f43cd10b737b6343956dee421cff8c50b602c788",
                CurrentCompilerVersion + "x", HistoricalCompilerVersion + "x", " " + CurrentCompilerVersion,
                CurrentCompilerVersion + " ", HistoricalCompilerVersion + "\n", CurrentCompilerVersion.ToUpperInvariant(),
                HistoricalCompilerVersion[..^1] + "1", CurrentCompilerVersion.Replace("25567.12", "25567.11", StringComparison.Ordinal),
                CurrentCompilerVersion.Replace("25567.12", "25567.13", StringComparison.Ordinal),
                HistoricalCompilerVersion.Replace("25451.107", "25451.106", StringComparison.Ordinal),
                HistoricalCompilerVersion.Replace("25451.107", "25451.108", StringComparison.Ordinal)
            })
            {
                yield return [version];
            }
        }

        [Theory]
        [MemberData(nameof(UnsupportedVersions))]
        public void ExactAllowlist_NeverAcceptsRangesPrefixesMissingCommitsOrNormalization(string version)
        {
            foreach (ExceptionFlowCompilerProvenance provenance in new[] { ExceptionFlowCompilerProvenance.CurrentCompilation, ExceptionFlowCompilerProvenance.ValidatedPortablePdb })
            {
                AssertFailure(Select(new(provenance, version)), ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        public void UnknownOrUnvalidatedProvenance_CannotSelectEvenAKnownVersion(int provenance)
        {
            foreach (string version in new[] { CurrentCompilerVersion, HistoricalCompilerVersion })
            {
                AssertFailure(Select(new((ExceptionFlowCompilerProvenance)provenance, version)), ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedProvenance);
            }
        }

        [Fact]
        public void NullAndDefaultContext_AreNotCurrent()
        {
            AssertFailure(Select(null), ExceptionFlowAnalyzerSelectionFailureCode.MissingContext);
            AssertFailure(Select(new()), ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedProvenance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public void MissingCompilerEvidence_FailsClosed(string? version)
            => AssertFailure(Select(new(ExceptionFlowCompilerProvenance.CurrentCompilation, version)), ExceptionFlowAnalyzerSelectionFailureCode.MissingCompilerVersion);

        [Fact]
        public void CurrentOwnedContextWithHistoricalRequirement_IsConflictingEvidence()
            => AssertFailure(Select(new(ExceptionFlowCompilerProvenance.CurrentCompilation, HistoricalCompilerVersion)), ExceptionFlowAnalyzerSelectionFailureCode.ConflictingEvidence);

        [Fact]
        public void SelectionIsPureDeterministicAndHasNoExecutionOwnerOrMutableState()
        {
            ExceptionFlowAnalyzerSelectionContext context = new(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, HistoricalCompilerVersion);
            ExceptionFlowAnalyzerSelectionDecision expected = Select(context);
            Parallel.For(0, 1000, _ => Assert.Equal(expected, Select(context)));
            Assert.All(typeof(ExceptionFlowAnalyzerSelectionPolicy).GetFields(BindingFlags.Static | BindingFlags.NonPublic), field => Assert.True(field.IsLiteral));
            Assert.Empty(typeof(ExceptionFlowAnalyzerSelectionPolicy).GetFields(BindingFlags.Instance | BindingFlags.NonPublic));
            string source = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Analysis", "ExceptionFlowAnalyzerSelectionPolicy.cs"));
            foreach (string forbidden in new[] { "Microsoft.CodeAnalysis", "ExceptionFlowAnalysisRouter", "HistoricalWorkerClient", "Process", "File.", "Directory.", "Assembly.", "Summary", "JsonSerializer", "StartsWith", "Trim", "Version.Parse", "Dapper" })
            {
                Assert.DoesNotContain(forbidden, source);
            }
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzerSelectionPolicy).Assembly.GetReferencedAssemblies(), reference =>
                reference.Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical");
        }

        [Fact]
        public void RealTargetValidatedPortablePdb_ProjectsExactVersionAndSelectsCurrent()
        {
            ExternalCompilationProvenanceDescriptor descriptor = RealProvenance();
            ExceptionFlowAnalyzerSelectionContext context = ExceptionFlowAnalyzerSelectionContext.FromValidatedPortablePdb(descriptor);
            Assert.Equal(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, context.Provenance);
            Assert.Equal(CurrentCompilerVersion, context.CompilerVersion);
            Assert.True(descriptor.CompilationOptions!.TryGetValue("compiler-version", out string exact));
            Assert.Equal(exact, context.CompilerVersion);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Current, Select(context).Selection);
            Assert.True(Select(context).Succeeded);
        }

        [Theory]
        [InlineData("null-descriptor")]
        [InlineData("missing-options")]
        [InlineData("missing-language")]
        [InlineData("wrong-language")]
        [InlineData("missing-schema")]
        [InlineData("wrong-schema")]
        [InlineData("missing-compiler")]
        public void IncompleteOrUnsupportedDescriptorProjection_NeverManufacturesCurrent(string scenario)
        {
            ExternalCompilationProvenanceDescriptor original = RealProvenance();
            // Synthetic option variants test the projection contract, not fresh cryptographic PDB validation.
            ExternalCompilationOptionsDescriptor options = new([
                new("language", scenario == "wrong-language" ? "Visual Basic" : "C#"),
                new("version", scenario == "wrong-schema" ? "3" : "2"), new("compiler-version", CurrentCompilerVersion)]);
            string? omitted = scenario switch { "missing-language" => "language", "missing-schema" => "version", "missing-compiler" => "compiler-version", _ => null };
            options = new([.. options.Options.Where(option => option.Key != omitted)]);
            ExternalCompilationProvenanceDescriptor? descriptor = scenario == "null-descriptor" ? null
                : new(original.DebugDirectory, original.PortablePdb, scenario == "missing-options" ? null : options, original.MetadataReferences);
            ExceptionFlowAnalyzerSelectionDecision decision = Select(ExceptionFlowAnalyzerSelectionContext.FromValidatedPortablePdb(descriptor));
            Assert.False(decision.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Unspecified, decision.Selection);
            Assert.NotNull(decision.Failure);
        }

        [Fact]
        public async Task SelectedCurrent_ExecutesExistingRouterWithNoWorkerRequirement()
        {
            ExceptionFlowAnalyzerSelectionDecision decision = Select(new(ExceptionFlowCompilerProvenance.CurrentCompilation, Info(typeof(CSharpCompilation).Assembly)));
            Assert.True(decision.Succeeded);
            (MethodDeclarationSyntax member, ExceptionFlowSummaryAnalysisSession session) = CurrentInput();
            ExceptionFlowAnalysisRouter router = new(new HistoricalWorkerClient(Path.Combine(Root(), "artifacts", "p5o2b6", "never-start"), WorkerPath(), TimeSpan.FromSeconds(30)));
            ExceptionFlowAnalysisRoutingResult routed = await router.AnalyzeAsync(new(decision.Selection, Current: new(member, session)));
            Assert.True(routed.Succeeded);
            Assert.NotNull(routed.CurrentAnalysis);
            Assert.Null(routed.HistoricalAnalysis);
            Assert.Equal(RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)), routed.Result);
        }

        [Fact]
        public async Task SelectedHistorical_ExecutesGenuineB5RouteWithParityAndIsolation()
        {
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            Guid beforeCommon = common.ManifestModule.ModuleVersionId;
            Guid beforeCsharp = csharp.ManifestModule.ModuleVersionId;
            ExceptionFlowAnalyzerSelectionDecision historicalDecision = Select(new(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, HistoricalCompilerVersion));
            ExceptionFlowAnalyzerSelectionDecision currentDecision = Select(new(ExceptionFlowCompilerProvenance.CurrentCompilation, Info(csharp)));
            Assert.True(historicalDecision.Succeeded);
            Assert.True(currentDecision.Succeeded);
            ExceptionFlowAnalysisRouter router = new(new HistoricalWorkerClient("dotnet", WorkerPath(), TimeSpan.FromSeconds(30)));
            (MethodDeclarationSyntax member, ExceptionFlowSummaryAnalysisSession session) = CurrentInput();
            ExceptionFlowAnalysisRoutingResult current = await router.AnalyzeAsync(new(currentDecision.Selection, Current: new(member, session)));
            ExceptionFlowAnalysisRoutingResult historical = await router.AnalyzeAsync(new(historicalDecision.Selection, Historical: new(Source, "Fixture", "Root", new())));
            Assert.True(current.Succeeded);
            Assert.True(historical.Succeeded, historical.HistoricalFailure?.Message);
            Assert.Equal(current.Result, historical.Result);
            Assert.Same(historical.HistoricalAnalysis!.Result, historical.Result);
            Assert.Null(historical.CurrentAnalysis);
            Assert.Equal("NullReferenceException", Assert.Single(historical.Result!.Entries).ExceptionType.MetadataName);
            int pid = historical.HistoricalAnalysis.ProcessId!.Value;
            Assert.NotEqual(Environment.ProcessId, pid);
            try { using Process child = Process.GetProcessById(pid); Assert.True(child.HasExited); } catch (ArgumentException) { }
            Assert.Equal(beforeCommon, common.ManifestModule.ModuleVersionId);
            Assert.Equal(beforeCsharp, csharp.ManifestModule.ModuleVersionId);
            Assert.All(historical.HistoricalAnalysis.Identity!.LoadedRoslyn, engine => Assert.Equal(HistoricalCompilerVersion, engine.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
                assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
                || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
            await File.WriteAllTextAsync(Path.Combine(Root(), "artifacts", "p5o2b6", "selection-runtime-parity.json"), JsonSerializer.Serialize(new
            {
                CurrentDecision = currentDecision,
                HistoricalDecision = historicalDecision,
                Current = new[] { Identity(common), Identity(csharp) },
                Historical = historical.HistoricalAnalysis.Identity,
                historical.HistoricalAnalysis.ProcessId,
                historical.HistoricalAnalysis.Provenance,
                Result = historical.Result,
                CanonicalParity = true,
                PolicyExecutedAnalysis = false,
                PolicyStartedWorker = false,
                ExecutedByExistingRouter = true,
                HistoricalLoadedInCaller = false,
                ProcessStopped = true
            }, WorkerProtocol.JsonOptions));
        }

        [Fact]
        public async Task SelectionFailure_RemainsNonExecutableAtTheUnchangedRouter()
        {
            ExceptionFlowAnalyzerSelectionDecision decision = Select(new(ExceptionFlowCompilerProvenance.ValidatedPortablePdb, "unknown"));
            AssertFailure(decision, ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion);
            (MethodDeclarationSyntax member, ExceptionFlowSummaryAnalysisSession session) = CurrentInput();
            ExceptionFlowAnalysisRoutingResult result = await new ExceptionFlowAnalysisRouter().AnalyzeAsync(new(decision.Selection, Current: new(member, session)));
            Assert.False(result.Succeeded);
            Assert.Null(result.CurrentAnalysis);
            Assert.Null(result.HistoricalAnalysis);
            Assert.Null(result.Result);
            Assert.Equal(ExceptionFlowAnalysisRoutingFailureCode.InvalidSelection, result.RoutingFailure!.Code);
        }

        private static void AssertFailure(ExceptionFlowAnalyzerSelectionDecision decision, ExceptionFlowAnalyzerSelectionFailureCode expected)
        {
            Assert.False(decision.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Unspecified, decision.Selection);
            Assert.Equal(expected, decision.Failure!.Code);
        }
        private static ExternalCompilationProvenanceDescriptor RealProvenance()
        {
            var emitted = ExternalCompilationProvenanceDescriptorFactoryTests.EmitPortablePdb("SelectionProof", "public class Fixture { }",
                new CSharpParseOptions(LanguageVersion.CSharp12), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(emitted.DebugDescriptor, [.. emitted.PdbImage], out ExternalCompilationProvenanceDescriptor descriptor));
            return descriptor;
        }
        private static (MethodDeclarationSyntax, ExceptionFlowSummaryAnalysisSession) CurrentInput()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            CSharpCompilation compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            ExceptionFlowSemanticEnvironment environment = new(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            return (tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root"),
                ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(environment));
        }
        private static string Info(Assembly assembly) => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        private static WorkerAssemblyIdentity Identity(Assembly assembly) => new(assembly.GetName().Name!, assembly.GetName().Version!.ToString(), Info(assembly),
            assembly.ManifestModule.ModuleVersionId.ToString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))));
        private static string WorkerPath() => Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker", "bin",
            new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
        private static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
    }
}
