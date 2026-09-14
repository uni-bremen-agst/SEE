using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests immutable materialization of P5B-validated metadata-reference PE
    /// candidates.
    /// </summary>
    public sealed class ValidatedExternalMetadataReferenceMaterialFactoryTests
    {
        /// <summary>
        /// Materializes a normal assembly stream and retains the exact bytes
        /// used to create its P5B candidate descriptor.
        /// </summary>
        [Fact]
        public void AssemblyStream_ExactValidatedImageIsRetained()
        {
            byte[] image = EmitAssembly(
                "MaterialAssembly",
                "public sealed class MaterialType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "Original.dll");
            using MemoryStream stream = new(image, writable: false);

            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                stream,
                out ValidatedExternalMetadataReferenceMaterial material));

            Assert.True(material.Image.AsSpan().SequenceEqual(image));
            Assert.Same(expected, material.Candidate.ExpectedReference);
            Assert.Equal(MetadataImageKind.Assembly, material.Candidate.Kind);
            Assert.NotNull(material.Candidate.AssemblyIdentity);
            Assert.Null(material.Candidate.FilePath);
            AssertMaterialMatchesImage(material);
        }

        /// <summary>
        /// Materializes a dependency using expected provenance produced by the
        /// real P3, P4A, P4B, and P5A chain.
        /// </summary>
        [Fact]
        public void RealP5AReference_DependencyMaterializesEndToEnd()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] dependencyImage = EmitAssembly(
                    "MaterialDependency",
                    "public sealed class MaterialDependencyType { }");
                string dependencyPath = Path.Combine(directory, "dependency.dll");
                File.WriteAllBytes(dependencyPath, dependencyImage);
                PortableExecutableReference dependencyReference =
                    MetadataReference.CreateFromFile(dependencyPath);
                P5BTests.EmittedPortablePdb consumer = P5BTests.EmitPortablePdb(
                    "MaterialConsumer",
                    "public sealed class ConsumerType { public MaterialDependencyType? Value; }",
                    MetadataReferences.Default.Append(dependencyReference));
                ExternalCompilationProvenanceDescriptor provenance =
                    P5BTests.ReadCompilationProvenance(consumer);
                ExternalCompilationMetadataReferencesDescriptor references = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                ExternalCompilationMetadataReferenceDescriptor expected = Assert.Single(
                    references.References.Where(reference => reference.Name == "dependency.dll"));

                using MemoryStream stream = new(dependencyImage, writable: false);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                    expected,
                    stream,
                    out ValidatedExternalMetadataReferenceMaterial material));
                Assert.True(material.Image.AsSpan().SequenceEqual(dependencyImage));
                Assert.Same(expected, material.Candidate.ExpectedReference);
                AssertMaterialMatchesImage(material);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Opens and materializes one explicitly supplied candidate file.
        /// </summary>
        [Fact]
        public void FileCandidate_ExplicitPathIsPreservedAsProvenance()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "FileMaterial",
                    "public sealed class FileMaterialType { }");
                ExternalCompilationMetadataReferenceDescriptor expected =
                    P5BTests.CreateExpectedReference(image, "Lookup.dll");
                string candidatePath = Path.Combine(directory, "Candidate.bin");
                File.WriteAllBytes(candidatePath, image);

                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    candidatePath,
                    out ValidatedExternalMetadataReferenceMaterial material));
                Assert.Equal(candidatePath, material.Candidate.FilePath);
                Assert.True(material.Image.AsSpan().SequenceEqual(image));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Retains build A after the candidate path is replaced with build B.
        /// </summary>
        [Fact]
        public void FileReplacementAfterMaterialization_DoesNotChangeMaterial()
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
                ExternalCompilationMetadataReferenceDescriptor expected =
                    P5BTests.CreateExpectedReference(firstImage, "dependency.dll");
                string candidatePath = Path.Combine(directory, "dependency.dll");
                File.WriteAllBytes(candidatePath, firstImage);

                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    candidatePath,
                    out ValidatedExternalMetadataReferenceMaterial material));
                File.WriteAllBytes(candidatePath, secondImage);

                Assert.True(material.Image.AsSpan().SequenceEqual(firstImage));
                Assert.False(material.Image.AsSpan().SequenceEqual(secondImage));
                AssertMaterialMatchesImage(material);
                Assert.False(TryCreate(expected, secondImage, out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps complete validated bytes usable after deleting the candidate
        /// file.
        /// </summary>
        [Fact]
        public void FileDeletionAfterMaterialization_DoesNotInvalidateMaterial()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "DeletedDependency",
                    "public sealed class DeletedDependencyType { }");
                ExternalCompilationMetadataReferenceDescriptor expected =
                    P5BTests.CreateExpectedReference(image, "dependency.dll");
                string candidatePath = Path.Combine(directory, "dependency.dll");
                File.WriteAllBytes(candidatePath, image);
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    candidatePath,
                    out ValidatedExternalMetadataReferenceMaterial material));

                File.Delete(candidatePath);

                Assert.False(File.Exists(candidatePath));
                Assert.True(material.Image.AsSpan().SequenceEqual(image));
                AssertMaterialMatchesImage(material);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a different concrete build and produces no material.
        /// </summary>
        [Fact]
        public void WrongCandidate_FailsClosed()
        {
            byte[] expectedImage = EmitAssembly(
                "WrongCandidate",
                "public sealed class ExpectedBuildType { }");
            byte[] candidateImage = EmitAssembly(
                "WrongCandidate",
                "public sealed class OtherBuildType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(expectedImage, "same.dll");

            Assert.False(TryCreate(expected, candidateImage, out _));
        }

        /// <summary>
        /// Preserves P5B's exact MVID validation gate.
        /// </summary>
        [Fact]
        public void MvidMismatch_FailsClosed()
        {
            byte[] image = EmitAssembly("MaterialMvid", "public sealed class MvidType { }");
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "MaterialMvid.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = P5BTests.CopyExpected(
                actual,
                moduleVersionId: Guid.NewGuid());

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Preserves P5B's exact COFF timestamp validation gate.
        /// </summary>
        [Fact]
        public void TimeDateStampMismatch_FailsClosed()
        {
            byte[] image = EmitAssembly(
                "MaterialTimestamp",
                "public sealed class TimestampType { }");
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "MaterialTimestamp.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = P5BTests.CopyExpected(
                actual,
                timestamp: unchecked(actual.Timestamp + 1));

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Preserves P5B's exact PE image-size validation gate.
        /// </summary>
        [Fact]
        public void ImageSizeMismatch_FailsClosed()
        {
            byte[] image = EmitAssembly(
                "MaterialImageSize",
                "public sealed class ImageSizeType { }");
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "MaterialImageSize.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = P5BTests.CopyExpected(
                actual,
                imageSize: unchecked(actual.ImageSize + 1));

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Rejects assembly material when the expected kind is module.
        /// </summary>
        [Fact]
        public void AssemblyExpectedAsModule_FailsClosed()
        {
            byte[] image = EmitAssembly(
                "AssemblyKindMaterial",
                "public sealed class AssemblyKindType { }");
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "AssemblyKindMaterial.dll");

            Assert.False(TryCreate(
                P5BTests.CopyExpected(actual, kind: MetadataImageKind.Module),
                image,
                out _));
        }

        /// <summary>
        /// Rejects module material when the expected kind is assembly.
        /// </summary>
        [Fact]
        public void ModuleExpectedAsAssembly_FailsClosed()
        {
            byte[] image = P5BTests.EmitPe(
                "ModuleKindMaterial",
                "public sealed class ModuleKindType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor actual =
                P5BTests.CreateExpectedReference(image, "ModuleKindMaterial.netmodule");

            Assert.False(TryCreate(
                P5BTests.CopyExpected(actual, kind: MetadataImageKind.Assembly),
                image,
                out _));
        }

        /// <summary>
        /// Materializes a real Roslyn netmodule without inventing an assembly
        /// identity.
        /// </summary>
        [Fact]
        public void NetModule_ExactValidatedImageIsRetained()
        {
            byte[] image = P5BTests.EmitPe(
                "MaterialModule",
                "public sealed class MaterialModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "MaterialModule.netmodule");

            Assert.True(TryCreate(
                expected,
                image,
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.Equal(MetadataImageKind.Module, material.Candidate.Kind);
            Assert.Null(material.Candidate.AssemblyIdentity);
            Assert.True(material.Image.AsSpan().SequenceEqual(image));
            AssertMaterialMatchesImage(material);
        }

        /// <summary>
        /// Materializes a renamed candidate because lookup name is not binary
        /// identity.
        /// </summary>
        [Fact]
        public void RenamedCandidate_NameDoesNotAffectMaterialization()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "Original",
                    "public sealed class OriginalType { }");
                ExternalCompilationMetadataReferenceDescriptor expected =
                    P5BTests.CreateExpectedReference(image, "Original.dll");
                string candidatePath = Path.Combine(directory, "Renamed.bin");
                File.WriteAllBytes(candidatePath, image);

                Assert.NotEqual(expected.Name, Path.GetFileName(candidatePath));
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    candidatePath,
                    out ValidatedExternalMetadataReferenceMaterial material));
                Assert.Equal(candidatePath, material.Candidate.FilePath);
                Assert.True(material.Image.AsSpan().SequenceEqual(image));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes identical bytes from two paths without treating either
        /// path as identity.
        /// </summary>
        [Fact]
        public void SameBinaryAtDifferentPaths_ProducesEqualImages()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly(
                    "PathMaterial",
                    "public sealed class PathMaterialType { }");
                ExternalCompilationMetadataReferenceDescriptor expected =
                    P5BTests.CreateExpectedReference(image, "Lookup.dll");
                string firstPath = Path.Combine(directory, "First.bin");
                string secondPath = Path.Combine(directory, "Second.bin");
                File.WriteAllBytes(firstPath, image);
                File.WriteAllBytes(secondPath, image);

                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    firstPath,
                    out ValidatedExternalMetadataReferenceMaterial first));
                Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                    expected,
                    secondPath,
                    out ValidatedExternalMetadataReferenceMaterial second));
                Assert.NotEqual(first.Candidate.FilePath, second.Candidate.FilePath);
                Assert.True(first.Image.AsSpan().SequenceEqual(second.Image.AsSpan()));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps aliases as reference properties without changing binary
        /// material validation.
        /// </summary>
        [Fact]
        public void Aliases_DoNotAffectMaterialization()
        {
            byte[] image = EmitAssembly("AliasMaterial", "public sealed class AliasType { }");
            ExternalCompilationMetadataReferenceDescriptor withoutAlias =
                P5BTests.CreateExpectedReference(image, "AliasMaterial.dll");
            ExternalCompilationMetadataReferenceDescriptor withAlias = P5BTests.CopyExpected(
                withoutAlias,
                aliases: ImmutableArray.Create("externAlias"));

            Assert.True(TryCreate(
                withoutAlias,
                image,
                out ValidatedExternalMetadataReferenceMaterial first));
            Assert.True(TryCreate(
                withAlias,
                image,
                out ValidatedExternalMetadataReferenceMaterial second));
            Assert.Same(withoutAlias, first.Candidate.ExpectedReference);
            Assert.Same(withAlias, second.Candidate.ExpectedReference);
        }

        /// <summary>
        /// Keeps EmbedInteropTypes as a reference property without changing
        /// binary material validation.
        /// </summary>
        [Fact]
        public void EmbedInteropTypes_DoesNotAffectMaterialization()
        {
            byte[] image = EmitAssembly(
                "InteropMaterial",
                "public sealed class InteropType { }");
            ExternalCompilationMetadataReferenceDescriptor withoutEmbedding =
                P5BTests.CreateExpectedReference(image, "InteropMaterial.dll");
            ExternalCompilationMetadataReferenceDescriptor withEmbedding =
                P5BTests.CopyExpected(withoutEmbedding, embedInteropTypes: true);

            Assert.True(TryCreate(
                withoutEmbedding,
                image,
                out ValidatedExternalMetadataReferenceMaterial first));
            Assert.True(TryCreate(
                withEmbedding,
                image,
                out ValidatedExternalMetadataReferenceMaterial second));
            Assert.False(first.Candidate.ExpectedReference.EmbedInteropTypes);
            Assert.True(second.Candidate.ExpectedReference.EmbedInteropTypes);
        }

        /// <summary>
        /// Materializes only PE bytes following the caller's current stream
        /// position.
        /// </summary>
        [Fact]
        public void CurrentStreamPosition_DefinesMaterialStart()
        {
            byte[] image = EmitAssembly(
                "OffsetMaterial",
                "public sealed class OffsetType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "OffsetMaterial.dll");
            byte[] prefixed = Enumerable.Repeat((byte)0xa5, 37).Concat(image).ToArray();
            using MemoryStream stream = new(prefixed, writable: false);
            stream.Position = 37;

            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                stream,
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.True(material.Image.AsSpan().SequenceEqual(image));
        }

        /// <summary>
        /// Materializes a readable non-seekable stream in one forward pass.
        /// </summary>
        [Fact]
        public void NonSeekableStream_MaterializesWithoutSeeking()
        {
            byte[] image = EmitAssembly(
                "NonSeekableMaterial",
                "public sealed class NonSeekableType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "NonSeekableMaterial.dll");
            using CountingNonSeekableStream stream = new(image);

            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                stream,
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.Equal(image.Length, stream.TotalBytesRead);
            Assert.Equal(0, stream.SeekCount);
            Assert.True(stream.CanRead);
            Assert.True(material.Image.AsSpan().SequenceEqual(image));
        }

        /// <summary>
        /// Leaves caller streams open after success, provenance mismatch, and
        /// malformed input.
        /// </summary>
        [Fact]
        public void CallerStreams_RemainOpenForAllOutcomes()
        {
            byte[] image = EmitAssembly(
                "OwnershipMaterial",
                "public sealed class OwnershipType { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(image, "OwnershipMaterial.dll");
            using MemoryStream successful = new(image, writable: false);
            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                successful,
                out _));
            Assert.True(successful.CanRead);

            ExternalCompilationMetadataReferenceDescriptor mismatch = P5BTests.CopyExpected(
                expected,
                moduleVersionId: Guid.NewGuid());
            using MemoryStream validationFailure = new(image, writable: false);
            Assert.False(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                mismatch,
                validationFailure,
                out _));
            Assert.True(validationFailure.CanRead);

            using MemoryStream malformed = new(new byte[] { 0x4d, 0x5a }, writable: false);
            Assert.False(ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                malformed,
                out _));
            Assert.True(malformed.CanRead);
        }

        /// <summary>
        /// Rejects malformed PE bytes without returning partial material.
        /// </summary>
        [Fact]
        public void MalformedPe_FailsClosed()
        {
            byte[] valid = EmitAssembly("MalformedExpected", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(valid, "MalformedExpected.dll");

            Assert.False(TryCreate(
                expected,
                new byte[] { 0x4d, 0x5a, 0x00 },
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.Null(material);
        }

        /// <summary>
        /// Rejects an empty snapshot without returning partial material.
        /// </summary>
        [Fact]
        public void EmptyStream_FailsClosed()
        {
            byte[] valid = EmitAssembly("EmptyExpected", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(valid, "EmptyExpected.dll");

            Assert.False(TryCreate(
                expected,
                Array.Empty<byte>(),
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.Null(material);
        }

        /// <summary>
        /// Rejects a structurally valid PE without managed metadata.
        /// </summary>
        [Fact]
        public void PeWithoutManagedMetadata_FailsClosed()
        {
            byte[] managedImage = EmitAssembly(
                "NoMetadataExpected",
                "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected =
                P5BTests.CreateExpectedReference(managedImage, "NoMetadataExpected.dll");
            byte[] nativeImage = P5BTests.RemoveCliHeaderDirectory(managedImage);

            Assert.False(TryCreate(
                expected,
                nativeImage,
                out ValidatedExternalMetadataReferenceMaterial material));
            Assert.Null(material);
        }

        /// <summary>
        /// Materializes an in-memory candidate for concise test assertions.
        /// </summary>
        private static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expected,
            byte[] image,
            out ValidatedExternalMetadataReferenceMaterial material)
        {
            using MemoryStream stream = new(image, writable: false);
            return ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                expected,
                stream,
                out material);
        }

        /// <summary>
        /// Emits a deterministic assembly through the shared P5B test helper.
        /// </summary>
        private static byte[] EmitAssembly(string assemblyName, string source)
        {
            return P5BTests.EmitPe(
                assemblyName,
                source,
                OutputKind.DynamicallyLinkedLibrary);
        }

        /// <summary>
        /// Verifies independently that retained candidate provenance was read
        /// from the retained immutable image.
        /// </summary>
        private static void AssertMaterialMatchesImage(
            ValidatedExternalMetadataReferenceMaterial material)
        {
            using PEReader peReader = new(material.Image);
            MetadataReader metadataReader = peReader.GetMetadataReader();
            ModuleDefinition module = metadataReader.GetModuleDefinition();
            PEHeader peHeader = Assert.IsType<PEHeader>(peReader.PEHeaders.PEHeader);

            Assert.Equal(
                metadataReader.GetGuid(module.Mvid),
                material.Candidate.Module.ModuleVersionId);
            Assert.Equal(
                peReader.PEHeaders.CoffHeader.TimeDateStamp,
                material.Candidate.TimeDateStamp);
            Assert.Equal(peHeader.SizeOfImage, material.Candidate.ImageSize);
        }

        /// <summary>
        /// Provides a readable forward-only stream and records byte and seek
        /// activity.
        /// </summary>
        private sealed class CountingNonSeekableStream : Stream
        {
            /// <summary>
            /// The underlying readable image stream.
            /// </summary>
            private readonly MemoryStream stream;

            /// <summary>
            /// Whether this wrapper has been disposed.
            /// </summary>
            private bool disposed;

            /// <summary>
            /// Initializes a readable non-seekable stream over an image.
            /// </summary>
            public CountingNonSeekableStream(byte[] image)
            {
                stream = new MemoryStream(image, writable: false);
            }

            /// <inheritdoc/>
            public override bool CanRead => !disposed;

            /// <inheritdoc/>
            public override bool CanSeek => false;

            /// <inheritdoc/>
            public override bool CanWrite => false;

            /// <inheritdoc/>
            public override long Length => throw new NotSupportedException();

            /// <inheritdoc/>
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            /// <summary>
            /// Gets the total number of payload bytes read.
            /// </summary>
            public int TotalBytesRead { get; private set; }

            /// <summary>
            /// Gets the number of attempted seek operations.
            /// </summary>
            public int SeekCount { get; private set; }

            /// <inheritdoc/>
            public override void Flush()
            {
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = stream.Read(buffer, offset, count);
                TotalBytesRead += bytesRead;
                return bytesRead;
            }

            /// <inheritdoc/>
            public override int Read(Span<byte> buffer)
            {
                int bytesRead = stream.Read(buffer);
                TotalBytesRead += bytesRead;
                return bytesRead;
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                SeekCount++;
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    stream.Dispose();
                }

                disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
