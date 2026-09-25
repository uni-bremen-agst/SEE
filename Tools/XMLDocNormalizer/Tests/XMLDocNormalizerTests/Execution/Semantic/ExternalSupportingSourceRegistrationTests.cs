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
using FidelityWorkspace = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.FidelityWorkspace;
using ReconstructionPair = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.ReconstructionPair;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests context-local registration of validated P5K compilations without
    /// activating external exception-flow traversal.
    /// </summary>
    public sealed class ExternalSupportingSourceRegistrationTests
    {
        /// <summary>
        /// Registers a complete P3-P5K result, looks it up through the exact
        /// metadata binary, and reuses existing cross-compilation navigation.
        /// </summary>
        [Fact]
        public void CompleteP5KResult_RegistersAndProvidesSourceNavigation()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructCrossSourceDependency(
                workspace,
                "External.Navigation");
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "first/location/External.Navigation.dll");
            CSharpCompilation consumer = CreateConsumer(
                "Navigation.Consumer",
                "public static class Target { public static void Run() => External.Api.A(); }",
                reference);
            ProjectClosureSemanticContext context = CreateContext(consumer);
            IAssemblySymbol metadataAssembly = GetRequiredAssembly(consumer, reference);
            IMethodSymbol metadataMethod = GetRequiredMethod(metadataAssembly, "External.Api", "A");
            ExternalSupportingSourceCompilation supportingSource =
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource);

            Assert.True(context.TryRegisterExternalSupportingSource(
                supportingSource,
                out SemanticCompilationScope registeredScope));
            Assert.Equal(2, supportingSource.SourceMaterials.Length);
            Assert.All(
                supportingSource.SourceMaterials,
                static material => Assert.Equal(
                    ExternalSourceMaterialExactness.DirectExact,
                    material.Exactness));
            Assert.Equal(
                SemanticCompilationScopeKind.SupportingSourceDependency,
                registeredScope.Kind);
            Assert.Same(dependency.Reconstructed, registeredScope.Compilation);
            Assert.True(context.TryGetExternalSupportingSourceScope(
                consumer,
                metadataAssembly,
                out SemanticCompilationScope resolvedScope));
            Assert.Same(registeredScope, resolvedScope);

            IMethodSymbol sourceMethod = Assert.IsAssignableFrom<IMethodSymbol>(
                ExceptionFlowCrossCompilationResolver.ResolveMethod(
                    metadataMethod,
                    registeredScope.Compilation));
            SyntaxReference declaration = Assert.Single(sourceMethod.DeclaringSyntaxReferences);
            Assert.Equal("/_/ExternalA.cs", declaration.SyntaxTree.FilePath);
            Assert.Contains(sourceMethod.Locations, location => location.IsInSource);
            Assert.True(context.TryGetSemanticModel(
                declaration.SyntaxTree,
                out SemanticModel semanticModel));
            Assert.Same(dependency.Reconstructed, semanticModel.Compilation);

            InvocationExpressionSyntax invocation = Assert.Single(
                declaration.GetSyntax()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>());
            IMethodSymbol sourceTarget = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(invocation).Symbol);
            SyntaxReference targetDeclaration = Assert.Single(
                sourceTarget.DeclaringSyntaxReferences);
            Assert.Equal("/_/ExternalB.cs", targetDeclaration.SyntaxTree.FilePath);
        }

        /// <summary>
        /// Keeps multiple exact external binary registrations independent.
        /// </summary>
        [Fact]
        public void MultipleDependencies_ResolveToTheirOwnCompilations()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair first = ReconstructSimpleDependency(
                workspace,
                "External.First",
                "FirstApi");
            ReconstructionPair second = ReconstructSimpleDependency(
                workspace,
                "External.Second",
                "SecondApi");
            PortableExecutableReference firstReference = CreateReference(
                first.PeImage,
                "first.dll");
            PortableExecutableReference secondReference = CreateReference(
                second.PeImage,
                "second.dll");
            CSharpCompilation consumer = CreateConsumer(
                "Multiple.Consumer",
                "public sealed class Target { }",
                firstReference,
                secondReference);
            ProjectClosureSemanticContext context = CreateContext(consumer);

            Assert.True(context.TryRegisterExternalSupportingSource(
                Assert.IsType<ExternalSupportingSourceCompilation>(first.SupportingSource),
                out _));
            Assert.True(context.TryRegisterExternalSupportingSource(
                Assert.IsType<ExternalSupportingSourceCompilation>(second.SupportingSource),
                out _));
            Assert.True(context.TryGetExternalSupportingSourceScope(
                consumer,
                GetRequiredAssembly(consumer, firstReference),
                out SemanticCompilationScope firstScope));
            Assert.True(context.TryGetExternalSupportingSourceScope(
                consumer,
                GetRequiredAssembly(consumer, secondReference),
                out SemanticCompilationScope secondScope));
            Assert.Same(first.Reconstructed, firstScope.Compilation);
            Assert.Same(second.Reconstructed, secondScope.Compilation);
            Assert.NotSame(firstScope, secondScope);
        }

        /// <summary>
        /// Prevents one semantic context from observing another context's
        /// external registration.
        /// </summary>
        [Fact]
        public void Registration_IsContextLocal()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructSimpleDependency(
                workspace,
                "External.Isolated",
                "IsolatedApi");
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "isolated.dll");
            CSharpCompilation consumer = CreateConsumer(
                "Isolation.Consumer",
                "public sealed class Target { }",
                reference);
            ProjectClosureSemanticContext firstContext = CreateContext(consumer);
            ProjectClosureSemanticContext secondContext = CreateContext(consumer);
            IAssemblySymbol assembly = GetRequiredAssembly(consumer, reference);

            Assert.True(firstContext.TryRegisterExternalSupportingSource(
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource),
                out _));
            Assert.True(firstContext.TryGetExternalSupportingSourceScope(
                consumer,
                assembly,
                out _));
            Assert.False(secondContext.TryGetExternalSupportingSourceScope(
                consumer,
                assembly,
                out _));
        }

        /// <summary>
        /// Distinguishes equal assembly identities by manifest MVID and allows
        /// both exact binaries to coexist in one context.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentMvids_RemainDistinct()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair first = ReconstructSimpleDependency(
                workspace,
                "External.Rebuilt",
                "FirstBuildApi");
            ReconstructionPair second = ReconstructSimpleDependency(
                workspace,
                "External.Rebuilt",
                "SecondBuildApi");
            ExternalSupportingSourceCompilation firstSource =
                Assert.IsType<ExternalSupportingSourceCompilation>(first.SupportingSource);
            ExternalSupportingSourceCompilation secondSource =
                Assert.IsType<ExternalSupportingSourceCompilation>(second.SupportingSource);
            Assert.Equal(
                firstSource.TargetAssembly.AssemblyIdentity,
                secondSource.TargetAssembly.AssemblyIdentity);
            Assert.NotEqual(
                firstSource.TargetAssembly.Modules[0].ModuleVersionId,
                secondSource.TargetAssembly.Modules[0].ModuleVersionId);

            PortableExecutableReference firstReference = CreateReference(first.PeImage, "build-a.dll");
            PortableExecutableReference secondReference = CreateReference(second.PeImage, "build-b.dll");
            CSharpCompilation firstConsumer = CreateConsumer(
                "First.Consumer",
                "public sealed class FirstTarget { }",
                firstReference);
            CSharpCompilation secondConsumer = CreateConsumer(
                "Second.Consumer",
                "public sealed class SecondTarget { }",
                secondReference);
            ProjectClosureSemanticContext context = CreateContext(
                firstConsumer,
                secondConsumer);

            Assert.True(context.TryRegisterExternalSupportingSource(secondSource, out _));
            Assert.False(context.TryGetExternalSupportingSourceScope(
                firstConsumer,
                GetRequiredAssembly(firstConsumer, firstReference),
                out _));
            Assert.True(context.TryGetExternalSupportingSourceScope(
                secondConsumer,
                GetRequiredAssembly(secondConsumer, secondReference),
                out SemanticCompilationScope secondScope));
            Assert.Same(second.Reconstructed, secondScope.Compilation);

            Assert.True(context.TryRegisterExternalSupportingSource(firstSource, out _));
            Assert.True(context.TryGetExternalSupportingSourceScope(
                firstConsumer,
                GetRequiredAssembly(firstConsumer, firstReference),
                out SemanticCompilationScope firstScope));
            Assert.Same(first.Reconstructed, firstScope.Compilation);
        }

        /// <summary>
        /// Rejects a different full assembly identity even when module test
        /// data is deliberately reused.
        /// </summary>
        [Fact]
        public void DifferentAssemblyIdentity_DoesNotMatch()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructSimpleDependency(
                workspace,
                "External.Identity",
                "IdentityApi");
            ExternalSupportingSourceCompilation source =
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource);
            ProjectClosureSemanticContext context = CreateContext(
                CreateConsumer("Identity.Consumer", "public sealed class Target { }"));
            ExternalAssemblyReferenceDescriptor wrongIdentity = new(
                new AssemblyIdentity("Different.Identity"),
                source.TargetAssembly.Modules,
                filePath: source.TargetAssembly.FilePath,
                source.TargetAssembly.IsReferenceAssembly);

            Assert.True(context.TryRegisterExternalSupportingSource(source, out _));
            Assert.False(context.TryGetExternalSupportingSourceScope(
                wrongIdentity,
                out _));
        }

        /// <summary>
        /// Ignores physical paths when the complete binary identity is equal.
        /// </summary>
        [Fact]
        public void SameBinaryAtDifferentPath_Matches()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructSimpleDependency(
                workspace,
                "External.Paths",
                "PathApi");
            ExternalSupportingSourceCompilation source =
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource);
            ProjectClosureSemanticContext context = CreateContext(
                CreateConsumer("Path.Consumer", "public sealed class Target { }"));
            ExternalAssemblyReferenceDescriptor relocated = new(
                source.TargetAssembly.AssemblyIdentity,
                source.TargetAssembly.Modules,
                filePath: "completely/different/location.dll",
                source.TargetAssembly.IsReferenceAssembly);

            Assert.True(context.TryRegisterExternalSupportingSource(
                source,
                out SemanticCompilationScope registered));
            Assert.True(context.TryGetExternalSupportingSourceScope(
                relocated,
                out SemanticCompilationScope resolved));
            Assert.Same(registered, resolved);
        }

        /// <summary>
        /// Makes registration of the same identity and compilation idempotent.
        /// </summary>
        [Fact]
        public void DuplicateRegistration_IsIdempotent()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructSimpleDependency(
                workspace,
                "External.Duplicate",
                "DuplicateApi");
            ExternalSupportingSourceCompilation source =
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource);
            ProjectClosureSemanticContext context = CreateContext(
                CreateConsumer("Duplicate.Consumer", "public sealed class Target { }"));

            Assert.True(context.TryRegisterExternalSupportingSource(
                source,
                out SemanticCompilationScope first));
            Assert.True(context.TryRegisterExternalSupportingSource(
                source,
                out SemanticCompilationScope second));
            Assert.Same(first, second);
        }

        /// <summary>
        /// Fails closed when one binary identity is paired with a different
        /// P5K compilation instance and retains the original registration.
        /// </summary>
        [Fact]
        public void ConflictingRegistration_FailsClosedWithoutReplacement()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                "External.Conflict",
                [new SourceInput("/_/Conflict.cs", "public sealed class ConflictApi { }")],
                createConflictingSupportingSource: true);
            ExternalSupportingSourceCompilation first =
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource);
            ExternalSupportingSourceCompilation conflicting =
                Assert.IsType<ExternalSupportingSourceCompilation>(
                    dependency.ConflictingSupportingSource);
            ProjectClosureSemanticContext context = CreateContext(
                CreateConsumer("Conflict.Consumer", "public sealed class Target { }"));

            Assert.True(context.TryRegisterExternalSupportingSource(
                first,
                out SemanticCompilationScope retained));
            Assert.False(context.TryRegisterExternalSupportingSource(
                conflicting,
                out SemanticCompilationScope conflictResult));
            Assert.Same(retained, conflictResult);
            Assert.True(context.TryGetExternalSupportingSourceScope(
                first.TargetAssembly,
                out SemanticCompilationScope resolved));
            Assert.Same(retained, resolved);
            Assert.Same(first.Compilation, resolved.Compilation);
        }

        /// <summary>
        /// Leaves an unregistered metadata dependency on the existing
        /// metadata-only path.
        /// </summary>
        [Fact]
        public void UnregisteredMetadataReference_HasNoSupportingSource()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = ReconstructSimpleDependency(
                workspace,
                "External.MetadataOnly",
                "MetadataOnlyApi");
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "metadata-only.dll");
            CSharpCompilation consumer = CreateConsumer(
                "MetadataOnly.Consumer",
                "public sealed class Target { }",
                reference);
            ProjectClosureSemanticContext context = CreateContext(consumer);

            Assert.False(context.TryGetExternalSupportingSourceScope(
                consumer,
                GetRequiredAssembly(consumer, reference),
                out _));
        }

        /// <summary>
        /// Keeps P6A registration outside reporting and summary enumeration so
        /// an external throwing body cannot change target findings yet.
        /// </summary>
        [Fact]
        public void Registration_IsFindingNeutralAndDoesNotActivateTraversal()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                "External.Findings",
                [
                    new SourceInput(
                        "/_/External.cs",
                        "public static class ExternalApi { public static void Run() => " +
                        "throw new System.InvalidOperationException(); }")
                ]);
            PortableExecutableReference reference = CreateReference(
                dependency.PeImage,
                "findings.dll");
            CSharpCompilation consumer = CreateConsumer(
                "Finding.Consumer",
                "public static class Target { public static void Run() => ExternalApi.Run(); }",
                reference);
            SyntaxTree reportingTree = Assert.Single(consumer.SyntaxTrees);
            ProjectClosureSemanticContext context = CreateContext(consumer);
            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive
            };
            List<Finding> before = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                reportingTree,
                "Target.cs",
                consumer.GetSemanticModel(reportingTree),
                context,
                options);

            Assert.True(context.TryRegisterExternalSupportingSource(
                Assert.IsType<ExternalSupportingSourceCompilation>(dependency.SupportingSource),
                out SemanticCompilationScope externalScope));
            List<Finding> after = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                reportingTree,
                "Target.cs",
                consumer.GetSemanticModel(reportingTree),
                context,
                options);

            Assert.Equal(
                before.Select(FindingKey),
                after.Select(FindingKey));
            Assert.DoesNotContain(
                externalScope,
                context.GetAnalysisCompilationScopes());
            Assert.All(
                dependency.Reconstructed.SyntaxTrees,
                tree => Assert.False(context.IsInReportingScope(tree)));
            Assert.All(
                dependency.Reconstructed.SyntaxTrees,
                tree => Assert.True(context.IsInAnalysisScope(tree)));
        }

        /// <summary>
        /// Reconstructs a two-file external dependency through P3-P5K.
        /// </summary>
        private static ReconstructionPair ReconstructCrossSourceDependency(
            FidelityWorkspace workspace,
            string assemblyName)
        {
            return workspace.Reconstruct(
                assemblyName,
                [
                    new SourceInput(
                        "/_/ExternalA.cs",
                        "namespace External { public static class Api { " +
                        "public static void A() => Helper.B(); } }"),
                    new SourceInput(
                        "/_/ExternalB.cs",
                        "namespace External { public static class Helper { " +
                        "public static void B() { } } }")
                ]);
        }

        /// <summary>
        /// Reconstructs one simple external dependency through P3-P5K.
        /// </summary>
        private static ReconstructionPair ReconstructSimpleDependency(
            FidelityWorkspace workspace,
            string assemblyName,
            string typeName)
        {
            return workspace.Reconstruct(
                assemblyName,
                [
                    new SourceInput(
                        "/_/Dependency.cs",
                        $"public static class {typeName} {{ public static void Run() {{ }} }}")
                ]);
        }

        /// <summary>
        /// Creates one in-memory PE reference with explicit path provenance.
        /// </summary>
        private static PortableExecutableReference CreateReference(
            byte[] image,
            string filePath)
        {
            return MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(image),
                filePath: filePath);
        }

        /// <summary>
        /// Creates a controlled analysis-target or referenced-project
        /// compilation.
        /// </summary>
        private static CSharpCompilation CreateConsumer(
            string assemblyName,
            string source,
            params MetadataReference[] references)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source, path: "/_/Consumer.cs")],
                MetadataReferences.Default.Concat(references),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }

        /// <summary>
        /// Creates a context whose first compilation is the reporting target
        /// and whose remaining compilations are referenced projects.
        /// </summary>
        private static ProjectClosureSemanticContext CreateContext(
            CSharpCompilation analysisTarget,
            params CSharpCompilation[] referencedProjects)
        {
            List<SemanticCompilationScope> scopes = new();
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesByTree =
                new(ReferenceEqualityComparer.Instance);
            SemanticCompilationScope analysisScope =
                SemanticCompilationScope.CreateAnalysisTarget(
                    analysisTarget,
                    ProjectId.CreateNewId());
            scopes.Add(analysisScope);
            AddTrees(analysisScope, scopesByTree);

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

        /// <summary>
        /// Adds a scope's exact syntax-tree instances to an ownership map.
        /// </summary>
        private static void AddTrees(
            SemanticCompilationScope scope,
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesByTree)
        {
            foreach (SyntaxTree tree in scope.Compilation.SyntaxTrees)
            {
                scopesByTree.Add(tree, scope);
            }
        }

        /// <summary>
        /// Gets the exact assembly symbol bound to one reference.
        /// </summary>
        private static IAssemblySymbol GetRequiredAssembly(
            CSharpCompilation compilation,
            MetadataReference reference)
        {
            return Assert.IsAssignableFrom<IAssemblySymbol>(
                compilation.GetAssemblyOrModuleSymbol(reference));
        }

        /// <summary>
        /// Gets one metadata method from a bound assembly.
        /// </summary>
        private static IMethodSymbol GetRequiredMethod(
            IAssemblySymbol assembly,
            string metadataTypeName,
            string methodName)
        {
            INamedTypeSymbol type = Assert.IsAssignableFrom<INamedTypeSymbol>(
                assembly.GetTypeByMetadataName(metadataTypeName));
            return Assert.Single(type.GetMembers(methodName).OfType<IMethodSymbol>());
        }

        /// <summary>
        /// Creates a stable finding key for before/after comparison.
        /// </summary>
        private static string FindingKey(Finding finding)
        {
            return string.Join(
                "|",
                finding.Smell.ID,
                finding.FilePath,
                finding.Line,
                finding.Column,
                finding.Message);
        }
    }
}
