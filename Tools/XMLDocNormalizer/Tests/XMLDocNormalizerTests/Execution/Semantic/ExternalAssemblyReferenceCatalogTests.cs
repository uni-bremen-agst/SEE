using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests demand-driven external assembly descriptor caching in semantic
    /// analysis contexts.
    /// </summary>
    public sealed class ExternalAssemblyReferenceCatalogTests
    {
        /// <summary>
        /// Reuses the same descriptor instance for repeated successful
        /// lookups in one binding compilation.
        /// </summary>
        [Fact]
        public void RepeatedExternalLookup_ReturnsSameDescriptorInstance()
        {
            PortableExecutableReference reference = CreateExternalReference(
                "ExternalA",
                "public sealed class ExternalType { }");
            CSharpCompilation consumer = CreateConsumer("Consumer", reference);
            ProjectClosureSemanticContext context = CreateContext(consumer);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor first));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor second));

            Assert.Same(first, second);
            Assert.Equal(assemblySymbol.Identity, first.AssemblyIdentity);
        }

        /// <summary>
        /// Keeps descriptors for distinct assemblies in the same compilation
        /// separate and independently reusable.
        /// </summary>
        [Fact]
        public void MultipleExternalAssemblies_AreCachedSeparately()
        {
            PortableExecutableReference firstReference = CreateExternalReference(
                "ExternalA",
                "public sealed class FirstExternalType { }");
            PortableExecutableReference secondReference = CreateExternalReference(
                "ExternalB",
                "public sealed class SecondExternalType { }");
            CSharpCompilation consumer = CreateConsumer(
                "Consumer",
                firstReference,
                secondReference);
            ProjectClosureSemanticContext context = CreateContext(consumer);
            IAssemblySymbol firstSymbol = GetRequiredAssemblySymbol(consumer, firstReference);
            IAssemblySymbol secondSymbol = GetRequiredAssemblySymbol(consumer, secondReference);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                firstSymbol,
                out ExternalAssemblyReferenceDescriptor first));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                secondSymbol,
                out ExternalAssemblyReferenceDescriptor second));
            Assert.NotEqual(first.AssemblyIdentity, second.AssemblyIdentity);
            Assert.NotSame(first, second);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                firstSymbol,
                out ExternalAssemblyReferenceDescriptor firstAgain));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                secondSymbol,
                out ExternalAssemblyReferenceDescriptor secondAgain));
            Assert.Same(first, firstAgain);
            Assert.Same(second, secondAgain);
        }

        /// <summary>
        /// Keeps equal assembly identities with different MVIDs separate when
        /// they are bound by different compilations in one context.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentBuilds_AcrossCompilationsRemainSeparate()
        {
            PortableExecutableReference firstReference = CreateExternalReference(
                "SharedExternal",
                "public sealed class FirstBuildType { }");
            PortableExecutableReference secondReference = CreateExternalReference(
                "SharedExternal",
                "public sealed class SecondBuildType { }");
            CSharpCompilation firstConsumer = CreateConsumer("ConsumerA", firstReference);
            CSharpCompilation secondConsumer = CreateConsumer("ConsumerB", secondReference);
            ProjectClosureSemanticContext context = CreateContext(
                firstConsumer,
                secondConsumer);
            IAssemblySymbol firstSymbol = GetRequiredAssemblySymbol(firstConsumer, firstReference);
            IAssemblySymbol secondSymbol = GetRequiredAssemblySymbol(secondConsumer, secondReference);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                firstConsumer,
                firstSymbol,
                out ExternalAssemblyReferenceDescriptor first));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                secondConsumer,
                secondSymbol,
                out ExternalAssemblyReferenceDescriptor second));

            Assert.Equal(first.AssemblyIdentity, second.AssemblyIdentity);
            Assert.NotEqual(
                first.Modules[0].ModuleVersionId,
                second.Modules[0].ModuleVersionId);
            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Produces equal descriptor values without sharing cache instances
        /// when separate compilations bind the same PE binary.
        /// </summary>
        [Fact]
        public void SameBinaryAcrossCompilations_ValuesMatchButInstancesRemainLocal()
        {
            byte[] image = Emit(CreateLibrary(
                "SharedExternal",
                "public sealed class SharedExternalType { }"));
            PortableExecutableReference firstReference = CreateReference(image);
            PortableExecutableReference secondReference = CreateReference(image);
            CSharpCompilation firstConsumer = CreateConsumer("ConsumerA", firstReference);
            CSharpCompilation secondConsumer = CreateConsumer("ConsumerB", secondReference);
            ProjectClosureSemanticContext context = CreateContext(
                firstConsumer,
                secondConsumer);
            IAssemblySymbol firstSymbol = GetRequiredAssemblySymbol(firstConsumer, firstReference);
            IAssemblySymbol secondSymbol = GetRequiredAssemblySymbol(secondConsumer, secondReference);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                firstConsumer,
                firstSymbol,
                out ExternalAssemblyReferenceDescriptor first));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                secondConsumer,
                secondSymbol,
                out ExternalAssemblyReferenceDescriptor second));

            Assert.Equal(first, second);
            Assert.NotSame(first, second);
        }

        /// <summary>
        /// Caches a stable negative result for a source-backed compilation
        /// reference without inventing an external binary.
        /// </summary>
        [Fact]
        public void CompilationReference_RepeatedLookupRemainsNegative()
        {
            CSharpCompilation dependency = CreateLibrary(
                "SourceDependency",
                "public sealed class DependencyType { }");
            CompilationReference reference = dependency.ToMetadataReference();
            CSharpCompilation consumer = CreateConsumer("Consumer", reference);
            ProjectClosureSemanticContext context = CreateContext(consumer);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

            Assert.False(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                assemblySymbol,
                out _));
            Assert.False(context.TryGetExternalAssemblyReferenceDescriptor(
                consumer,
                assemblySymbol,
                out _));
        }

        /// <summary>
        /// Rejects a compilation that does not belong to the semantic context.
        /// </summary>
        [Fact]
        public void ForeignCompilation_IsRejectedWithoutRegistration()
        {
            CSharpCompilation ownedCompilation = CreateConsumer("OwnedConsumer");
            ProjectClosureSemanticContext context = CreateContext(ownedCompilation);
            PortableExecutableReference reference = CreateExternalReference(
                "ExternalA",
                "public sealed class ExternalType { }");
            CSharpCompilation foreignCompilation = CreateConsumer("ForeignConsumer", reference);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(
                foreignCompilation,
                reference);

            Assert.False(context.TryGetExternalAssemblyReferenceDescriptor(
                foreignCompilation,
                assemblySymbol,
                out _));
        }

        /// <summary>
        /// Allows a previously unknown compilation to resolve its external
        /// dependency after it is registered as supporting source.
        /// </summary>
        [Fact]
        public void ForeignCompilation_AfterSupportingRegistrationCanResolveAndCache()
        {
            CSharpCompilation consumer = CreateConsumer("Consumer");
            ProjectClosureSemanticContext context = CreateContext(consumer);
            PortableExecutableReference reference = CreateExternalReference(
                "TransitiveExternal",
                "public sealed class TransitiveType { }");
            CSharpCompilation supportingCompilation = CreateConsumer(
                "SupportingSource",
                reference);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(
                supportingCompilation,
                reference);

            Assert.False(context.TryGetExternalAssemblyReferenceDescriptor(
                supportingCompilation,
                assemblySymbol,
                out _));

            context.RegisterSupportingSource(supportingCompilation);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                supportingCompilation,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor first));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                supportingCompilation,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor second));
            Assert.Equal(assemblySymbol.Identity, first.AssemblyIdentity);
            Assert.NotEqual(Guid.Empty, first.Modules[0].ModuleVersionId);
            Assert.Same(first, second);
        }

        /// <summary>
        /// Resolves external PE bindings owned by a referenced-project scope.
        /// </summary>
        [Fact]
        public void ReferencedProjectCompilation_CanResolveExternalDescriptor()
        {
            CSharpCompilation analysisTarget = CreateConsumer("AnalysisTarget");
            PortableExecutableReference reference = CreateExternalReference(
                "ReferencedExternal",
                "public sealed class ReferencedExternalType { }");
            CSharpCompilation referencedProject = CreateConsumer(
                "ReferencedProject",
                reference);
            ProjectClosureSemanticContext context = CreateContext(
                analysisTarget,
                referencedProject);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(
                referencedProject,
                reference);

            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                referencedProject,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor descriptor));
            Assert.Equal(assemblySymbol.Identity, descriptor.AssemblyIdentity);
        }

        /// <summary>
        /// Rejects an assembly symbol from another compilation even when the
        /// receiving compilation binds the same external binary.
        /// </summary>
        [Fact]
        public void CrossCompilationAssemblySymbol_DoesNotUseIdentityFallback()
        {
            byte[] image = Emit(CreateLibrary(
                "SharedExternal",
                "public sealed class SharedExternalType { }"));
            PortableExecutableReference firstReference = CreateReference(image);
            PortableExecutableReference secondReference = CreateReference(image);
            CSharpCompilation firstConsumer = CreateConsumer("ConsumerA", firstReference);
            CSharpCompilation secondConsumer = CreateConsumer("ConsumerB", secondReference);
            ProjectClosureSemanticContext context = CreateContext(
                firstConsumer,
                secondConsumer);
            IAssemblySymbol firstSymbol = GetRequiredAssemblySymbol(firstConsumer, firstReference);
            IAssemblySymbol secondSymbol = GetRequiredAssemblySymbol(secondConsumer, secondReference);

            Assert.False(context.TryGetExternalAssemblyReferenceDescriptor(
                secondConsumer,
                firstSymbol,
                out _));
            Assert.True(context.TryGetExternalAssemblyReferenceDescriptor(
                secondConsumer,
                secondSymbol,
                out ExternalAssemblyReferenceDescriptor descriptor));
            Assert.Equal(secondSymbol.Identity, descriptor.AssemblyIdentity);
        }

        /// <summary>
        /// Creates a context whose first compilation is the analysis target
        /// and whose remaining compilations are referenced projects.
        /// </summary>
        /// <param name="analysisTarget">The analysis-target compilation.</param>
        /// <param name="referencedProjects">The referenced-project compilations.</param>
        /// <returns>The completed semantic context.</returns>
        private static ProjectClosureSemanticContext CreateContext(
            CSharpCompilation analysisTarget,
            params CSharpCompilation[] referencedProjects)
        {
            List<SemanticCompilationScope> scopes = new();
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesBySyntaxTree =
                new(ReferenceEqualityComparer.Instance);
            SemanticCompilationScope analysisScope =
                SemanticCompilationScope.CreateAnalysisTarget(
                    analysisTarget,
                    ProjectId.CreateNewId());
            scopes.Add(analysisScope);
            AddSyntaxTreeScopes(analysisScope, scopesBySyntaxTree);

            foreach (CSharpCompilation referencedProject in referencedProjects)
            {
                SemanticCompilationScope referencedScope =
                    SemanticCompilationScope.CreateReferencedProject(
                        referencedProject,
                        ProjectId.CreateNewId());
                scopes.Add(referencedScope);
                AddSyntaxTreeScopes(referencedScope, scopesBySyntaxTree);
            }

            return new ProjectClosureSemanticContext(scopes, scopesBySyntaxTree);
        }

        /// <summary>
        /// Adds every syntax tree owned by a semantic compilation scope.
        /// </summary>
        /// <param name="scope">The owning semantic scope.</param>
        /// <param name="scopesBySyntaxTree">The destination ownership map.</param>
        private static void AddSyntaxTreeScopes(
            SemanticCompilationScope scope,
            Dictionary<SyntaxTree, SemanticCompilationScope> scopesBySyntaxTree)
        {
            foreach (SyntaxTree tree in scope.Compilation.SyntaxTrees)
            {
                scopesBySyntaxTree.Add(tree, scope);
            }
        }

        /// <summary>
        /// Creates an external in-memory PE reference.
        /// </summary>
        /// <param name="assemblyName">The external assembly name.</param>
        /// <param name="source">The complete external source.</param>
        /// <returns>The emitted PE reference.</returns>
        private static PortableExecutableReference CreateExternalReference(
            string assemblyName,
            string source)
        {
            return CreateReference(Emit(CreateLibrary(assemblyName, source)));
        }

        /// <summary>
        /// Creates an in-memory PE reference from emitted bytes.
        /// </summary>
        /// <param name="image">The complete PE image.</param>
        /// <returns>The PE reference.</returns>
        private static PortableExecutableReference CreateReference(byte[] image)
        {
            return MetadataReference.CreateFromImage(ImmutableArray.CreateRange(image));
        }

        /// <summary>
        /// Creates a source-backed library compilation.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="source">The complete source.</param>
        /// <returns>The library compilation.</returns>
        private static CSharpCompilation CreateLibrary(
            string assemblyName,
            string source)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source) },
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompilationErrors(compilation);
            return compilation;
        }

        /// <summary>
        /// Creates a consumer compilation with optional references.
        /// </summary>
        /// <param name="assemblyName">The consumer assembly name.</param>
        /// <param name="references">The additional references.</param>
        /// <returns>The consumer compilation.</returns>
        private static CSharpCompilation CreateConsumer(
            string assemblyName,
            params MetadataReference[] references)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText("public sealed class ConsumerType { }") },
                MetadataReferences.Default.Concat(references),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompilationErrors(compilation);
            return compilation;
        }

        /// <summary>
        /// Emits a compilation as an in-memory PE image.
        /// </summary>
        /// <param name="compilation">The compilation to emit.</param>
        /// <returns>The emitted PE image.</returns>
        private static byte[] Emit(CSharpCompilation compilation)
        {
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);
            Assert.True(
                result.Success,
                string.Join(Environment.NewLine, result.Diagnostics));
            return stream.ToArray();
        }

        /// <summary>
        /// Gets the assembly symbol bound to a metadata reference.
        /// </summary>
        /// <param name="compilation">The binding compilation.</param>
        /// <param name="reference">The bound metadata reference.</param>
        /// <returns>The bound assembly symbol.</returns>
        private static IAssemblySymbol GetRequiredAssemblySymbol(
            CSharpCompilation compilation,
            MetadataReference reference)
        {
            return Assert.IsAssignableFrom<IAssemblySymbol>(
                compilation.GetAssemblyOrModuleSymbol(reference));
        }

        /// <summary>
        /// Asserts that a compilation has no compiler errors.
        /// </summary>
        /// <param name="compilation">The compilation to inspect.</param>
        private static void AssertNoCompilationErrors(CSharpCompilation compilation)
        {
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
    }
}
