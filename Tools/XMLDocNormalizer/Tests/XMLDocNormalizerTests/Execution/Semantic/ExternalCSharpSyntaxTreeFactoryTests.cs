using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using P4BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalPortablePdbDescriptorFactoryTests;
using P5ATests = XMLDocNormalizerTests.Execution.Semantic.ExternalCompilationProvenanceDescriptorFactoryTests;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests source-text and C# syntax-tree reconstruction from P5G and P5H.
    /// </summary>
    public sealed class ExternalCSharpSyntaxTreeFactoryTests
    {
        /// <summary>
        /// Supplies every Unicode byte-order mark supported by P5I.
        /// </summary>
        /// <returns>The encoding and expected runtime web name.</returns>
        public static IEnumerable<object[]> BomEncodings()
        {
            yield return new object[] { new UTF8Encoding(true), "utf-8" };
            yield return new object[] { new UnicodeEncoding(false, true), "utf-16" };
            yield return new object[] { new UnicodeEncoding(true, true), "utf-16BE" };
        }

        /// <summary>
        /// Rejects UTF-32 BOMs because Roslyn 5.0's original-byte API cannot
        /// decode them without losing exact BOM semantics.
        /// </summary>
        /// <param name="bigEndian">Whether to use big-endian UTF-32.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Utf32Bom_FailsClosedWhenRoslynCannotDecodeItExactly(bool bigEndian)
        {
            Encoding encoding = new UTF32Encoding(bigEndian, byteOrderMark: true);
            byte[] source = EncodeWithPreamble("class Utf32Source { }", encoding);
            ValidatedExternalSourceMaterial material = CreateMaterial(source);

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                CreateConfiguration(),
                out _));
        }

        /// <summary>
        /// Decodes BOM-less UTF-8 with the explicit Roslyn default policy.
        /// </summary>
        [Fact]
        public void Utf8WithoutBom_UsesUtf8AndExactP5GParseOptions()
        {
            const string content = "public sealed class Café { }\r\n";
            byte[] source = new UTF8Encoding(false).GetBytes(content);
            ValidatedExternalSourceMaterial material = CreateMaterial(source);
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();

            ExternalCSharpSyntaxTree result = CreateTree(material, configuration);

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal("utf-8", result.Text.Encoding?.WebName);
            Assert.Same(configuration.ParseOptions, result.Tree.Options);
            Assert.Same(result.Text, result.Tree.GetText());
            Assert.Equal(material.Document.Name, result.Tree.FilePath);
        }

        /// <summary>
        /// Lets each recognized BOM override conflicting encoding provenance
        /// while retaining the original-byte checksum.
        /// </summary>
        /// <param name="encoding">The BOM-bearing Unicode encoding.</param>
        /// <param name="expectedWebName">The expected actual encoding.</param>
        [Theory]
        [MemberData(nameof(BomEncodings))]
        public void UnicodeBom_OverridesConfigurationAndPreservesChecksum(
            Encoding encoding,
            string expectedWebName)
        {
            const string content = "public sealed class EncodedΩ { }\n";
            byte[] source = EncodeWithPreamble(content, encoding);
            ValidatedExternalSourceMaterial material = CreateMaterial(source);
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                defaultEncoding: "iso-8859-1",
                fallbackEncoding: "windows-1252");

            ExternalCSharpSyntaxTree result = CreateTree(material, configuration);

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal(expectedWebName, result.Text.Encoding?.WebName);
            Assert.Equal(material.Document.Hash, result.Text.GetChecksum());
            Assert.NotEqual('\ufeff', result.Text[0]);
        }

        /// <summary>
        /// Uses the recorded default encoding for BOM-less source.
        /// </summary>
        [Fact]
        public void BomlessSource_DefaultEncodingIsUsed()
        {
            Encoding windows1252 = GetCodePageEncoding("windows-1252");
            const string content = "public sealed class Euro { string Value = \"€\"; }";
            byte[] source = windows1252.GetBytes(content);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration(defaultEncoding: windows1252.WebName));

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal(windows1252.WebName, result.Text.Encoding?.WebName);
        }

        /// <summary>
        /// Uses the fallback encoding when no default encoding is recorded.
        /// </summary>
        [Fact]
        public void BomlessSource_FallbackEncodingIsUsedWhenDefaultIsAbsent()
        {
            const string content = "public sealed class Latin { string Value = \"é\"; }";
            byte[] source = Encoding.Latin1.GetBytes(content);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration(fallbackEncoding: Encoding.Latin1.WebName));

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal(Encoding.Latin1.WebName, result.Text.Encoding?.WebName);
        }

        /// <summary>
        /// Gives the default encoding strict priority when both keys exist.
        /// </summary>
        [Fact]
        public void BomlessSource_DefaultEncodingWinsOverFallbackEncoding()
        {
            Encoding windows1252 = GetCodePageEncoding("windows-1252");
            byte[] source = [0x2f, 0x2f, 0x20, 0x80];

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration(
                    defaultEncoding: windows1252.WebName,
                    fallbackEncoding: Encoding.Latin1.WebName));

            Assert.Equal("// €", result.Text.ToString());
            Assert.Equal(windows1252.WebName, result.Text.Encoding?.WebName);
        }

        /// <summary>
        /// Uses UTF-8 without BOM when neither encoding key exists.
        /// </summary>
        [Fact]
        public void BomlessSource_WithoutEncodingKeysUsesUtf8()
        {
            const string content = "// π";
            byte[] source = new UTF8Encoding(false).GetBytes(content);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration());

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal("utf-8", result.Text.Encoding?.WebName);
        }

        /// <summary>
        /// Does not resolve an unusable web name when a BOM determines the
        /// effective encoding.
        /// </summary>
        [Fact]
        public void UnknownEncoding_WithBomStillSucceeds()
        {
            const string content = "public sealed class BomWins { }";
            byte[] source = EncodeWithPreamble(content, Encoding.Unicode);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration(defaultEncoding: "not-a-real-encoding"));

            Assert.Equal(content, result.Text.ToString());
            Assert.Equal(Encoding.Unicode.WebName, result.Text.Encoding?.WebName);
        }

        /// <summary>
        /// Fails closed when the required BOM-less default or fallback
        /// encoding is unavailable.
        /// </summary>
        /// <param name="isDefault">
        /// Whether the unknown web name is the default rather than fallback.
        /// </param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnknownRequiredEncoding_WithoutBomFailsClosed(bool isDefault)
        {
            ValidatedExternalSourceMaterial material = CreateMaterial("source"u8.ToArray());
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                defaultEncoding: isDefault ? "not-a-real-encoding" : null,
                fallbackEncoding: isDefault ? null : "not-a-real-encoding");

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                configuration,
                out ExternalCSharpSyntaxTree result));
            Assert.Null(result);
        }

        /// <summary>
        /// Does not try fallback provenance after an unusable default encoding.
        /// </summary>
        [Fact]
        public void UnknownDefaultEncoding_DoesNotTryFallbackEncoding()
        {
            ValidatedExternalSourceMaterial material = CreateMaterial("source"u8.ToArray());

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                CreateConfiguration(
                    defaultEncoding: "not-a-real-encoding",
                    fallbackEncoding: "utf-8"),
                out _));
        }

        /// <summary>
        /// Treats a syntactically invalid encoding name as unavailable rather
        /// than leaking an encoding-registry exception.
        /// </summary>
        [Fact]
        public void InvalidEncodingName_WithoutBomFailsClosed()
        {
            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                CreateMaterial("source"u8.ToArray()),
                CreateConfiguration(defaultEncoding: "\0"),
                out _));
        }

        /// <summary>
        /// Resolves a historical code page directly without global provider
        /// registration.
        /// </summary>
        [Fact]
        public void HistoricalCodePage_IsResolvedDirectly()
        {
            Encoding windows1252 = GetCodePageEncoding("windows-1252");
            const string content = "// smart quote “value”";
            byte[] source = windows1252.GetBytes(content);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration(defaultEncoding: windows1252.WebName));

            Assert.Equal(content, result.Text.ToString());
        }

        /// <summary>
        /// Maps PDB SHA-1 and SHA-256 to the exact Roslyn checksum algorithm.
        /// </summary>
        /// <param name="identifier">The Portable PDB hash identifier.</param>
        /// <param name="expected">The exact Roslyn algorithm.</param>
        [Theory]
        [InlineData("ff1816ec-aa5e-4d10-87f7-6f4963833460", SourceHashAlgorithm.Sha1)]
        [InlineData("8829d00f-11b8-4213-878b-770e8597ac16", SourceHashAlgorithm.Sha256)]
        public void RepresentableChecksumAlgorithm_IsPreserved(
            string identifier,
            SourceHashAlgorithm expected)
        {
            byte[] source = "checksum source"u8.ToArray();
            ValidatedExternalSourceMaterial material = CreateMaterial(
                source,
                new Guid(identifier));

            ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

            Assert.Equal(expected, result.Text.ChecksumAlgorithm);
            Assert.Equal(material.Document.Hash, result.Text.GetChecksum());
        }

        /// <summary>
        /// Keeps SHA-384 and SHA-512 valid in P5H but unsupported in P5I.
        /// </summary>
        /// <param name="identifier">The unrepresentable PDB algorithm.</param>
        [Theory]
        [InlineData("d99cfeb1-8c43-444a-8a6c-b61269d2a0bf")]
        [InlineData("ef2d1afc-6550-46d6-b14b-d70afe9a5566")]
        public void UnrepresentableChecksumAlgorithm_FailsWithoutApproximation(
            string identifier)
        {
            ValidatedExternalSourceMaterial material = CreateMaterial(
                "unsupported checksum"u8.ToArray(),
                new Guid(identifier));

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                CreateConfiguration(),
                out _));
        }

        /// <summary>
        /// Confirms the centralized C# language identifier through a real
        /// Roslyn Portable PDB emit.
        /// </summary>
        [Fact]
        public void RealRoslynEmit_UsesCentralCSharpLanguageIdentifier()
        {
            P4BTests.TestSource source = new(
                "/_/Language.cs",
                "public sealed class LanguageType { }",
                SourceHashAlgorithm.Sha256,
                embed: false);
            ExternalPortablePdbDescriptor pdb = EmitAndReadP4B(
                "P5ICSharpLanguage",
                source);

            Assert.Equal(
                ExternalSourceDocumentLanguageIdentifiers.CSharp,
                Assert.Single(pdb.Documents).Language);
        }

        /// <summary>
        /// Rejects otherwise valid material for a non-C# document.
        /// </summary>
        [Fact]
        public void NonCSharpDocument_FailsBeforeTextCreation()
        {
            ValidatedExternalSourceMaterial material = CreateMaterial(
                "valid bytes"u8.ToArray(),
                language: Guid.NewGuid());

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                CreateConfiguration(defaultEncoding: "not-a-real-encoding"),
                out _));
        }

        /// <summary>
        /// Uses the exact document name as opaque syntax-tree path provenance.
        /// </summary>
        [Fact]
        public void DocumentName_IsPreservedExactlyAsTreePath()
        {
            const string documentName = @"C:\mapped\..\Source.cs";
            ValidatedExternalSourceMaterial material = CreateMaterial(
                "class PathType { }"u8.ToArray(),
                name: documentName);

            ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

            Assert.Equal(documentName, result.Tree.FilePath);
        }

        /// <summary>
        /// Ignores candidate path provenance when selecting the tree path.
        /// </summary>
        [Fact]
        public void CandidateFilePath_IsNotUsedAsTreePath()
        {
            byte[] source = "class CandidatePath { }"u8.ToArray();
            const string documentName = @"C:\build\Source.cs";
            string directory = P5BTests.CreateTempDirectory();
            string candidatePath = Path.Combine(directory, "renamed.txt");
            File.WriteAllBytes(candidatePath, source);

            try
            {
                ExternalSourceDocumentDescriptor document = CreateDocument(
                    source,
                    P4BTests.Sha256DocumentHashAlgorithm,
                    ExternalSourceDocumentLanguageIdentifiers.CSharp,
                    documentName);
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    candidatePath,
                    out ValidatedExternalSourceMaterial material));

                ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

                Assert.Equal(candidatePath, material.FilePath);
                Assert.Equal(documentName, result.Tree.FilePath);
                Assert.NotEqual(material.FilePath, result.Tree.FilePath);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Reconstructs from the retained snapshot after deleting its candidate.
        /// </summary>
        [Fact]
        public void CandidateDeletedBeforeP5I_DoesNotAffectReconstruction()
        {
            byte[] source = "class DeletedCandidate { }"u8.ToArray();
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "source.cs");
            File.WriteAllBytes(path, source);

            try
            {
                ExternalSourceDocumentDescriptor document = CreateDocument(source);
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    path,
                    out ValidatedExternalSourceMaterial material));
                File.Delete(path);

                ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

                Assert.Equal(Encoding.UTF8.GetString(source), result.Text.ToString());
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Reconstructs from source A after its candidate is replaced by B.
        /// </summary>
        [Fact]
        public void CandidateReplacedBeforeP5I_UsesOriginalSnapshot()
        {
            byte[] original = "class OriginalCandidate { }"u8.ToArray();
            byte[] replacement = "class ReplacedCandidate { }"u8.ToArray();
            string directory = P5BTests.CreateTempDirectory();
            string path = Path.Combine(directory, "source.cs");
            File.WriteAllBytes(path, original);

            try
            {
                ExternalSourceDocumentDescriptor document = CreateDocument(original);
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                    document,
                    path,
                    out ValidatedExternalSourceMaterial material));
                File.WriteAllBytes(path, replacement);

                ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

                Assert.Equal(Encoding.UTF8.GetString(original), result.Text.ToString());
                Assert.NotEqual(File.ReadAllText(path), result.Text.ToString());
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Reconstructs with Source Link provenance present without resolving it.
        /// </summary>
        [Fact]
        public void SourceLinkPresent_RequiresNoNetwork()
        {
            P4BTests.TestSource source = new(
                "/_/SourceLink.cs",
                "public sealed class SourceLinkType { }",
                SourceHashAlgorithm.Sha256,
                embed: false);
            P4BTests.PortablePdbTestData testData = P4BTests.EmitPortablePdb(
                "P5ISourceLink",
                new[] { source },
                "{\"documents\":{\"/_/*\":\"https://network.invalid/source/*\"}}");
            ExternalPortablePdbDescriptor pdb = P4BTests.ReadRequiredDescriptor(
                testData.DebugDescriptor,
                testData.PdbImage);
            Assert.NotNull(pdb.SourceLink);
            ValidatedExternalSourceMaterial material = CreateMaterial(
                Assert.Single(pdb.Documents),
                source.Image);

            ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

            Assert.Equal(source.Path, result.Tree.FilePath);
        }

        /// <summary>
        /// Reconstructs a real Embedded Source through P4B, P5H, and P5I.
        /// </summary>
        [Fact]
        public void EmbeddedSource_RoundTripsThroughP4BAndP5I()
        {
            P4BTests.TestSource source = new(
                "/_/Embedded.cs",
                "public sealed class EmbeddedType { }",
                SourceHashAlgorithm.Sha256,
                embed: true);
            ExternalSourceDocumentDescriptor document = Assert.Single(
                EmitAndReadP4B("P5IEmbedded", source).Documents);
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                document,
                out ValidatedExternalSourceMaterial material));

            ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

            Assert.Equal(source.Text.ToString(), result.Text.ToString());
            Assert.Equal(source.Path, result.Tree.FilePath);
        }

        /// <summary>
        /// Reconstructs a real explicit source through P4B, P5H, and P5I.
        /// </summary>
        [Fact]
        public void ExplicitSource_RoundTripsThroughP4BAndP5I()
        {
            P4BTests.TestSource source = new(
                "/_/Explicit.cs",
                "public sealed class ExplicitType { }",
                SourceHashAlgorithm.Sha256,
                embed: false);
            ExternalSourceDocumentDescriptor document = Assert.Single(
                EmitAndReadP4B("P5IExplicit", source).Documents);
            ValidatedExternalSourceMaterial material = CreateMaterial(
                document,
                source.Image);

            ExternalCSharpSyntaxTree result = CreateTree(material, CreateConfiguration());

            Assert.Equal(source.Text.ToString(), result.Text.ToString());
            Assert.Equal(source.Path, result.Tree.FilePath);
        }

        /// <summary>
        /// Reconstructs real P5G and P5H provenance into one P5I tree.
        /// </summary>
        [Fact]
        public void RealPortablePdb_P5GAndP5HComposeIntoP5I()
        {
            const string source =
                "#if FEATURE_A\npublic class SelectedType { }\n#else\n" +
                "public class WrongType { }\n#endif\n";
            CSharpParseOptions parseOptions = new(
                LanguageVersion.CSharp12,
                preprocessorSymbols: new[] { "FEATURE_A" });
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.PortablePdb);
            emitOptions = emitOptions.WithDefaultSourceFileEncoding(
                new UTF8Encoding(false));
            P5ATests.PortablePdbTestData testData = P5ATests.EmitPortablePdb(
                "P5IComplete",
                source,
                parseOptions,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true,
                    nullableContextOptions: NullableContextOptions.Enable),
                emitOptions: emitOptions);
            ExternalCompilationProvenanceDescriptor provenance =
                P5ATests.ReadRequiredDescriptor(testData);
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            ExternalSourceDocumentDescriptor document = Assert.Single(
                provenance.PortablePdb.Documents);
            ValidatedExternalSourceMaterial material = CreateMaterial(
                document,
                EncodeWithPreamble(source, Encoding.UTF8));

            ExternalCSharpSyntaxTree result = CreateTree(material, configuration);
            string[] activeClasses = result.Tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            Assert.Equal(source, result.Text.ToString());
            Assert.Equal("utf-8", result.Text.Encoding?.WebName);
            Assert.Equal("/_/Source.cs", result.Tree.FilePath);
            Assert.Same(configuration.ParseOptions, result.Tree.Options);
            Assert.Equal(
                LanguageVersion.CSharp12,
                configuration.ParseOptions.LanguageVersion);
            Assert.Equal(new[] { "FEATURE_A" }, result.Tree.Options.PreprocessorSymbolNames);
            Assert.Equal(new[] { "SelectedType" }, activeClasses);
            Assert.Equal(
                NullableContextOptions.Enable,
                configuration.CompilationOptions.NullableContextOptions);
        }

        /// <summary>
        /// Uses P5G preprocessor symbols to select the active source branch.
        /// </summary>
        [Fact]
        public void DefineSymbol_SelectsExpectedSyntaxBranch()
        {
            const string source =
                "#if FEATURE_A\nclass SelectedType { }\n#else\n" +
                "class WrongType { }\n#endif";
            CSharpParseOptions options = new(
                LanguageVersion.CSharp12,
                preprocessorSymbols: new[] { "FEATURE_A" });

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(Encoding.UTF8.GetBytes(source)),
                CreateConfiguration(parseOptions: options));
            string[] classNames = result.Tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            Assert.Equal(new[] { "SelectedType" }, classNames);
        }

        /// <summary>
        /// Preserves the exact language version while Roslyn defers feature
        /// availability diagnostics until compilation.
        /// </summary>
        [Fact]
        public void LanguageVersion_IsRetainedWhileFeatureDiagnosticsAreDeferred()
        {
            byte[] source = "class Sample { string Value = \"\"\"text\"\"\"; }"u8.ToArray();
            ValidatedExternalSourceMaterial material = CreateMaterial(source);
            CSharpParseOptions csharp10Options = new(LanguageVersion.CSharp10);
            CSharpParseOptions csharp11Options = new(LanguageVersion.CSharp11);
            ExternalCSharpSyntaxTree csharp10 = CreateTree(
                material,
                CreateConfiguration(parseOptions: csharp10Options));
            ExternalCSharpSyntaxTree csharp11 = CreateTree(
                material,
                CreateConfiguration(parseOptions: csharp11Options));

            Assert.Same(csharp10Options, csharp10.Tree.Options);
            Assert.Same(csharp11Options, csharp11.Tree.Options);
            Assert.Equal(LanguageVersion.CSharp10, csharp10Options.LanguageVersion);
            Assert.Equal(LanguageVersion.CSharp11, csharp11Options.LanguageVersion);
            Assert.Empty(csharp10.Tree.GetDiagnostics());
            Assert.Empty(csharp11.Tree.GetDiagnostics());
        }

        /// <summary>
        /// Preserves the logical mixed-newline representation without custom
        /// normalization.
        /// </summary>
        [Fact]
        public void MixedNewlines_ArePreserved()
        {
            const string content = "first\r\nsecond\nthird\r\n";

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(Encoding.UTF8.GetBytes(content)),
                CreateConfiguration());

            Assert.Equal(content, result.Text.ToString());
        }

        /// <summary>
        /// Reconstructs an empty validated source and parses an empty tree.
        /// </summary>
        [Fact]
        public void EmptySource_ReconstructsSuccessfully()
        {
            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(Array.Empty<byte>()),
                CreateConfiguration());

            Assert.Equal(0, result.Text.Length);
            Assert.Empty(result.Tree.GetRoot().ChildNodes());
        }

        /// <summary>
        /// Returns a tree with syntax diagnostics instead of rejecting it.
        /// </summary>
        [Fact]
        public void SyntaxError_DoesNotFailReconstruction()
        {
            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial("class {"u8.ToArray()),
                CreateConfiguration());

            Assert.Contains(
                result.Tree.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Does not introduce a stricter consecutive-NUL binary policy.
        /// </summary>
        [Fact]
        public void ConsecutiveNuls_DoNotFailTextReconstruction()
        {
            byte[] source = [0x2f, 0x2f, 0x20, 0x00, 0x00];

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial(source),
                CreateConfiguration());

            Assert.Equal("// \0\0", result.Text.ToString());
        }

        /// <summary>
        /// Leaves the immutable P5H byte snapshot unchanged after zero-copy use.
        /// </summary>
        [Fact]
        public void MaterialImage_RemainsByteIdenticalAfterReconstruction()
        {
            ValidatedExternalSourceMaterial material = CreateMaterial(
                "class ImmutableBytes { }"u8.ToArray());
            byte[] before = material.Image.ToArray();

            CreateTree(material, CreateConfiguration());

            Assert.Equal(before, material.Image);
        }

        /// <summary>
        /// Rejects internally contradictory material through the SourceText
        /// original-byte checksum postcondition.
        /// </summary>
        [Fact]
        public void ContradictoryMaterial_FailsChecksumPostcondition()
        {
            byte[] expected = "class Expected { }"u8.ToArray();
            byte[] actual = "class Different { }"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            ValidatedExternalSourceMaterial material = new(
                document,
                ImmutableArray.CreateRange(actual),
                ExternalSourceMaterialOrigin.ExplicitStream,
                filePath: null);

            Assert.False(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                CreateConfiguration(),
                out _));
        }

        /// <summary>
        /// Does not interpret single-document reconstruction through the P5G
        /// source-file count.
        /// </summary>
        [Fact]
        public void SourceFileCount_DoesNotConstrainSingleDocumentFactory()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                sourceFileCount: 99);

            ExternalCSharpSyntaxTree result = CreateTree(
                CreateMaterial("class OneDocument { }"u8.ToArray()),
                configuration);

            Assert.NotNull(result.Tree);
        }

        /// <summary>
        /// Applies the project argument-null convention to both factory inputs.
        /// </summary>
        [Fact]
        public void NullInputs_ThrowArgumentNullException()
        {
            ValidatedExternalSourceMaterial material = CreateMaterial("source"u8.ToArray());
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();

            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpSyntaxTreeFactory.TryCreate(
                    null!,
                    configuration,
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpSyntaxTreeFactory.TryCreate(
                    material,
                    null!,
                    out _));
        }

        /// <summary>
        /// Creates a P5G configuration with controlled parse and encoding data.
        /// </summary>
        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            string? defaultEncoding = null,
            string? fallbackEncoding = null,
            CSharpParseOptions? parseOptions = null,
            int sourceFileCount = 1)
        {
            return new ExternalCSharpCompilationConfiguration(
                parseOptions ?? new CSharpParseOptions(LanguageVersion.CSharp12),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                "compiler-version",
                "runtime-version",
                sourceFileCount,
                defaultEncoding,
                fallbackEncoding);
        }

        /// <summary>
        /// Creates P5H material through checksum validation of exact bytes.
        /// </summary>
        private static ValidatedExternalSourceMaterial CreateMaterial(
            byte[] source,
            Guid? hashAlgorithm = null,
            Guid? language = null,
            string name = "/_/Source.cs")
        {
            Guid effectiveHashAlgorithm = hashAlgorithm
                ?? P4BTests.Sha256DocumentHashAlgorithm;
            ExternalSourceDocumentDescriptor document = CreateDocument(
                source,
                effectiveHashAlgorithm,
                language ?? ExternalSourceDocumentLanguageIdentifiers.CSharp,
                name);
            return CreateMaterial(document, source);
        }

        /// <summary>
        /// Creates P5H material for an existing P4B document descriptor.
        /// </summary>
        private static ValidatedExternalSourceMaterial CreateMaterial(
            ExternalSourceDocumentDescriptor document,
            byte[] source)
        {
            using MemoryStream stream = new(source, writable: false);
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out ValidatedExternalSourceMaterial material));
            return material;
        }

        /// <summary>
        /// Creates exact source-document provenance for controlled bytes.
        /// </summary>
        private static ExternalSourceDocumentDescriptor CreateDocument(
            byte[] source,
            Guid? hashAlgorithm = null,
            Guid? language = null,
            string name = "/_/Source.cs")
        {
            Guid effectiveHashAlgorithm = hashAlgorithm
                ?? P4BTests.Sha256DocumentHashAlgorithm;
            HashAlgorithmName algorithm = GetHashAlgorithm(effectiveHashAlgorithm);
            byte[] hash = CalculateHash(source, algorithm);
            return new ExternalSourceDocumentDescriptor(
                name,
                effectiveHashAlgorithm,
                ImmutableArray.CreateRange(hash),
                language ?? ExternalSourceDocumentLanguageIdentifiers.CSharp,
                embeddedSource: null);
        }

        /// <summary>
        /// Maps controlled test identifiers to cryptographic algorithms.
        /// </summary>
        private static HashAlgorithmName GetHashAlgorithm(Guid identifier)
        {
            if (identifier == P4BTests.Sha1DocumentHashAlgorithm)
            {
                return HashAlgorithmName.SHA1;
            }

            if (identifier == P4BTests.Sha256DocumentHashAlgorithm)
            {
                return HashAlgorithmName.SHA256;
            }

            if (identifier == P4BTests.Sha384DocumentHashAlgorithm)
            {
                return HashAlgorithmName.SHA384;
            }

            if (identifier == P4BTests.Sha512DocumentHashAlgorithm)
            {
                return HashAlgorithmName.SHA512;
            }

            throw new ArgumentOutOfRangeException(nameof(identifier));
        }

        /// <summary>
        /// Calculates the controlled source checksum.
        /// </summary>
        private static byte[] CalculateHash(
            byte[] source,
            HashAlgorithmName algorithm)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(algorithm);
            hash.AppendData(source);
            return hash.GetHashAndReset();
        }

        /// <summary>
        /// Encodes text with the selected encoding's byte-order mark.
        /// </summary>
        private static byte[] EncodeWithPreamble(string content, Encoding encoding)
        {
            return encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray();
        }

        /// <summary>
        /// Resolves a historical encoding directly through the code-pages provider.
        /// </summary>
        private static Encoding GetCodePageEncoding(string webName)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(webName)
                ?? throw new InvalidOperationException(
                    $"The test code page '{webName}' is unavailable.");
        }

        /// <summary>
        /// Invokes P5I and requires a successful complete result.
        /// </summary>
        private static ExternalCSharpSyntaxTree CreateTree(
            ValidatedExternalSourceMaterial material,
            ExternalCSharpCompilationConfiguration configuration)
        {
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                configuration,
                out ExternalCSharpSyntaxTree result));
            Assert.Same(material.Document, result.Document);
            return result;
        }

        /// <summary>
        /// Emits and reads one real Portable PDB through the existing P4B helper.
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
    }
}
