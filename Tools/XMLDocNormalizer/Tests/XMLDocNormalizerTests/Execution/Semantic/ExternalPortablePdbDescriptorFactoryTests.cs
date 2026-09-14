using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests explicit Portable PDB candidate validation and source provenance.
    /// </summary>
    public sealed class ExternalPortablePdbDescriptorFactoryTests
    {
        /// <summary>
        /// The standardized Portable PDB SHA-1 document hash identifier.
        /// </summary>
        private static readonly Guid Sha1DocumentHashAlgorithm =
            new("ff1816ec-aa5e-4d10-87f7-6f4963833460");

        /// <summary>
        /// The standardized Portable PDB SHA-256 document hash identifier.
        /// </summary>
        private static readonly Guid Sha256DocumentHashAlgorithm =
            new("8829d00f-11b8-4213-878b-770e8597ac16");

        /// <summary>
        /// The standardized Portable PDB SHA-384 document hash identifier.
        /// </summary>
        private static readonly Guid Sha384DocumentHashAlgorithm =
            new("d99cfeb1-8c43-444a-8a6c-b61269d2a0bf");

        /// <summary>
        /// The standardized Portable PDB SHA-512 document hash identifier.
        /// </summary>
        private static readonly Guid Sha512DocumentHashAlgorithm =
            new("ef2d1afc-6550-46d6-b14b-d70afe9a5566");

        /// <summary>
        /// Validates a matching Roslyn Portable PDB by both identity and its
        /// PE-provided checksum.
        /// </summary>
        [Fact]
        public void MatchingPortablePdb_ValidatesIdentityAndChecksum()
        {
            PortablePdbTestData testData = CreateDefaultTestData("MatchingLibrary");

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);

            Assert.Equal(ReadPortablePdbId(testData.PdbImage), descriptor.Id);
            Assert.Equal(
                PortablePdbValidationKind.IdentityAndChecksum,
                descriptor.ValidationKind);
        }

        /// <summary>
        /// Rejects a valid Portable PDB whose content identity belongs to a
        /// different build.
        /// </summary>
        [Fact]
        public void WrongPortablePdb_FailsClosed()
        {
            PortablePdbTestData first = CreateDefaultTestData(
                "SharedLibrary",
                "public sealed class FirstBuildType { }");
            PortablePdbTestData second = CreateDefaultTestData(
                "SharedLibrary",
                "public sealed class SecondBuildType { }");
            Assert.NotEqual(
                ReadPortablePdbId(first.PdbImage),
                ReadPortablePdbId(second.PdbImage));

            using MemoryStream stream = new(second.PdbImage, writable: false);
            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                first.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects a still-readable candidate whose PDB ID is unchanged but
        /// whose remaining content no longer matches the PE checksum.
        /// </summary>
        [Fact]
        public void TamperedPortablePdb_WithMatchingIdFailsChecksumValidation()
        {
            const string sourceLinkJson =
                "{\"documents\":{\"/_/*\":\"https://example.test/tamper/*\"}}";
            PortablePdbTestData testData = EmitPortablePdb(
                "TamperLibrary",
                new[]
                {
                    new TestSource(
                        "/_/Tamper.cs",
                        "public sealed class TamperType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                },
                sourceLinkJson);
            byte[] tampered = (byte[])testData.PdbImage.Clone();
            byte[] marker = Encoding.UTF8.GetBytes("example.test");
            int markerOffset = FindSequence(tampered, marker);
            Assert.True(markerOffset >= 0);
            int idStartOffset = ReadPortablePdbIdStartOffset(tampered);
            Assert.DoesNotContain(
                markerOffset,
                Enumerable.Range(idStartOffset, 20));
            tampered[markerOffset] = (byte)'E';

            Assert.Equal(
                ReadPortablePdbId(testData.PdbImage),
                ReadPortablePdbId(tampered));

            using MemoryStream stream = new(tampered, writable: false);
            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Accepts a matching PDB by identity when the PE provides no checksum.
        /// </summary>
        [Fact]
        public void NoExpectedPdbChecksum_UsesIdentityValidationKind()
        {
            PortablePdbTestData testData = CreateDefaultTestData("IdentityOnlyLibrary");
            ExternalPeDebugDirectoryDescriptor identityOnly = CopyDebugDescriptor(
                testData.DebugDescriptor,
                ImmutableArray<ExternalPdbChecksum>.Empty);

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                identityOnly,
                testData.PdbImage);

            Assert.Equal(PortablePdbValidationKind.Identity, descriptor.ValidationKind);
        }

        /// <summary>
        /// Rejects a candidate when P4A contains no expected Portable PDB ID.
        /// </summary>
        [Fact]
        public void NoExpectedPortablePdbIdentity_FailsClosed()
        {
            PortablePdbTestData testData = CreateDefaultTestData("NoIdentityLibrary");
            ExternalPeDebugDirectoryDescriptor noIdentity = new(
                testData.DebugDescriptor.ManifestModule,
                testData.DebugDescriptor.IsDeterministic,
                ImmutableArray<ExternalCodeViewPdbReference>.Empty,
                ImmutableArray<BlobContentId>.Empty,
                testData.DebugDescriptor.PdbChecksums);

            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                noIdentity,
                stream,
                out _));
        }

        /// <summary>
        /// Supplies supported PE checksum algorithms for validation tests.
        /// </summary>
        /// <returns>The exact algorithm names and concrete algorithms.</returns>
        public static IEnumerable<object[]> SupportedPdbChecksumAlgorithms()
        {
            yield return new object[] { "SHA256", HashAlgorithmName.SHA256 };
            yield return new object[] { "SHA384", HashAlgorithmName.SHA384 };
            yield return new object[] { "SHA512", HashAlgorithmName.SHA512 };
        }

        /// <summary>
        /// Validates each explicitly supported case-sensitive PDB checksum
        /// algorithm.
        /// </summary>
        /// <param name="algorithmName">The exact PE algorithm name.</param>
        /// <param name="hashAlgorithm">The concrete test hash algorithm.</param>
        [Theory]
        [MemberData(nameof(SupportedPdbChecksumAlgorithms))]
        public void SupportedPdbChecksumAlgorithm_Validates(
            string algorithmName,
            HashAlgorithmName hashAlgorithm)
        {
            PortablePdbTestData testData = CreateDefaultTestData(
                $"Checksum{algorithmName}Library");
            ImmutableArray<byte> checksum = ImmutableArray.CreateRange(
                CalculatePortablePdbChecksum(testData.PdbImage, hashAlgorithm));
            ExternalPeDebugDirectoryDescriptor expected = CopyDebugDescriptor(
                testData.DebugDescriptor,
                ImmutableArray.Create(new ExternalPdbChecksum(algorithmName, checksum)));

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                expected,
                testData.PdbImage);
            Assert.Equal(
                PortablePdbValidationKind.IdentityAndChecksum,
                descriptor.ValidationKind);
        }

        /// <summary>
        /// Rejects case-variant or otherwise unsupported PDB checksum names.
        /// </summary>
        [Fact]
        public void UnsupportedPdbChecksumAlgorithm_FailsClosed()
        {
            PortablePdbTestData testData = CreateDefaultTestData(
                "UnsupportedChecksumLibrary");
            ExternalPeDebugDirectoryDescriptor expected = CopyDebugDescriptor(
                testData.DebugDescriptor,
                ImmutableArray.Create(
                    new ExternalPdbChecksum(
                        "sha256",
                        testData.DebugDescriptor.PdbChecksums[0].Checksum)));

            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                expected,
                stream,
                out _));
        }

        /// <summary>
        /// Uses an explicitly supplied candidate file even when the CodeView
        /// path is intentionally nonexistent.
        /// </summary>
        [Fact]
        public void ExplicitCandidatePath_DoesNotUseCodeViewPath()
        {
            PortablePdbTestData testData = CreateDefaultTestData(
                "ExplicitCandidateLibrary");
            string codeViewPath = testData.DebugDescriptor.CodeViewPdbReferences[0].Path;
            Assert.False(File.Exists(codeViewPath));
            string candidatePath = CreateTemporaryPdbPath();
            File.WriteAllBytes(candidatePath, testData.PdbImage);

            try
            {
                Assert.True(ExternalPortablePdbDescriptorFactory.TryCreateFromFile(
                    testData.DebugDescriptor,
                    candidatePath,
                    out ExternalPortablePdbDescriptor descriptor));
                Assert.Equal(ReadPortablePdbId(testData.PdbImage), descriptor.Id);
                Assert.False(File.Exists(codeViewPath));
            }
            finally
            {
                File.Delete(candidatePath);
            }
        }

        /// <summary>
        /// Validates identical PDB bytes equally at two different caller-chosen
        /// paths.
        /// </summary>
        [Fact]
        public void SamePdbAtDifferentCandidatePaths_HasSameValidationResult()
        {
            PortablePdbTestData testData = CreateDefaultTestData(
                "PathIndependentLibrary");
            string firstPath = CreateTemporaryPdbPath();
            string secondPath = CreateTemporaryPdbPath();
            File.WriteAllBytes(firstPath, testData.PdbImage);
            File.WriteAllBytes(secondPath, testData.PdbImage);

            try
            {
                Assert.True(ExternalPortablePdbDescriptorFactory.TryCreateFromFile(
                    testData.DebugDescriptor,
                    firstPath,
                    out ExternalPortablePdbDescriptor first));
                Assert.True(ExternalPortablePdbDescriptorFactory.TryCreateFromFile(
                    testData.DebugDescriptor,
                    secondPath,
                    out ExternalPortablePdbDescriptor second));
                Assert.Equal(first.Id, second.Id);
                Assert.Equal(first.ValidationKind, second.ValidationKind);
            }
            finally
            {
                File.Delete(firstPath);
                File.Delete(secondPath);
            }
        }

        /// <summary>
        /// Rejects bytes that are not a Portable PDB.
        /// </summary>
        [Fact]
        public void InvalidPortablePdb_FailsClosed()
        {
            PortablePdbTestData testData = CreateDefaultTestData("InvalidPdbLibrary");
            using MemoryStream stream = new(
                new byte[] { 0x42, 0x53, 0x4a, 0x42 },
                writable: false);

            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Leaves caller-owned streams open after both successful and failed
        /// candidate validation.
        /// </summary>
        [Fact]
        public void CandidateStreams_RemainOpen()
        {
            PortablePdbTestData testData = CreateDefaultTestData("StreamLibrary");
            using MemoryStream validStream = new(testData.PdbImage, writable: false);
            using MemoryStream invalidStream = new(
                new byte[] { 0x00, 0x01 },
                writable: false);

            Assert.True(ExternalPortablePdbDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                validStream,
                out _));
            Assert.False(ExternalPortablePdbDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                invalidStream,
                out _));
            Assert.True(validStream.CanRead);
            Assert.True(invalidStream.CanRead);
        }

        /// <summary>
        /// Preserves two document rows and all independently read metadata
        /// fields in table order.
        /// </summary>
        [Fact]
        public void SourceDocuments_PreserveTableOrderAndMetadata()
        {
            PortablePdbTestData testData = EmitPortablePdb(
                "DocumentLibrary",
                new[]
                {
                    new TestSource(
                        "/_/First.cs",
                        "public sealed class FirstType { }",
                        SourceHashAlgorithm.Sha1,
                        embed: false),
                    new TestSource(
                        "/_/Second.cs",
                        "public sealed class SecondType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                });

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
            using MemoryStream stream = new(testData.PdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(
                    stream,
                    MetadataStreamOptions.LeaveOpen);
            MetadataReader reader = provider.GetMetadataReader();
            DocumentHandle[] handles = reader.Documents.ToArray();
            Assert.Equal(handles.Length, descriptor.Documents.Length);

            for (int index = 0; index < handles.Length; index++)
            {
                System.Reflection.Metadata.Document expected =
                    reader.GetDocument(handles[index]);
                ExternalSourceDocumentDescriptor actual = descriptor.Documents[index];
                Assert.Equal(reader.GetString(expected.Name), actual.Name);
                Assert.Equal(reader.GetGuid(expected.HashAlgorithm), actual.HashAlgorithm);
                Assert.True(reader.GetBlobContent(expected.Hash).SequenceEqual(actual.Hash));
                Assert.Equal(reader.GetGuid(expected.Language), actual.Language);
            }

            Assert.Equal(Sha1DocumentHashAlgorithm, descriptor.Documents[0].HashAlgorithm);
            Assert.Equal(Sha256DocumentHashAlgorithm, descriptor.Documents[1].HashAlgorithm);
        }

        /// <summary>
        /// Parses Source Link once, preserves mapping order, and resolves using
        /// case-insensitive most-specific matching.
        /// </summary>
        [Fact]
        public void SourceLink_PreservesMappingsAndUsesMostSpecificMatch()
        {
            const string sourceLinkJson = """
                {"documents":{
                  "/_/*":"https://example.test/root/*",
                  "/_/sub/*":"https://example.test/sub/*",
                  "/_/sub/Special.cs":"https://example.test/exact/Special.cs"
                }}
                """;
            PortablePdbTestData testData = EmitPortablePdb(
                "SourceLinkLibrary",
                new[]
                {
                    new TestSource(
                        "/_/Root.cs",
                        "public sealed class RootType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                    new TestSource(
                        "/_/sub/Other.cs",
                        "public sealed class OtherType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                    new TestSource(
                        "/_/sub/Special.cs",
                        "public sealed class SpecialType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                },
                sourceLinkJson);

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
            ExternalSourceLinkDescriptor sourceLink = Assert.IsType<ExternalSourceLinkDescriptor>(
                descriptor.SourceLink);
            Assert.Equal(3, sourceLink.Mappings.Length);
            Assert.Equal("/_/*", sourceLink.Mappings[0].DocumentPattern);
            Assert.True(sourceLink.TryResolveDocument(
                "/_/Root.cs",
                out string general));
            Assert.Equal("https://example.test/root/Root.cs", general);
            Assert.True(sourceLink.TryResolveDocument(
                "/_/SUB/Other.cs",
                out string specific));
            Assert.Equal("https://example.test/sub/Other.cs", specific);
            Assert.True(sourceLink.TryResolveDocument(
                "/_/SUB/SPECIAL.CS",
                out string exact));
            Assert.Equal("https://example.test/exact/Special.cs", exact);
        }

        /// <summary>
        /// Supplies malformed Source Link blobs covering required schema and
        /// wildcard validation rules.
        /// </summary>
        /// <returns>The malformed UTF-8 JSON strings.</returns>
        public static IEnumerable<object[]> MalformedSourceLinkJson()
        {
            yield return new object[] { "{" };
            yield return new object[] { "{}" };
            yield return new object[] { "{\"documents\":[]}" };
            yield return new object[] { "{\"documents\":{}}" };
            yield return new object[] { "{\"documents\":{\"/_/*\":1}}" };
            yield return new object[]
            {
                "{\"documents\":{\"/_/*/bad*\":\"https://example.test/*\"}}",
            };
            yield return new object[]
            {
                "{\"documents\":{\"/_/*/bad\":\"https://example.test/*\"}}",
            };
            yield return new object[]
            {
                "{\"documents\":{\"/_/*\":\"https://example.test/no-wildcard\"}}",
            };
            yield return new object[]
            {
                "{\"documents\":{\"/_/Exact.cs\":\"https://example.test/*\"}}",
            };
            yield return new object[]
            {
                "{\"documents\":{\"/_/One.cs\":\"one\",\"/_/one.cs\":\"two\"}}",
            };
            yield return new object[]
            {
                "{\"documents\":{},\"documents\":{\"/_/*\":\"target/*\"}}",
            };
        }

        /// <summary>
        /// Rejects malformed Source Link JSON or mappings without partially
        /// preserving provenance.
        /// </summary>
        /// <param name="json">The malformed Source Link JSON.</param>
        [Theory]
        [MemberData(nameof(MalformedSourceLinkJson))]
        public void MalformedSourceLink_FailsClosed(string json)
        {
            Assert.False(ExternalSourceLinkDescriptor.TryCreate(
                ImmutableArray.CreateRange(Encoding.UTF8.GetBytes(json)),
                out _));
        }

        /// <summary>
        /// Keeps document provenance valid when no Source Link CDI exists.
        /// </summary>
        [Fact]
        public void NoSourceLink_PreservesDocuments()
        {
            PortablePdbTestData testData = CreateDefaultTestData("NoSourceLinkLibrary");

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);

            Assert.Null(descriptor.SourceLink);
            Assert.NotEmpty(descriptor.Documents);
        }

        /// <summary>
        /// Preserves coexisting Source Link and checksum-validated raw and
        /// compressed Embedded Source provenance without retaining source bytes.
        /// </summary>
        [Fact]
        public void EmbeddedSources_RawAndCompressedCanCoexistWithSourceLink()
        {
            string largeSource =
                "public sealed class LargeEmbeddedType { public const string Value = \"" +
                new string('a', 5000) +
                "\"; }";
            const string sourceLinkJson =
                "{\"documents\":{\"/_/*\":\"https://example.test/source/*\"}}";
            PortablePdbTestData testData = EmitPortablePdb(
                "EmbeddedSourceLibrary",
                new[]
                {
                    new TestSource(
                        "/_/Small.cs",
                        "public sealed class SmallType { }",
                        SourceHashAlgorithm.Sha256,
                        embed: true),
                    new TestSource(
                        "/_/Large.cs",
                        largeSource,
                        SourceHashAlgorithm.Sha256,
                        embed: true),
                },
                sourceLinkJson);

            ExternalPortablePdbDescriptor descriptor = ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
            ExternalSourceDocumentDescriptor small = Assert.Single(
                descriptor.Documents.Where(document => document.Name == "/_/Small.cs"));
            ExternalSourceDocumentDescriptor large = Assert.Single(
                descriptor.Documents.Where(document => document.Name == "/_/Large.cs"));
            Assert.True(small.EmbeddedSource.HasValue);
            Assert.False(small.EmbeddedSource.Value.IsCompressed);
            Assert.True(small.EmbeddedSource.Value.IsDocumentChecksumValidated);
            Assert.True(large.EmbeddedSource.HasValue);
            Assert.True(large.EmbeddedSource.Value.IsCompressed);
            Assert.True(large.EmbeddedSource.Value.IsDocumentChecksumValidated);
            Assert.True(large.EmbeddedSource.Value.UncompressedSize >
                small.EmbeddedSource.Value.UncompressedSize);
            Assert.NotNull(descriptor.SourceLink);
        }

        /// <summary>
        /// Supplies all standardized document hash algorithms.
        /// </summary>
        /// <returns>The algorithm GUIDs and concrete hash names.</returns>
        public static IEnumerable<object[]> SupportedDocumentHashAlgorithms()
        {
            yield return new object[] { Sha1DocumentHashAlgorithm, HashAlgorithmName.SHA1 };
            yield return new object[] { Sha256DocumentHashAlgorithm, HashAlgorithmName.SHA256 };
            yield return new object[] { Sha384DocumentHashAlgorithm, HashAlgorithmName.SHA384 };
            yield return new object[] { Sha512DocumentHashAlgorithm, HashAlgorithmName.SHA512 };
        }

        /// <summary>
        /// Validates raw Embedded Source with each standardized document hash
        /// algorithm.
        /// </summary>
        /// <param name="identifier">The standardized algorithm GUID.</param>
        /// <param name="hashAlgorithm">The concrete hash algorithm.</param>
        [Theory]
        [MemberData(nameof(SupportedDocumentHashAlgorithms))]
        public void EmbeddedSource_KnownDocumentHashAlgorithmValidates(
            Guid identifier,
            HashAlgorithmName hashAlgorithm)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes("public sealed class Embedded { }");
            ImmutableArray<byte> blob = CreateRawEmbeddedSource(sourceBytes);
            ImmutableArray<byte> expectedHash = ImmutableArray.CreateRange(
                CalculateHash(sourceBytes, hashAlgorithm));

            Assert.True(ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                blob,
                identifier,
                expectedHash,
                out ExternalEmbeddedSourceProvenance provenance));
            Assert.True(provenance.IsDocumentChecksumValidated);
            Assert.False(provenance.IsCompressed);
            Assert.Equal(sourceBytes.Length, provenance.UncompressedSize);
        }

        /// <summary>
        /// Preserves Embedded Source with an unknown document hash algorithm
        /// without claiming checksum validation.
        /// </summary>
        [Fact]
        public void EmbeddedSource_UnknownDocumentHashPreservesUnvalidatedProvenance()
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes("unknown hash source");

            Assert.True(ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                CreateRawEmbeddedSource(sourceBytes),
                Guid.NewGuid(),
                ImmutableArray.Create((byte)0x01),
                out ExternalEmbeddedSourceProvenance provenance));
            Assert.False(provenance.IsDocumentChecksumValidated);
            Assert.Equal(sourceBytes.Length, provenance.UncompressedSize);
        }

        /// <summary>
        /// Rejects compressed Embedded Source whose actual size differs from
        /// its declared uncompressed size.
        /// </summary>
        [Fact]
        public void EmbeddedSource_CompressedSizeMismatchFailsClosed()
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(new string('x', 1000));
            ImmutableArray<byte> blob = CreateCompressedEmbeddedSource(
                sourceBytes,
                declaredSize: sourceBytes.Length + 1);
            ImmutableArray<byte> expectedHash = ImmutableArray.CreateRange(
                SHA256.HashData(sourceBytes));

            Assert.False(ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                blob,
                Sha256DocumentHashAlgorithm,
                expectedHash,
                out _));
        }

        /// <summary>
        /// Rejects Embedded Source that contradicts a known document checksum.
        /// </summary>
        [Fact]
        public void EmbeddedSource_KnownDocumentHashMismatchFailsClosed()
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes("mismatching source");

            Assert.False(ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                CreateRawEmbeddedSource(sourceBytes),
                Sha256DocumentHashAlgorithm,
                ImmutableArray.CreateRange(SHA256.HashData("other"u8)),
                out _));
        }

        /// <summary>
        /// Creates a one-document Portable PDB test build.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="source">The complete source.</param>
        /// <returns>The PE, PDB, and validated P4A provenance.</returns>
        private static PortablePdbTestData CreateDefaultTestData(
            string assemblyName,
            string source = "public sealed class DefaultType { }")
        {
            return EmitPortablePdb(
                assemblyName,
                new[]
                {
                    new TestSource(
                        "/_/Default.cs",
                        source,
                        SourceHashAlgorithm.Sha256,
                        embed: false),
                });
        }

        /// <summary>
        /// Emits deterministic PE and Portable PDB bytes with optional Source
        /// Link and Embedded Source data.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="sources">The source documents.</param>
        /// <param name="sourceLinkJson">The optional Source Link JSON.</param>
        /// <returns>The emitted bytes and P4A debug provenance.</returns>
        private static PortablePdbTestData EmitPortablePdb(
            string assemblyName,
            IReadOnlyCollection<TestSource> sources,
            string? sourceLinkJson = null)
        {
            List<SyntaxTree> syntaxTrees = new();
            List<EmbeddedText> embeddedTexts = new();

            foreach (TestSource source in sources)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                    source.Text,
                    path: source.Path));

                if (source.Embed)
                {
                    embeddedTexts.Add(EmbeddedText.FromSource(source.Path, source.Text));
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                MetadataReferences.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            string codeViewPath = Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p4b-{Guid.NewGuid():N}",
                "candidate.pdb");
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                pdbFilePath: codeViewPath);
            using MemoryStream peStream = new();
            using MemoryStream pdbStream = new();
            using MemoryStream? sourceLinkStream = sourceLinkJson == null
                ? null
                : new MemoryStream(
                    Encoding.UTF8.GetBytes(sourceLinkJson),
                    writable: false);
            EmitResult result = compilation.Emit(
                peStream,
                pdbStream,
                options: emitOptions,
                sourceLinkStream: sourceLinkStream,
                embeddedTexts: embeddedTexts);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            byte[] peImage = peStream.ToArray();
            byte[] pdbImage = pdbStream.ToArray();
            return new PortablePdbTestData(
                peImage,
                pdbImage,
                CreateP4ADebugDescriptor(peImage));
        }

        /// <summary>
        /// Creates P4A debug provenance for emitted PE bytes through the P3
        /// and P4A production pipelines.
        /// </summary>
        /// <param name="peImage">The PE image.</param>
        /// <returns>The validated P4A descriptor.</returns>
        private static ExternalPeDebugDirectoryDescriptor CreateP4ADebugDescriptor(
            byte[] peImage)
        {
            PortableExecutableReference reference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(peImage));
            CSharpCompilation consumer = CSharpCompilation.Create(
                "Consumer",
                new[] { CSharpSyntaxTree.ParseText("public sealed class ConsumerType { }") },
                MetadataReferences.Default.Append(reference),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol assemblySymbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                consumer,
                assemblySymbol,
                out ExternalAssemblyReferenceDescriptor assemblyDescriptor));
            using MemoryStream stream = new(peImage, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                assemblyDescriptor,
                stream,
                out ExternalPeDebugDirectoryDescriptor debugDescriptor));
            return debugDescriptor;
        }

        /// <summary>
        /// Reads a required P4B descriptor from candidate bytes.
        /// </summary>
        /// <param name="expected">The P4A debug provenance.</param>
        /// <param name="pdbImage">The candidate Portable PDB bytes.</param>
        /// <returns>The validated P4B descriptor.</returns>
        private static ExternalPortablePdbDescriptor ReadRequiredDescriptor(
            ExternalPeDebugDirectoryDescriptor expected,
            byte[] pdbImage)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            Assert.True(ExternalPortablePdbDescriptorFactory.TryCreate(
                expected,
                stream,
                out ExternalPortablePdbDescriptor descriptor));
            return descriptor;
        }

        /// <summary>
        /// Copies P4A provenance while replacing the PDB checksum entries.
        /// </summary>
        /// <param name="source">The source descriptor.</param>
        /// <param name="checksums">The replacement checksums.</param>
        /// <returns>The copied descriptor.</returns>
        private static ExternalPeDebugDirectoryDescriptor CopyDebugDescriptor(
            ExternalPeDebugDirectoryDescriptor source,
            ImmutableArray<ExternalPdbChecksum> checksums)
        {
            return new ExternalPeDebugDirectoryDescriptor(
                source.ManifestModule,
                source.IsDeterministic,
                source.CodeViewPdbReferences,
                source.EmbeddedPortablePdbIds,
                checksums);
        }

        /// <summary>
        /// Reads a Portable PDB content ID independently from production code.
        /// </summary>
        /// <param name="pdbImage">The candidate bytes.</param>
        /// <returns>The PDB content ID.</returns>
        private static BlobContentId ReadPortablePdbId(byte[] pdbImage)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(
                    stream,
                    MetadataStreamOptions.LeaveOpen);
            MetadataReader reader = provider.GetMetadataReader();
            DebugMetadataHeader header = Assert.IsType<DebugMetadataHeader>(
                reader.DebugMetadataHeader);
            return new BlobContentId(header.Id);
        }

        /// <summary>
        /// Reads the metadata-reported PDB ID offset independently.
        /// </summary>
        /// <param name="pdbImage">The candidate bytes.</param>
        /// <returns>The PDB ID start offset.</returns>
        private static int ReadPortablePdbIdStartOffset(byte[] pdbImage)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(
                    stream,
                    MetadataStreamOptions.LeaveOpen);
            MetadataReader reader = provider.GetMetadataReader();
            DebugMetadataHeader header = Assert.IsType<DebugMetadataHeader>(
                reader.DebugMetadataHeader);
            return header.IdStartOffset;
        }

        /// <summary>
        /// Independently calculates a Portable PDB checksum with its ID bytes
        /// zeroed at the metadata-reported offset.
        /// </summary>
        /// <param name="pdbImage">The complete PDB bytes.</param>
        /// <param name="hashAlgorithm">The concrete hash algorithm.</param>
        /// <returns>The calculated checksum.</returns>
        private static byte[] CalculatePortablePdbChecksum(
            byte[] pdbImage,
            HashAlgorithmName hashAlgorithm)
        {
            int idStartOffset = ReadPortablePdbIdStartOffset(pdbImage);
            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(pdbImage, 0, idStartOffset);
            hash.AppendData(new byte[20]);
            int trailingOffset = idStartOffset + 20;
            hash.AppendData(pdbImage, trailingOffset, pdbImage.Length - trailingOffset);
            return hash.GetHashAndReset();
        }

        /// <summary>
        /// Calculates a complete hash for test source bytes.
        /// </summary>
        /// <param name="bytes">The source bytes.</param>
        /// <param name="hashAlgorithm">The hash algorithm.</param>
        /// <returns>The hash bytes.</returns>
        private static byte[] CalculateHash(
            byte[] bytes,
            HashAlgorithmName hashAlgorithm)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(bytes);
            return hash.GetHashAndReset();
        }

        /// <summary>
        /// Creates a raw Embedded Source CDI blob.
        /// </summary>
        /// <param name="sourceBytes">The uncompressed source bytes.</param>
        /// <returns>The complete CDI blob.</returns>
        private static ImmutableArray<byte> CreateRawEmbeddedSource(byte[] sourceBytes)
        {
            byte[] blob = new byte[sizeof(int) + sourceBytes.Length];
            sourceBytes.CopyTo(blob, sizeof(int));
            return ImmutableArray.CreateRange(blob);
        }

        /// <summary>
        /// Creates a Deflate-compressed Embedded Source CDI blob.
        /// </summary>
        /// <param name="sourceBytes">The uncompressed source bytes.</param>
        /// <param name="declaredSize">The declared uncompressed size.</param>
        /// <returns>The complete CDI blob.</returns>
        private static ImmutableArray<byte> CreateCompressedEmbeddedSource(
            byte[] sourceBytes,
            int declaredSize)
        {
            using MemoryStream stream = new();
            stream.Write(new byte[sizeof(int)]);

            using (DeflateStream deflateStream = new(
                       stream,
                       CompressionLevel.Optimal,
                       leaveOpen: true))
            {
                deflateStream.Write(sourceBytes);
            }

            byte[] blob = stream.ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(blob.AsSpan(0, sizeof(int)), declaredSize);
            return ImmutableArray.CreateRange(blob);
        }

        /// <summary>
        /// Finds an exact byte sequence within a test image.
        /// </summary>
        /// <param name="source">The complete source bytes.</param>
        /// <param name="value">The sequence to find.</param>
        /// <returns>The first offset, or -1 when absent.</returns>
        private static int FindSequence(byte[] source, byte[] value)
        {
            for (int offset = 0; offset <= source.Length - value.Length; offset++)
            {
                if (source.AsSpan(offset, value.Length).SequenceEqual(value))
                {
                    return offset;
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates a caller-selected temporary PDB path.
        /// </summary>
        /// <returns>The unique path.</returns>
        private static string CreateTemporaryPdbPath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p4b-{Guid.NewGuid():N}.pdb");
        }

        /// <summary>
        /// Describes one source document used by a Roslyn emit test.
        /// </summary>
        private sealed class TestSource
        {
            /// <summary>
            /// Initializes a test source document.
            /// </summary>
            /// <param name="path">The Portable PDB document name.</param>
            /// <param name="content">The complete source content.</param>
            /// <param name="hashAlgorithm">The source checksum algorithm.</param>
            /// <param name="embed">Whether to embed the source.</param>
            public TestSource(
                string path,
                string content,
                SourceHashAlgorithm hashAlgorithm,
                bool embed)
            {
                Path = path;
                Text = SourceText.From(content, Encoding.UTF8, hashAlgorithm);
                Embed = embed;
            }

            /// <summary>
            /// Gets the PDB document name.
            /// </summary>
            /// <value>The exact source path.</value>
            public string Path { get; }

            /// <summary>
            /// Gets the Roslyn source text.
            /// </summary>
            /// <value>The source text with encoding and checksum algorithm.</value>
            public SourceText Text { get; }

            /// <summary>
            /// Gets whether the source is embedded.
            /// </summary>
            /// <value>Whether Roslyn should emit Embedded Source CDI.</value>
            public bool Embed { get; }
        }

        /// <summary>
        /// Stores emitted PE/PDB bytes and their validated P4A provenance.
        /// </summary>
        private sealed class PortablePdbTestData
        {
            /// <summary>
            /// Initializes Portable PDB test data.
            /// </summary>
            /// <param name="peImage">The emitted PE bytes.</param>
            /// <param name="pdbImage">The emitted Portable PDB bytes.</param>
            /// <param name="debugDescriptor">The validated P4A provenance.</param>
            public PortablePdbTestData(
                byte[] peImage,
                byte[] pdbImage,
                ExternalPeDebugDirectoryDescriptor debugDescriptor)
            {
                PeImage = peImage;
                PdbImage = pdbImage;
                DebugDescriptor = debugDescriptor;
            }

            /// <summary>
            /// Gets the emitted PE bytes.
            /// </summary>
            /// <value>The complete PE image.</value>
            public byte[] PeImage { get; }

            /// <summary>
            /// Gets the emitted Portable PDB bytes.
            /// </summary>
            /// <value>The complete Portable PDB image.</value>
            public byte[] PdbImage { get; }

            /// <summary>
            /// Gets the validated P4A debug provenance.
            /// </summary>
            /// <value>The PE debug-directory descriptor.</value>
            public ExternalPeDebugDirectoryDescriptor DebugDescriptor { get; }
        }
    }
}
