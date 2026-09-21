using System.Collections.Immutable;
using System.Net;
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
    /// Tests P7B integration with P7A/P6C reconstruction and source-backed
    /// P6B exception analysis.
    /// </summary>
    public sealed class ExternalSupportingSourceAcquisitionTests
    {
        private const string SourceLinkJson =
            "{\"documents\":{\"/_/*\":\"https://public.test/*\"}}";

        [Fact]
        public void UnusedSourceLinkPlan_PerformsNoRequest()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Unused");
            ConsumerFixture fixture = CreateConsumer(
                "P7B.Unused.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            _ = BuildGraph(fixture, "M");

            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void EmbeddedSource_EndToEndUsesNoFileOrNetworkCandidate()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(
                workspace,
                "P7B.Embedded",
                embedSources: true,
                sourceLinkJson: SourceLinkJson);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Embedded.Consumer",
                documented: true);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            ConfigureBinaries(fixture.Context, dependency);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreateDiscoveryPlan(descriptor)));

            List<Finding> findings = Find(fixture, ExceptionAnalysisMode.SolutionTransitive);

            AssertSingleDoc611(findings);
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void LocalMapping_EndToEndProducesSourceBackedFinding()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7B.Local");
            string root = CreateLocalRoot(workspace, dependency, 0);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Local.Consumer",
                documented: true);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", root)]));
            _ = RegisterAcquisitionPlan(fixture, dependency);

            AssertSingleDoc611(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
        }

        [Fact]
        public void InvalidLocalCandidate_EndToEndRetainsMetadataFallback()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = Prepare(workspace, "P7B.Local.Invalid");
            string root = Path.Combine(workspace.DirectoryPath, "invalid-local");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "External.cs"), "wrong bytes");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Local.Invalid.Consumer",
                documented: true);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", root)]));
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.Empty(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
        }

        [Fact]
        public void SourceLinkDisabled_EndToEndPerformsNoRequestAndFallsBack()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Disabled");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Disabled.Consumer",
                documented: true);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.Empty(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
        }

        [Fact]
        public void SourceLinkWrongHash_EndToEndRequestsOnceAndFallsBack()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.WrongHash");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.WrongHash.Consumer",
                documented: true);
            FakeHandler handler = FakeHandler.Success("wrong source"u8.ToArray());
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.Empty(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLinkSuccess_EndToEndProducesSourceBackedFinding()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.SourceLink");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.SourceLink.Consumer",
                documented: true);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            AssertSingleDoc611(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void InvalidExplicitCandidate_DoesNotFallBackToSourceLink()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Explicit.Invalid");
            string wrongPath = Path.Combine(workspace.DirectoryPath, "wrong-source.cs");
            File.WriteAllText(wrongPath, "wrong");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Explicit.Invalid.Consumer",
                documented: true);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            ConfigureBinaries(fixture.Context, dependency);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                ExternalSupportingSourceReconstructionPlan.CreateWithLocalReferenceDiscovery(
                    descriptor,
                    dependency.TargetPath,
                    dependency.PdbPath,
                    [new ExternalSourceReconstructionInput(0, wrongPath)])));

            Assert.Empty(Find(fixture, ExceptionAnalysisMode.SolutionTransitive));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void MixedEmbeddedLocalAndSourceLinkTreesPreserveOrdinals()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            SourceInput[] sources =
            [
                new("/_/Embedded.cs", "namespace Mixed { public static class Embedded { } }"),
                new("/_/Local.cs", "namespace Mixed { public static class Local { } }"),
                new("/_/Remote.cs", "using System; namespace Mixed { public static class Remote { "
                    + "public static void Throw() { throw new InvalidOperationException(); } } }")
            ];
            PreparedDependency dependency = workspace.Prepare(
                "P7B.Mixed",
                sources,
                sourceLinkJson: SourceLinkJson,
                embeddedSourceOrdinals: [0]);
            string localRoot = Path.Combine(workspace.DirectoryPath, "mixed-local");
            Directory.CreateDirectory(localRoot);
            File.Copy(dependency.SourcePaths[1], Path.Combine(localRoot, "Local.cs"));
            ConsumerFixture fixture = CreateConsumer(
                "P7B.Mixed.Consumer",
                "public static class Consumer { public static void M() { Mixed.Remote.Throw(); } }",
                dependency.Reference);
            FakeHandler handler = new(request => request.RequestUri!.AbsolutePath.EndsWith(
                    "/Remote.cs",
                    StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(File.ReadAllBytes(dependency.SourcePaths[2]))
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
            ConfigureSourceLink(fixture.Context, handler);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", localRoot)]));
            ConfigureBinaries(fixture.Context, dependency);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                ExternalSupportingSourceReconstructionPlan.CreateWithLocalReferenceDiscovery(
                    descriptor,
                    dependency.TargetPath,
                    dependency.PdbPath,
                    [
                        new ExternalSourceReconstructionInput(0, candidatePath: null),
                        ExternalSourceReconstructionInput.CreateAcquirable(1),
                        ExternalSourceReconstructionInput.CreateAcquirable(2)
                    ])));

            Assert.True(TryResolve(fixture, dependency.Reference, out var scope));
            Assert.Equal(
                sources.Select(static source => source.Path),
                scope.Compilation.SyntaxTrees.Select(static tree => tree.FilePath));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void LineDirectiveDocument_DoesNotCreateAnotherSourceTreeOrRequest()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P7B.Line",
                [new SourceInput(
                    "/_/Primary.cs",
                    "#line 1 \"/_/Generated.cs\"\npublic sealed class Value { }\n#line default")],
                sourceLinkJson: SourceLinkJson);
            string root = CreateLocalRoot(workspace, dependency, 0, "Primary.cs");
            ConsumerFixture fixture = CreateConsumer(
                "P7B.Line.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", root)]));
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.True(TryResolve(fixture, dependency.Reference, out var scope));
            Assert.Single(scope.Compilation.SyntaxTrees);
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void PartialSourceAcquisition_DoesNotCreatePartialCompilation()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = workspace.Prepare(
                "P7B.Partial",
                [
                    new SourceInput("/_/First.cs", "namespace Partial { public sealed class First { } }"),
                    new SourceInput("/_/Second.cs", "namespace Partial { public sealed class Second { } }"),
                    new SourceInput("/_/Missing.cs", "namespace Partial { public sealed class Missing { } }")
                ]);
            string root = Path.Combine(workspace.DirectoryPath, "partial-local");
            Directory.CreateDirectory(root);
            File.Copy(dependency.SourcePaths[0], Path.Combine(root, "First.cs"));
            File.Copy(dependency.SourcePaths[1], Path.Combine(root, "Second.cs"));
            ConsumerFixture fixture = CreateConsumer(
                "P7B.Partial.Consumer",
                "public static class Consumer { public static void M() { } }",
                dependency.Reference);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", root)]));
            ExternalAssemblyReferenceDescriptor descriptor = RegisterAcquisitionPlan(
                fixture,
                dependency);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            Assert.False(fixture.Context.TryGetExternalSupportingSourceScope(
                descriptor,
                out _));
        }

        [Fact]
        public void CrossExternalCall_AcquiresSourceLinkOnlyWhenReachedFromLocalSource()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency downstream = workspace.Prepare(
                "P7B.Chain.Downstream",
                [new SourceInput("/_/Downstream.cs", "using System; namespace Downstream { "
                    + "public static class Api { public static void B() { "
                    + "throw new InvalidOperationException(); } } }")],
                sourceLinkJson: SourceLinkJson);
            string downstreamCandidatePath = Path.Combine(
                workspace.DirectoryPath,
                "P7B.Chain.Downstream.dll");
            File.WriteAllBytes(downstreamCandidatePath, downstream.PeImage);
            PortableExecutableReference downstreamCandidate = MetadataReference.CreateFromImage(
                ImmutableArray.Create(downstream.PeImage),
                filePath: downstreamCandidatePath);
            PreparedDependency upstream = workspace.Prepare(
                "P7B.Chain.Upstream",
                [new SourceInput("/_/Upstream.cs", "namespace Upstream { public static class Api { "
                    + "public static void A() { Downstream.Api.B(); } } }")],
                additionalReferences: [downstreamCandidate]);
            string localRoot = CreateLocalRoot(
                workspace,
                upstream,
                index: 0,
                fileName: "Upstream.cs");
            ConsumerFixture fixture = CreateConsumer(
                "P7B.Chain.Consumer",
                "public static class Consumer { public static void M() { Upstream.Api.A(); } }",
                downstream.Reference,
                upstream.Reference);
            Assert.True(fixture.Context.TryConfigureExternalSourceMappings(
                [new ExternalSourcePathMapping("/_/", localRoot)]));
            FakeHandler handler = FakeHandler.Success(ReadSource(downstream));
            ConfigureSourceLink(fixture.Context, handler);
            Assert.True(fixture.Context.TryConfigureExternalBinarySearchRoots(
                downstream.ReferencePaths
                    .Concat(upstream.ReferencePaths)
                    .Select(static path => Path.GetDirectoryName(path)!)
                    .Append(workspace.DirectoryPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)));
            ExternalAssemblyReferenceDescriptor downstreamDescriptor = GetDescriptor(
                fixture,
                downstream.Reference);
            ExternalAssemblyReferenceDescriptor upstreamDescriptor = GetDescriptor(
                fixture,
                upstream.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                downstream.CreateAcquisitionPlan(downstreamDescriptor)));
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                upstream.CreateAcquisitionPlan(upstreamDescriptor)));

            Assert.Equal(0, handler.RequestCount);
            _ = BuildGraph(fixture, "M");

            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(
                upstreamDescriptor,
                out _));
            Assert.True(fixture.Context.TryGetExternalSupportingSourceScope(
                downstreamDescriptor,
                out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLinkSuccess_IsExactlyOnceAcrossRepeatedResolution()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Once.Success");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Once.Success.Consumer");
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.True(TryResolve(fixture, dependency.Reference, out var first));
            Assert.True(TryResolve(fixture, dependency.Reference, out var second));
            Assert.Same(first, second);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLinkFailure_IsExactlyOnceAcrossRepeatedResolution()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Once.Failure");
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Once.Failure.Consumer");
            FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            Assert.False(TryResolve(fixture, dependency.Reference, out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void AcquisitionConfigurationAndCachesAreContextLocal()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Context");
            ConsumerFixture first = CreateCallingConsumer(dependency, "P7B.Context.First");
            ConsumerFixture second = CreateCallingConsumer(dependency, "P7B.Context.Second");
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(first.Context, handler);
            _ = RegisterAcquisitionPlan(first, dependency);
            _ = RegisterAcquisitionPlan(second, dependency);

            Assert.True(TryResolve(first, dependency.Reference, out _));
            Assert.False(TryResolve(second, dependency.Reference, out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Theory]
        [InlineData("Direct", false)]
        [InlineData("ProjectTransitiveDeclaredExceptions", false)]
        [InlineData("ProjectTransitive", true)]
        [InlineData("SolutionTransitive", true)]
        public void AnalysisModes_AcquireOnlyWhenExternalBodiesAreNeeded(
            string modeName,
            bool expectsRequest)
        {
            ExceptionAnalysisMode mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(
                workspace,
                "P7B.Mode." + modeName);
            ConsumerFixture fixture = CreateCallingConsumer(
                dependency,
                "P7B.Mode.Consumer." + modeName,
                documented: true);
            FakeHandler handler = FakeHandler.Success(ReadSource(dependency));
            ConfigureSourceLink(fixture.Context, handler);
            _ = RegisterAcquisitionPlan(fixture, dependency);

            _ = Find(fixture, mode);

            Assert.Equal(expectsRequest ? 1 : 0, handler.RequestCount);
        }

        [Fact]
        public void ControlledFindingDiff_OnlyValidatedAcquisitionAddsDoc611()
        {
            using ExternalReconstructionPlanTestWorkspace workspace = new();
            PreparedDependency dependency = PrepareSourceLink(workspace, "P7B.Findings");
            ConsumerFixture disabled = CreateCallingConsumer(
                dependency, "P7B.Findings.Disabled", documented: true);
            ConsumerFixture valid = CreateCallingConsumer(
                dependency, "P7B.Findings.Valid", documented: true);
            ConsumerFixture invalid = CreateCallingConsumer(
                dependency, "P7B.Findings.Invalid", documented: true);
            FakeHandler validHandler = FakeHandler.Success(ReadSource(dependency));
            FakeHandler invalidHandler = FakeHandler.Success("wrong"u8.ToArray());
            ConfigureSourceLink(valid.Context, validHandler);
            ConfigureSourceLink(invalid.Context, invalidHandler);
            _ = RegisterAcquisitionPlan(disabled, dependency);
            _ = RegisterAcquisitionPlan(valid, dependency);
            _ = RegisterAcquisitionPlan(invalid, dependency);

            List<Finding> disabledFindings = Find(
                disabled, ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> validFindings = Find(valid, ExceptionAnalysisMode.SolutionTransitive);
            List<Finding> invalidFindings = Find(
                invalid, ExceptionAnalysisMode.SolutionTransitive);

            Assert.Empty(disabledFindings);
            AssertSingleDoc611(validFindings);
            Assert.Empty(invalidFindings);
        }

        private static PreparedDependency Prepare(
            ExternalReconstructionPlanTestWorkspace workspace,
            string assemblyName,
            bool embedSources = false,
            string? sourceLinkJson = null)
        {
            return workspace.Prepare(
                assemblyName,
                [new SourceInput("/_/External.cs", "using System; namespace External { "
                    + "public static class Api { public static void A() { "
                    + "throw new InvalidOperationException(); } } }")],
                embedSources: embedSources,
                sourceLinkJson: sourceLinkJson);
        }

        private static PreparedDependency PrepareSourceLink(
            ExternalReconstructionPlanTestWorkspace workspace,
            string assemblyName)
        {
            return Prepare(workspace, assemblyName, sourceLinkJson: SourceLinkJson);
        }

        private static byte[] ReadSource(PreparedDependency dependency, int index = 0)
        {
            return File.ReadAllBytes(dependency.SourcePaths[index]);
        }

        private static string CreateLocalRoot(
            ExternalReconstructionPlanTestWorkspace workspace,
            PreparedDependency dependency,
            int index,
            string fileName = "External.cs")
        {
            string root = Path.Combine(
                workspace.DirectoryPath,
                "local-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.Copy(dependency.SourcePaths[index], Path.Combine(root, fileName));
            return root;
        }

        private static ExternalAssemblyReferenceDescriptor RegisterAcquisitionPlan(
            ConsumerFixture fixture,
            PreparedDependency dependency)
        {
            ConfigureBinaries(fixture.Context, dependency);
            ExternalAssemblyReferenceDescriptor descriptor = GetDescriptor(
                fixture,
                dependency.Reference);
            Assert.True(fixture.Context.TryRegisterExternalSupportingSourceReconstructionPlan(
                dependency.CreateAcquisitionPlan(descriptor)));
            return descriptor;
        }

        private static void ConfigureBinaries(
            ProjectClosureSemanticContext context,
            params PreparedDependency[] dependencies)
        {
            Assert.True(context.TryConfigureExternalBinarySearchRoots(
                dependencies
                    .SelectMany(static dependency => dependency.ReferencePaths)
                    .Select(static path => Path.GetDirectoryName(path)!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)));
        }

        private static void ConfigureSourceLink(
            ProjectClosureSemanticContext context,
            FakeHandler handler)
        {
            Assert.True(context.TryEnableExternalSourceLink(
                new ExternalSourceLinkClient(
                    handler,
                    new FakeResolver(),
                    TimeSpan.FromSeconds(5))));
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

        private static void AssertSingleDoc611(IReadOnlyCollection<Finding> findings)
        {
            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingTransitiveExceptionDocumentation.ID, finding.Smell.ID);
            Assert.Contains("InvalidOperationException", finding.Message);
        }

        private sealed class FakeResolver : IExternalSourceHostResolver
        {
            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new[] { IPAddress.Parse("8.8.8.8") });
            }
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                this.responseFactory = responseFactory;
            }

            public int RequestCount { get; private set; }

            public static FakeHandler Success(byte[] bytes)
            {
                return new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(bytes)
                });
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequestCount++;
                return Task.FromResult(responseFactory(request));
            }
        }

        private sealed record ConsumerFixture(
            SyntaxTree Tree,
            CSharpCompilation Compilation,
            ProjectClosureSemanticContext Context);
    }
}
