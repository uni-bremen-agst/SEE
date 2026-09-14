using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests validated PE debug-directory provenance extraction.
    /// </summary>
    public sealed class ExternalPeDebugDirectoryDescriptorFactoryTests
    {
        /// <summary>
        /// Reads a Portable PDB CodeView identity from the PE and matches it
        /// against the independently read Portable PDB identifier.
        /// </summary>
        [Fact]
        public void PortablePdb_UsesCodeViewGuidAndStampForContentId()
        {
            string pdbPath = CreateNonexistentPdbPath();
            EmittedAssembly emitted = EmitAssembly(
                "PortableLibrary",
                "public sealed class PortableType { public void Method() { } }",
                DebugInformationFormat.PortablePdb,
                pdbPath,
                deterministic: true);
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);
            ExternalCodeViewPdbReference codeView = Assert.Single(
                descriptor.CodeViewPdbReferences.Where(reference => reference.IsPortable));
            BlobContentId actualPdbId = ReadPortablePdbId(emitted.PdbImage!);

            Assert.Equal(pdbPath, codeView.Path);
            Assert.True(codeView.PortablePdbId.HasValue);
            Assert.Equal(actualPdbId, codeView.PortablePdbId.Value);
            Assert.Equal(
                new BlobContentId(codeView.Guid, codeView.Stamp),
                codeView.PortablePdbId.Value);
            Assert.NotEqual((uint)codeView.Age, codeView.Stamp);
        }

        /// <summary>
        /// Treats a nonexistent CodeView path as provenance without opening
        /// or resolving it.
        /// </summary>
        [Fact]
        public void NonexistentCodeViewPath_DoesNotPreventPeAnalysis()
        {
            string pdbPath = CreateNonexistentPdbPath();
            Assert.False(File.Exists(pdbPath));
            Assert.False(Directory.Exists(Path.GetDirectoryName(pdbPath)));
            EmittedAssembly emitted = EmitAssembly(
                "PathLibrary",
                "public sealed class PathType { }",
                DebugInformationFormat.PortablePdb,
                pdbPath,
                deterministic: true);
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);

            Assert.Contains(
                descriptor.CodeViewPdbReferences,
                reference => reference.Path == pdbPath);
            Assert.False(File.Exists(pdbPath));
            Assert.False(Directory.Exists(Path.GetDirectoryName(pdbPath)));
        }

        /// <summary>
        /// Reads an embedded Portable PDB identity through the framework
        /// provider and confirms its CodeView association by content ID.
        /// </summary>
        [Fact]
        public void EmbeddedPortablePdb_UsesEmbeddedMetadataIdentity()
        {
            EmittedAssembly emitted = EmitAssembly(
                "EmbeddedLibrary",
                "public sealed class EmbeddedType { public int Value => 1; }",
                DebugInformationFormat.Embedded,
                CreateNonexistentPdbPath(),
                deterministic: true);
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);
            BlobContentId independentlyReadId = ReadEmbeddedPortablePdbId(
                emitted.PeImage);

            Assert.Contains(independentlyReadId, descriptor.EmbeddedPortablePdbIds);

            foreach (ExternalCodeViewPdbReference codeView in
                     descriptor.CodeViewPdbReferences.Where(reference => reference.IsPortable))
            {
                Assert.Equal(independentlyReadId, codeView.PortablePdbId);
            }
        }

        /// <summary>
        /// Describes a valid assembly that has no PDB-related debug entries.
        /// </summary>
        [Fact]
        public void PeWithoutPdb_ReturnsEmptyPdbCollections()
        {
            EmittedAssembly emitted = EmitAssemblyWithoutPdb(
                "NoPdbLibrary",
                "public sealed class NoPdbType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);

            Assert.Empty(descriptor.CodeViewPdbReferences);
            Assert.Empty(descriptor.EmbeddedPortablePdbIds);
            Assert.Empty(descriptor.PdbChecksums);
            Assert.Equal(expected.Modules[0], descriptor.ManifestModule);
        }

        /// <summary>
        /// Uses only the reproducible debug-directory entry to classify a
        /// deterministic PE.
        /// </summary>
        [Fact]
        public void DeterministicBuild_HasReproducibleDebugEntry()
        {
            EmittedAssembly emitted = EmitAssembly(
                "DeterministicLibrary",
                "public sealed class DeterministicType { }",
                DebugInformationFormat.PortablePdb,
                CreateNonexistentPdbPath(),
                deterministic: true);
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);

            Assert.True(descriptor.IsDeterministic);
        }

        /// <summary>
        /// Preserves PDB checksum declarations and compares checksum bytes by
        /// value rather than immutable-array backing identity.
        /// </summary>
        [Fact]
        public void PdbChecksum_PreservesFrameworkDataWithValueEquality()
        {
            EmittedAssembly emitted = EmitAssembly(
                "ChecksumLibrary",
                "public sealed class ChecksumType { }",
                DebugInformationFormat.PortablePdb,
                CreateNonexistentPdbPath(),
                deterministic: true);
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);
            ExternalPdbChecksum checksum = Assert.Single(descriptor.PdbChecksums);
            PdbChecksumDebugDirectoryData independentlyRead = ReadPdbChecksum(
                emitted.PeImage);
            ExternalPdbChecksum copiedChecksum = new(
                checksum.AlgorithmName,
                ImmutableArray.CreateRange(checksum.Checksum));

            Assert.Equal(independentlyRead.AlgorithmName, checksum.AlgorithmName);
            Assert.True(independentlyRead.Checksum.SequenceEqual(checksum.Checksum));
            Assert.NotEmpty(checksum.Checksum);
            Assert.Equal(checksum, copiedChecksum);
            Assert.Equal(checksum.GetHashCode(), copiedChecksum.GetHashCode());
        }

        /// <summary>
        /// Rejects a replacement binary at the same path when its MVID no
        /// longer matches the original Roslyn-bound snapshot.
        /// </summary>
        [Fact]
        public void SamePathReplacement_WithDifferentMvidFailsClosed()
        {
            string filePath = Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p4a-{Guid.NewGuid():N}.dll");
            EmittedAssembly first = EmitAssemblyWithoutPdb(
                "ReplaceableLibrary",
                "public sealed class FirstBuildType { }");
            EmittedAssembly second = EmitAssemblyWithoutPdb(
                "ReplaceableLibrary",
                "public sealed class SecondBuildType { }");
            File.WriteAllBytes(filePath, first.PeImage);

            try
            {
                ExternalAssemblyReferenceDescriptor expected =
                    CreateExpectedDescriptor(first.PeImage, filePath);
                ExternalAssemblyReferenceDescriptor replacement =
                    CreateExpectedDescriptor(second.PeImage);
                Assert.Equal(expected.AssemblyIdentity, replacement.AssemblyIdentity);
                Assert.NotEqual(
                    expected.Modules[0].ModuleVersionId,
                    replacement.Modules[0].ModuleVersionId);
                File.WriteAllBytes(filePath, second.PeImage);

                Assert.False(ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                    expected,
                    out _));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Fails closed for a missing file path while allowing the same PE
        /// bytes to be analyzed explicitly through the Core API.
        /// </summary>
        [Fact]
        public void NullFilePath_FailsFileLookupButCoreStillWorks()
        {
            EmittedAssembly emitted = EmitAssemblyWithoutPdb(
                "InMemoryLibrary",
                "public sealed class InMemoryType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);
            Assert.Null(expected.FilePath);

            Assert.False(ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                expected,
                out _));

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(expected, emitted.PeImage);
            Assert.Equal(expected.Modules[0], descriptor.ManifestModule);
        }

        /// <summary>
        /// Rejects PE bytes with the same assembly identity but a different
        /// manifest MVID.
        /// </summary>
        [Fact]
        public void MvidMismatch_FailsClosed()
        {
            EmittedAssembly first = EmitAssemblyWithoutPdb(
                "SharedIdentity",
                "public sealed class FirstType { }");
            EmittedAssembly second = EmitAssemblyWithoutPdb(
                "SharedIdentity",
                "public sealed class SecondType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(first.PeImage);
            ExternalAssemblyReferenceDescriptor actual =
                CreateExpectedDescriptor(second.PeImage);
            Assert.Equal(expected.AssemblyIdentity, actual.AssemblyIdentity);
            Assert.NotEqual(expected.Modules[0], actual.Modules[0]);

            using MemoryStream stream = new(second.PeImage, writable: false);
            Assert.False(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects PE bytes whose complete assembly identity differs from the
        /// expected Roslyn-bound identity.
        /// </summary>
        [Fact]
        public void AssemblyIdentityMismatch_FailsClosed()
        {
            EmittedAssembly first = EmitAssemblyWithoutPdb(
                "IdentityA",
                "public sealed class IdentityAType { }");
            EmittedAssembly second = EmitAssemblyWithoutPdb(
                "IdentityB",
                "public sealed class IdentityBType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(first.PeImage);
            ExternalAssemblyReferenceDescriptor actual =
                CreateExpectedDescriptor(second.PeImage);
            Assert.NotEqual(expected.AssemblyIdentity, actual.AssemblyIdentity);

            using MemoryStream stream = new(second.PeImage, writable: false);
            Assert.False(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects malformed PE bytes without returning partial provenance.
        /// </summary>
        [Fact]
        public void InvalidPe_FailsClosed()
        {
            EmittedAssembly valid = EmitAssemblyWithoutPdb(
                "ValidLibrary",
                "public sealed class ValidType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(valid.PeImage);
            using MemoryStream stream = new(
                new byte[] { 0x50, 0x45, 0x00, 0x00 },
                writable: false);

            Assert.False(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
        }

        /// <summary>
        /// Leaves a caller-owned Core stream open after successful analysis.
        /// </summary>
        [Fact]
        public void CoreStream_RemainsOpen()
        {
            EmittedAssembly emitted = EmitAssemblyWithoutPdb(
                "StreamLibrary",
                "public sealed class StreamType { }");
            ExternalAssemblyReferenceDescriptor expected =
                CreateExpectedDescriptor(emitted.PeImage);
            using MemoryStream stream = new(emitted.PeImage, writable: false);

            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
            Assert.True(stream.CanRead);
        }

        /// <summary>
        /// Allows technical debug-directory inspection of a reference
        /// assembly without treating it as an implementation binary.
        /// </summary>
        [Fact]
        public void ReferenceAssemblyDescriptor_CanBeInspectedTechnically()
        {
            EmittedAssembly emitted = EmitAssemblyWithoutPdb(
                "ReferenceLibrary",
                "public sealed class ReferenceType { }");
            ExternalAssemblyReferenceDescriptor original =
                CreateExpectedDescriptor(emitted.PeImage);
            ExternalAssemblyReferenceDescriptor referenceDescriptor = new(
                original.AssemblyIdentity,
                original.Modules,
                original.FilePath,
                isReferenceAssembly: true);

            ExternalPeDebugDirectoryDescriptor descriptor =
                ReadRequiredDescriptor(referenceDescriptor, emitted.PeImage);
            Assert.Equal(referenceDescriptor.Modules[0], descriptor.ManifestModule);
        }

        /// <summary>
        /// Revalidates the complete strong-named identity of a real
        /// file-backed framework binary before reading its debug directory.
        /// </summary>
        [Fact]
        public void FileBackedFrameworkAssembly_RevalidatesStrongNameIdentity()
        {
            PortableExecutableReference reference =
                Assert.IsAssignableFrom<PortableExecutableReference>(
                    MetadataReferences.Default[0]);
            CSharpCompilation compilation = CreateCompilation(
                "FrameworkConsumer",
                "public sealed class FrameworkConsumerType { }",
                references: Array.Empty<MetadataReference>());
            IAssemblySymbol assemblySymbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                compilation.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                compilation,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor expected));
            Assert.False(expected.AssemblyIdentity.PublicKeyToken.IsDefaultOrEmpty);

            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                expected,
                out ExternalPeDebugDirectoryDescriptor descriptor));
            Assert.Equal(expected.Modules[0], descriptor.ManifestModule);
        }

        /// <summary>
        /// Reads a required P4A descriptor from in-memory PE bytes.
        /// </summary>
        /// <param name="expected">The expected P3 descriptor.</param>
        /// <param name="peImage">The PE bytes to inspect.</param>
        /// <returns>The validated P4A descriptor.</returns>
        private static ExternalPeDebugDirectoryDescriptor ReadRequiredDescriptor(
            ExternalAssemblyReferenceDescriptor expected,
            byte[] peImage)
        {
            using MemoryStream stream = new(peImage, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                expected,
                stream,
                out ExternalPeDebugDirectoryDescriptor descriptor));
            return descriptor;
        }

        /// <summary>
        /// Creates a P3 descriptor for an in-memory PE reference.
        /// </summary>
        /// <param name="peImage">The PE image.</param>
        /// <param name="filePath">The optional provenance path.</param>
        /// <returns>The descriptor created through the P3 factory.</returns>
        private static ExternalAssemblyReferenceDescriptor CreateExpectedDescriptor(
            byte[] peImage,
            string? filePath = null)
        {
            PortableExecutableReference reference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(peImage),
                filePath: filePath);
            CSharpCompilation consumer = CreateCompilation(
                "Consumer",
                "public sealed class ConsumerType { }",
                new[] { reference });
            IAssemblySymbol assemblySymbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor descriptor));
            return descriptor;
        }

        /// <summary>
        /// Emits an assembly and a separate or embedded Portable PDB.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="source">The complete source.</param>
        /// <param name="debugInformationFormat">The requested PDB format.</param>
        /// <param name="pdbFilePath">The provenance path recorded in the PE.</param>
        /// <param name="deterministic">Whether to emit deterministically.</param>
        /// <returns>The emitted PE and optional separate PDB bytes.</returns>
        private static EmittedAssembly EmitAssembly(
            string assemblyName,
            string source,
            DebugInformationFormat debugInformationFormat,
            string pdbFilePath,
            bool deterministic)
        {
            CSharpCompilation compilation = CreateCompilation(
                assemblyName,
                source,
                references: Array.Empty<MetadataReference>(),
                deterministic);
            EmitOptions emitOptions = new(
                debugInformationFormat: debugInformationFormat,
                pdbFilePath: pdbFilePath);
            using MemoryStream peStream = new();
            using MemoryStream? pdbStream = debugInformationFormat == DebugInformationFormat.Embedded
                ? null
                : new MemoryStream();
            EmitResult result = compilation.Emit(
                peStream,
                pdbStream: pdbStream,
                options: emitOptions);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return new EmittedAssembly(
                peStream.ToArray(),
                pdbStream?.ToArray());
        }

        /// <summary>
        /// Emits an assembly without PDB data.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="source">The complete source.</param>
        /// <returns>The emitted PE bytes.</returns>
        private static EmittedAssembly EmitAssemblyWithoutPdb(
            string assemblyName,
            string source)
        {
            CSharpCompilation compilation = CreateCompilation(
                assemblyName,
                source,
                references: Array.Empty<MetadataReference>(),
                deterministic: true);
            using MemoryStream peStream = new();
            EmitResult result = compilation.Emit(peStream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return new EmittedAssembly(peStream.ToArray(), pdbImage: null);
        }

        /// <summary>
        /// Creates a deterministic library compilation with UTF-8 source.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="source">The complete source.</param>
        /// <param name="references">Additional metadata references.</param>
        /// <param name="deterministic">Whether to emit deterministically.</param>
        /// <returns>The compilation.</returns>
        private static CSharpCompilation CreateCompilation(
            string assemblyName,
            string source,
            IEnumerable<MetadataReference> references,
            bool deterministic = true)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                Microsoft.CodeAnalysis.Text.SourceText.From(source, Encoding.UTF8));
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                MetadataReferences.Default.Concat(references),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: deterministic));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }

        /// <summary>
        /// Creates a PDB provenance path in a directory that does not exist.
        /// </summary>
        /// <returns>The nonexistent platform-neutral path.</returns>
        private static string CreateNonexistentPdbPath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p4a-{Guid.NewGuid():N}",
                "external.pdb");
        }

        /// <summary>
        /// Reads the content identifier from separate Portable PDB bytes.
        /// </summary>
        /// <param name="pdbImage">The Portable PDB image.</param>
        /// <returns>The Portable PDB content identifier.</returns>
        private static BlobContentId ReadPortablePdbId(byte[] pdbImage)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(
                    stream,
                    MetadataStreamOptions.LeaveOpen);
            MetadataReader metadataReader = provider.GetMetadataReader();
            DebugMetadataHeader debugHeader = Assert.IsType<DebugMetadataHeader>(
                metadataReader.DebugMetadataHeader);
            return new BlobContentId(debugHeader.Id);
        }

        /// <summary>
        /// Independently reads the first embedded Portable PDB identifier.
        /// </summary>
        /// <param name="peImage">The PE image.</param>
        /// <returns>The embedded Portable PDB content identifier.</returns>
        private static BlobContentId ReadEmbeddedPortablePdbId(byte[] peImage)
        {
            using MemoryStream stream = new(peImage, writable: false);
            using PEReader peReader = new(
                stream,
                PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage);
            DebugDirectoryEntry entry = Assert.Single(
                peReader.ReadDebugDirectory().Where(
                    candidate => candidate.Type ==
                        DebugDirectoryEntryType.EmbeddedPortablePdb));
            using MetadataReaderProvider provider =
                peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
            MetadataReader metadataReader = provider.GetMetadataReader();
            DebugMetadataHeader debugHeader = Assert.IsType<DebugMetadataHeader>(
                metadataReader.DebugMetadataHeader);
            return new BlobContentId(debugHeader.Id);
        }

        /// <summary>
        /// Independently reads the first PDB checksum declaration.
        /// </summary>
        /// <param name="peImage">The PE image.</param>
        /// <returns>The framework checksum data.</returns>
        private static PdbChecksumDebugDirectoryData ReadPdbChecksum(byte[] peImage)
        {
            using MemoryStream stream = new(peImage, writable: false);
            using PEReader peReader = new(
                stream,
                PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchEntireImage);
            DebugDirectoryEntry entry = Assert.Single(
                peReader.ReadDebugDirectory().Where(
                    candidate => candidate.Type == DebugDirectoryEntryType.PdbChecksum));
            return peReader.ReadPdbChecksumDebugDirectoryData(entry);
        }

        /// <summary>
        /// Stores PE and optional separate PDB bytes emitted by a test.
        /// </summary>
        private sealed class EmittedAssembly
        {
            /// <summary>
            /// Initializes emitted assembly bytes.
            /// </summary>
            /// <param name="peImage">The PE image.</param>
            /// <param name="pdbImage">The optional separate PDB image.</param>
            public EmittedAssembly(byte[] peImage, byte[]? pdbImage)
            {
                PeImage = peImage;
                PdbImage = pdbImage;
            }

            /// <summary>
            /// Gets the PE image.
            /// </summary>
            /// <value>The complete PE bytes.</value>
            public byte[] PeImage { get; }

            /// <summary>
            /// Gets the optional separate PDB image.
            /// </summary>
            /// <value>The PDB bytes, or <see langword="null"/>.</value>
            public byte[]? PdbImage { get; }
        }
    }
}
