using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests descriptors for external PE assemblies bound by Roslyn.
    /// </summary>
    public sealed class ExternalAssemblyReferenceDescriptorFactoryTests
    {
        /// <summary>
        /// Describes an in-memory external assembly from its bound metadata.
        /// </summary>
        [Fact]
        public void InMemoryExternalAssembly_BoundPeMetadataIsDescribed()
        {
            CSharpCompilation dependency = CreateDependency(
                "public sealed class DependencyType { }");
            PortableExecutableReference reference = CreateInMemoryReference(Emit(dependency));
            CSharpCompilation consumer = CreateConsumer(reference);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

            bool created = ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor descriptor);

            Assert.True(created);
            Assert.Same(reference, consumer.GetMetadataReference(assemblySymbol));
            Assert.Equal(assemblySymbol.Identity, descriptor.AssemblyIdentity);
            ExternalModuleIdentity module = Assert.Single(descriptor.Modules);
            Assert.Equal(GetManifestModule(reference).Name, module.Name);
            Assert.NotEqual(Guid.Empty, module.ModuleVersionId);
            Assert.Null(descriptor.FilePath);
            Assert.False(descriptor.IsReferenceAssembly);
        }

        /// <summary>
        /// Retains the actual path and MVID of a file-backed PE reference.
        /// </summary>
        [Fact]
        public void FileBackedExternalAssembly_ReferencePathAndMetadataAreDescribed()
        {
            string directory = CreateTempDirectory();

            try
            {
                CSharpCompilation dependency = CreateDependency(
                    "public sealed class DependencyType { }");
                string assemblyPath = Path.Combine(directory, "ExternalDependency.dll");
                File.WriteAllBytes(assemblyPath, Emit(dependency));
                PortableExecutableReference reference = MetadataReference.CreateFromFile(assemblyPath);
                CSharpCompilation consumer = CreateConsumer(reference);
                IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

                Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                    consumer,
                    assemblySymbol,
                    out ExternalAssemblyReferenceDescriptor descriptor));
                Assert.Equal(assemblySymbol.Identity, descriptor.AssemblyIdentity);
                Assert.Equal(assemblyPath, descriptor.FilePath);
                Assert.Equal(
                    GetManifestModule(reference).GetModuleVersionId(),
                    Assert.Single(descriptor.Modules).ModuleVersionId);
            }
            finally
            {
                DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Ignores reference paths when the bound binary identity is equal.
        /// </summary>
        [Fact]
        public void SameBinaryAtDifferentPaths_DescriptorsAreEqual()
        {
            string directory = CreateTempDirectory();

            try
            {
                byte[] image = Emit(CreateDependency(
                    "public sealed class DependencyType { }"));
                string firstPath = Path.Combine(directory, "First.dll");
                string secondPath = Path.Combine(directory, "Second.dll");
                File.WriteAllBytes(firstPath, image);
                File.WriteAllBytes(secondPath, image);

                ExternalAssemblyReferenceDescriptor first = CreateDescriptor(
                    MetadataReference.CreateFromFile(firstPath));
                ExternalAssemblyReferenceDescriptor second = CreateDescriptor(
                    MetadataReference.CreateFromFile(secondPath));

                Assert.NotEqual(first.FilePath, second.FilePath);
                Assert.Equal(first.AssemblyIdentity, second.AssemblyIdentity);
                Assert.Equal(first.Modules, second.Modules);
                Assert.Equal(first, second);
                Assert.Equal(first.GetHashCode(), second.GetHashCode());
            }
            finally
            {
                DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Distinguishes builds with the same assembly identity by MVID.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentBuilds_DescriptorsAreDifferent()
        {
            ExternalAssemblyReferenceDescriptor first = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency("public sealed class FirstType { }"))));
            ExternalAssemblyReferenceDescriptor second = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency("public sealed class SecondType { }"))));

            Assert.Equal(first.AssemblyIdentity, second.AssemblyIdentity);
            Assert.NotEqual(
                first.Modules[0].ModuleVersionId,
                second.Modules[0].ModuleVersionId);
            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Distinguishes otherwise similarly named assemblies by full assembly
        /// identity before considering their module identities.
        /// </summary>
        [Fact]
        public void DifferentAssemblyVersions_DescriptorsAreDifferent()
        {
            ExternalAssemblyReferenceDescriptor first = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency(
                        "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")] " +
                        "public sealed class DependencyType { }"))));
            ExternalAssemblyReferenceDescriptor second = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency(
                        "[assembly: System.Reflection.AssemblyVersion(\"2.0.0.0\")] " +
                        "public sealed class DependencyType { }"))));

            Assert.NotEqual(first.AssemblyIdentity, second.AssemblyIdentity);
            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Detects only the framework reference-assembly marker on the bound
        /// external assembly.
        /// </summary>
        [Fact]
        public void FrameworkReferenceAssemblyAttribute_IsDetected()
        {
            ExternalAssemblyReferenceDescriptor marked = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency(
                        "[assembly: System.Runtime.CompilerServices.ReferenceAssembly] " +
                        "public sealed class DependencyType { }"))));
            ExternalAssemblyReferenceDescriptor unmarked = CreateDescriptor(
                CreateInMemoryReference(
                    Emit(CreateDependency(
                        "public sealed class DependencyType { }"))));

            Assert.True(marked.IsReferenceAssembly);
            Assert.False(unmarked.IsReferenceAssembly);
        }

        /// <summary>
        /// Rejects an unrelated attribute that shares only the framework
        /// marker's simple type name.
        /// </summary>
        [Fact]
        public void UnrelatedReferenceAssemblyAttribute_IsNotDetected()
        {
            const string source =
                "[assembly: Other.Namespace.ReferenceAssembly] " +
                "namespace Other.Namespace { " +
                "[System.AttributeUsage(System.AttributeTargets.Assembly)] " +
                "public sealed class ReferenceAssemblyAttribute : System.Attribute { } }";
            ExternalAssemblyReferenceDescriptor descriptor = CreateDescriptor(
                CreateInMemoryReference(Emit(CreateDependency(source))));

            Assert.False(descriptor.IsReferenceAssembly);
        }

        /// <summary>
        /// Excludes source-backed compilation references from external binary
        /// descriptor creation.
        /// </summary>
        [Fact]
        public void CompilationReference_IsNotExternalBinary()
        {
            CSharpCompilation dependency = CreateDependency(
                "public sealed class DependencyType { }");
            CompilationReference reference = dependency.ToMetadataReference();
            CSharpCompilation consumer = CreateConsumer(reference);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

            bool created = ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor descriptor);

            Assert.False(created);
            Assert.Null(descriptor);
            Assert.Same(reference, consumer.GetMetadataReference(assemblySymbol));
        }

        /// <summary>
        /// Creates one dependency library with a stable simple assembly name.
        /// </summary>
        /// <param name="source">The complete dependency source.</param>
        /// <returns>The dependency compilation.</returns>
        private static CSharpCompilation CreateDependency(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "ExternalDependency",
                new[] { tree },
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompilationErrors(compilation);
            return compilation;
        }

        /// <summary>
        /// Creates a consumer compilation containing one metadata reference.
        /// </summary>
        /// <param name="reference">The dependency reference.</param>
        /// <returns>The consumer compilation.</returns>
        private static CSharpCompilation CreateConsumer(MetadataReference reference)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Consumer",
                new[] { CSharpSyntaxTree.ParseText("public sealed class ConsumerType { }") },
                MetadataReferences.Default.Append(reference),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            AssertNoCompilationErrors(compilation);
            return compilation;
        }

        /// <summary>
        /// Creates an in-memory PE reference from an emitted image.
        /// </summary>
        /// <param name="image">The emitted PE image.</param>
        /// <returns>The in-memory PE reference.</returns>
        private static PortableExecutableReference CreateInMemoryReference(byte[] image)
        {
            return MetadataReference.CreateFromImage(ImmutableArray.CreateRange(image));
        }

        /// <summary>
        /// Emits a dependency compilation to a PE image.
        /// </summary>
        /// <param name="compilation">The compilation to emit.</param>
        /// <returns>The emitted PE bytes.</returns>
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
        /// Creates the required descriptor for an external PE reference.
        /// </summary>
        /// <param name="reference">The PE reference to bind.</param>
        /// <returns>The created descriptor.</returns>
        private static ExternalAssemblyReferenceDescriptor CreateDescriptor(
            PortableExecutableReference reference)
        {
            CSharpCompilation consumer = CreateConsumer(reference);
            IAssemblySymbol assemblySymbol = GetRequiredAssemblySymbol(consumer, reference);

            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor descriptor));
            return descriptor;
        }

        /// <summary>
        /// Gets the assembly symbol bound to a reference by a consumer.
        /// </summary>
        /// <param name="consumer">The binding consumer compilation.</param>
        /// <param name="reference">The reference whose symbol is required.</param>
        /// <returns>The bound assembly symbol.</returns>
        private static IAssemblySymbol GetRequiredAssemblySymbol(
            CSharpCompilation consumer,
            MetadataReference reference)
        {
            return Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(reference));
        }

        /// <summary>
        /// Gets the manifest module metadata from a PE assembly reference.
        /// </summary>
        /// <param name="reference">The PE assembly reference.</param>
        /// <returns>The manifest module metadata.</returns>
        private static ModuleMetadata GetManifestModule(PortableExecutableReference reference)
        {
            AssemblyMetadata assemblyMetadata = Assert.IsType<AssemblyMetadata>(
                reference.GetMetadata());
            return assemblyMetadata.GetModules()[0];
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

        /// <summary>
        /// Creates an isolated temporary directory.
        /// </summary>
        /// <returns>The created directory path.</returns>
        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// Deletes a temporary directory if it still exists.
        /// </summary>
        /// <param name="directory">The temporary directory path.</param>
        private static void DeleteDirectoryIfExists(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
