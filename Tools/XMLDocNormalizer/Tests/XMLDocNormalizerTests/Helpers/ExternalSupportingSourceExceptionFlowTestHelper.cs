using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using FidelityWorkspace = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.FidelityWorkspace;
using ReconstructionPair = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.ReconstructionPair;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Builds focused summary graphs over real P3-P5K reconstructed external
    /// supporting compilations.
    /// </summary>
    internal static class ExternalSupportingSourceExceptionFlowTestHelper
    {
        /// <summary>
        /// Builds one consumer over one reconstructed external dependency.
        /// </summary>
        public static ExceptionFlowSummaryGraphTestRun Build(
            IReadOnlyList<SourceInput> dependencySources,
            string consumerSource,
            string methodName,
            bool registerExternalSource = true,
            string assemblyName = "P6B.External")
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                assemblyName,
                dependencySources);
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                assemblyName + ".dll");

            return BuildConsumer(
                consumerSource,
                methodName,
                [reference],
                registerExternalSource
                    ? [GetSupportingSource(dependency)]
                    : []);
        }

        /// <summary>
        /// Runs the production finding detector over a consumer with one
        /// optional exact external registration.
        /// </summary>
        public static List<Finding> Find(
            IReadOnlyList<SourceInput> dependencySources,
            string consumerSource,
            ExceptionAnalysisMode mode,
            bool registerExternalSource)
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                "P6B.Findings.External",
                dependencySources);
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "P6B.Findings.External.dll");
            SyntaxTree consumerTree = CSharpSyntaxTree.ParseText(
                consumerSource,
                path: ExceptionFlowAnalyzerTestHelper.SourcePath);
            CSharpCompilation consumer = CSharpCompilation.Create(
                "P6B.Findings.Consumer",
                [consumerTree],
                MetadataReferences.Default.Append(reference),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree,
                    consumer);

            if (registerExternalSource)
            {
                Assert.True(context.TryRegisterExternalSupportingSource(
                    GetSupportingSource(dependency),
                    out _));
            }

            return XmlDocExceptionSemanticDetector.FindExceptionSmells(
                consumerTree,
                ExceptionFlowAnalyzerTestHelper.SourcePath,
                consumer.GetSemanticModel(consumerTree),
                context,
                new XmlDocOptions { ExceptionAnalysisMode = mode });
        }

        /// <summary>
        /// Builds one consumer over two reconstructed external dependencies,
        /// where the first dependency references the second.
        /// </summary>
        public static ExceptionFlowSummaryGraphTestRun BuildChain(
            IReadOnlyList<SourceInput> downstreamSources,
            IReadOnlyList<SourceInput> upstreamSources,
            string consumerSource,
            string methodName)
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair downstream = workspace.Reconstruct(
                "P6B.Downstream",
                downstreamSources);
            PortableExecutableReference downstreamReference =
                workspace.CreateReferenceFromImage(
                    "P6B.Downstream.dll",
                    downstream.PeImage);
            ReconstructionPair upstream = workspace.Reconstruct(
                "P6B.Upstream",
                upstreamSources,
                additionalReferences: [downstreamReference]);
            PortableExecutableReference upstreamReference = CreateReference(
                upstream.PeImage,
                "P6B.Upstream.dll");

            return BuildConsumer(
                consumerSource,
                methodName,
                [downstreamReference, upstreamReference],
                [GetSupportingSource(downstream), GetSupportingSource(upstream)]);
        }

        /// <summary>
        /// Builds one consumer over two independently reconstructed aliased
        /// binaries that may expose identical fully qualified member names.
        /// </summary>
        public static ExceptionFlowSummaryGraphTestRun BuildAliasedPair(
            IReadOnlyList<SourceInput> firstSources,
            IReadOnlyList<SourceInput> secondSources,
            string consumerSource,
            string methodName)
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair first = workspace.Reconstruct(
                "P6B.SameName.First",
                firstSources);
            ReconstructionPair second = workspace.Reconstruct(
                "P6B.SameName.Second",
                secondSources);
            PortableExecutableReference firstReference = CreateReference(
                first.PeImage,
                "first.dll",
                new MetadataReferenceProperties(
                    MetadataImageKind.Assembly,
                    aliases: ImmutableArray.Create("first")));
            PortableExecutableReference secondReference = CreateReference(
                second.PeImage,
                "second.dll",
                new MetadataReferenceProperties(
                    MetadataImageKind.Assembly,
                    aliases: ImmutableArray.Create("second")));

            return BuildConsumer(
                consumerSource,
                methodName,
                [firstReference, secondReference],
                [GetSupportingSource(first), GetSupportingSource(second)]);
        }

        /// <summary>
        /// Builds a consumer that references one binary while a different
        /// same-identity binary is registered.
        /// </summary>
        public static ExceptionFlowSummaryGraphTestRun BuildWithWrongMvid(
            IReadOnlyList<SourceInput> referencedSources,
            IReadOnlyList<SourceInput> registeredSources,
            string consumerSource,
            string methodName)
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair referenced = workspace.Reconstruct(
                "P6B.Mvid",
                referencedSources);
            ReconstructionPair registered = workspace.Reconstruct(
                "P6B.Mvid",
                registeredSources);
            PortableExecutableReference reference = CreateReference(
                referenced.PeImage,
                "P6B.Mvid.dll");

            return BuildConsumer(
                consumerSource,
                methodName,
                [reference],
                [GetSupportingSource(registered)]);
        }

        /// <summary>
        /// Attempts to resolve an emitted async state-machine method that has
        /// no corresponding declaration in the reconstructed source symbols.
        /// </summary>
        public static bool TryResolveRegisteredGeneratedMethod()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                "P6B.Unresolved",
                [new SourceInput(
                    "/_/Async.cs",
                    "using System.Threading.Tasks; namespace External { public static class Api { " +
                    "public static async Task A() { await Task.Yield(); } } }")]);
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "P6B.Unresolved.dll");
            SyntaxTree consumerTree = CSharpSyntaxTree.ParseText(
                "public static class Consumer { public static void M() { } }",
                path: ExceptionFlowAnalyzerTestHelper.SourcePath);
            CSharpCompilation consumer = CSharpCompilation.Create(
                "P6B.Unresolved.Consumer",
                [consumerTree],
                MetadataReferences.Default.Append(reference),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    metadataImportOptions: MetadataImportOptions.All));
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree,
                    consumer);
            Assert.True(context.TryRegisterExternalSupportingSource(
                GetSupportingSource(dependency),
                out _));
            IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(reference));
            INamedTypeSymbol api = Assert.Single(
                assembly.GlobalNamespace.GetNamespaceMembers()
                    .Single(item => item.Name == "External")
                    .GetTypeMembers("Api"));
            INamedTypeSymbol stateMachine = Assert.Single(
                api.GetTypeMembers(),
                type => type.Name.Contains("<A>d__", StringComparison.Ordinal));
            IMethodSymbol moveNext = Assert.Single(
                stateMachine.GetMembers("MoveNext").OfType<IMethodSymbol>());

            return SupportingSourceSymbolResolver.TryResolveMethod(
                moveNext,
                consumer,
                context,
                out _,
                out _);
        }

        private static ExceptionFlowSummaryGraphTestRun BuildConsumer(
            string consumerSource,
            string methodName,
            IReadOnlyList<MetadataReference> references,
            IReadOnlyList<ExternalSupportingSourceCompilation> supportingSources)
        {
            SyntaxTree consumerTree = CSharpSyntaxTree.ParseText(
                consumerSource,
                path: ExceptionFlowAnalyzerTestHelper.SourcePath);
            CSharpCompilation consumer = CSharpCompilation.Create(
                "P6B.Consumer",
                [consumerTree],
                MetadataReferences.Default.Concat(references),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
            Diagnostic[] errors = consumer.GetDiagnostics()
                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Length != 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        errors.Select(static error => error.ToString())));
            }

            MethodDeclarationSyntax root = Assert.Single(
                consumerTree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Identifier.ValueText == methodName));
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree,
                    consumer);

            foreach (ExternalSupportingSourceCompilation source in supportingSources)
            {
                Assert.True(context.TryRegisterExternalSupportingSource(source, out _));
            }

            Assert.True(ExceptionFlowAnalyzer.TryBuildTransitiveSummaryGraph(
                root,
                new ExceptionFlowSemanticEnvironment(context),
                out ExceptionFlowSummaryGraph graph,
                out ExceptionFlowCallableKey? rootKey));

            return new ExceptionFlowSummaryGraphTestRun(
                graph,
                Assert.IsType<ExceptionFlowCallableKey>(rootKey),
                consumer);
        }

        private static ExternalSupportingSourceCompilation GetSupportingSource(
            ReconstructionPair pair)
        {
            return Assert.IsType<ExternalSupportingSourceCompilation>(
                pair.SupportingSource);
        }

        private static PortableExecutableReference CreateReference(
            byte[] image,
            string filePath,
            MetadataReferenceProperties? properties = null)
        {
            return MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(image),
                properties ?? MetadataReferenceProperties.Assembly,
                filePath: filePath);
        }
    }
}
