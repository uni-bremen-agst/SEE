using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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
    /// Tests Portable PDB compilation-metadata provenance extraction.
    /// </summary>
    public sealed class ExternalCompilationProvenanceDescriptorFactoryTests
    {
        /// <summary>
        /// The standardized Compilation Options CDI identifier.
        /// </summary>
        private static readonly Guid CompilationOptionsKind =
            new("b5feec05-8cd0-4a83-96da-466284bb4bd8");

        /// <summary>
        /// The standardized Compilation Metadata References CDI identifier.
        /// </summary>
        private static readonly Guid CompilationMetadataReferencesKind =
            new("7e4d4708-096e-4c5c-aeda-cb10ba6a740d");

        /// <summary>
        /// Reads deliberately selected real C# compilation options and keeps
        /// the exact CDI order.
        /// </summary>
        [Fact]
        public void RealCompilationOptions_ArePreservedWithoutDefaults()
        {
            PortablePdbTestData testData = EmitPortablePdb(
                "OptionsLibrary",
                "#if FIRST\npublic unsafe sealed class OptionType { public int* Value; }\n#endif",
                new CSharpParseOptions(
                    LanguageVersion.CSharp12,
                    preprocessorSymbols: new[] { "FIRST", "SECOND" }),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    platform: Platform.X64,
                    checkOverflow: true,
                    allowUnsafe: true,
                    nullableContextOptions: NullableContextOptions.Enable,
                    deterministic: true));
            ExternalCompilationProvenanceDescriptor descriptor =
                ReadRequiredDescriptor(testData);
            ExternalCompilationOptionsDescriptor options = Assert.IsType<
                ExternalCompilationOptionsDescriptor>(descriptor.CompilationOptions);
            ImmutableArray<KeyValuePair<string, string>> independentlyRead =
                ReadCompilationOptions(testData.PdbImage);

            Assert.Equal(independentlyRead.Length, options.Options.Length);

            for (int index = 0; index < independentlyRead.Length; index++)
            {
                Assert.Equal(independentlyRead[index].Key, options.Options[index].Key);
                Assert.Equal(independentlyRead[index].Value, options.Options[index].Value);
            }

            AssertOption(options, "language", "C#");
            Assert.True(options.TryGetValue("compiler-version", out string compilerVersion));
            Assert.False(string.IsNullOrEmpty(compilerVersion));
            Assert.True(options.TryGetValue("runtime-version", out string runtimeVersion));
            Assert.False(string.IsNullOrEmpty(runtimeVersion));
            AssertOption(options, "source-file-count", "1");
            AssertOption(options, "optimization", "release");
            AssertOption(options, "output-kind", "DynamicallyLinkedLibrary");
            AssertOption(options, "platform", "X64");
            AssertOption(options, "language-version", "12.0");
            AssertOption(options, "define", "FIRST,SECOND");
            AssertOption(options, "checked", "True");
            AssertOption(options, "nullable", "Enable");
            AssertOption(options, "unsafe", "True");
        }

        /// <summary>
        /// Preserves unknown future compiler-option keys and empty values.
        /// </summary>
        [Fact]
        public void UnknownCompilationOption_IsPreserved()
        {
            ImmutableArray<byte> blob = EncodeStrings(
                "future-option",
                "future-value",
                "empty-value",
                string.Empty);

            Assert.True(ExternalCompilationOptionsDescriptorFactory.TryCreate(
                blob,
                out ExternalCompilationOptionsDescriptor descriptor));
            Assert.Equal(2, descriptor.Options.Length);
            AssertOption(descriptor, "future-option", "future-value");
            AssertOption(descriptor, "empty-value", string.Empty);
        }

        /// <summary>
        /// Supplies malformed compilation-options blobs.
        /// </summary>
        /// <returns>Malformed complete blob images.</returns>
        public static IEnumerable<object[]> MalformedOptionsBlobs()
        {
            yield return new object[] { Encoding.UTF8.GetBytes("key") };
            yield return new object[] { Encoding.UTF8.GetBytes("key\0") };
            yield return new object[] { Encoding.UTF8.GetBytes("key\0value") };
            yield return new object[] { Encoding.UTF8.GetBytes("\0value\0") };
            yield return new object[] { Encoding.UTF8.GetBytes("key\0one\0key\0two\0") };
            yield return new object[] { new byte[] { 0xff, 0, (byte)'v', 0 } };
            yield return new object[] { Encoding.UTF8.GetBytes("key\0value\0trailing") };
        }

        /// <summary>
        /// Rejects incomplete, duplicate, empty-key, and invalid UTF-8 option
        /// data without returning partial provenance.
        /// </summary>
        /// <param name="blob">The malformed blob.</param>
        [Theory]
        [MemberData(nameof(MalformedOptionsBlobs))]
        public void MalformedCompilationOptions_FailClosed(byte[] blob)
        {
            Assert.False(ExternalCompilationOptionsDescriptorFactory.TryCreate(
                ImmutableArray.CreateRange(blob),
                out _));
        }

        /// <summary>
        /// Distinguishes an explicitly present empty options blob from an
        /// absent CDI.
        /// </summary>
        [Fact]
        public void EmptyCompilationOptionsBlob_IsPresentAndValid()
        {
            Assert.True(ExternalCompilationOptionsDescriptorFactory.TryCreate(
                ImmutableArray<byte>.Empty,
                out ExternalCompilationOptionsDescriptor descriptor));
            Assert.Empty(descriptor.Options);
        }

        /// <summary>
        /// Supplies every defined combination of reference property bits.
        /// </summary>
        /// <returns>The encoded byte, kind, and interop flag.</returns>
        public static IEnumerable<object[]> KnownReferenceProperties()
        {
            yield return new object[] { (byte)0, MetadataImageKind.Module, false };
            yield return new object[] { (byte)1, MetadataImageKind.Assembly, false };
            yield return new object[] { (byte)2, MetadataImageKind.Module, true };
            yield return new object[] { (byte)3, MetadataImageKind.Assembly, true };
        }

        /// <summary>
        /// Decodes assembly/module and EmbedInteropTypes bits exactly.
        /// </summary>
        /// <param name="properties">The encoded properties byte.</param>
        /// <param name="kind">The expected image kind.</param>
        /// <param name="embedInteropTypes">The expected interop flag.</param>
        [Theory]
        [MemberData(nameof(KnownReferenceProperties))]
        public void ReferenceProperties_AreDecodedExactly(
            byte properties,
            MetadataImageKind kind,
            bool embedInteropTypes)
        {
            Guid mvid = Guid.NewGuid();
            ImmutableArray<byte> blob = CreateReferenceBlob(
                new TestReference("dependency.dll", "zeta,alpha", properties, 17, 23, mvid));

            Assert.True(ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                blob,
                out ExternalCompilationMetadataReferencesDescriptor descriptor));
            ExternalCompilationMetadataReferenceDescriptor reference =
                Assert.Single(descriptor.References);
            Assert.Equal(kind, reference.Kind);
            Assert.Equal(embedInteropTypes, reference.EmbedInteropTypes);
            Assert.Equal(new[] { "zeta", "alpha" }, reference.Aliases);
            Assert.Equal(17, reference.Timestamp);
            Assert.Equal(23, reference.ImageSize);
            Assert.Equal(mvid, reference.ModuleVersionId);
        }

        /// <summary>
        /// Rejects every property byte containing an unknown high bit.
        /// </summary>
        /// <param name="properties">The malformed properties byte.</param>
        [Theory]
        [InlineData((byte)4)]
        [InlineData((byte)8)]
        [InlineData((byte)255)]
        public void UnknownReferencePropertyBits_FailClosed(byte properties)
        {
            ImmutableArray<byte> blob = CreateReferenceBlob(
                new TestReference("dependency.dll", string.Empty, properties, 0, 0, Guid.Empty));

            Assert.False(ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                blob,
                out _));
        }

        /// <summary>
        /// Supplies independently truncated or invalid reference entries.
        /// </summary>
        /// <returns>Malformed complete blob images.</returns>
        public static IEnumerable<object[]> MalformedReferenceBlobs()
        {
            yield return new object[] { Encoding.UTF8.GetBytes("name") };
            yield return new object[] { Encoding.UTF8.GetBytes("name\0alias") };
            yield return new object[] { Encoding.UTF8.GetBytes("name\0alias\0") };
            yield return new object[] { Append(Encoding.UTF8.GetBytes("name\0alias\0"), 1, 2, 3, 4) };
            yield return new object[] { Append(Encoding.UTF8.GetBytes("name\0alias\0"), 1, 2, 3, 4, 5, 6, 7, 8) };
            yield return new object[]
            {
                CreateReferenceBlob(
                    new TestReference("name", "alias", 1, 1, 2, Guid.NewGuid()))
                    .SkipLast(1)
                    .ToArray()
            };
            yield return new object[] { new byte[] { 0xff, 0, 0 } };
        }

        /// <summary>
        /// Rejects truncated fields and invalid UTF-8 without partial results.
        /// </summary>
        /// <param name="blob">The malformed blob.</param>
        [Theory]
        [MemberData(nameof(MalformedReferenceBlobs))]
        public void MalformedMetadataReference_FailsClosed(byte[] blob)
        {
            Assert.False(ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                ImmutableArray.CreateRange(blob),
                out _));
        }

        /// <summary>
        /// Rejects alias lists containing an empty element.
        /// </summary>
        [Fact]
        public void MetadataReferenceAliasWithEmptyElement_FailsClosed()
        {
            ImmutableArray<byte> blob = CreateReferenceBlob(
                new TestReference("dependency.dll", "first,,second", 1, 0, 0, Guid.Empty));

            Assert.False(ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                blob,
                out _));
        }

        /// <summary>
        /// Preserves multiple references, their order, and distinct MVIDs even
        /// when their names are equal.
        /// </summary>
        [Fact]
        public void MultipleReferences_WithSameNameRemainDistinctAndOrdered()
        {
            Guid firstMvid = Guid.NewGuid();
            Guid secondMvid = Guid.NewGuid();
            ImmutableArray<byte> blob = CreateReferenceBlob(
                new TestReference("same.dll", string.Empty, 1, 10, 20, firstMvid),
                new TestReference("same.dll", "alias", 1, 30, 40, secondMvid));

            Assert.True(ExternalCompilationMetadataReferencesDescriptorFactory.TryCreate(
                blob,
                out ExternalCompilationMetadataReferencesDescriptor descriptor));
            Assert.Equal(2, descriptor.References.Length);
            Assert.Equal(firstMvid, descriptor.References[0].ModuleVersionId);
            Assert.Equal(secondMvid, descriptor.References[1].ModuleVersionId);
            Assert.Empty(descriptor.References[0].Aliases);
            Assert.Equal(new[] { "alias" }, descriptor.References[1].Aliases);
        }

        /// <summary>
        /// Reads real Roslyn metadata references and validates one entry
        /// independently against its PE image.
        /// </summary>
        [Fact]
        public void RealMetadataReferences_MatchReferencedPeProvenance()
        {
            string dependencyPath = EmitDependency("ControlledDependency", "dependency.dll");

            try
            {
                MetadataReference dependency = MetadataReference.CreateFromFile(dependencyPath);
                PortablePdbTestData testData = EmitPortablePdb(
                    "ReferencesLibrary",
                    "public sealed class ReferenceType { }",
                    CSharpParseOptions.Default,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        deterministic: true),
                    MetadataReferences.Default.Append(dependency));
                ExternalCompilationProvenanceDescriptor descriptor =
                    ReadRequiredDescriptor(testData);
                ExternalCompilationMetadataReferencesDescriptor references = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(descriptor.MetadataReferences);
                ExternalCompilationMetadataReferenceDescriptor reference = Assert.Single(
                    references.References.Where(item => item.Name == "dependency.dll"));
                PeProvenance expected = ReadPeProvenance(dependencyPath);

                Assert.Equal(MetadataImageKind.Assembly, reference.Kind);
                Assert.False(reference.EmbedInteropTypes);
                Assert.Equal(expected.Timestamp, reference.Timestamp);
                Assert.Equal(expected.ImageSize, reference.ImageSize);
                Assert.Equal(expected.ModuleVersionId, reference.ModuleVersionId);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(dependencyPath)!, recursive: true);
            }
        }

        /// <summary>
        /// Preserves explicit real Roslyn aliases in their original order.
        /// </summary>
        [Fact]
        public void RealMetadataReferenceAliases_ArePreservedInOrder()
        {
            string dependencyPath = EmitDependency("AliasedDependency", "aliased.dll");

            try
            {
                MetadataReference aliasedDependency = MetadataReference.CreateFromFile(
                    dependencyPath,
                    new MetadataReferenceProperties(
                        MetadataImageKind.Assembly,
                        aliases: ImmutableArray.Create("zeta", "alpha")));
                PortablePdbTestData testData = EmitPortablePdb(
                    "AliasLibrary",
                    "public sealed class AliasType { }",
                    CSharpParseOptions.Default,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        deterministic: true),
                    MetadataReferences.Default.Append(aliasedDependency));
                ExternalCompilationMetadataReferencesDescriptor references = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(
                    ReadRequiredDescriptor(testData).MetadataReferences);
                ExternalCompilationMetadataReferenceDescriptor reference = Assert.Single(
                    references.References.Where(item => item.Name == "aliased.dll"));

                Assert.Equal(new[] { "alpha", "zeta" }, reference.Aliases);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(dependencyPath)!, recursive: true);
            }
        }

        /// <summary>
        /// Accepts a P4B-valid PDB when both compilation metadata CDIs are absent.
        /// </summary>
        [Fact]
        public void MissingCompilationMetadataCdis_AreRepresentedAsAbsent()
        {
            SyntheticPdb testData = CreateSyntheticPdb();
            ExternalCompilationProvenanceDescriptor descriptor =
                ReadRequiredDescriptor(testData);

            Assert.Null(descriptor.CompilationOptions);
            Assert.Null(descriptor.MetadataReferences);
        }

        /// <summary>
        /// Distinguishes present empty CDI blobs from absent compilation
        /// metadata provenance.
        /// </summary>
        [Fact]
        public void PresentEmptyCompilationMetadataCdis_ArePreserved()
        {
            SyntheticPdb testData = CreateSyntheticPdb(
                new SyntheticCdi(
                    CompilationOptionsKind,
                    ImmutableArray<byte>.Empty,
                    TableIndex.Module),
                new SyntheticCdi(
                    CompilationMetadataReferencesKind,
                    ImmutableArray<byte>.Empty,
                    TableIndex.Module));
            ExternalCompilationProvenanceDescriptor descriptor =
                ReadRequiredDescriptor(testData);

            Assert.Empty(Assert.IsType<ExternalCompilationOptionsDescriptor>(
                descriptor.CompilationOptions).Options);
            Assert.Empty(Assert.IsType<ExternalCompilationMetadataReferencesDescriptor>(
                descriptor.MetadataReferences).References);
        }

        /// <summary>
        /// Rejects duplicate module-level P5A CDI records.
        /// </summary>
        /// <param name="kind">The duplicated standardized CDI kind.</param>
        [Theory]
        [InlineData("b5feec05-8cd0-4a83-96da-466284bb4bd8")]
        [InlineData("7e4d4708-096e-4c5c-aeda-cb10ba6a740d")]
        public void DuplicateCompilationMetadataCdi_FailsClosed(string kind)
        {
            Guid identifier = Guid.Parse(kind);
            SyntheticPdb testData = CreateSyntheticPdb(
                new SyntheticCdi(identifier, ImmutableArray<byte>.Empty, TableIndex.Module),
                new SyntheticCdi(identifier, ImmutableArray<byte>.Empty, TableIndex.Module));

            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Ignores standardized P5A CDI kinds attached to a non-module parent.
        /// </summary>
        [Fact]
        public void CompilationMetadataCdiWithWrongParent_IsIgnored()
        {
            SyntheticPdb testData = CreateSyntheticPdb(
                new SyntheticCdi(
                    CompilationOptionsKind,
                    EncodeStrings("language", "C#"),
                    TableIndex.MethodDef),
                new SyntheticCdi(
                    CompilationMetadataReferencesKind,
                    CreateReferenceBlob(new TestReference("a.dll", string.Empty, 1, 0, 0, Guid.NewGuid())),
                    TableIndex.MethodDef));
            ExternalCompilationProvenanceDescriptor descriptor =
                ReadRequiredDescriptor(testData);

            Assert.Null(descriptor.CompilationOptions);
            Assert.Null(descriptor.MetadataReferences);
        }

        /// <summary>
        /// Rejects compilation provenance from a different valid PDB build.
        /// </summary>
        [Fact]
        public void WrongPortablePdb_FailsThroughP4BGatekeeper()
        {
            PortablePdbTestData first = EmitDefaultPortablePdb("SharedName");
            PortablePdbTestData second = EmitPortablePdb(
                "SharedName",
                "public sealed class DifferentType { }",
                CSharpParseOptions.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));

            using MemoryStream stream = new(second.PdbImage, writable: false);
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                first.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Preserves P4B checksum rejection when non-ID PDB content is changed.
        /// </summary>
        [Fact]
        public void TamperedPortablePdb_FailsThroughP4BChecksumGate()
        {
            const string sourceLink =
                "{\"documents\":{\"/_/*\":\"https://example.test/source/*\"}}";
            PortablePdbTestData testData = EmitDefaultPortablePdb(
                "TamperedLibrary",
                sourceLink);
            byte[] tampered = (byte[])testData.PdbImage.Clone();
            byte[] marker = Encoding.UTF8.GetBytes("example.test");
            int markerOffset = FindSequence(tampered, marker);
            Assert.True(markerOffset >= 0);
            tampered[markerOffset] = (byte)'E';

            Assert.Equal(ReadPdbId(testData.PdbImage), ReadPdbId(tampered));

            using MemoryStream stream = new(tampered, writable: false);
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out _));
        }

        /// <summary>
        /// Leaves caller-owned streams open after success and failure.
        /// </summary>
        /// <param name="useMatchingPdb">Whether to validate the matching PDB.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CallerStream_RemainsOpen(bool useMatchingPdb)
        {
            PortablePdbTestData expected = EmitDefaultPortablePdb("StreamLibrary");
            PortablePdbTestData other = EmitPortablePdb(
                "OtherStreamLibrary",
                "public sealed class OtherStreamType { }",
                CSharpParseOptions.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));
            byte[] candidate = useMatchingPdb ? expected.PdbImage : other.PdbImage;
            using MemoryStream stream = new(candidate, writable: false);

            bool result = ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                expected.DebugDescriptor,
                stream,
                out _);

            Assert.Equal(useMatchingPdb, result);
            Assert.True(stream.CanRead);
        }

        /// <summary>
        /// Uses only the explicitly supplied candidate file and ignores the
        /// nonexistent CodeView path.
        /// </summary>
        [Fact]
        public void ExplicitCandidateFile_DoesNotUseCodeViewPath()
        {
            PortablePdbTestData testData = EmitDefaultPortablePdb("FileLibrary");
            Assert.False(File.Exists(testData.DebugDescriptor.CodeViewPdbReferences[0].Path));
            string candidatePath = Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p5a-{Guid.NewGuid():N}.pdb");
            File.WriteAllBytes(candidatePath, testData.PdbImage);

            try
            {
                Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(
                    testData.DebugDescriptor,
                    candidatePath,
                    out _));
            }
            finally
            {
                File.Delete(candidatePath);
            }
        }

        /// <summary>
        /// Asserts one exact serialized compiler option.
        /// </summary>
        private static void AssertOption(
            ExternalCompilationOptionsDescriptor descriptor,
            string key,
            string expectedValue)
        {
            Assert.True(descriptor.TryGetValue(key, out string actualValue));
            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// Emits a default real Roslyn Portable PDB.
        /// </summary>
        private static PortablePdbTestData EmitDefaultPortablePdb(
            string assemblyName,
            string? sourceLink = null)
        {
            return EmitPortablePdb(
                assemblyName,
                "public sealed class DefaultType { }",
                CSharpParseOptions.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true),
                sourceLink: sourceLink);
        }

        /// <summary>
        /// Emits a real Roslyn PE and Portable PDB with selected compilation inputs.
        /// </summary>
        private static PortablePdbTestData EmitPortablePdb(
            string assemblyName,
            string source,
            CSharpParseOptions parseOptions,
            CSharpCompilationOptions compilationOptions,
            IEnumerable<MetadataReference>? references = null,
            string? sourceLink = null)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                parseOptions,
                "/_/Source.cs",
                Encoding.UTF8);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references ?? MetadataReferences.Default,
                compilationOptions);
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            string codeViewPath = Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p5a-codeview-{Guid.NewGuid():N}",
                "candidate.pdb");
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                pdbFilePath: codeViewPath);
            using MemoryStream peStream = new();
            using MemoryStream pdbStream = new();
            using MemoryStream? sourceLinkStream = sourceLink == null
                ? null
                : new MemoryStream(Encoding.UTF8.GetBytes(sourceLink), writable: false);
            EmitResult result = compilation.Emit(
                peStream,
                pdbStream,
                options: emitOptions,
                sourceLinkStream: sourceLinkStream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            byte[] peImage = peStream.ToArray();
            byte[] pdbImage = pdbStream.ToArray();
            return new PortablePdbTestData(
                peImage,
                pdbImage,
                CreateP4ADebugDescriptor(peImage));
        }

        /// <summary>
        /// Creates P4A provenance for an emitted PE through production code.
        /// </summary>
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
        /// Reads one required P5A descriptor from real Roslyn test data.
        /// </summary>
        private static ExternalCompilationProvenanceDescriptor ReadRequiredDescriptor(
            PortablePdbTestData testData)
        {
            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out ExternalCompilationProvenanceDescriptor descriptor));
            Assert.Equal(ReadPdbId(testData.PdbImage), descriptor.PortablePdb.Id);
            return descriptor;
        }

        /// <summary>
        /// Reads one required P5A descriptor from a synthetic PDB.
        /// </summary>
        private static ExternalCompilationProvenanceDescriptor ReadRequiredDescriptor(
            SyntheticPdb testData)
        {
            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out ExternalCompilationProvenanceDescriptor descriptor));
            return descriptor;
        }

        /// <summary>
        /// Independently reads real Compilation Options CDI entries.
        /// </summary>
        private static ImmutableArray<KeyValuePair<string, string>> ReadCompilationOptions(
            byte[] pdbImage)
        {
            ImmutableArray<byte> blob = ReadRequiredModuleBlob(
                pdbImage,
                CompilationOptionsKind);
            ImmutableArray<KeyValuePair<string, string>>.Builder options =
                ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
            int offset = 0;

            while (offset < blob.Length)
            {
                int keyLength = blob.AsSpan().Slice(offset).IndexOf((byte)0);
                Assert.True(keyLength >= 0);
                string key = Encoding.UTF8.GetString(blob.AsSpan(offset, keyLength));
                offset += keyLength + 1;
                int valueLength = blob.AsSpan().Slice(offset).IndexOf((byte)0);
                Assert.True(valueLength >= 0);
                string value = Encoding.UTF8.GetString(blob.AsSpan(offset, valueLength));
                offset += valueLength + 1;
                options.Add(KeyValuePair.Create(key, value));
            }

            return options.ToImmutable();
        }

        /// <summary>
        /// Finds one required module-level CDI blob in a real PDB.
        /// </summary>
        private static ImmutableArray<byte> ReadRequiredModuleBlob(
            byte[] pdbImage,
            Guid kind)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(stream);
            MetadataReader reader = provider.GetMetadataReader();
            EntityHandle moduleHandle = MetadataTokens.EntityHandle(TableIndex.Module, 1);
            CustomDebugInformationHandle handle = Assert.Single(
                reader.GetCustomDebugInformation(moduleHandle).Where(
                    item => reader.GetGuid(reader.GetCustomDebugInformation(item).Kind) == kind));
            return reader.GetBlobContent(reader.GetCustomDebugInformation(handle).Value);
        }

        /// <summary>
        /// Reads PE provenance independently from a controlled reference file.
        /// </summary>
        private static PeProvenance ReadPeProvenance(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using PEReader reader = new(stream);
            MetadataReader metadata = reader.GetMetadataReader();
            ModuleDefinition module = metadata.GetModuleDefinition();
            return new PeProvenance(
                reader.PEHeaders.CoffHeader.TimeDateStamp,
                Assert.IsType<PEHeader>(reader.PEHeaders.PEHeader).SizeOfImage,
                metadata.GetGuid(module.Mvid));
        }

        /// <summary>
        /// Emits one controlled deterministic assembly reference to a unique
        /// temporary directory.
        /// </summary>
        private static string EmitDependency(string assemblyName, string fileName)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[]
                {
                    CSharpSyntaxTree.ParseText(
                        "public sealed class ControlledDependencyType { }")
                },
                MetadataReferences.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"xmldocnormalizer-p5a-reference-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, stream.ToArray());
            return path;
        }

        /// <summary>
        /// Creates a minimal valid Portable PDB containing selected CDI rows.
        /// </summary>
        private static SyntheticPdb CreateSyntheticPdb(params SyntheticCdi[] customDebugInformation)
        {
            MetadataBuilder metadata = new();
            int[] rowCounts = new int[MetadataTokens.TableCount];
            rowCounts[(int)TableIndex.Module] = 1;

            foreach (SyntheticCdi cdi in customDebugInformation)
            {
                rowCounts[(int)cdi.ParentTable] = Math.Max(
                    rowCounts[(int)cdi.ParentTable],
                    1);
                EntityHandle parent = MetadataTokens.EntityHandle(cdi.ParentTable, 1);
                metadata.AddCustomDebugInformation(
                    parent,
                    metadata.GetOrAddGuid(cdi.Kind),
                    metadata.GetOrAddBlob(cdi.Value));
            }

            PortablePdbBuilder builder = new(
                metadata,
                ImmutableArray.CreateRange(rowCounts),
                entryPoint: default);
            BlobBuilder image = new();
            builder.Serialize(image);
            byte[] pdbImage = image.ToArray();
            BlobContentId id = ReadPdbId(pdbImage);
            ExternalPeDebugDirectoryDescriptor expected = new(
                new ExternalModuleIdentity("synthetic.dll", Guid.Empty),
                isDeterministic: false,
                ImmutableArray<ExternalCodeViewPdbReference>.Empty,
                ImmutableArray.Create(id),
                ImmutableArray<ExternalPdbChecksum>.Empty);
            return new SyntheticPdb(pdbImage, expected);
        }

        /// <summary>
        /// Reads a Portable PDB content ID independently.
        /// </summary>
        private static BlobContentId ReadPdbId(byte[] pdbImage)
        {
            using MemoryStream stream = new(pdbImage, writable: false);
            using MetadataReaderProvider provider =
                MetadataReaderProvider.FromPortablePdbStream(stream);
            DebugMetadataHeader header = Assert.IsType<DebugMetadataHeader>(
                provider.GetMetadataReader().DebugMetadataHeader);
            return new BlobContentId(header.Id);
        }

        /// <summary>
        /// Encodes strings as strict null-terminated UTF-8 entries.
        /// </summary>
        private static ImmutableArray<byte> EncodeStrings(params string[] values)
        {
            List<byte> bytes = new();

            foreach (string value in values)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(value));
                bytes.Add(0);
            }

            return ImmutableArray.CreateRange(bytes);
        }

        /// <summary>
        /// Encodes complete metadata-reference entries.
        /// </summary>
        private static ImmutableArray<byte> CreateReferenceBlob(params TestReference[] references)
        {
            List<byte> bytes = new();

            foreach (TestReference reference in references)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(reference.Name));
                bytes.Add(0);
                bytes.AddRange(Encoding.UTF8.GetBytes(reference.Aliases));
                bytes.Add(0);
                bytes.Add(reference.Properties);
                byte[] integer = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32LittleEndian(integer, reference.Timestamp);
                bytes.AddRange(integer);
                BinaryPrimitives.WriteInt32LittleEndian(integer, reference.ImageSize);
                bytes.AddRange(integer);
                bytes.AddRange(reference.ModuleVersionId.ToByteArray());
            }

            return ImmutableArray.CreateRange(bytes);
        }

        /// <summary>
        /// Appends bytes to a prefix for truncated-entry tests.
        /// </summary>
        private static byte[] Append(byte[] prefix, params byte[] suffix)
        {
            return prefix.Concat(suffix).ToArray();
        }

        /// <summary>
        /// Finds a byte sequence within an image.
        /// </summary>
        private static int FindSequence(byte[] image, byte[] sequence)
        {
            for (int index = 0; index <= image.Length - sequence.Length; index++)
            {
                if (image.AsSpan(index, sequence.Length).SequenceEqual(sequence))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Represents real Roslyn test output and its P4A descriptor.
        /// </summary>
        private sealed record PortablePdbTestData(
            byte[] PeImage,
            byte[] PdbImage,
            ExternalPeDebugDirectoryDescriptor DebugDescriptor);

        /// <summary>
        /// Represents one synthetic reference entry.
        /// </summary>
        private sealed record TestReference(
            string Name,
            string Aliases,
            byte Properties,
            int Timestamp,
            int ImageSize,
            Guid ModuleVersionId);

        /// <summary>
        /// Represents one synthetic custom debug information row.
        /// </summary>
        private sealed record SyntheticCdi(
            Guid Kind,
            ImmutableArray<byte> Value,
            TableIndex ParentTable);

        /// <summary>
        /// Represents a synthetic PDB and matching identity-only expectation.
        /// </summary>
        private sealed record SyntheticPdb(
            byte[] PdbImage,
            ExternalPeDebugDirectoryDescriptor DebugDescriptor);

        /// <summary>
        /// Represents independently read PE provenance.
        /// </summary>
        private sealed record PeProvenance(
            int Timestamp,
            int ImageSize,
            Guid ModuleVersionId);
    }
}
