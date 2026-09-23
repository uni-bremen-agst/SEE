using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using P4BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalPortablePdbDescriptorFactoryTests;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests exact checksum-validated materialization of single source documents.
    /// </summary>
    public sealed class ValidatedExternalSourceMaterialFactoryTests
    {
        /// <summary>
        /// Supplies all Portable PDB document algorithms supported by P4B/P5H.
        /// </summary>
        /// <returns>The exact identifiers, algorithms, and digest lengths.</returns>
        public static IEnumerable<object[]> SupportedHashAlgorithms()
        {
            yield return new object[]
            {
                P4BTests.Sha1DocumentHashAlgorithm,
                HashAlgorithmName.SHA1,
                20
            };
            yield return new object[]
            {
                P4BTests.Sha256DocumentHashAlgorithm,
                HashAlgorithmName.SHA256,
                32
            };
            yield return new object[]
            {
                P4BTests.Sha384DocumentHashAlgorithm,
                HashAlgorithmName.SHA384,
                48
            };
            yield return new object[]
            {
                P4BTests.Sha512DocumentHashAlgorithm,
                HashAlgorithmName.SHA512,
                64
            };
        }

        /// <summary>
        /// Materializes a normal explicit stream and preserves exact bytes.
        /// </summary>
        [Fact]
        public void ExplicitStream_MatchingSourceCreatesImmutableMaterial()
        {
            byte[] source = Encoding.UTF8.GetBytes(
                "public static class Example\r\n{\r\n}\r\n");
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            using MemoryStream stream = new(source, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Same(document, material.Document);
            Assert.Equal(source, material.Image);
            Assert.Equal(ExternalSourceMaterialOrigin.ExplicitStream, material.Origin);
            Assert.Null(material.FilePath);
            Assert.Null(material.SourceIdentity);
            Assert.Equal(ExternalSourceMaterialExactness.DirectExact, material.Exactness);
            Assert.Equal(ExternalSourceLineEndingTransformation.None, material.Transformation);
        }

        /// <summary>
        /// Materializes a normal explicit file once and records only its path
        /// as location provenance.
        /// </summary>
        [Fact]
        public void ExplicitFile_MatchingSourceCreatesImmutableMaterial()
        {
            byte[] source = Encoding.UTF8.GetBytes("public static class Example\n{\n}\n");
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "candidate.cs");
            File.WriteAllBytes(path, source);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    path,
                    out ValidatedExternalSourceMaterial material));
                Assert.Equal(source, material.Image);
                Assert.Equal(ExternalSourceMaterialOrigin.ExplicitFile, material.Origin);
                Assert.Equal(path, material.FilePath);
                Assert.Equal(path, material.SourceIdentity);
                Assert.Equal(ExternalSourceMaterialExactness.DirectExact, material.Exactness);
                Assert.Equal(ExternalSourceLineEndingTransformation.None, material.Transformation);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Reads a forward-only stream without seeking or rereading bytes.
        /// </summary>
        [Fact]
        public void NonSeekableStream_IsReadForwardOnceWithoutSeeking()
        {
            byte[] source = Encoding.UTF8.GetBytes("public sealed class ForwardOnly { }");
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            using CountingNonSeekableStream stream = new(source);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source, material.Image);
            Assert.Equal(source.Length, stream.BytesRead);
            Assert.Equal(0, stream.SeekCalls);
        }

        /// <summary>
        /// Materializes only bytes after the caller-selected current position.
        /// </summary>
        [Fact]
        public void CurrentStreamPosition_ExcludesPrefixFromHashAndMaterial()
        {
            byte[] prefix = "ignored-prefix"u8.ToArray();
            byte[] source = "public sealed class Positioned { }"u8.ToArray();
            byte[] combined = prefix.Concat(source).ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            using MemoryStream stream = new(combined, writable: false)
            {
                Position = prefix.Length
            };

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source, material.Image);
            Assert.Equal(combined.Length, stream.Position);
        }

        /// <summary>
        /// Leaves caller-owned streams open after success and checksum failure.
        /// </summary>
        /// <param name="matches">Whether the candidate matches.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CallerStream_RemainsOpen(bool matches)
        {
            byte[] expected = "expected source"u8.ToArray();
            byte[] candidate = matches ? expected : "different source"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            using MemoryStream stream = new(candidate, writable: false);

            bool result = ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out _);

            Assert.Equal(matches, result);
            Assert.True(stream.CanRead);
        }

        /// <summary>
        /// Rejects a candidate after any byte changes.
        /// </summary>
        [Fact]
        public void ChecksumMismatch_FailsWithoutMaterial()
        {
            byte[] expected = "public sealed class Expected { }"u8.ToArray();
            byte[] candidate = (byte[])expected.Clone();
            candidate[^1] ^= 1;
            using MemoryStream stream = new(candidate, writable: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                CreateDocument(expected),
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Null(material);
        }

        /// <summary>
        /// Rejects contradictory direct/reconstructed provenance before a
        /// checksum-valid material can be created.
        /// </summary>
        [Fact]
        public void ReconstructionProvenance_InconsistentPairFailsClosed()
        {
            byte[] source = "class C { }"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            ImmutableArray<byte> image = ImmutableArray.Create(source);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromAcquiredImage(
                document,
                image,
                ExternalSourceMaterialOrigin.SourceLink,
                filePath: null,
                "https://public.test/Value.cs",
                ExternalSourceMaterialExactness.DirectExact,
                ExternalSourceLineEndingTransformation.LfToCrlf,
                out _));
            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromAcquiredImage(
                document,
                image,
                ExternalSourceMaterialOrigin.SourceLink,
                filePath: null,
                "https://public.test/Value.cs",
                ExternalSourceMaterialExactness.ReconstructedExact,
                ExternalSourceLineEndingTransformation.None,
                out _));
        }

        /// <summary>
        /// Rejects same-length bytes instead of treating length as identity.
        /// </summary>
        [Fact]
        public void SameLengthDifferentBytes_FailsClosed()
        {
            byte[] expected = "source-A"u8.ToArray();
            byte[] candidate = "source-B"u8.ToArray();
            Assert.Equal(expected.Length, candidate.Length);
            using MemoryStream stream = new(candidate, writable: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                CreateDocument(expected),
                stream,
                out _));
        }

        /// <summary>
        /// Rejects SHA-256 bytes declared as SHA-1 without guessing by length.
        /// </summary>
        [Fact]
        public void WrongHashAlgorithm_FailsWithoutAlgorithmHeuristic()
        {
            byte[] source = "wrong algorithm"u8.ToArray();
            ExternalSourceDocumentDescriptor document = new(
                "WrongAlgorithm.cs",
                P4BTests.Sha1DocumentHashAlgorithm,
                ImmutableArray.CreateRange(SHA256.HashData(source)),
                Guid.NewGuid(),
                embeddedSource: null);
            using MemoryStream stream = new(source, writable: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects an unknown document hash algorithm.
        /// </summary>
        [Fact]
        public void UnknownHashAlgorithm_FailsClosed()
        {
            byte[] source = "unknown algorithm"u8.ToArray();
            ExternalSourceDocumentDescriptor document = new(
                "UnknownAlgorithm.cs",
                Guid.NewGuid(),
                ImmutableArray.Create((byte)1),
                Guid.NewGuid(),
                embeddedSource: null);
            using MemoryStream stream = new(source, writable: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects every known algorithm when its expected digest length is
        /// malformed.
        /// </summary>
        /// <param name="identifier">The known algorithm identifier.</param>
        /// <param name="hashAlgorithm">The concrete test algorithm.</param>
        /// <param name="hashLength">The required digest length.</param>
        [Theory]
        [MemberData(nameof(SupportedHashAlgorithms))]
        public void MalformedHashLength_FailsClosed(
            Guid identifier,
            HashAlgorithmName hashAlgorithm,
            int hashLength)
        {
            byte[] source = "malformed digest"u8.ToArray();
            byte[] validHash = CalculateHash(source, hashAlgorithm);
            Assert.Equal(hashLength, validHash.Length);
            ExternalSourceDocumentDescriptor document = new(
                "MalformedHash.cs",
                identifier,
                ImmutableArray.CreateRange(validHash[..^1]),
                Guid.NewGuid(),
                embeddedSource: null);
            using MemoryStream stream = new(source, writable: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out _));
        }

        /// <summary>
        /// Accepts a zero-byte source when its checksum is the true empty hash.
        /// </summary>
        [Fact]
        public void EmptySource_WithMatchingHashSucceeds()
        {
            byte[] source = Array.Empty<byte>();
            using MemoryStream stream = new(source, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                CreateDocument(source),
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Empty(material.Image);
        }

        /// <summary>
        /// Preserves BOM and mixed newline bytes without normalization.
        /// </summary>
        [Fact]
        public void BomAndNewlines_ArePreservedByteForByte()
        {
            byte[] source =
            [
                0xef, 0xbb, 0xbf,
                .. Encoding.UTF8.GetBytes("first\r\nsecond\nthird\r\n")
            ];
            using MemoryStream stream = new(source, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                CreateDocument(source),
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source, material.Image);
        }

        /// <summary>
        /// Validates a renamed candidate independently of the document name.
        /// </summary>
        [Fact]
        public void RenamedCandidate_UsesChecksumRatherThanName()
        {
            byte[] source = "public sealed class Renamed { }"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(
                source,
                @"C:\original\Example.cs");
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "renamed.txt");
            File.WriteAllBytes(path, source);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    path,
                    out ValidatedExternalSourceMaterial material));
                Assert.Equal(path, material.FilePath);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Validates identical source at two paths while preserving each path.
        /// </summary>
        [Fact]
        public void SameBytesAtDifferentPaths_PreserveDistinctLocations()
        {
            byte[] source = "public sealed class SameBytes { }"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(source);
            string firstDirectory = P5BTests.CreateTempDirectory();
            string secondDirectory = P5BTests.CreateTempDirectory();
            string firstPath = Path.Combine(firstDirectory, "first.cs");
            string secondPath = Path.Combine(secondDirectory, "second.txt");
            File.WriteAllBytes(firstPath, source);
            File.WriteAllBytes(secondPath, source);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    firstPath,
                    out ValidatedExternalSourceMaterial first));
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    secondPath,
                    out ValidatedExternalSourceMaterial second));
                Assert.Equal(first.Image, second.Image);
                Assert.Equal(firstPath, first.FilePath);
                Assert.Equal(secondPath, second.FilePath);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(firstDirectory);
                P5BTests.DeleteDirectoryIfExists(secondDirectory);
            }
        }

        /// <summary>
        /// Ignores an existing wrong file named by the document and validates
        /// only the explicitly supplied candidate.
        /// </summary>
        [Fact]
        public void DocumentName_IsNeverOpenedAsCandidatePath()
        {
            byte[] source = "public sealed class ExplicitCandidate { }"u8.ToArray();
            string directory = P5BTests.CreateTempDirectory();
            string documentPath = Path.Combine(directory, "document-name.cs");
            string candidatePath = Path.Combine(directory, "explicit.cs");
            File.WriteAllBytes(documentPath, "wrong bytes"u8.ToArray());
            File.WriteAllBytes(candidatePath, source);
            ExternalSourceDocumentDescriptor document = CreateDocument(source, documentPath);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    candidatePath,
                    out ValidatedExternalSourceMaterial material));
                Assert.Equal(source, material.Image);
                Assert.Equal(candidatePath, material.FilePath);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps the validated snapshot after its candidate file is deleted.
        /// </summary>
        [Fact]
        public void FileDeletedAfterMaterialization_SnapshotRemainsUsable()
        {
            byte[] source = "public sealed class DeletedLater { }"u8.ToArray();
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "source.cs");
            File.WriteAllBytes(path, source);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    CreateDocument(source),
                    path,
                    out ValidatedExternalSourceMaterial material));
                File.Delete(path);

                Assert.Equal(source, material.Image);
                Assert.False(File.Exists(path));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps source A after the candidate path is replaced with source B.
        /// </summary>
        [Fact]
        public void FileReplacedAfterMaterialization_SnapshotRemainsOriginal()
        {
            byte[] original = "public sealed class Original { }"u8.ToArray();
            byte[] replacement = "public sealed class Replaced { }"u8.ToArray();
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "source.cs");
            File.WriteAllBytes(path, original);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    CreateDocument(original),
                    path,
                    out ValidatedExternalSourceMaterial material));
                File.WriteAllBytes(path, replacement);

                Assert.Equal(original, material.Image);
                Assert.NotEqual(File.ReadAllBytes(path), material.Image);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes checksum-validated retained embedded bytes without I/O.
        /// </summary>
        [Fact]
        public void EmbeddedSource_ValidatedBytesCreateMaterial()
        {
            byte[] source = "public sealed class Embedded { }"u8.ToArray();
            ExternalEmbeddedSourceProvenance embedded = CreateEmbeddedProvenance(
                source,
                isChecksumValidated: true);
            ExternalSourceDocumentDescriptor document = CreateDocument(
                source,
                embeddedSource: embedded);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                document,
                out ValidatedExternalSourceMaterial material));
            Assert.Same(document, material.Document);
            Assert.Equal(source, material.Image);
            Assert.Equal(ExternalSourceMaterialOrigin.Embedded, material.Origin);
            Assert.Null(material.FilePath);
        }

        /// <summary>
        /// Rejects a document without Embedded Source provenance.
        /// </summary>
        [Fact]
        public void NoEmbeddedSource_FailsClosed()
        {
            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                CreateDocument("not embedded"u8.ToArray()),
                out _));
        }

        /// <summary>
        /// Rejects retained embedded bytes whose checksum was not validated.
        /// </summary>
        [Fact]
        public void UnvalidatedEmbeddedSource_FailsClosed()
        {
            byte[] source = "unvalidated embedded"u8.ToArray();
            ExternalEmbeddedSourceProvenance embedded = CreateEmbeddedProvenance(
                source,
                isChecksumValidated: false);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                CreateDocument(source, embeddedSource: embedded),
                out _));
        }

        /// <summary>
        /// Preserves unknown-algorithm Embedded Source in P4B but rejects it
        /// as unverifiable material in P5H.
        /// </summary>
        [Fact]
        public void UnknownHashEmbeddedSource_P4BPreservesAndP5HFailsClosed()
        {
            byte[] source = "unknown embedded hash"u8.ToArray();
            byte[] blob = new byte[sizeof(int) + source.Length];
            source.CopyTo(blob, sizeof(int));
            Guid unknownHashAlgorithm = Guid.NewGuid();

            Assert.True(ExternalEmbeddedSourceProvenanceFactory.TryCreate(
                ImmutableArray.CreateRange(blob),
                unknownHashAlgorithm,
                ImmutableArray.Create((byte)1),
                out ExternalEmbeddedSourceProvenance embedded));
            Assert.False(embedded.IsDocumentChecksumValidated);
            Assert.Equal(source, embedded.Image);
            ExternalSourceDocumentDescriptor document = new(
                "UnknownEmbeddedHash.cs",
                unknownHashAlgorithm,
                ImmutableArray.Create((byte)1),
                Guid.NewGuid(),
                embedded);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                document,
                out _));
        }

        /// <summary>
        /// Revalidates retained embedded bytes instead of trusting a stale flag.
        /// </summary>
        [Fact]
        public void EmbeddedSource_WithContradictoryDocumentHashFailsClosed()
        {
            byte[] embeddedBytes = "embedded A"u8.ToArray();
            byte[] expectedBytes = "embedded B"u8.ToArray();
            Assert.Equal(embeddedBytes.Length, expectedBytes.Length);
            ExternalEmbeddedSourceProvenance embedded = CreateEmbeddedProvenance(
                embeddedBytes,
                isChecksumValidated: true);

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                CreateDocument(expectedBytes, embeddedSource: embedded),
                out _));
        }

        /// <summary>
        /// Materializes a real raw Embedded Source through P4A/P4B and P5H.
        /// </summary>
        [Fact]
        public void RealPortablePdb_RawEmbeddedSourceRoundTripsThroughP4BAndP5H()
        {
            P4BTests.TestSource source = new(
                "/_/RawEmbedded.cs",
                "public sealed class RawEmbedded { }",
                SourceHashAlgorithm.Sha256,
                embed: true);
            ExternalPortablePdbDescriptor pdb = EmitAndReadP4B(
                "P5HRawEmbedded",
                source);
            ExternalSourceDocumentDescriptor document = Assert.Single(pdb.Documents);
            Assert.True(document.EmbeddedSource.HasValue);
            Assert.False(document.EmbeddedSource.Value.IsCompressed);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                document,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source.Image, material.Image);
        }

        /// <summary>
        /// Materializes a real Deflate Embedded Source through P4A/P4B and P5H.
        /// </summary>
        [Fact]
        public void RealPortablePdb_CompressedEmbeddedSourceRoundTripsThroughP4BAndP5H()
        {
            string content =
                "public sealed class Compressed { public const string Value = \"" +
                new string('x', 6000) +
                "\"; }";
            P4BTests.TestSource source = new(
                "/_/CompressedEmbedded.cs",
                content,
                SourceHashAlgorithm.Sha256,
                embed: true);
            ExternalPortablePdbDescriptor pdb = EmitAndReadP4B(
                "P5HCompressedEmbedded",
                source);
            ExternalSourceDocumentDescriptor document = Assert.Single(pdb.Documents);
            Assert.True(document.EmbeddedSource.HasValue);
            Assert.True(document.EmbeddedSource.Value.IsCompressed);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                document,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source.Image, material.Image);
        }

        /// <summary>
        /// Validates an explicit file against a real P4B document checksum.
        /// </summary>
        [Fact]
        public void RealPortablePdb_ExplicitFileRoundTripsThroughP4BAndP5H()
        {
            P4BTests.TestSource source = new(
                "/_/Original.cs",
                "public sealed class ExplicitFile { }",
                SourceHashAlgorithm.Sha256,
                embed: false);
            ExternalPortablePdbDescriptor pdb = EmitAndReadP4B(
                "P5HExplicitFile",
                source);
            ExternalSourceDocumentDescriptor document = Assert.Single(pdb.Documents);
            Assert.Null(document.EmbeddedSource);
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "renamed-source.txt");
            File.WriteAllBytes(path, source.Image);

            try
            {
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    path,
                    out ValidatedExternalSourceMaterial material));
                Assert.Equal(source.Image, material.Image);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Validates an explicit candidate when Source Link is present without
        /// resolving or accessing its target.
        /// </summary>
        [Fact]
        public void SourceLinkPresent_ExplicitCandidateUsesOnlyDocumentChecksum()
        {
            P4BTests.TestSource source = new(
                "/_/SourceLink.cs",
                "public sealed class SourceLinkCandidate { }",
                SourceHashAlgorithm.Sha256,
                embed: false);
            const string sourceLinkJson =
                "{\"documents\":{\"/_/*\":\"https://network.invalid/source/*\"}}";
            P4BTests.PortablePdbTestData testData = P4BTests.EmitPortablePdb(
                "P5HSourceLink",
                new[] { source },
                sourceLinkJson);
            ExternalPortablePdbDescriptor pdb = P4BTests.ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
            Assert.NotNull(pdb.SourceLink);
            using MemoryStream stream = new(source.Image, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                Assert.Single(pdb.Documents),
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(source.Image, material.Image);
        }

        /// <summary>
        /// Validates matching bytes and rejects changed bytes for every shared
        /// supported document hash algorithm.
        /// </summary>
        /// <param name="identifier">The known algorithm identifier.</param>
        /// <param name="hashAlgorithm">The concrete test algorithm.</param>
        /// <param name="hashLength">The required digest length.</param>
        [Theory]
        [MemberData(nameof(SupportedHashAlgorithms))]
        public void EverySupportedHashAlgorithm_MatchesAndRejectsChange(
            Guid identifier,
            HashAlgorithmName hashAlgorithm,
            int hashLength)
        {
            byte[] source = Encoding.UTF8.GetBytes("hash algorithm " + hashAlgorithm.Name);
            byte[] hash = CalculateHash(source, hashAlgorithm);
            Assert.Equal(hashLength, hash.Length);
            ExternalSourceDocumentDescriptor document = new(
                "HashAlgorithm.cs",
                identifier,
                ImmutableArray.CreateRange(hash),
                Guid.NewGuid(),
                embeddedSource: null);
            using MemoryStream valid = new(source, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                valid,
                out _));

            byte[] changed = (byte[])source.Clone();
            changed[0] ^= 1;
            using MemoryStream invalid = new(changed, writable: false);
            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                invalid,
                out _));
        }

        /// <summary>
        /// Keeps materialization language-neutral for an unknown language GUID.
        /// </summary>
        [Fact]
        public void UnknownLanguageGuid_DoesNotPreventByteValidation()
        {
            byte[] source = "language-neutral bytes"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(
                source,
                language: Guid.NewGuid());
            using MemoryStream stream = new(source, writable: false);

            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out _));
        }

        /// <summary>
        /// Rejects a non-readable stream without requiring seek support.
        /// </summary>
        [Fact]
        public void NonReadableStream_FailsClosed()
        {
            byte[] source = "unreadable"u8.ToArray();
            using UnreadableStream stream = new();

            Assert.False(ValidatedExternalSourceMaterialFactory.TryCreate(
                CreateDocument(source),
                stream,
                out _));
        }

        /// <summary>
        /// Applies argument-null conventions to every public factory input.
        /// </summary>
        [Fact]
        public void NullInputs_ThrowArgumentNullException()
        {
            ExternalSourceDocumentDescriptor document = CreateDocument("source"u8.ToArray());
            using MemoryStream stream = new();

            Assert.Throws<ArgumentNullException>(
                () => ValidatedExternalSourceMaterialFactory.TryCreate(
                    null!,
                    stream,
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ValidatedExternalSourceMaterialFactory.TryCreate(
                    document,
                    null!,
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    null!,
                    "candidate.cs",
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    null!,
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                    null!,
                    out _));
        }

        /// <summary>
        /// Treats expected candidate-file failures as unsuccessful validation.
        /// </summary>
        [Fact]
        public void MissingOrDirectoryCandidate_FailsClosed()
        {
            ExternalSourceDocumentDescriptor document = CreateDocument("source"u8.ToArray());
            string directory = P5BTests.CreateTempDirectory();
            string missing = Path.Combine(directory, "missing.cs");

            try
            {
                Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    missing,
                    out _));
                Assert.False(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    directory,
                    out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Creates SHA-256 document provenance for exact source bytes.
        /// </summary>
        private static ExternalSourceDocumentDescriptor CreateDocument(
            byte[] source,
            string name = "/_/Document.cs",
            ExternalEmbeddedSourceProvenance? embeddedSource = null,
            Guid? language = null)
        {
            return new ExternalSourceDocumentDescriptor(
                name,
                P4BTests.Sha256DocumentHashAlgorithm,
                ImmutableArray.CreateRange(SHA256.HashData(source)),
                language ?? Guid.NewGuid(),
                embeddedSource);
        }

        /// <summary>
        /// Creates retained embedded provenance for a controlled snapshot.
        /// </summary>
        private static ExternalEmbeddedSourceProvenance CreateEmbeddedProvenance(
            byte[] source,
            bool isChecksumValidated)
        {
            Assert.True(ExternalEmbeddedSourceProvenance.TryCreate(
                ImmutableArray.CreateRange(source),
                isCompressed: false,
                isChecksumValidated,
                out ExternalEmbeddedSourceProvenance provenance));
            return provenance;
        }

        /// <summary>
        /// Emits one real P4B fixture through the existing helper and reads it.
        /// </summary>
        private static ExternalPortablePdbDescriptor EmitAndReadP4B(
            string assemblyName,
            P4BTests.TestSource source)
        {
            P4BTests.PortablePdbTestData testData = P4BTests.EmitPortablePdb(
                assemblyName,
                new[] { source });
            return P4BTests.ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
        }

        /// <summary>
        /// Calculates a source digest with the selected test algorithm.
        /// </summary>
        private static byte[] CalculateHash(
            byte[] source,
            HashAlgorithmName hashAlgorithm)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);
            hash.AppendData(source);
            return hash.GetHashAndReset();
        }

        /// <summary>
        /// Counts forward reads and rejects every seek operation.
        /// </summary>
        private sealed class CountingNonSeekableStream : Stream
        {
            /// <summary>
            /// The underlying readable memory stream.
            /// </summary>
            private readonly MemoryStream stream;

            /// <summary>
            /// Initializes a forward-only stream over controlled bytes.
            /// </summary>
            public CountingNonSeekableStream(byte[] image)
            {
                stream = new MemoryStream(image, writable: false);
            }

            /// <inheritdoc/>
            public override bool CanRead => true;

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
            /// Gets the total source bytes returned to the consumer.
            /// </summary>
            public int BytesRead { get; private set; }

            /// <summary>
            /// Gets the number of attempted seek operations.
            /// </summary>
            public int SeekCalls { get; private set; }

            /// <inheritdoc/>
            public override void Flush()
            {
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = stream.Read(buffer, offset, count);
                BytesRead += bytesRead;
                return bytesRead;
            }

            /// <inheritdoc/>
            public override int Read(Span<byte> buffer)
            {
                int bytesRead = stream.Read(buffer);
                BytesRead += bytesRead;
                return bytesRead;
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                SeekCalls++;
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

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Represents a stream that explicitly cannot be read.
        /// </summary>
        private sealed class UnreadableStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead => false;

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

            /// <inheritdoc/>
            public override void Flush()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
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
        }
    }
}
