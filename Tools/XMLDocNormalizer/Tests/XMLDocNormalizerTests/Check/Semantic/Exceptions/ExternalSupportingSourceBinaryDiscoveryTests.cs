using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using PreparedDependency = XMLDocNormalizerTests.Helpers.ExternalReconstructionPlanTestWorkspace.PreparedDependency;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Tests P7A integration with demand-driven P6C reconstruction and P6B
    /// exception-flow analysis.
    /// </summary>
    public sealed class ExternalSupportingSourceBinaryDiscoveryTests
    {
        /// <summary>
        /// Keeps roots and discovery plans inert until an external body is needed.
        /// </summary>
        [Fact]
        public void UnusedDiscoveryPlan_PerformsNoRegistration()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Unused");
            ConsumerFixture fixture = CreateConsumer(
                "P7A.Unused.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            ExternalAssemblyReferenceDescriptor descriptor = RegisterDiscoveryPlan(
                fixture,
                dependency,
                GetReferenceRoots(dependency));

            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Fails closed when discovery was requested without configured roots.
        /// </summary>
        [Fact]
        public void DiscoveryPlanWithoutRoots_RetainsMetadataFallback()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.NoRoots");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P7A.NoRoots.Consumer");
            RegisterDiscoveryPlan(fixture, dependency, roots: null);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Discovers all P5A references and completes existing P6C/P6A/P6B resolution.
        /// </summary>
        [Fact]
        public void ExactLocalReferences_EnableSourceBackedResolution()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Success");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P7A.Success.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = RegisterDiscoveryPlan(
                fixture,
                dependency,
                GetReferenceRoots(dependency));

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Reconstructs through already loaded exact metadata-reference paths.
        /// </summary>
        [Fact]
        public void LoadedMetadataReferences_EnableSourceBackedResolution()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "G1.Loaded.Success");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "G1.Loaded.Success.Consumer");
            Assert.True(ExternalReferenceArtifactSourceConfiguration.TryCreate(
                dependency.ReferencePaths,
                nuGetGlobalPackagesFolder: null,
                dotNetRoots: [],
                out ExternalReferenceArtifactSourceConfiguration configuration));
            Assert.True(fixture.Context.TryConfigureExternalBinaryArtifactSources(
                configuration));
            ExternalAssemblyReferenceDescriptor descriptor = RegisterDiscoveryPlan(
                fixture,
                dependency,
                roots: null);

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Finds one renamed reference through fallback while preserving every ordinal.
        /// </summary>
        [Fact]
        public void RenamedReferenceCandidate_CompletesReconstruction()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Renamed");
            string root = CreateCandidateRoot(workspace, dependency, renamedIndex: 0);
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P7A.Renamed.Consumer");
            RegisterDiscoveryPlan(fixture, dependency, [root]);

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Does not create a partial compilation when one required reference is missing.
        /// </summary>
        [Fact]
        public void PartialReferenceSet_FailsWholeReconstruction()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Partial");
            string root = CreateCandidateRoot(workspace, dependency, omittedIndex: 0);
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P7A.Partial.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = RegisterDiscoveryPlan(
                fixture,
                dependency,
                [root]);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Keeps an explicit P6C reference sequence authoritative and bypasses discovery.
        /// </summary>
        [Fact]
        public void ExplicitReferenceCandidates_BypassDiscovery()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Explicit");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P7A.Explicit.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(
                [Path.Combine(workspace.DirectoryPath, "missing-root")]));
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor)));

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Does not silently replace a wrong authoritative explicit path with discovery.
        /// </summary>
        [Fact]
        public void InvalidExplicitReferenceCandidate_DoesNotUseDiscovery()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.InvalidExplicit");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7A.InvalidExplicit.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            string[] candidates = dependency.ReferencePaths.ToArray();
            candidates[0] = dependency.TargetPath;
            Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(
                GetReferenceRoots(dependency)));
            Assert.True(ExternalReferenceArtifactSourceConfiguration.TryCreate(
                dependency.ReferencePaths,
                nuGetGlobalPackagesFolder: null,
                dotNetRoots: [],
                out ExternalReferenceArtifactSourceConfiguration artifactSources));
            Assert.True(fixture.Context.TryConfigureExternalBinaryArtifactSources(
                artifactSources));
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor, referencePaths: candidates)));

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Reuses successful discovery and P6A registration after candidate removal.
        /// </summary>
        [Fact]
        public void SuccessfulDiscovery_IsExactlyOnce()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Once.Success");
            string root = CreateCandidateRoot(workspace, dependency);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7A.Once.Success.Consumer");
            RegisterDiscoveryPlan(fixture, dependency, [root]);
            Assert.True(TryResolve(fixture, dependency.Reference, out SemanticCompilationScope first));

            foreach (string path in Directory.EnumerateFiles(root))
            {
                File.Delete(path);
            }

            Assert.True(TryResolve(fixture, dependency.Reference, out SemanticCompilationScope second));
            Assert.Same(first, second);
        }

        /// <summary>
        /// Caches a failed discovery even when matching files appear later.
        /// </summary>
        [Fact]
        public void FailedDiscovery_IsExactlyOnce()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Once.Failure");
            string root = Path.Combine(workspace.DirectoryPath, "empty-root");
            Directory.CreateDirectory(root);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7A.Once.Failure.Consumer");
            RegisterDiscoveryPlan(fixture, dependency, [root]);
            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            CopyReferenceCandidates(dependency, root);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Discovers only dependencies reached by the existing summary graph.
        /// </summary>
        [Fact]
        public void MultipleDependencies_DiscoverOnlyReachedPlans()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency first = workspace.Prepare(
                "P7A.Multi.First",
                [new SourceInput("/_/First.cs", "namespace First { public static class Api { "
                    + "public static void A() { } } }")]);
            PreparedDependency unused = workspace.Prepare(
                "P7A.Multi.Unused",
                [new SourceInput("/_/Unused.cs", "namespace Unused { public static class Api { "
                    + "public static void B() { } } }")]);
            PreparedDependency third = workspace.Prepare(
                "P7A.Multi.Third",
                [new SourceInput("/_/Third.cs", "namespace Third { public static class Api { "
                    + "public static void C() { } } }")]);
            ConsumerFixture fixture = CreateConsumer(
                "P7A.Multi.Consumer",
                "public static class Consumer { public static void M() { "
                + "First.Api.A(); Third.Api.C(); } }",
                first.Reference,
                unused.Reference,
                third.Reference);
            Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(
                GetReferenceRoots(first, unused, third)));
            ExternalAssemblyReferenceDescriptor firstDescriptor = RegisterDiscoveryPlan(
                fixture,
                first,
                roots: null);
            ExternalAssemblyReferenceDescriptor unusedDescriptor = RegisterDiscoveryPlan(
                fixture,
                unused,
                roots: null);
            ExternalAssemblyReferenceDescriptor thirdDescriptor = RegisterDiscoveryPlan(
                fixture,
                third,
                roots: null);

            _ = BuildGraph(fixture, "M");

            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(firstDescriptor, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(unusedDescriptor, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(thirdDescriptor, out _));
        }

        /// <summary>
        /// Discovers B only when analysis of reconstructed A reaches B.
        /// </summary>
        [Fact]
        public void CrossExternalCall_DiscoversEachDependencyLazily()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency downstream = workspace.Prepare(
                "P7A.Chain.Downstream",
                [new SourceInput("/_/Downstream.cs", "using System; namespace Downstream { "
                    + "public static class Api { public static void B() { "
                    + "throw new InvalidOperationException(); } } }")]);
            string downstreamCandidatePath = Path.Combine(
                workspace.DirectoryPath,
                "P7A.Chain.Downstream.dll");
            File.WriteAllBytes(downstreamCandidatePath, downstream.PeImage);
            PortableExecutableReference downstreamCandidate = MetadataReference.CreateFromImage(
                ImmutableArray.Create(downstream.PeImage),
                filePath: downstreamCandidatePath);
            PreparedDependency upstream = workspace.Prepare(
                "P7A.Chain.Upstream",
                [new SourceInput("/_/Upstream.cs", "namespace Upstream { public static class Api { "
                    + "public static void A() { Downstream.Api.B(); } } }")],
                additionalReferences: [downstreamCandidate]);
            ConsumerFixture fixture = CreateConsumer(
                "P7A.Chain.Consumer",
                "public static class Consumer { public static void M() { Upstream.Api.A(); } }",
                downstream.Reference,
                upstream.Reference);
            Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(
                GetReferenceRoots(downstream, upstream)
                    .Append(workspace.DirectoryPath)));
            ExternalAssemblyReferenceDescriptor downstreamDescriptor = RegisterDiscoveryPlan(
                fixture,
                downstream,
                roots: null);
            ExternalAssemblyReferenceDescriptor upstreamDescriptor = RegisterDiscoveryPlan(
                fixture,
                upstream,
                roots: null);

            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(
                downstreamDescriptor,
                out _));
            ExceptionFlowSummaryGraphTestRun run = BuildGraph(fixture, "M");

            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(upstreamDescriptor, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(downstreamDescriptor, out _));
            ExceptionFlowSummaryCallEdge upstreamEdge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge downstreamEdge = Assert.Single(
                run.GetRequiredSummary(upstreamEdge.Target).CallEdges);
            Assert.Contains(
                run.GetRequiredSummary(downstreamEdge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Keeps root configuration and discovery caches isolated per context.
        /// </summary>
        [Fact]
        public void DiscoveryConfiguration_IsContextLocal()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Context");
            ConsumerFixture first = CreateCallingConsumer(dependency, "P7A.Context.First");
            ConsumerFixture second = CreateCallingConsumer(dependency, "P7A.Context.Second");
            RegisterDiscoveryPlan(first, dependency, GetReferenceRoots(dependency));
            RegisterDiscoveryPlan(second, dependency, roots: null);

            Assert.True(TryResolve(first, dependency.Reference, out _));
            Assert.False(TryResolve(second, dependency.Reference, out _));
        }

        /// <summary>
        /// Preserves the four existing mode decisions about external body demand.
        /// </summary>
        [Theory]
        [InlineData("Direct", false)]
        [InlineData("ProjectTransitiveDeclaredExceptions", false)]
        [InlineData("ProjectTransitive", true)]
        [InlineData("SolutionTransitive", true)]
        public void AnalysisModes_DiscoverOnlyWhenExternalBodiesAreNeeded(
            string modeName,
            bool expectsDiscovery)
        {
            ExceptionAnalysisMode mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Mode." + modeName);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7A.Mode.Consumer." + modeName,
                documented: true);
            ExternalAssemblyReferenceDescriptor descriptor = RegisterDiscoveryPlan(
                fixture,
                dependency,
                GetReferenceRoots(dependency));

            _ = Find(fixture, mode);

            Assert.Equal(
                expectsDiscovery,
                fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Produces the required disabled/exact/wrong-candidate finding diff.
        /// </summary>
        [Fact]
        public void ControlledFindingDiff_OnlyExactDiscoveryAddsSourceFlow()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7A.Findings");
            ConsumerFixture disabled = CreateCallingConsumer(
                dependency,
                "P7A.Findings.Disabled",
                documented: true);
            ConsumerFixture exact = CreateCallingConsumer(
                dependency,
                "P7A.Findings.Exact",
                documented: true);
            ConsumerFixture wrong = CreateCallingConsumer(
                dependency,
                "P7A.Findings.Wrong",
                documented: true);
            RegisterDiscoveryPlan(exact, dependency, GetReferenceRoots(dependency));
            string wrongRoot = Path.Combine(workspace.DirectoryPath, "wrong-root");
            Directory.CreateDirectory(wrongRoot);
            File.WriteAllText(Path.Combine(wrongRoot, "wrong.dll"), "not a PE");
            RegisterDiscoveryPlan(wrong, dependency, [wrongRoot]);

            List<Finding> disabledFindings = Find(
                disabled,
                ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> exactFindings = Find(
                exact,
                ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> wrongFindings = Find(
                wrong,
                ExceptionAnalysisMode.SolutionTransitive);

            Assert.Empty(disabledFindings);
            Finding finding = Assert.Single(exactFindings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("InvalidOperationException", finding.Message);
            Assert.Empty(wrongFindings);
            Assert.Equal(disabledFindings, wrongFindings);
            Assert.Equal(ExceptionFlowAnalyzerTestHelper.SourcePath, finding.FilePath);
        }

        private static PreparedDependency Prepare(
            ExternalReconstructionPlanTestWorkspace workspace,
            string assemblyName)
        {
            return workspace.Prepare(
                assemblyName,
                [new SourceInput("/_/External.cs", "using System; namespace External { "
                    + "public static class Api { public static void A() { "
                    + "throw new InvalidOperationException(); } } }")]);
        }

        private static string[] GetReferenceRoots(params PreparedDependency[] dependencies)
        {
            return dependencies
                .SelectMany(static dependency => dependency.ReferencePaths)
                .Select(static path => Path.GetDirectoryName(path)!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string CreateCandidateRoot(
            ExternalReconstructionPlanTestWorkspace workspace,
            PreparedDependency dependency,
            int? omittedIndex = null,
            int? renamedIndex = null)
        {
            string root = Path.Combine(
                workspace.DirectoryPath,
                "reference-candidates-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            for (int index = 0; index < dependency.ReferencePaths.Length; index++)
            {
                if (index == omittedIndex)
                {
                    continue;
                }

                string name = index == renamedIndex
                    ? "renamed-reference.dll"
                    : Path.GetFileName(dependency.ReferencePaths[index]);
                File.Copy(dependency.ReferencePaths[index], Path.Combine(root, name));
            }

            return root;
        }

        private static void CopyReferenceCandidates(
            PreparedDependency dependency,
            string root)
        {
            foreach (string source in dependency.ReferencePaths)
            {
                File.Copy(source, Path.Combine(root, Path.GetFileName(source)));
            }
        }

        private static ConsumerFixture CreateCallingConsumer(
            PreparedDependency dependency,
            string assemblyName,
            bool documented = false)
        {
            string documentation = documented
                ? "/// <summary>Runs.</summary>\n"
                : string.Empty;
            return CreateConsumer(
                assemblyName,
                "public static class Consumer { " + documentation
                + "public static void M() { External.Api.A(); } }",
                dependency.Reference);
        }

        private static ConsumerFixture CreateConsumer(
            string assemblyName,
            string source,
            params PortableExecutableReference[] references)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                source,
                path: ExceptionFlowAnalyzerTestHelper.SourcePath);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                [tree],
                MetadataReferences.Default.Concat(references),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(compilation.GetDiagnostics().Where(
                static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            return new ConsumerFixture(tree, compilation, context);
        }

        private static ExternalAssemblyReferenceDescriptor RegisterDiscoveryPlan(
            ConsumerFixture fixture,
            PreparedDependency dependency,
            IEnumerable<string>? roots)
        {
            if (roots != null)
            {
                Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(roots));
            }

            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreateDiscoveryPlan(descriptor)));
            return descriptor;
        }

        private static ExternalAssemblyReferenceDescriptor GetDescriptor(
            ConsumerFixture fixture,
            PortableExecutableReference reference)
        {
            IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                fixture.Compilation.GetAssemblyOrModuleSymbol(reference));
            Assert.True(fixture.Context.TryGetExternalAssemblyReferenceDescriptor(
                fixture.Compilation,
                assembly,
                out ExternalAssemblyReferenceDescriptor descriptor));
            return descriptor;
        }

        private static bool TryResolve(
            ConsumerFixture fixture,
            PortableExecutableReference reference,
            out SemanticCompilationScope scope)
        {
            IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                fixture.Compilation.GetAssemblyOrModuleSymbol(reference));
            return fixture.Context.TryGetExternalSupportingSourceScope(
                fixture.Compilation,
                assembly,
                out scope);
        }

        private static ExceptionFlowSummaryGraphTestRun BuildGraph(
            ConsumerFixture fixture,
            string methodName)
        {
            MethodDeclarationSyntax method = Assert.Single(
                fixture.Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(),
                declaration => declaration.Identifier.ValueText == methodName);
            Assert.True(ExceptionFlowAnalyzer.TryBuildTransitiveSummaryGraph(
                method,
                new ExceptionFlowSemanticEnvironment(fixture.Context),
                out ExceptionFlowSummaryGraph graph,
                out ExceptionFlowCallableKey? rootKey));
            return new ExceptionFlowSummaryGraphTestRun(
                graph,
                Assert.IsType<ExceptionFlowCallableKey>(rootKey),
                fixture.Compilation);
        }

        private static List<Finding> Find(
            ConsumerFixture fixture,
            ExceptionAnalysisMode mode)
        {
            return XmlDocExceptionSemanticDetector.FindExceptionSmells(
                fixture.Tree,
                ExceptionFlowAnalyzerTestHelper.SourcePath,
                fixture.Compilation.GetSemanticModel(fixture.Tree),
                fixture.Context,
                new XmlDocOptions { ExceptionAnalysisMode = mode });
        }

        private sealed record ConsumerFixture(
            SyntaxTree Tree,
            CSharpCompilation Compilation,
            ProjectClosureSemanticContext Context);
    }
}
