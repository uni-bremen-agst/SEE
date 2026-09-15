using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using P4BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalPortablePdbDescriptorFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Verifies ordered P5J syntax-tree aggregation and the real Roslyn
    /// Portable PDB semantics on which its validation policy depends.
    /// </summary>
    public sealed class ExternalCSharpSyntaxTreeSetFactoryTests
    {
        /// <summary>
        /// Establishes the one-source baseline for source-file and document
        /// counts in a real Roslyn 5.0 Portable PDB.
        /// </summary>
        [Fact]
        public void RoslynPortablePdb_OneNormalSource_HasOneSourceFileAndDocument()
        {
            StudyResult result = EmitAndRead(
                "P5JStudyOne",
                Source("/_/Only.cs", "public sealed class Only { public void M() { } }"));

            Assert.Equal(1, result.SourceFileCount);
            Assert.Single(result.Documents);
            Assert.Equal("/_/Only.cs", result.Documents[0].Name);
        }

        /// <summary>
        /// Establishes ordering and cardinality for three ordinary sources.
        /// </summary>
        [Fact]
        public void RoslynPortablePdb_ThreeSources_PreserveTreeCardinalityAndOrder()
        {
            StudyResult result = EmitAndRead(
                "P5JStudyThree",
                Source("/_/First.cs", "public sealed class First { public void M() { } }"),
                Source("/_/Second.cs", "public sealed class Second { public void M() { } }"),
                Source("/_/Third.cs", "public sealed class Third { public void M() { } }"));

            Assert.Equal(3, result.SourceFileCount);
            Assert.Equal(
                new[] { "/_/First.cs", "/_/Second.cs", "/_/Third.cs" },
                result.Documents.Select(document => document.Name));
        }

        /// <summary>
        /// Establishes that a line-mapped document is additional PDB
        /// provenance and is not another compilation syntax tree.
        /// </summary>
        [Fact]
        public void RoslynPortablePdb_LineDirective_AddsMappedDocumentOnly()
        {
            StudyResult result = EmitAndRead(
                "P5JStudyLine",
                Source(
                    "/_/Original.cs",
                    "#line 100 \"Mapped.cs\"\npublic sealed class Mapped { public void M() { } }\n#line default"));

            Assert.Equal(1, result.SourceFileCount);
            Assert.Equal(
                new[] { "/_/Original.cs", "Mapped.cs" },
                result.Documents.Select(document => document.Name));
        }

        /// <summary>
        /// Establishes whether a source with no methods or sequence points is
        /// nevertheless represented in the document table.
        /// </summary>
        [Fact]
        public void RoslynPortablePdb_SourceWithoutMethods_RemainsDocument()
        {
            StudyResult result = EmitAndRead(
                "P5JStudyNoMethods",
                Source("/_/Namespace.cs", "namespace EmptyNamespace { }"));

            Assert.Equal(1, result.SourceFileCount);
            Assert.Single(result.Documents);
            Assert.Equal("/_/Namespace.cs", result.Documents[0].Name);
        }

        /// <summary>
        /// Establishes mixed-source cardinality when one source has no method
        /// body or sequence point of its own.
        /// </summary>
        [Fact]
        public void RoslynPortablePdb_MultipleSourcesIncludingNoMethods_PreserveBothDocuments()
        {
            StudyResult result = EmitAndRead(
                "P5JStudyMixed",
                Source("/_/Active.cs", "public sealed class Active { public void M() { } }"),
                Source("/_/Namespace.cs", "namespace EmptyNamespace { }"));

            Assert.Equal(2, result.SourceFileCount);
            Assert.Equal(
                new[] { "/_/Active.cs", "/_/Namespace.cs" },
                result.Documents.Select(document => document.Name));
        }

        /// <summary>
        /// Aggregates three real P5I results and retains their exact tree
        /// references in original source order.
        /// </summary>
        [Fact]
        public void MultipleSources_EndToEnd_PreserveExactTreesInOrder()
        {
            RealPipelineResult pipeline = CreateRealPipeline(
                Source("/_/First.cs", "public sealed class First { }"),
                Source("/_/Second.cs", "public sealed class Second { }"),
                Source("/_/Third.cs", "public sealed class Third { }"));

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                pipeline.Configuration,
                pipeline.Documents,
                pipeline.SourceTrees);

            Assert.Equal(3, result.Trees.Length);

            for (int index = 0; index < result.Trees.Length; index++)
            {
                Assert.Same(pipeline.SourceTrees[index].Tree, result.Trees[index]);
            }
        }

        /// <summary>
        /// Rejects an incomplete ordinal sequence atomically.
        /// </summary>
        [Fact]
        public void MissingTree_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(2);

            Assert.False(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                inputs.Configuration,
                inputs.Documents,
                inputs.SourceTrees.Take(1).ToArray(),
                out _));
        }

        /// <summary>
        /// Rejects a sequence containing more trees than the explicit scope.
        /// </summary>
        [Fact]
        public void ExtraTree_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);

            Assert.False(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                inputs.Configuration,
                inputs.Documents,
                new[] { inputs.SourceTrees[0], inputs.SourceTrees[0] },
                out _));
        }

        /// <summary>
        /// Rejects swapped inputs instead of searching for matching documents.
        /// </summary>
        [Fact]
        public void SwappedTrees_ReturnsFalseWithoutReordering()
        {
            TestInputs inputs = CreateInputs(2);

            Assert.False(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                inputs.Configuration,
                inputs.Documents,
                new[] { inputs.SourceTrees[1], inputs.SourceTrees[0] },
                out _));
        }

        /// <summary>
        /// Rejects a document with a different language identifier.
        /// </summary>
        [Fact]
        public void DifferentDocumentLanguage_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                language: Guid.NewGuid());

            Assert.False(TryCreate(inputs, new[] { expected }));
        }

        /// <summary>
        /// Accepts equivalent source identity represented by another descriptor
        /// instance.
        /// </summary>
        [Fact]
        public void SemanticallyIdenticalSeparateDocument_IsAccepted()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(inputs.Documents[0]);

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                inputs.Configuration,
                new[] { expected },
                inputs.SourceTrees);

            Assert.NotSame(expected, inputs.SourceTrees[0].Document);
            Assert.Same(inputs.SourceTrees[0].Tree, Assert.Single(result.Trees));
        }

        /// <summary>
        /// Rejects the same document name with different source bytes.
        /// </summary>
        [Fact]
        public void SameNameDifferentHash_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                hash: ImmutableArray.CreateRange(SHA256.HashData("different"u8)));

            Assert.False(TryCreate(inputs, new[] { expected }));
        }

        /// <summary>
        /// Rejects the same source hash under a different document name.
        /// </summary>
        [Fact]
        public void SameHashDifferentName_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                name: "/_/Other.cs");

            Assert.False(TryCreate(inputs, new[] { expected }));
        }

        /// <summary>
        /// Rejects a different hash-algorithm GUID even when hash bytes match.
        /// </summary>
        [Fact]
        public void DifferentHashAlgorithm_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                hashAlgorithm: P4BTests.Sha1DocumentHashAlgorithm);

            Assert.False(TryCreate(inputs, new[] { expected }));
        }

        /// <summary>
        /// Applies ordinal document-name comparison with exact casing.
        /// </summary>
        [Fact]
        public void DocumentNameCaseDifference_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                name: inputs.Documents[0].Name.ToUpperInvariant());

            Assert.False(TryCreate(inputs, new[] { expected }));
        }

        /// <summary>
        /// Rejects a tree whose path no longer matches its document provenance.
        /// </summary>
        [Fact]
        public void TreeFilePathMismatch_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalCSharpSyntaxTree original = inputs.SourceTrees[0];
            SyntaxTree mismatchedTree = CSharpSyntaxTree.ParseText(
                original.Text,
                inputs.Configuration.ParseOptions,
                "/_/Other.cs");
            ExternalCSharpSyntaxTree mismatched = new(
                original.Document,
                original.Text,
                mismatchedTree);

            Assert.False(TryCreate(inputs, inputs.Documents, new[] { mismatched }));
        }

        /// <summary>
        /// Rejects a tree parsed with a different parse-options instance.
        /// </summary>
        [Fact]
        public void ParseOptionsInstanceMismatch_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalCSharpSyntaxTree original = inputs.SourceTrees[0];
            CSharpParseOptions otherOptions = inputs.Configuration.ParseOptions
                .WithPreprocessorSymbols("OTHER");
            SyntaxTree mismatchedTree = CSharpSyntaxTree.ParseText(
                original.Text,
                otherOptions,
                original.Document.Name);
            ExternalCSharpSyntaxTree mismatched = new(
                original.Document,
                original.Text,
                mismatchedTree);

            Assert.False(TryCreate(inputs, inputs.Documents, new[] { mismatched }));
        }

        /// <summary>
        /// Rejects a wrapper whose tree was parsed from a different SourceText
        /// instance.
        /// </summary>
        [Fact]
        public void SourceTextTreeIdentityMismatch_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalCSharpSyntaxTree original = inputs.SourceTrees[0];
            SourceText otherText = SourceText.From(
                original.Text.ToString(),
                Encoding.UTF8,
                original.Text.ChecksumAlgorithm);
            SyntaxTree mismatchedTree = CSharpSyntaxTree.ParseText(
                otherText,
                inputs.Configuration.ParseOptions,
                original.Document.Name);
            ExternalCSharpSyntaxTree mismatched = new(
                original.Document,
                original.Text,
                mismatchedTree);

            Assert.False(TryCreate(inputs, inputs.Documents, new[] { mismatched }));
        }

        /// <summary>
        /// Allows an explicitly empty compilation source scope and returns an
        /// initialized empty immutable array.
        /// </summary>
        [Fact]
        public void EmptyScope_ReturnsInitializedEmptySet()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(0);

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                configuration,
                Array.Empty<ExternalSourceDocumentDescriptor>(),
                Array.Empty<ExternalCSharpSyntaxTree>());

            Assert.Empty(result.Trees);
            Assert.False(result.Trees.IsDefault);
        }

        /// <summary>
        /// Retains duplicate ordinals and their exact references.
        /// </summary>
        [Fact]
        public void DuplicateTrees_AreNotDeduplicated()
        {
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(2);
            const string content = "public sealed class Duplicate { }";
            ExternalSourceDocumentDescriptor document = CreateDocument(
                "/_/Duplicate.cs",
                content);
            ExternalCSharpSyntaxTree sourceTree = CreateSourceTree(
                document,
                configuration,
                content);

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                configuration,
                new[] { document, document },
                new[] { sourceTree, sourceTree });

            Assert.Equal(2, result.Trees.Length);
            Assert.Same(result.Trees[0], result.Trees[1]);
        }

        /// <summary>
        /// Uses an explicit original-document scope for a real PDB containing
        /// an additional line-mapped document.
        /// </summary>
        [Fact]
        public void LineDirective_ExplicitOriginalDocumentSucceeds()
        {
            P4BTests.TestSource source = Source(
                "/_/Original.cs",
                "#line 100 \"Mapped.cs\"\npublic sealed class Mapped { public void M() { } }\n#line default");
            RealPipelineResult pipeline = CreateRealPipeline(source);

            Assert.Equal(2, pipeline.AllPdbDocuments.Length);
            ExternalCSharpSyntaxTreeSet result = CreateSet(
                pipeline.Configuration,
                pipeline.Documents,
                pipeline.SourceTrees);
            Assert.Single(result.Trees);
            Assert.Equal("/_/Original.cs", result.Trees[0].FilePath);
        }

        /// <summary>
        /// Reconstructs a real source with no methods through the complete
        /// P4/P5 pipeline.
        /// </summary>
        [Fact]
        public void SourceWithoutMethods_EndToEndSucceeds()
        {
            RealPipelineResult pipeline = CreateRealPipeline(
                Source("/_/Namespace.cs", "namespace EmptyNamespace { }"));

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                pipeline.Configuration,
                pipeline.Documents,
                pipeline.SourceTrees);

            Assert.Single(result.Trees);
        }

        /// <summary>
        /// Rejects an explicit scope that disagrees with the verified
        /// source-file-count provenance.
        /// </summary>
        [Fact]
        public void SourceFileCountMismatch_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(2);

            Assert.False(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                configuration,
                inputs.Documents,
                inputs.SourceTrees,
                out _));
        }

        /// <summary>
        /// Excludes embedded-source acquisition details from ordinal document
        /// identity after content identity has already been validated.
        /// </summary>
        [Fact]
        public void DifferentEmbeddedSourceProvenance_IsAccepted()
        {
            TestInputs inputs = CreateInputs(1);
            Assert.True(ExternalEmbeddedSourceProvenance.TryCreate(
                ImmutableArray.Create((byte)1),
                isCompressed: true,
                isDocumentChecksumValidated: false,
                out ExternalEmbeddedSourceProvenance embedded));
            ExternalSourceDocumentDescriptor expected = CopyDocument(
                inputs.Documents[0],
                embeddedSource: embedded);

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                inputs.Configuration,
                new[] { expected },
                inputs.SourceTrees);

            Assert.Single(result.Trees);
        }

        /// <summary>
        /// Treats document paths as opaque and performs no file access during
        /// aggregation.
        /// </summary>
        [Fact]
        public void OpaqueNonexistentPath_UsesOnlyInMemoryInputs()
        {
            const string path = "Z:\\path-that-must-not-be-opened\\Source.cs";
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(1);
            ExternalSourceDocumentDescriptor document = CreateDocument(
                path,
                "public sealed class InMemoryOnly { }");
            ExternalCSharpSyntaxTree tree = CreateSourceTree(
                document,
                configuration,
                "public sealed class InMemoryOnly { }");

            ExternalCSharpSyntaxTreeSet result = CreateSet(
                configuration,
                new[] { document },
                new[] { tree });

            Assert.Equal(path, Assert.Single(result.Trees).FilePath);
        }

        /// <summary>
        /// Verifies in test code that the aggregated trees can supply all
        /// source types to the next compilation-composition stage.
        /// </summary>
        [Fact]
        public void AggregatedTrees_TestCompilationContainsAllSourceTypes()
        {
            RealPipelineResult pipeline = CreateRealPipeline(
                Source("/_/First.cs", "public sealed class First { }"),
                Source("/_/Second.cs", "public sealed class Second { }"),
                Source("/_/Third.cs", "public sealed class Third { }"));
            ExternalCSharpSyntaxTreeSet result = CreateSet(
                pipeline.Configuration,
                pipeline.Documents,
                pipeline.SourceTrees);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "P5JTestCompilation",
                result.Trees,
                options: pipeline.Configuration.CompilationOptions);

            Assert.NotNull(compilation.GetTypeByMetadataName("First"));
            Assert.NotNull(compilation.GetTypeByMetadataName("Second"));
            Assert.NotNull(compilation.GetTypeByMetadataName("Third"));
        }

        /// <summary>
        /// Rejects null configuration input with the standard argument
        /// contract.
        /// </summary>
        [Fact]
        public void NullConfiguration_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                    null!,
                    Array.Empty<ExternalSourceDocumentDescriptor>(),
                    Array.Empty<ExternalCSharpSyntaxTree>(),
                    out _));
        }

        /// <summary>
        /// Rejects a null expected-document sequence.
        /// </summary>
        [Fact]
        public void NullExpectedDocuments_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                    CreateConfiguration(0),
                    null!,
                    Array.Empty<ExternalCSharpSyntaxTree>(),
                    out _));
        }

        /// <summary>
        /// Rejects a null source-tree sequence.
        /// </summary>
        [Fact]
        public void NullSourceTrees_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                    CreateConfiguration(0),
                    Array.Empty<ExternalSourceDocumentDescriptor>(),
                    null!,
                    out _));
        }

        /// <summary>
        /// Fails closed for a null descriptor at an expected ordinal.
        /// </summary>
        [Fact]
        public void NullExpectedDocumentElement_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);

            Assert.False(TryCreate(
                inputs,
                new ExternalSourceDocumentDescriptor[] { null! }));
        }

        /// <summary>
        /// Fails closed for a null P5I result at a source ordinal.
        /// </summary>
        [Fact]
        public void NullSourceTreeElement_ReturnsFalse()
        {
            TestInputs inputs = CreateInputs(1);

            Assert.False(TryCreate(
                inputs,
                inputs.Documents,
                new ExternalCSharpSyntaxTree[] { null! }));
        }

        /// <summary>
        /// Creates controlled, fully valid in-memory P5J inputs.
        /// </summary>
        /// <param name="count">The number of source ordinals.</param>
        /// <returns>The configuration, documents, and P5I results.</returns>
        private static TestInputs CreateInputs(int count)
        {
            ExternalCSharpCompilationConfiguration configuration =
                CreateConfiguration(count);
            List<ExternalSourceDocumentDescriptor> documents = new(count);
            List<ExternalCSharpSyntaxTree> sourceTrees = new(count);

            for (int index = 0; index < count; index++)
            {
                string content = $"public sealed class Type{index} {{ }}";
                ExternalSourceDocumentDescriptor document = CreateDocument(
                    $"/_/Source{index}.cs",
                    content);
                documents.Add(document);
                sourceTrees.Add(CreateSourceTree(document, configuration, content));
            }

            return new TestInputs(configuration, documents, sourceTrees);
        }

        /// <summary>
        /// Creates a controlled P5G configuration with one shared parse-options
        /// instance.
        /// </summary>
        /// <param name="sourceFileCount">The recorded source-tree count.</param>
        /// <returns>The configuration.</returns>
        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            int sourceFileCount)
        {
            return new ExternalCSharpCompilationConfiguration(
                new CSharpParseOptions(
                    LanguageVersion.CSharp14,
                    DocumentationMode.Parse,
                    SourceCodeKind.Regular),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                compilerVersion: "test",
                runtimeVersion: "test",
                sourceFileCount,
                defaultEncodingWebName: "utf-8",
                fallbackEncodingWebName: null);
        }

        /// <summary>
        /// Creates SHA-256 P4B-style provenance for controlled UTF-8 content.
        /// </summary>
        /// <param name="name">The opaque document name.</param>
        /// <param name="content">The complete source content.</param>
        /// <returns>The source-document descriptor.</returns>
        private static ExternalSourceDocumentDescriptor CreateDocument(
            string name,
            string content)
        {
            return new ExternalSourceDocumentDescriptor(
                name,
                P4BTests.Sha256DocumentHashAlgorithm,
                ImmutableArray.CreateRange(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
                ExternalSourceDocumentLanguageIdentifiers.CSharp,
                embeddedSource: null);
        }

        /// <summary>
        /// Copies a document while selectively replacing identity or
        /// acquisition-provenance fields.
        /// </summary>
        /// <param name="source">The source descriptor.</param>
        /// <param name="name">The optional replacement name.</param>
        /// <param name="hashAlgorithm">The optional replacement algorithm.</param>
        /// <param name="hash">The optional replacement hash.</param>
        /// <param name="language">The optional replacement language.</param>
        /// <param name="embeddedSource">
        /// The optional replacement embedded-source provenance.
        /// </param>
        /// <returns>The separate descriptor instance.</returns>
        private static ExternalSourceDocumentDescriptor CopyDocument(
            ExternalSourceDocumentDescriptor source,
            string? name = null,
            Guid? hashAlgorithm = null,
            ImmutableArray<byte>? hash = null,
            Guid? language = null,
            ExternalEmbeddedSourceProvenance? embeddedSource = null)
        {
            return new ExternalSourceDocumentDescriptor(
                name ?? source.Name,
                hashAlgorithm ?? source.HashAlgorithm,
                hash ?? source.Hash,
                language ?? source.Language,
                embeddedSource ?? source.EmbeddedSource);
        }

        /// <summary>
        /// Materializes controlled source bytes through P5H and reconstructs
        /// the corresponding P5I result.
        /// </summary>
        /// <param name="document">The expected source identity.</param>
        /// <param name="configuration">The P5G configuration.</param>
        /// <param name="content">The exact UTF-8 source content.</param>
        /// <returns>The reconstructed source tree.</returns>
        private static ExternalCSharpSyntaxTree CreateSourceTree(
            ExternalSourceDocumentDescriptor document,
            ExternalCSharpCompilationConfiguration configuration,
            string content)
        {
            byte[] image = Encoding.UTF8.GetBytes(content);
            using MemoryStream stream = new(image, writable: false);
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                document,
                stream,
                out ValidatedExternalSourceMaterial material));
            Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(
                material,
                configuration,
                out ExternalCSharpSyntaxTree sourceTree));
            return sourceTree;
        }

        /// <summary>
        /// Invokes P5J and requires a successful complete result.
        /// </summary>
        /// <param name="configuration">The P5G configuration.</param>
        /// <param name="documents">The explicit source-document scope.</param>
        /// <param name="sourceTrees">The ordinal P5I results.</param>
        /// <returns>The P5J result.</returns>
        private static ExternalCSharpSyntaxTreeSet CreateSet(
            ExternalCSharpCompilationConfiguration configuration,
            IReadOnlyList<ExternalSourceDocumentDescriptor> documents,
            IReadOnlyList<ExternalCSharpSyntaxTree> sourceTrees)
        {
            Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                configuration,
                documents,
                sourceTrees,
                out ExternalCSharpSyntaxTreeSet result));
            return result;
        }

        /// <summary>
        /// Invokes P5J with alternate expected documents.
        /// </summary>
        /// <param name="inputs">The controlled base inputs.</param>
        /// <param name="documents">The alternate expected documents.</param>
        /// <returns>The P5J success result.</returns>
        private static bool TryCreate(
            TestInputs inputs,
            IReadOnlyList<ExternalSourceDocumentDescriptor> documents)
        {
            return TryCreate(inputs, documents, inputs.SourceTrees);
        }

        /// <summary>
        /// Invokes P5J with alternate documents and source trees.
        /// </summary>
        /// <param name="inputs">The controlled base inputs.</param>
        /// <param name="documents">The alternate expected documents.</param>
        /// <param name="sourceTrees">The alternate P5I results.</param>
        /// <returns>The P5J success result.</returns>
        private static bool TryCreate(
            TestInputs inputs,
            IReadOnlyList<ExternalSourceDocumentDescriptor> documents,
            IReadOnlyList<ExternalCSharpSyntaxTree> sourceTrees)
        {
            return ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                inputs.Configuration,
                documents,
                sourceTrees,
                out _);
        }

        /// <summary>
        /// Emits and reconstructs real source inputs through all P4/P5 stages
        /// required before P5J.
        /// </summary>
        /// <param name="sources">The original compilation sources.</param>
        /// <returns>The real configuration, explicit scope, and P5I results.</returns>
        private static RealPipelineResult CreateRealPipeline(
            params P4BTests.TestSource[] sources)
        {
            P4BTests.PortablePdbTestData testData = P4BTests.EmitPortablePdb(
                $"P5JReal{Guid.NewGuid():N}",
                sources);
            using MemoryStream pdbStream = new(testData.PdbImage, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                pdbStream,
                out ExternalCompilationProvenanceDescriptor provenance));
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            List<ExternalSourceDocumentDescriptor> documents = new(sources.Length);
            List<ExternalCSharpSyntaxTree> sourceTrees = new(sources.Length);

            foreach (P4BTests.TestSource source in sources)
            {
                ExternalSourceDocumentDescriptor document = Assert.Single(
                    provenance.PortablePdb.Documents.Where(
                        candidate => candidate.Name == source.Path));
                using MemoryStream sourceStream = new(source.Image, writable: false);
                Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                    document,
                    sourceStream,
                    out ValidatedExternalSourceMaterial material));
                Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(
                    material,
                    configuration,
                    out ExternalCSharpSyntaxTree sourceTree));
                documents.Add(document);
                sourceTrees.Add(sourceTree);
            }

            return new RealPipelineResult(
                configuration,
                documents,
                sourceTrees,
                provenance.PortablePdb.Documents);
        }

        /// <summary>
        /// Creates one UTF-8 SHA-256 test source without embedded source data.
        /// </summary>
        /// <param name="path">The opaque source path.</param>
        /// <param name="content">The complete source text.</param>
        /// <returns>The source emission input.</returns>
        private static P4BTests.TestSource Source(string path, string content)
        {
            return new P4BTests.TestSource(
                path,
                content,
                SourceHashAlgorithm.Sha256,
                embed: false);
        }

        /// <summary>
        /// Emits real Roslyn PE/PDB bytes and reads them through P4A, P4B,
        /// P5A, and P5G production factories.
        /// </summary>
        /// <param name="assemblyName">The unique test assembly name.</param>
        /// <param name="sources">The source trees supplied to compilation.</param>
        /// <returns>The observed source-file count and PDB documents.</returns>
        private static StudyResult EmitAndRead(
            string assemblyName,
            params P4BTests.TestSource[] sources)
        {
            P4BTests.PortablePdbTestData testData = P4BTests.EmitPortablePdb(
                assemblyName,
                sources);
            using MemoryStream stream = new(testData.PdbImage, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                testData.DebugDescriptor,
                stream,
                out ExternalCompilationProvenanceDescriptor provenance));
            Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            return new StudyResult(
                configuration.SourceFileCount,
                provenance.PortablePdb.Documents);
        }

        /// <summary>
        /// Stores the relevant observations from one real Roslyn emission.
        /// </summary>
        /// <param name="SourceFileCount">The compilation-options CDI count.</param>
        /// <param name="Documents">The PDB documents in table order.</param>
        private sealed record StudyResult(
            int SourceFileCount,
            ImmutableArray<ExternalSourceDocumentDescriptor> Documents);

        /// <summary>
        /// Stores controlled in-memory P5J inputs.
        /// </summary>
        /// <param name="Configuration">The P5G configuration.</param>
        /// <param name="Documents">The expected source-document scope.</param>
        /// <param name="SourceTrees">The P5I results.</param>
        private sealed record TestInputs(
            ExternalCSharpCompilationConfiguration Configuration,
            IReadOnlyList<ExternalSourceDocumentDescriptor> Documents,
            IReadOnlyList<ExternalCSharpSyntaxTree> SourceTrees);

        /// <summary>
        /// Stores a real P4B/P5A/P5G/P5H/P5I pipeline result for P5J tests.
        /// </summary>
        /// <param name="Configuration">The reconstructed P5G configuration.</param>
        /// <param name="Documents">The explicitly selected source documents.</param>
        /// <param name="SourceTrees">The reconstructed P5I results.</param>
        /// <param name="AllPdbDocuments">All PDB documents, including mappings.</param>
        private sealed record RealPipelineResult(
            ExternalCSharpCompilationConfiguration Configuration,
            IReadOnlyList<ExternalSourceDocumentDescriptor> Documents,
            IReadOnlyList<ExternalCSharpSyntaxTree> SourceTrees,
            ImmutableArray<ExternalSourceDocumentDescriptor> AllPdbDocuments);
    }
}
