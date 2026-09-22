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

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests demand-driven orchestration from explicit immutable candidate
    /// plans into existing P6A/P6B behavior.
    /// </summary>
    public sealed class ExternalSupportingSourceReconstructionTests
    {
        /// <summary>
        /// Keeps registration free of candidate I/O and leaves an unused plan
        /// unmaterialized.
        /// </summary>
        [Fact]
        public void RegisteredButUnusedPlan_PerformsNoReconstruction()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Unused");
            ConsumerFixture fixture = CreateConsumer(
                "P6C.Unused.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            ExternalSupportingSourceReconstructionPlan plan =
                dependency.CreatePlan(descriptor);

            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(plan));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
            Assert.True(File.Exists(dependency.TargetPath));
            Assert.True(File.Exists(dependency.PdbPath));
        }

        /// <summary>
        /// Uses immutable defensive snapshots and rejects conflicting plans
        /// without replacing the first plan.
        /// </summary>
        [Fact]
        public void PlanRegistration_IsImmutableIdempotentAndConflictSafe()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Plan");
            ConsumerFixture fixture = CreateConsumer(
                "P6C.Plan.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            List<string> references = dependency.ReferencePaths.ToList();
            List<ExternalSourceReconstructionInput> sources =
            [
                new(0, dependency.SourcePaths[0])
            ];
            ExternalSupportingSourceReconstructionPlan first = new(
                descriptor,
                dependency.TargetPath,
                dependency.PdbPath,
                references,
                sources);
            ExternalSupportingSourceReconstructionPlan equal = new(
                descriptor,
                dependency.TargetPath,
                dependency.PdbPath,
                dependency.ReferencePaths,
                sources);
            ExternalSupportingSourceReconstructionPlan conflicting = new(
                descriptor,
                dependency.TargetPath + ".different",
                dependency.PdbPath,
                dependency.ReferencePaths,
                sources);
            references.Clear();
            sources.Clear();

            Assert.NotEmpty(first.ReferenceCandidatePaths);
            Assert.Single(first.SourceInputs);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(first));
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(equal));
            Assert.False(
                fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(conflicting));
        }

        /// <summary>
        /// Reconstructs only on the binding-aware P6B lookup and makes the
        /// resulting scope available through the ordinary P6A lookup.
        /// </summary>
        [Fact]
        public void NeededCallable_ReconstructsAndRegistersLazily()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Lazy");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.Lazy.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = RegisterPlan(
                fixture,
                dependency);

            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
            Assert.True(TryResolve(fixture, dependency.Reference, out SemanticCompilationScope scope));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(
                descriptor,
                out SemanticCompilationScope registered));
            Assert.Same(scope, registered);
            Assert.Equal(
                SemanticCompilationScopeKind.SupportingSourceDependency,
                scope.Kind);
        }

        /// <summary>
        /// Leaves a dependency without an exact plan on the prior metadata path.
        /// </summary>
        [Fact]
        public void NoPlan_RetainsMetadataFallback()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.NoPlan");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.NoPlan.Consumer");

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Revalidates the explicit target candidate against the plan key.
        /// </summary>
        [Fact]
        public void WrongTargetCandidate_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency expected = Prepare(workspace, "P6C.Target.Expected");
            PreparedDependency wrong = Prepare(workspace, "P6C.Target.Wrong");
            ConsumerFixture fixture = CreateCallingConsumer(expected, "P6C.Target.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                expected.Reference);

            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                expected.CreatePlan(descriptor, targetPath: wrong.TargetPath)));
            Assert.False(TryResolve(fixture, expected.Reference, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Fails closed for a Portable PDB belonging to another build.
        /// </summary>
        [Fact]
        public void WrongPortablePdb_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency expected = Prepare(workspace, "P6C.Pdb.Expected");
            PreparedDependency wrong = workspace.Prepare(
                "P6C.Pdb.Wrong",
                [new SourceInput("/_/Different.cs", "public sealed class Different { }")]);
            ConsumerFixture fixture = CreateCallingConsumer(expected, "P6C.Pdb.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                expected.Reference);
            Assert.True(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                [expected.PdbPath],
                [],
                [],
                out ExternalPortablePdbAcquisitionConfiguration pdbSources));
            Assert.True(fixture.Context.TryConfigureExternalPortablePdbSources(pdbSources));

            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                expected.CreatePlan(descriptor, pdbPath: wrong.PdbPath)));
            Assert.False(TryResolve(fixture, expected.Reference, out _));
        }

        /// <summary>
        /// Uses an exact embedded Portable PDB only when a reached callable
        /// triggers its explicitly acquisition-enabled plan.
        /// </summary>
        [Fact]
        public void EmbeddedPortablePdb_ReconstructsOnDemand()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P6C.EmbeddedPdb",
                [Source("EmbeddedPdb.cs")],
                embedSources: true,
                embedPortablePdb: true);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P6C.EmbeddedPdb.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            ExternalSupportingSourceReconstructionPlan plan =
                ExternalSupportingSourceReconstructionPlan
                    .CreateWithLocalPortablePdbDiscovery(
                        descriptor,
                        dependency.TargetPath,
                        dependency.ReferencePaths,
                        [new ExternalSourceReconstructionInput(0, candidatePath: null)]);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(plan));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Fails closed when one ordinal reference candidate is wrong.
        /// </summary>
        [Fact]
        public void WrongReferenceCandidate_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Reference");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.Reference.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            string[] referencePaths = dependency.ReferencePaths.ToArray();
            referencePaths[0] = dependency.TargetPath;

            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor, referencePaths: referencePaths)));
            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Fails closed when an explicitly mapped source file violates its PDB checksum.
        /// </summary>
        [Fact]
        public void WrongSourceCandidate_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Source");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.Source.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            string wrongSource = Path.Combine(workspace.DirectoryPath, "wrong-source.candidate");
            File.WriteAllText(wrongSource, "public sealed class Wrong { }");

            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(
                    descriptor,
                    sourceInputs: [new ExternalSourceReconstructionInput(0, wrongSource)])));
            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Uses validated embedded source without requiring a source-file candidate.
        /// </summary>
        [Fact]
        public void EmbeddedSource_ReconstructsWithoutSourceFileCandidate()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P6C.Embedded",
                [Source("Embedded.cs")],
                embedSources: true);
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.Embedded.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = RegisterPlan(fixture, dependency);
            File.Delete(dependency.SourcePaths[0]);

            Assert.True(TryResolve(fixture, dependency.Reference, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Preserves P5G's fail-closed handling of a language-version shape
        /// that cannot be reconstructed by the supported policy.
        /// </summary>
        [Fact]
        public void UnsupportedCompilationConfiguration_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P6C.UnsupportedConfiguration",
                [Source("Preview.cs")],
                parseOptions: new CSharpParseOptions(
                    LanguageVersion.Preview,
                    DocumentationMode.Parse,
                    SourceCodeKind.Regular));
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P6C.UnsupportedConfiguration.Consumer");
            RegisterPlan(fixture, dependency);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Preserves P5I's explicit refusal to approximate UTF-32 input.
        /// </summary>
        [Fact]
        public void UnsupportedSyntaxTreeEncoding_FailsClosed()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P6C.UnsupportedEncoding",
                [Source("Utf32.cs")],
                sourceEncoding: new System.Text.UTF32Encoding(
                    bigEndian: false,
                    byteOrderMark: true));
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P6C.UnsupportedEncoding.Consumer");
            RegisterPlan(fixture, dependency);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
        }

        /// <summary>
        /// Caches a deterministic failed attempt and does not retry after the
        /// candidate becomes valid later in the same context.
        /// </summary>
        [Fact]
        public void FailedAttempt_IsNotRetriedPerCallable()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.FailedOnce");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.FailedOnce.Consumer");
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            string initiallyMissing = Path.Combine(workspace.DirectoryPath, "later-target.bin");
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor, targetPath: initiallyMissing)));

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            File.Copy(dependency.TargetPath, initiallyMissing);
            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Reuses one successful P5K result even after all candidate files are gone.
        /// </summary>
        [Fact]
        public void SuccessfulAttempt_IsReusedWithoutCandidateIo()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.SuccessOnce");
            ConsumerFixture fixture = CreateCallingConsumer(dependency, "P6C.SuccessOnce.Consumer");
            RegisterPlan(fixture, dependency);

            Assert.True(TryResolve(fixture, dependency.Reference, out SemanticCompilationScope first));
            File.Delete(dependency.TargetPath);
            File.Delete(dependency.PdbPath);
            File.Delete(dependency.SourcePaths[0]);
            Assert.True(TryResolve(fixture, dependency.Reference, out SemanticCompilationScope second));
            Assert.Same(first, second);
        }

        /// <summary>
        /// Keeps plans and negative/positive attempts isolated per semantic context.
        /// </summary>
        [Fact]
        public void PlansAndAttempts_AreContextLocal()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Context");
            ConsumerFixture first = CreateCallingConsumer(dependency, "P6C.Context.First");
            ConsumerFixture second = CreateCallingConsumer(dependency, "P6C.Context.Second");
            RegisterPlan(first, dependency);

            Assert.True(TryResolve(first, dependency.Reference, out _));
            Assert.False(TryResolve(second, dependency.Reference, out _));
        }

        /// <summary>
        /// Distinguishes equal AssemblyIdentity values by manifest MVID.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentMvid_UsesOnlyExactPlan()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency first = Prepare(workspace, "P6C.SameIdentity");
            PreparedDependency second = workspace.Prepare(
                "P6C.SameIdentity",
                [new SourceInput(
                    "/_/Second.cs",
                    "using System; namespace External { public static class Api { "
                    + "public static void A() { throw new InvalidOperationException(); } } "
                    + "internal sealed class BuildMarker { } }")]);
            ConsumerFixture firstFixture = CreateConsumer(
                "P6C.SameIdentity.FirstConsumer",
                "public sealed class FirstConsumer { }",
                first.Reference);
            ConsumerFixture secondFixture = CreateConsumer(
                "P6C.SameIdentity.SecondConsumer",
                "public sealed class SecondConsumer { }",
                second.Reference);
            ProjectClosureSemanticContext sharedContext = CreateContext(
                firstFixture.Compilation,
                secondFixture.Compilation);
            firstFixture = firstFixture with { Context = sharedContext };
            secondFixture = secondFixture with { Context = sharedContext };
            ExternalAssemblyReferenceDescriptor firstDescriptor = GetDescriptor(
                firstFixture,
                first.Reference);
            ExternalAssemblyReferenceDescriptor secondDescriptor = GetDescriptor(
                secondFixture,
                second.Reference);
            Assert.Equal(firstDescriptor.AssemblyIdentity, secondDescriptor.AssemblyIdentity);
            Assert.NotEqual(
                firstDescriptor.Modules[0].ModuleVersionId,
                secondDescriptor.Modules[0].ModuleVersionId);
            Assert.True(sharedContext.TryRegisterExternalSupportingSourceReconstructionPlan(
                first.CreatePlan(firstDescriptor)));
            Assert.True(sharedContext.TryRegisterExternalSupportingSourceReconstructionPlan(
                second.CreatePlan(secondDescriptor)));

            Assert.True(TryResolve(secondFixture, second.Reference, out _));
            Assert.False(sharedContext.TryGetExternalSupportingSourceScope(
                firstDescriptor,
                out _));
            Assert.True(TryResolve(firstFixture, first.Reference, out _));
        }

        /// <summary>
        /// Attempts only plans whose dependencies are reached by the summary graph.
        /// </summary>
        [Fact]
        public void MultiplePlans_ReconstructOnlyReachedDependencies()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency first = workspace.Prepare(
                "P6C.Multi.First",
                [new SourceInput(
                    "/_/First.cs",
                    "namespace First { public static class Api { public static void A() { } } }")]);
            PreparedDependency unused = workspace.Prepare(
                "P6C.Multi.Unused",
                [new SourceInput(
                    "/_/Unused.cs",
                    "namespace Unused { public static class Api { public static void B() { } } }")]);
            PreparedDependency third = workspace.Prepare(
                "P6C.Multi.Third",
                [new SourceInput(
                    "/_/Third.cs",
                    "namespace Third { public static class Api { public static void C() { } } }")]);
            ConsumerFixture fixture = CreateConsumer(
                "P6C.Multi.Consumer",
                "public static class Consumer { public static void M() { "
                + "First.Api.A(); Third.Api.C(); } }",
                first.Reference,
                unused.Reference,
                third.Reference);
            ExternalAssemblyReferenceDescriptor firstDescriptor = RegisterPlan(fixture, first);
            ExternalAssemblyReferenceDescriptor unusedDescriptor = RegisterPlan(fixture, unused);
            ExternalAssemblyReferenceDescriptor thirdDescriptor = RegisterPlan(fixture, third);

            _ = BuildGraph(fixture, "M");
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(firstDescriptor, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(unusedDescriptor, out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(thirdDescriptor, out _));
        }

        /// <summary>
        /// Reconstructs B only when analysis of reconstructed A reaches B.
        /// </summary>
        [Fact]
        public void CrossExternalCall_ReconstructsEachDependencyOnDemand()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency downstream = workspace.Prepare(
                "P6C.Chain.Downstream",
                [new SourceInput(
                    "/_/Downstream.cs",
                    "using System; namespace Downstream { public static class Api { "
                    + "public static void B() { throw new InvalidOperationException(); } } }")]);
            PortableExecutableReference downstreamCandidate = MetadataReference.CreateFromImage(
                ImmutableArray.Create(downstream.PeImage),
                filePath: downstream.TargetPath);
            PreparedDependency upstream = workspace.Prepare(
                "P6C.Chain.Upstream",
                [new SourceInput(
                    "/_/Upstream.cs",
                    "namespace Upstream { public static class Api { public static void A() { "
                    + "Downstream.Api.B(); } } }")],
                additionalReferences: [downstreamCandidate]);
            ConsumerFixture fixture = CreateConsumer(
                "P6C.Chain.Consumer",
                "public static class Consumer { public static void M() { Upstream.Api.A(); } }",
                downstream.Reference,
                upstream.Reference);
            ExternalAssemblyReferenceDescriptor downstreamDescriptor = RegisterPlan(
                fixture,
                downstream);
            ExternalAssemblyReferenceDescriptor upstreamDescriptor = RegisterPlan(
                fixture,
                upstream);

            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(
                downstreamDescriptor,
                out _));
            ExceptionFlowSummaryGraphTestRun run = BuildGraph(fixture, "M");

            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(
                upstreamDescriptor,
                out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(
                downstreamDescriptor,
                out _));
            ExceptionFlowSummaryCallEdge upstreamEdge = Assert.Single(
                run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge downstreamEdge = Assert.Single(
                run.GetRequiredSummary(upstreamEdge.Target).CallEdges);
            Assert.Contains(
                run.GetRequiredSummary(downstreamEdge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Preserves the existing four-mode decision about whether external
        /// callable bodies are needed.
        /// </summary>
        [Theory]
        [InlineData("Direct", false)]
        [InlineData("ProjectTransitiveDeclaredExceptions", false)]
        [InlineData("ProjectTransitive", true)]
        [InlineData("SolutionTransitive", true)]
        public void AnalysisModes_TriggerOnlyWhenExternalBodiesAreNeeded(
            string modeName,
            bool expectsReconstruction)
        {
            ExceptionAnalysisMode mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Modes." + mode);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P6C.Modes.Consumer." + mode,
                documented: true);
            ExternalAssemblyReferenceDescriptor descriptor = RegisterPlan(fixture, dependency);

            _ = Find(fixture, mode);

            Assert.Equal(
                expectsReconstruction,
                fixture.Context.TryGetExternalSupportingSourceScope(descriptor, out _));
        }

        /// <summary>
        /// Produces the required no-plan/valid-plan/invalid-plan finding diff.
        /// </summary>
        [Fact]
        public void ControlledFindingDiff_ValidPlanAloneAddsPreciseSourceFlow()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P6C.Findings");
            ConsumerFixture noPlan = CreateCallingConsumer(
                dependency,
                "P6C.Findings.NoPlan",
                documented: true);
            ConsumerFixture valid = CreateCallingConsumer(
                dependency,
                "P6C.Findings.Valid",
                documented: true);
            ConsumerFixture invalid = CreateCallingConsumer(
                dependency,
                "P6C.Findings.Invalid",
                documented: true);
            RegisterPlan(valid, dependency);
            ExternalAssemblyReferenceDescriptor invalidDescriptor = GetDescriptor(
                invalid,
                dependency.Reference);
            Assert.True(invalid.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(
                    invalidDescriptor,
                    targetPath: dependency.TargetPath + ".missing")));

            List<Finding> noPlanFindings = Find(
                noPlan,
                ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> validFindings = Find(
                valid,
                ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> invalidFindings = Find(
                invalid,
                ExceptionAnalysisMode.SolutionTransitive);

            Assert.Empty(noPlanFindings);
            Finding finding = Assert.Single(validFindings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("InvalidOperationException", finding.Message);
            Assert.Empty(invalidFindings);
            Assert.Equal(noPlanFindings, invalidFindings);
            Assert.Equal(ExceptionFlowAnalyzerTestHelper.SourcePath, finding.FilePath);
        }

        private static PreparedDependency Prepare(
            ExternalReconstructionPlanTestWorkspace workspace,
            string assemblyName)
        {
            return workspace.Prepare(assemblyName, [Source("External.cs")]);
        }

        private static SourceInput Source(string name)
        {
            return new SourceInput(
                "/_/" + name,
                "using System; namespace External { public static class Api { "
                + "public static void A() { throw new InvalidOperationException(); } } }");
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
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert.Empty(errors);
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            return new ConsumerFixture(tree, compilation, context);
        }

        private static ExternalAssemblyReferenceDescriptor RegisterPlan(
            ConsumerFixture fixture,
            PreparedDependency dependency)
        {
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreatePlan(descriptor)));
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
                fixture.Context,
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

        private static ProjectClosureSemanticContext CreateContext(
            CSharpCompilation analysisTarget,
            params CSharpCompilation[] referencedProjects)
        {
            List<SemanticCompilationScope> scopes = new();
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesByTree =
                new(ReferenceEqualityComparer.Instance);
            SemanticCompilationScope targetScope =
                SemanticCompilationScope.CreateAnalysisTarget(
                    analysisTarget,
                    ProjectId.CreateNewId());
            scopes.Add(targetScope);
            AddTrees(targetScope, scopesByTree);

            foreach (CSharpCompilation compilation in referencedProjects)
            {
                SemanticCompilationScope scope =
                    SemanticCompilationScope.CreateReferencedProject(
                        compilation,
                        ProjectId.CreateNewId());
                scopes.Add(scope);
                AddTrees(scope, scopesByTree);
            }

            return new ProjectClosureSemanticContext(scopes, scopesByTree);
        }

        private static void AddTrees(
            SemanticCompilationScope scope,
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesByTree)
        {
            foreach (SyntaxTree tree in scope.Compilation.SyntaxTrees)
            {
                scopesByTree.Add(tree, scope);
            }
        }

        private sealed record ConsumerFixture(
            SyntaxTree Tree,
            CSharpCompilation Compilation,
            ProjectClosureSemanticContext Context);
    }
}
