using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests reconstruction of Roslyn metadata references from exact
    /// P5C-validated PE material.
    /// </summary>
    public sealed class ExternalMetadataReferenceFactoryTests
    {
        /// <summary>
        /// Reconstructs a normal assembly and preserves every serialized
        /// metadata-reference property exactly.
        /// </summary>
        [Fact]
        public void AssemblyReference_ReconstructsExactPropertiesAndMetadata()
        {
            byte[] image = EmitAssembly(
                "ExactPropertiesDependency",
                "public sealed class ExactPropertiesType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(
                    image,
                    "ExactPropertiesDependency.dll",
                    ImmutableArray.Create("zeta", "alpha", "global"),
                    embedInteropTypes: true);
            ValidatedExternalMetadataReferenceMaterial material = CreateMaterial(expected, image);

            PortableExecutableReference reference = CreateReference(material);

            Assert.Equal(expected.Kind, reference.Properties.Kind);
            Assert.Equal(expected.Aliases, reference.Properties.Aliases);
            Assert.Equal(expected.EmbedInteropTypes, reference.Properties.EmbedInteropTypes);
            Assert.Equal(material.Candidate.Module.ModuleVersionId, ReadMvid(reference));
            Assert.Equal(material.Candidate.AssemblyIdentity, ReadAssemblyIdentity(reference));
        }

        /// <summary>
        /// Closes the real P3/P4/P5A through P5D chain for a dependency
        /// recorded in a consumer Portable PDB.
        /// </summary>
        [Fact]
        public void RealP5AReference_ReconstructsEndToEnd()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] dependencyImage = EmitAssembly(
                    "EndToEndDependency",
                    "public sealed class EndToEndDependencyType { }");
                string dependencyPath = Path.Combine(directory, "dependency.dll");
                File.WriteAllBytes(dependencyPath, dependencyImage);
                PortableExecutableReference dependencyReference =
                    MetadataReference.CreateFromFile(dependencyPath);
                P5BTests.EmittedPortablePdb consumer = P5BTests.EmitPortablePdb(
                    "EndToEndConsumer",
                    "public sealed class Consumer { public EndToEndDependencyType? Value; }",
                    MetadataReferences.Default.Append(dependencyReference));
                ExternalCompilationProvenanceDescriptor provenance =
                    P5BTests.ReadCompilationProvenance(consumer);
                ExternalCompilationMetadataReferencesDescriptor references = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                ExternalCompilationMetadataReferenceDescriptor expected = Assert.Single(
                    references.References.Where(reference => reference.Name == "dependency.dll"));
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    dependencyPath,
                    out ValidatedExternalMetadataReferenceMaterial material));

                PortableExecutableReference reconstructed = CreateReference(material);

                Assert.Equal(expected.Kind, reconstructed.Properties.Kind);
                Assert.Equal(expected.Aliases, reconstructed.Properties.Aliases);
                Assert.Equal(expected.EmbedInteropTypes, reconstructed.Properties.EmbedInteropTypes);
                Assert.Equal(material.Candidate.AssemblyIdentity, ReadAssemblyIdentity(reconstructed));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Uses the reconstructed reference to bind a dependency symbol in a
        /// controlled compilation.
        /// </summary>
        [Fact]
        public void AssemblyReference_BindsInControlledCompilation()
        {
            byte[] image = EmitAssembly(
                "BindableDependency",
                "public static class DependencyType { public static int Value => 42; }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "BindableDependency.dll");
            PortableExecutableReference reference = CreateReference(
                CreateMaterial(expected, image));

            CSharpCompilation compilation = CreateCompilation(
                "BindingConsumer",
                "public static class Consumer { public static int GetValue() => DependencyType.Value; }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default.Append(reference));

            AssertNoErrors(compilation);
            Assert.NotNull(compilation.GetTypeByMetadataName("DependencyType"));
        }

        /// <summary>
        /// Preserves an external alias and makes it usable by normal C#
        /// binding.
        /// </summary>
        [Fact]
        public void ExternalAlias_IsPreservedAndUsable()
        {
            byte[] image = EmitAssembly(
                "AliasedDependency",
                "namespace Aliased { public sealed class DependencyType { } }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(
                    image,
                    "AliasedDependency.dll",
                    ImmutableArray.Create("depAlias"));
            PortableExecutableReference reference = CreateReference(
                CreateMaterial(expected, image));

            CSharpCompilation compilation = CreateCompilation(
                "AliasConsumer",
                "extern alias depAlias; public sealed class Consumer { public depAlias::Aliased.DependencyType? Value; }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default.Append(reference));

            Assert.Equal(new[] { "depAlias" }, reference.Properties.Aliases);
            AssertNoErrors(compilation);
        }

        /// <summary>
        /// Reconstructs a validated standalone netmodule as a true module
        /// reference.
        /// </summary>
        [Fact]
        public void StandaloneNetModule_ReconstructsAsModule()
        {
            byte[] image = P5BTests.EmitPe(
                "StandaloneModule",
                "public sealed class StandaloneModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "StandaloneModule.netmodule");

            PortableExecutableReference reference = CreateReference(
                CreateMaterial(expected, image));

            Assert.Equal(MetadataImageKind.Module, reference.Properties.Kind);
            Assert.Equal(MetadataImageKind.Module, reference.GetMetadata().Kind);
            Assert.Equal(expected.ModuleVersionId, ReadMvid(reference));
        }

        /// <summary>
        /// Fails closed rather than removing aliases that Roslyn forbids on a
        /// standalone module reference.
        /// </summary>
        [Fact]
        public void ModuleWithAlias_FailsClosed()
        {
            byte[] image = P5BTests.EmitPe(
                "AliasedModule",
                "public sealed class AliasedModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "AliasedModule.netmodule");
            ExternalCompilationMetadataReferenceDescriptor expected = P5BTests.CopyExpected(
                actual,
                aliases: ImmutableArray.Create("moduleAlias"));
            ValidatedExternalMetadataReferenceMaterial material = CreateMaterial(expected, image);

            Assert.False(ExternalMetadataReferenceFactory.TryCreate(material, out _));
        }

        /// <summary>
        /// Fails closed rather than clearing EmbedInteropTypes when Roslyn
        /// forbids it on a standalone module reference.
        /// </summary>
        [Fact]
        public void ModuleWithEmbedInteropTypes_FailsClosed()
        {
            byte[] image = P5BTests.EmitPe(
                "InteropModule",
                "public sealed class InteropModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "InteropModule.netmodule");
            ExternalCompilationMetadataReferenceDescriptor expected = P5BTests.CopyExpected(
                actual,
                embedInteropTypes: true);
            ValidatedExternalMetadataReferenceMaterial material = CreateMaterial(expected, image);

            Assert.False(ExternalMetadataReferenceFactory.TryCreate(material, out _));
        }

        /// <summary>
        /// Reconstructs from retained bytes even when the candidate file was
        /// deleted before P5D begins.
        /// </summary>
        [Fact]
        public void CandidateDeletedBeforeReconstruction_RemainsUsable()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "DeletedBeforeDependency",
                    "public sealed class DeletedBeforeType { }");
                string path = Path.Combine(directory, "candidate.dll");
                ValidatedExternalMetadataReferenceMaterial material =
                    CreateFileMaterial(image, "Lookup.dll", path);
                File.Delete(path);

                PortableExecutableReference reference = CreateReference(material);

                Assert.False(File.Exists(path));
                Assert.Equal(path, reference.FilePath);
                Assert.Equal(material.Candidate.Module.ModuleVersionId, ReadMvid(reference));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps metadata and symbol binding usable when the candidate file is
        /// deleted after P5D created the reference.
        /// </summary>
        [Fact]
        public void CandidateDeletedAfterReconstruction_RemainsUsable()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "DeletedAfterDependency",
                    "public sealed class DeletedAfterType { }");
                string path = Path.Combine(directory, "candidate.dll");
                ValidatedExternalMetadataReferenceMaterial material =
                    CreateFileMaterial(image, "Lookup.dll", path);
                PortableExecutableReference reference = CreateReference(material);
                File.Delete(path);

                CSharpCompilation compilation = CreateCompilation(
                    "DeletedAfterConsumer",
                    "public sealed class Consumer { public DeletedAfterType? Value; }",
                    OutputKind.DynamicallyLinkedLibrary,
                    MetadataReferences.Default.Append(reference));

                Assert.Equal(material.Candidate.Module.ModuleVersionId, ReadMvid(reference));
                AssertNoErrors(compilation);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps build A after the candidate path is replaced by build B.
        /// </summary>
        [Fact]
        public void CandidateReplacedAfterReconstruction_KeepsOriginalMetadata()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] firstImage = EmitAssembly(
                    "ReplaceableDependency",
                    "public sealed class FirstBuildType { }");
                byte[] secondImage = EmitAssembly(
                    "ReplaceableDependency",
                    "public sealed class SecondBuildType { }");
                string path = Path.Combine(directory, "candidate.dll");
                ValidatedExternalMetadataReferenceMaterial material =
                    CreateFileMaterial(firstImage, "Lookup.dll", path);
                PortableExecutableReference reference = CreateReference(material);
                File.WriteAllBytes(path, secondImage);

                Assert.Equal(material.Candidate.Module.ModuleVersionId, ReadMvid(reference));
                Assert.NotEqual(
                    P5BTests.ReadModuleIdentity(secondImage).ModuleVersionId,
                    ReadMvid(reference));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Treats a renamed candidate path as opaque display provenance rather
        /// than binary identity.
        /// </summary>
        [Fact]
        public void RenamedCandidate_PreservesOpaqueFilePath()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "Original",
                    "public sealed class OriginalType { }");
                string path = Path.Combine(directory, "Renamed.bin");
                ValidatedExternalMetadataReferenceMaterial material =
                    CreateFileMaterial(image, "Original.dll", path);

                PortableExecutableReference reference = CreateReference(material);

                Assert.Equal(path, reference.FilePath);
                Assert.Equal(material.Candidate.AssemblyIdentity, ReadAssemblyIdentity(reference));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Leaves stream-originated material without an invented file path.
        /// </summary>
        [Fact]
        public void StreamMaterial_HasNoFilePath()
        {
            byte[] image = EmitAssembly(
                "StreamDependency",
                "public sealed class StreamDependencyType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "DoNotUseAsPath.dll");

            PortableExecutableReference reference = CreateReference(
                CreateMaterial(expected, image));

            Assert.Null(reference.FilePath);
            Assert.NotEqual(expected.Name, reference.FilePath);
        }

        /// <summary>
        /// Preserves different path provenance for semantically identical PE
        /// bytes without changing metadata identity.
        /// </summary>
        [Fact]
        public void SameBytesAtDifferentPaths_HaveSameMetadataAndDifferentLocations()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "PathIndependentDependency",
                    "public sealed class PathIndependentType { }");
                string firstPath = Path.Combine(directory, "First.bin");
                string secondPath = Path.Combine(directory, "Second.bin");
                PortableExecutableReference first = CreateReference(
                    CreateFileMaterial(image, "Lookup.dll", firstPath));
                PortableExecutableReference second = CreateReference(
                    CreateFileMaterial(image, "Lookup.dll", secondPath));

                Assert.NotEqual(first.FilePath, second.FilePath);
                Assert.Equal(ReadMvid(first), ReadMvid(second));
                Assert.Equal(ReadAssemblyIdentity(first), ReadAssemblyIdentity(second));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Changes alias properties without changing the reconstructed binary
        /// metadata identity.
        /// </summary>
        [Fact]
        public void Aliases_DoNotChangeMetadataIdentity()
        {
            byte[] image = EmitAssembly(
                "AliasIndependentDependency",
                "public sealed class AliasIndependentType { }");
            ExternalCompilationMetadataReferenceDescriptor plain =
                P5BTests.CreateExpectedReference(image, "AliasIndependentDependency.dll");
            ExternalCompilationMetadataReferenceDescriptor aliased = P5BTests.CopyExpected(
                plain,
                aliases: ImmutableArray.Create("zeta", "alpha"));

            PortableExecutableReference first = CreateReference(CreateMaterial(plain, image));
            PortableExecutableReference second = CreateReference(CreateMaterial(aliased, image));

            Assert.Empty(first.Properties.Aliases);
            Assert.Equal(new[] { "zeta", "alpha" }, second.Properties.Aliases);
            Assert.Equal(ReadMvid(first), ReadMvid(second));
            Assert.Equal(ReadAssemblyIdentity(first), ReadAssemblyIdentity(second));
        }

        /// <summary>
        /// Changes EmbedInteropTypes without changing reconstructed binary
        /// metadata identity.
        /// </summary>
        [Fact]
        public void EmbedInteropTypes_DoesNotChangeMetadataIdentity()
        {
            byte[] image = EmitAssembly(
                "InteropIndependentDependency",
                "public sealed class InteropIndependentType { }");
            ExternalCompilationMetadataReferenceDescriptor plain =
                P5BTests.CreateExpectedReference(image, "InteropIndependentDependency.dll");
            ExternalCompilationMetadataReferenceDescriptor embedded = P5BTests.CopyExpected(
                plain,
                embedInteropTypes: true);

            PortableExecutableReference first = CreateReference(CreateMaterial(plain, image));
            PortableExecutableReference second = CreateReference(CreateMaterial(embedded, image));

            Assert.False(first.Properties.EmbedInteropTypes);
            Assert.True(second.Properties.EmbedInteropTypes);
            Assert.Equal(ReadMvid(first), ReadMvid(second));
            Assert.Equal(ReadAssemblyIdentity(first), ReadAssemblyIdentity(second));
        }

        /// <summary>
        /// Rejects a manifest that refers to an additional managed module
        /// because P5C supplies only the manifest image.
        /// </summary>
        [Fact]
        public void MultiModuleAssembly_FailsClosed()
        {
            byte[] image = EmitMultiModuleAssembly();
            Assert.Contains(ReadAssemblyFiles(image), file => file.ContainsMetadata);
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "MultiModuleAssembly.dll");
            ValidatedExternalMetadataReferenceMaterial material = CreateMaterial(expected, image);

            Assert.False(ExternalMetadataReferenceFactory.TryCreate(material, out _));
        }

        /// <summary>
        /// Does not confuse an AssemblyFile entry for a linked resource with
        /// an additional managed metadata module.
        /// </summary>
        [Fact]
        public void ResourceOnlyAssemblyFile_DoesNotTriggerMultiModuleRejection()
        {
            byte[] image = EmitAssemblyWithLinkedResource();
            ImmutableArray<AssemblyFile> files = ReadAssemblyFiles(image);
            Assert.NotEmpty(files);
            Assert.All(files, file => Assert.False(file.ContainsMetadata));
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "ResourceAssembly.dll");

            PortableExecutableReference reference = CreateReference(
                CreateMaterial(expected, image));

            Assert.Equal(expected.ModuleVersionId, ReadMvid(reference));
        }

        /// <summary>
        /// Follows the surrounding factory convention for null inputs.
        /// </summary>
        [Fact]
        public void NullMaterial_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => ExternalMetadataReferenceFactory.TryCreate(null!, out _));
        }

        /// <summary>
        /// Creates validated material from one in-memory image.
        /// </summary>
        internal static ValidatedExternalMetadataReferenceMaterial CreateMaterial(
            ExternalCompilationMetadataReferenceDescriptor expected,
            byte[] image)
        {
            using MemoryStream stream = new(image, writable: false);
            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                stream,
                out ValidatedExternalMetadataReferenceMaterial material));
            return material;
        }

        /// <summary>
        /// Creates file-backed validated material with explicit path
        /// provenance.
        /// </summary>
        private static ValidatedExternalMetadataReferenceMaterial CreateFileMaterial(
            byte[] image,
            string expectedName,
            string candidatePath)
        {
            File.WriteAllBytes(candidatePath, image);
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, expectedName);
            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                expected,
                candidatePath,
                out ValidatedExternalMetadataReferenceMaterial material));
            return material;
        }

        /// <summary>
        /// Reconstructs one reference and requires success.
        /// </summary>
        private static PortableExecutableReference CreateReference(
            ValidatedExternalMetadataReferenceMaterial material)
        {
            Assert.True(ExternalMetadataReferenceFactory.TryCreate(material, out PortableExecutableReference reference));
            return reference;
        }

        /// <summary>
        /// Emits a deterministic single-module assembly.
        /// </summary>
        private static byte[] EmitAssembly(string assemblyName, string source)
        {
            return P5BTests.EmitPe(
                assemblyName,
                source,
                OutputKind.DynamicallyLinkedLibrary);
        }

        /// <summary>
        /// Emits an assembly manifest that links one separately emitted
        /// managed netmodule.
        /// </summary>
        internal static byte[] EmitMultiModuleAssembly()
        {
            byte[] moduleImage = P5BTests.EmitPe(
                "LinkedModule",
                "public sealed class LinkedModuleType { }",
                OutputKind.NetModule);
            PortableExecutableReference moduleReference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(moduleImage),
                new MetadataReferenceProperties(MetadataImageKind.Module),
                filePath: "LinkedModule.netmodule");
            CSharpCompilation compilation = CreateCompilation(
                "MultiModuleAssembly",
                "public sealed class ManifestType { public LinkedModuleType? Value; }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default.Append(moduleReference));
            return Emit(compilation);
        }

        /// <summary>
        /// Emits an assembly with one linked resource file entry.
        /// </summary>
        private static byte[] EmitAssemblyWithLinkedResource()
        {
            CSharpCompilation compilation = CreateCompilation(
                "ResourceAssembly",
                "public sealed class ResourceAssemblyType { }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default);
            ResourceDescription resource = new(
                "Payload",
                "payload.bin",
                () => new MemoryStream(new byte[] { 1, 2, 3 }, writable: false),
                isPublic: true);
            return Emit(compilation, new[] { resource });
        }

        /// <summary>
        /// Emits one compilation and returns its PE image.
        /// </summary>
        private static byte[] Emit(
            CSharpCompilation compilation,
            IEnumerable<ResourceDescription>? resources = null)
        {
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream, manifestResources: resources);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return stream.ToArray();
        }

        /// <summary>
        /// Creates a deterministic controlled compilation.
        /// </summary>
        private static CSharpCompilation CreateCompilation(
            string assemblyName,
            string source,
            OutputKind outputKind,
            IEnumerable<MetadataReference> references)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                Microsoft.CodeAnalysis.Text.SourceText.From(source, Encoding.UTF8),
                path: "/_/Source.cs");
            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(outputKind, deterministic: true));
        }

        /// <summary>
        /// Requires a controlled compilation to be free of errors.
        /// </summary>
        private static void AssertNoErrors(CSharpCompilation compilation)
        {
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Reads the manifest or standalone module MVID through the Roslyn
        /// reference product.
        /// </summary>
        internal static Guid ReadMvid(PortableExecutableReference reference)
        {
            MetadataReader reader = GetManifestModule(reference).GetMetadataReader();
            return reader.GetGuid(reader.GetModuleDefinition().Mvid);
        }

        /// <summary>
        /// Reads the complete assembly identity through reconstructed metadata.
        /// </summary>
        private static AssemblyIdentity ReadAssemblyIdentity(
            PortableExecutableReference reference)
        {
            MetadataReader reader = GetManifestModule(reference).GetMetadataReader();
            Assert.True(ExternalPeMetadataIdentityReader.TryReadAssemblyIdentity(
                reader,
                out AssemblyIdentity identity));
            return identity;
        }

        /// <summary>
        /// Gets the manifest module or the standalone module represented by a
        /// Roslyn reference.
        /// </summary>
        private static ModuleMetadata GetManifestModule(
            PortableExecutableReference reference)
        {
            Metadata metadata = reference.GetMetadata();
            return metadata switch
            {
                AssemblyMetadata assembly => Assert.Single(assembly.GetModules()),
                ModuleMetadata module => module,
                _ => throw new InvalidOperationException("Unexpected Roslyn metadata kind.")
            };
        }

        /// <summary>
        /// Reads every AssemblyFile row from an emitted manifest.
        /// </summary>
        private static ImmutableArray<AssemblyFile> ReadAssemblyFiles(byte[] image)
        {
            using MemoryStream stream = new(image, writable: false);
            using PEReader peReader = new(stream);
            MetadataReader reader = peReader.GetMetadataReader();
            ImmutableArray<AssemblyFile>.Builder files = ImmutableArray.CreateBuilder<AssemblyFile>();

            foreach (AssemblyFileHandle handle in reader.AssemblyFiles)
            {
                files.Add(reader.GetAssemblyFile(handle));
            }

            return files.ToImmutable();
        }
    }
}
