using System.Buffers.Binary;
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
    /// Tests exact PE candidate validation for serialized metadata-reference
    /// provenance.
    /// </summary>
    public sealed class ExternalMetadataReferenceCandidateDescriptorFactoryTests
    {
        /// <summary>
        /// Validates a normal managed assembly and retains its actual metadata
        /// identities and PE provenance.
        /// </summary>
        [Fact]
        public void AssemblyCandidate_ExactProvenanceIsDescribed()
        {
            byte[] image = EmitPe(
                "CandidateAssembly",
                "public sealed class CandidateType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(image, "Original.dll");

            using MemoryStream stream = new(image, writable: false);
            Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out ExternalMetadataReferenceCandidateDescriptor descriptor));

            Assert.Same(expected, descriptor.ExpectedReference);
            Assert.Equal(MetadataImageKind.Assembly, descriptor.Kind);
            Assert.Equal(expected.ModuleVersionId, descriptor.Module.ModuleVersionId);
            Assert.Equal("CandidateAssembly.dll", descriptor.Module.Name);
            Assert.Equal(ReadAssemblyIdentity(image), descriptor.AssemblyIdentity);
            Assert.Equal(expected.Timestamp, descriptor.TimeDateStamp);
            Assert.Equal(expected.ImageSize, descriptor.ImageSize);
            Assert.Null(descriptor.FilePath);
        }

        /// <summary>
        /// Validates a dependency candidate against provenance read through
        /// the real P3, P4A, P4B, and P5A production chain.
        /// </summary>
        [Fact]
        public void RealP5AReference_ExactDependencyCandidateMatches()
        {
            string directory = CreateTempDirectory();

            try
            {
                byte[] dependencyImage = EmitPe(
                    "EndToEndDependency",
                    "public sealed class EndToEndDependencyType { }",
                    OutputKind.DynamicallyLinkedLibrary);
                string dependencyPath = Path.Combine(directory, "dependency.dll");
                File.WriteAllBytes(dependencyPath, dependencyImage);
                PortableExecutableReference dependencyReference =
                    MetadataReference.CreateFromFile(dependencyPath);
                EmittedPortablePdb consumer = EmitPortablePdb(
                    "EndToEndConsumer",
                    "public sealed class ConsumerType { public EndToEndDependencyType? Value; }",
                    MetadataReferences.Default.Append(dependencyReference));
                ExternalCompilationProvenanceDescriptor provenance =
                    ReadCompilationProvenance(consumer);
                ExternalCompilationMetadataReferencesDescriptor references = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                ExternalCompilationMetadataReferenceDescriptor expected = Assert.Single(
                    references.References.Where(reference => reference.Name == "dependency.dll"));

                Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreateFromFile(
                    expected,
                    dependencyPath,
                    out ExternalMetadataReferenceCandidateDescriptor descriptor));
                Assert.Same(expected, descriptor.ExpectedReference);
                Assert.Equal(dependencyPath, descriptor.FilePath);
                Assert.Equal(expected.ModuleVersionId, descriptor.Module.ModuleVersionId);
            }
            finally
            {
                DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Accepts an identical candidate whose explicit file name differs
        /// from the serialized reference lookup name.
        /// </summary>
        [Fact]
        public void RenamedCandidate_NameIsNotBinaryIdentity()
        {
            string directory = CreateTempDirectory();

            try
            {
                byte[] image = EmitPe(
                    "Original",
                    "public sealed class OriginalType { }",
                    OutputKind.DynamicallyLinkedLibrary);
                ExternalCompilationMetadataReferenceDescriptor expected =
                    CreateExpectedReference(image, "Original.dll");
                string candidatePath = Path.Combine(directory, "RenamedCandidate.bin");
                File.WriteAllBytes(candidatePath, image);

                Assert.NotEqual(expected.Name, Path.GetFileName(candidatePath));
                Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreateFromFile(
                    expected,
                    candidatePath,
                    out ExternalMetadataReferenceCandidateDescriptor descriptor));
                Assert.Equal(candidatePath, descriptor.FilePath);
                Assert.Equal("Original.dll", descriptor.Module.Name);
            }
            finally
            {
                DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Accepts identical PE bytes at different paths while preserving each
        /// path only as candidate provenance.
        /// </summary>
        [Fact]
        public void SameBinaryAtDifferentPaths_ValidatesEqually()
        {
            string directory = CreateTempDirectory();

            try
            {
                byte[] image = EmitPe(
                    "PathIndependent",
                    "public sealed class PathIndependentType { }",
                    OutputKind.DynamicallyLinkedLibrary);
                ExternalCompilationMetadataReferenceDescriptor expected =
                    CreateExpectedReference(image, "LookupName.dll");
                string firstPath = Path.Combine(directory, "First.bin");
                string secondPath = Path.Combine(directory, "Second.bin");
                File.WriteAllBytes(firstPath, image);
                File.WriteAllBytes(secondPath, image);

                Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreateFromFile(
                    expected,
                    firstPath,
                    out ExternalMetadataReferenceCandidateDescriptor first));
                Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreateFromFile(
                    expected,
                    secondPath,
                    out ExternalMetadataReferenceCandidateDescriptor second));
                Assert.NotEqual(first.FilePath, second.FilePath);
                Assert.Equal(first.Kind, second.Kind);
                Assert.Equal(first.Module, second.Module);
                Assert.Equal(first.AssemblyIdentity, second.AssemblyIdentity);
                Assert.Equal(first.TimeDateStamp, second.TimeDateStamp);
                Assert.Equal(first.ImageSize, second.ImageSize);
            }
            finally
            {
                DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a candidate when only the expected MVID differs.
        /// </summary>
        [Fact]
        public void MvidMismatch_FailsClosed()
        {
            byte[] image = EmitPe(
                "MvidCandidate",
                "public sealed class MvidType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor actual =
                CreateExpectedReference(image, "MvidCandidate.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = CopyExpected(
                actual,
                moduleVersionId: Guid.NewGuid());

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Rejects a candidate when only the expected COFF timestamp differs.
        /// </summary>
        [Fact]
        public void TimeDateStampMismatch_FailsClosed()
        {
            byte[] image = EmitPe(
                "TimestampCandidate",
                "public sealed class TimestampType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor actual =
                CreateExpectedReference(image, "TimestampCandidate.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = CopyExpected(
                actual,
                timestamp: unchecked(actual.Timestamp + 1));

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Rejects a candidate when only the expected PE image size differs.
        /// </summary>
        [Fact]
        public void ImageSizeMismatch_FailsClosed()
        {
            byte[] image = EmitPe(
                "ImageSizeCandidate",
                "public sealed class ImageSizeType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor actual =
                CreateExpectedReference(image, "ImageSizeCandidate.dll");
            ExternalCompilationMetadataReferenceDescriptor expected = CopyExpected(
                actual,
                imageSize: unchecked(actual.ImageSize + 1));

            Assert.False(TryCreate(expected, image, out _));
        }

        /// <summary>
        /// Rejects assembly metadata when the expected image kind is module.
        /// </summary>
        [Fact]
        public void AssemblyCandidateExpectedAsModule_FailsClosed()
        {
            byte[] image = EmitPe(
                "AssemblyKindCandidate",
                "public sealed class AssemblyKindType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor actual =
                CreateExpectedReference(image, "AssemblyKindCandidate.dll");

            Assert.False(TryCreate(
                CopyExpected(actual, kind: MetadataImageKind.Module),
                image,
                out _));
        }

        /// <summary>
        /// Validates a real Roslyn netmodule and retains no assembly identity.
        /// </summary>
        [Fact]
        public void ModuleCandidate_ExactProvenanceIsDescribed()
        {
            byte[] image = EmitPe(
                "CandidateModule",
                "public sealed class ModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(image, "CandidateModule.netmodule");

            Assert.True(TryCreate(
                expected,
                image,
                out ExternalMetadataReferenceCandidateDescriptor descriptor));
            Assert.Equal(MetadataImageKind.Module, descriptor.Kind);
            Assert.Equal("CandidateModule.netmodule", descriptor.Module.Name);
            Assert.Equal(expected.ModuleVersionId, descriptor.Module.ModuleVersionId);
            Assert.Null(descriptor.AssemblyIdentity);
        }

        /// <summary>
        /// Rejects module metadata when the expected image kind is assembly.
        /// </summary>
        [Fact]
        public void ModuleCandidateExpectedAsAssembly_FailsClosed()
        {
            byte[] image = EmitPe(
                "ModuleKindCandidate",
                "public sealed class ModuleKindType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor actual =
                CreateExpectedReference(image, "ModuleKindCandidate.netmodule");

            Assert.False(TryCreate(
                CopyExpected(actual, kind: MetadataImageKind.Assembly),
                image,
                out _));
        }

        /// <summary>
        /// Rejects another concrete build even when its serialized lookup name
        /// and complete logical assembly identity are equal.
        /// </summary>
        [Fact]
        public void SameNameAndAssemblyIdentityDifferentBuild_FailsClosed()
        {
            byte[] firstImage = EmitPe(
                "SameLogicalAssembly",
                "public sealed class FirstBuildType { }",
                OutputKind.DynamicallyLinkedLibrary);
            byte[] secondImage = EmitPe(
                "SameLogicalAssembly",
                "public sealed class SecondBuildType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(firstImage, "same.dll");

            Assert.Equal(ReadAssemblyIdentity(firstImage), ReadAssemblyIdentity(secondImage));
            Assert.NotEqual(
                ReadModuleIdentity(firstImage).ModuleVersionId,
                ReadModuleIdentity(secondImage).ModuleVersionId);
            Assert.False(TryCreate(expected, secondImage, out _));
        }

        /// <summary>
        /// Shows that aliases remain expected reference properties and do not
        /// affect validation of identical binary provenance.
        /// </summary>
        [Fact]
        public void Aliases_DoNotAffectCandidateValidation()
        {
            byte[] image = EmitPe(
                "AliasCandidate",
                "public sealed class AliasType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor withoutAlias =
                CreateExpectedReference(image, "AliasCandidate.dll");
            ExternalCompilationMetadataReferenceDescriptor withAlias = CopyExpected(
                withoutAlias,
                aliases: ImmutableArray.Create("externAlias"));

            Assert.True(TryCreate(
                withoutAlias,
                image,
                out ExternalMetadataReferenceCandidateDescriptor first));
            Assert.True(TryCreate(
                withAlias,
                image,
                out ExternalMetadataReferenceCandidateDescriptor second));
            Assert.Same(withoutAlias, first.ExpectedReference);
            Assert.Same(withAlias, second.ExpectedReference);
            Assert.Empty(first.ExpectedReference.Aliases);
            Assert.Equal(new[] { "externAlias" }, second.ExpectedReference.Aliases);
        }

        /// <summary>
        /// Shows that EmbedInteropTypes remains an expected reference property
        /// and is not inferred from the candidate PE.
        /// </summary>
        [Fact]
        public void EmbedInteropTypes_DoesNotAffectCandidateValidation()
        {
            byte[] image = EmitPe(
                "InteropCandidate",
                "public sealed class InteropType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor withoutEmbedding =
                CreateExpectedReference(image, "InteropCandidate.dll");
            ExternalCompilationMetadataReferenceDescriptor withEmbedding = CopyExpected(
                withoutEmbedding,
                embedInteropTypes: true);

            Assert.True(TryCreate(
                withoutEmbedding,
                image,
                out ExternalMetadataReferenceCandidateDescriptor first));
            Assert.True(TryCreate(
                withEmbedding,
                image,
                out ExternalMetadataReferenceCandidateDescriptor second));
            Assert.False(first.ExpectedReference.EmbedInteropTypes);
            Assert.True(second.ExpectedReference.EmbedInteropTypes);
        }

        /// <summary>
        /// Supplies malformed and truncated candidate byte sequences.
        /// </summary>
        /// <returns>Invalid PE candidates.</returns>
        public static IEnumerable<object[]> InvalidCandidates()
        {
            yield return new object[] { Array.Empty<byte>() };
            yield return new object[] { new byte[] { 0x4d } };
            yield return new object[] { new byte[] { 0x4d, 0x5a } };
            yield return new object[] { Encoding.ASCII.GetBytes("not a PE image") };

            byte[] valid = EmitPe(
                "TruncatedCandidate",
                "public sealed class TruncatedType { }",
                OutputKind.DynamicallyLinkedLibrary);
            yield return new object[] { valid[..64] };
            yield return new object[] { valid[..(valid.Length / 2)] };
        }

        /// <summary>
        /// Rejects malformed and truncated PE input without returning a partial
        /// descriptor.
        /// </summary>
        /// <param name="image">The invalid candidate bytes.</param>
        [Theory]
        [MemberData(nameof(InvalidCandidates))]
        public void InvalidOrTruncatedCandidate_FailsClosed(byte[] image)
        {
            ExternalCompilationMetadataReferenceDescriptor expected =
                new(
                    "candidate.dll",
                    ImmutableArray<string>.Empty,
                    MetadataImageKind.Assembly,
                    embedInteropTypes: false,
                    timestamp: 1,
                    imageSize: 2,
                    Guid.NewGuid());

            using MemoryStream stream = new(image, writable: false);
            Assert.False(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out ExternalMetadataReferenceCandidateDescriptor descriptor));
            Assert.Null(descriptor);
        }

        /// <summary>
        /// Rejects a structurally valid PE whose CLR metadata directory has
        /// been removed.
        /// </summary>
        [Fact]
        public void PeWithoutManagedMetadata_FailsClosed()
        {
            byte[] managedImage = EmitPe(
                "ManagedSource",
                "public sealed class ManagedType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(managedImage, "ManagedSource.dll");
            byte[] nativeImage = RemoveCliHeaderDirectory(managedImage);

            using MemoryStream verificationStream = new(nativeImage, writable: false);
            using PEReader verificationReader = new(verificationStream);
            Assert.NotNull(verificationReader.PEHeaders.PEHeader);
            Assert.False(verificationReader.HasMetadata);
            Assert.False(TryCreate(expected, nativeImage, out _));
        }

        /// <summary>
        /// Reads the candidate from the caller's current stream position.
        /// </summary>
        [Fact]
        public void CurrentStreamPosition_IsCandidateStart()
        {
            byte[] image = EmitPe(
                "PositionCandidate",
                "public sealed class PositionType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(image, "PositionCandidate.dll");
            byte[] prefixed = Enumerable.Repeat((byte)0xa5, 31).Concat(image).ToArray();
            using MemoryStream stream = new(prefixed, writable: false);
            stream.Position = 31;

            Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
        }

        /// <summary>
        /// Leaves the caller-owned stream open after successful validation.
        /// </summary>
        [Fact]
        public void SuccessfulValidation_LeavesCallerStreamOpen()
        {
            byte[] image = EmitPe(
                "OwnershipSuccess",
                "public sealed class OwnershipType { }",
                OutputKind.DynamicallyLinkedLibrary);
            ExternalCompilationMetadataReferenceDescriptor expected =
                CreateExpectedReference(image, "OwnershipSuccess.dll");
            using MemoryStream stream = new(image, writable: false);

            Assert.True(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
            Assert.True(stream.CanRead);
        }

        /// <summary>
        /// Leaves the caller-owned stream open after failed validation.
        /// </summary>
        [Fact]
        public void FailedValidation_LeavesCallerStreamOpen()
        {
            ExternalCompilationMetadataReferenceDescriptor expected =
                new(
                    "candidate.dll",
                    ImmutableArray<string>.Empty,
                    MetadataImageKind.Assembly,
                    embedInteropTypes: false,
                    timestamp: 1,
                    imageSize: 2,
                    Guid.NewGuid());
            using MemoryStream stream = new(new byte[] { 0x4d, 0x5a }, writable: false);

            Assert.False(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
            Assert.True(stream.CanRead);
        }

        /// <summary>
        /// Fails closed when the explicit candidate file does not exist.
        /// </summary>
        [Fact]
        public void MissingCandidateFile_FailsClosed()
        {
            ExternalCompilationMetadataReferenceDescriptor expected =
                new(
                    "expected.dll",
                    ImmutableArray<string>.Empty,
                    MetadataImageKind.Assembly,
                    embedInteropTypes: false,
                    timestamp: 1,
                    imageSize: 2,
                    Guid.NewGuid());
            string path = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"),
                "missing.bin");

            Assert.False(ExternalMetadataReferenceCandidateDescriptorFactory.TryCreateFromFile(
                expected,
                path,
                out _));
        }

        /// <summary>
        /// Creates a candidate descriptor from in-memory PE bytes.
        /// </summary>
        private static bool TryCreate(
            ExternalCompilationMetadataReferenceDescriptor expected,
            byte[] image,
            out ExternalMetadataReferenceCandidateDescriptor descriptor)
        {
            using MemoryStream stream = new(image, writable: false);
            return ExternalMetadataReferenceCandidateDescriptorFactory.TryCreate(
                expected,
                stream,
                out descriptor);
        }

        /// <summary>
        /// Creates expected P5A provenance by independently reading a PE image.
        /// </summary>
        private static ExternalCompilationMetadataReferenceDescriptor CreateExpectedReference(
            byte[] image,
            string name,
            ImmutableArray<string> aliases = default,
            bool embedInteropTypes = false)
        {
            using MemoryStream stream = new(image, writable: false);
            using PEReader peReader = new(stream);
            MetadataReader metadataReader = peReader.GetMetadataReader();
            ModuleDefinition module = metadataReader.GetModuleDefinition();
            PEHeader peHeader = Assert.IsType<PEHeader>(peReader.PEHeaders.PEHeader);
            return new ExternalCompilationMetadataReferenceDescriptor(
                name,
                aliases,
                metadataReader.IsAssembly
                    ? MetadataImageKind.Assembly
                    : MetadataImageKind.Module,
                embedInteropTypes,
                peReader.PEHeaders.CoffHeader.TimeDateStamp,
                peHeader.SizeOfImage,
                metadataReader.GetGuid(module.Mvid));
        }

        /// <summary>
        /// Copies expected provenance while replacing selected test fields.
        /// </summary>
        private static ExternalCompilationMetadataReferenceDescriptor CopyExpected(
            ExternalCompilationMetadataReferenceDescriptor source,
            ImmutableArray<string>? aliases = null,
            MetadataImageKind? kind = null,
            bool? embedInteropTypes = null,
            int? timestamp = null,
            int? imageSize = null,
            Guid? moduleVersionId = null)
        {
            return new ExternalCompilationMetadataReferenceDescriptor(
                source.Name,
                aliases ?? source.Aliases,
                kind ?? source.Kind,
                embedInteropTypes ?? source.EmbedInteropTypes,
                timestamp ?? source.Timestamp,
                imageSize ?? source.ImageSize,
                moduleVersionId ?? source.ModuleVersionId);
        }

        /// <summary>
        /// Reads the actual assembly identity independently through Roslyn's
        /// bound symbol model.
        /// </summary>
        private static AssemblyIdentity ReadAssemblyIdentity(byte[] image)
        {
            PortableExecutableReference reference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(image));
            CSharpCompilation consumer = CreateCompilation(
                "IdentityConsumer",
                "public sealed class IdentityConsumerType { }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default.Append(reference));
            IAssemblySymbol symbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(reference));
            return symbol.Identity;
        }

        /// <summary>
        /// Reads the actual module identity independently from an emitted PE.
        /// </summary>
        private static ExternalModuleIdentity ReadModuleIdentity(byte[] image)
        {
            using MemoryStream stream = new(image, writable: false);
            using PEReader peReader = new(stream);
            MetadataReader metadataReader = peReader.GetMetadataReader();
            ModuleDefinition module = metadataReader.GetModuleDefinition();
            return new ExternalModuleIdentity(
                metadataReader.GetString(module.Name),
                metadataReader.GetGuid(module.Mvid));
        }

        /// <summary>
        /// Removes the CLI header data-directory entry while preserving an
        /// otherwise structurally valid PE image.
        /// </summary>
        private static byte[] RemoveCliHeaderDirectory(byte[] image)
        {
            byte[] result = image.ToArray();
            using MemoryStream stream = new(result, writable: false);
            using PEReader peReader = new(stream);
            PEHeader peHeader = Assert.IsType<PEHeader>(peReader.PEHeaders.PEHeader);
            int dataDirectoriesOffset = peReader.PEHeaders.PEHeaderStartOffset
                + (peHeader.Magic == PEMagic.PE32Plus ? 112 : 96);
            int cliHeaderDirectoryOffset = dataDirectoriesOffset + (14 * 8);
            BinaryPrimitives.WriteInt32LittleEndian(
                result.AsSpan(cliHeaderDirectoryOffset, sizeof(int)),
                0);
            BinaryPrimitives.WriteInt32LittleEndian(
                result.AsSpan(cliHeaderDirectoryOffset + sizeof(int), sizeof(int)),
                0);
            return result;
        }

        /// <summary>
        /// Emits a controlled managed PE image.
        /// </summary>
        private static byte[] EmitPe(string assemblyName, string source, OutputKind outputKind)
        {
            CSharpCompilation compilation = CreateCompilation(
                assemblyName,
                source,
                outputKind,
                MetadataReferences.Default);
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return stream.ToArray();
        }

        /// <summary>
        /// Emits a controlled assembly and separate Portable PDB.
        /// </summary>
        private static EmittedPortablePdb EmitPortablePdb(
            string assemblyName,
            string source,
            IEnumerable<MetadataReference> references)
        {
            CSharpCompilation compilation = CreateCompilation(
                assemblyName,
                source,
                OutputKind.DynamicallyLinkedLibrary,
                references);
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                pdbFilePath: Path.Combine(Path.GetTempPath(), "consumer.pdb"));
            using MemoryStream peStream = new();
            using MemoryStream pdbStream = new();
            EmitResult result = compilation.Emit(
                peStream,
                pdbStream,
                options: emitOptions);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return new EmittedPortablePdb(peStream.ToArray(), pdbStream.ToArray());
        }

        /// <summary>
        /// Creates a deterministic compilation with UTF-8 source.
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
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(outputKind, deterministic: true));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }

        /// <summary>
        /// Reads P5A compilation provenance through the production P3/P4 chain.
        /// </summary>
        private static ExternalCompilationProvenanceDescriptor ReadCompilationProvenance(
            EmittedPortablePdb emitted)
        {
            PortableExecutableReference reference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(emitted.PeImage));
            CSharpCompilation host = CreateCompilation(
                "ProvenanceHost",
                "public sealed class ProvenanceHostType { }",
                OutputKind.DynamicallyLinkedLibrary,
                MetadataReferences.Default.Append(reference));
            IAssemblySymbol symbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                host.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                host,
                symbol,
                out ExternalAssemblyReferenceDescriptor assemblyDescriptor));
            using MemoryStream peStream = new(emitted.PeImage, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                assemblyDescriptor,
                peStream,
                out ExternalPeDebugDirectoryDescriptor debugDescriptor));
            using MemoryStream pdbStream = new(emitted.PdbImage, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                debugDescriptor,
                pdbStream,
                out ExternalCompilationProvenanceDescriptor provenance));
            return provenance;
        }

        /// <summary>
        /// Creates an isolated temporary directory.
        /// </summary>
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
        /// Deletes a temporary directory if it exists.
        /// </summary>
        private static void DeleteDirectoryIfExists(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Stores a controlled emitted PE and its separate Portable PDB.
        /// </summary>
        private sealed record EmittedPortablePdb(byte[] PeImage, byte[] PdbImage);
    }
}
