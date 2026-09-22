using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Emits controlled explicit candidate files without running the
    /// production reconstruction pipeline before analysis.
    /// </summary>
    internal sealed class ExternalReconstructionPlanTestWorkspace : IDisposable
    {
        /// <summary>
        /// Initializes an isolated candidate directory.
        /// </summary>
        public ExternalReconstructionPlanTestWorkspace()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        /// <summary>
        /// Gets the isolated candidate directory.
        /// </summary>
        public string DirectoryPath { get; }

        /// <summary>
        /// Emits one dependency and writes every explicit P6C candidate.
        /// </summary>
        public PreparedDependency Prepare(
            string assemblyName,
            IReadOnlyList<SourceInput> sources,
            IReadOnlyList<PortableExecutableReference>? additionalReferences = null,
            CSharpCompilationOptions? compilationOptions = null,
            bool embedSources = false,
            CSharpParseOptions? parseOptions = null,
            Encoding? sourceEncoding = null,
            string? sourceLinkJson = null,
            IReadOnlyCollection<int>? embeddedSourceOrdinals = null,
            bool embedPortablePdb = false)
        {
            string candidatePrefix = assemblyName + "." + Guid.NewGuid().ToString("N");
            CSharpParseOptions actualParseOptions = parseOptions ?? new CSharpParseOptions(
                    LanguageVersion.CSharp12,
                    DocumentationMode.Parse,
                    SourceCodeKind.Regular);
            List<byte[]> sourceImages = sources
                .Select(source => CreateSourceImage(source.Source, sourceEncoding))
                .ToList();
            ImmutableArray<SyntaxTree> trees = sources
                .Select((source, index) => CreateTree(
                    source.Path,
                    source.Source,
                    sourceImages[index],
                    actualParseOptions,
                    sourceEncoding))
                .ToImmutableArray();
            ImmutableArray<PortableExecutableReference> references = MetadataReferences.Default
                .Select(static reference =>
                    Assert.IsAssignableFrom<PortableExecutableReference>(reference))
                .Concat(additionalReferences ?? [])
                .ToImmutableArray();
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                trees,
                references,
                compilationOptions ?? new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();
            Assert.Empty(errors);

            using MemoryStream peStream = new();
            using MemoryStream? pdbStream = embedPortablePdb ? null : new MemoryStream();
            ImmutableArray<EmbeddedText> embeddedTexts = trees
                .Where((_, index) => embedSources
                    || embeddedSourceOrdinals?.Contains(index) == true)
                .Select(static tree => EmbeddedText.FromSource(
                    tree.FilePath,
                    tree.GetText()))
                .ToImmutableArray();
            using MemoryStream? sourceLinkStream = sourceLinkJson == null
                ? null
                : new MemoryStream(Encoding.UTF8.GetBytes(sourceLinkJson), writable: false);
            EmitResult emit = compilation.Emit(
                peStream,
                pdbStream,
                options: new EmitOptions(
                    debugInformationFormat: embedPortablePdb
                        ? DebugInformationFormat.Embedded
                        : DebugInformationFormat.PortablePdb,
                    pdbFilePath: Path.Combine(DirectoryPath, candidatePrefix + ".pdb")),
                sourceLinkStream: sourceLinkStream,
                embeddedTexts: embeddedTexts);
            Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

            string targetPath = Path.Combine(
                DirectoryPath,
                candidatePrefix + ".target-candidate");
            string pdbPath = Path.Combine(DirectoryPath, candidatePrefix + ".pdb-candidate");
            File.WriteAllBytes(targetPath, peStream.ToArray());
            if (pdbStream != null)
            {
                File.WriteAllBytes(pdbPath, pdbStream.ToArray());
            }
            ImmutableArray<string>.Builder sourcePaths =
                ImmutableArray.CreateBuilder<string>(sources.Count);

            for (int index = 0; index < sources.Count; index++)
            {
                string sourcePath = Path.Combine(
                    DirectoryPath,
                    candidatePrefix + $".source-{index}.candidate");
                File.WriteAllBytes(sourcePath, sourceImages[index]);
                sourcePaths.Add(sourcePath);
            }

            byte[] peImage = peStream.ToArray();
            PortableExecutableReference reference = MetadataReference.CreateFromImage(
                ImmutableArray.Create(peImage),
                filePath: Path.Combine(
                    DirectoryPath,
                    "bound-" + Guid.NewGuid().ToString("N") + ".dll"));

            return new PreparedDependency(
                compilation,
                reference,
                peImage,
                targetPath,
                pdbPath,
                references.Select(static reference => reference.FilePath!).ToImmutableArray(),
                sourcePaths.MoveToImmutable(),
                embedSources);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }

        /// <summary>
        /// Creates one checksum-stable source tree.
        /// </summary>
        private static SyntaxTree CreateTree(
            string path,
            string source,
            byte[] image,
            CSharpParseOptions parseOptions,
            Encoding? encoding)
        {
            SourceText text = encoding == null
                ? SourceText.From(
                    image,
                    image.Length,
                    Encoding.UTF8,
                    SourceHashAlgorithm.Sha256,
                    throwIfBinaryDetected: false,
                    canBeEmbedded: true)
                : SourceText.From(
                    "\uFEFF" + source,
                    encoding,
                    SourceHashAlgorithm.Sha256);
            return CSharpSyntaxTree.ParseText(text, parseOptions, path);
        }

        /// <summary>
        /// Encodes one exact source candidate, including a requested BOM.
        /// </summary>
        private static byte[] CreateSourceImage(string source, Encoding? encoding)
        {
            if (encoding == null)
            {
                return Encoding.UTF8.GetBytes(source);
            }

            return encoding.GetBytes("\uFEFF" + source);
        }

        /// <summary>
        /// Stores one emitted binary and the explicit inputs needed to prepare
        /// a P6C plan later.
        /// </summary>
        internal sealed record PreparedDependency(
            CSharpCompilation Original,
            PortableExecutableReference Reference,
            byte[] PeImage,
            string TargetPath,
            string PdbPath,
            ImmutableArray<string> ReferencePaths,
            ImmutableArray<string> SourcePaths,
            bool UsesEmbeddedSources)
        {
            /// <summary>
            /// Creates a plan bound to the descriptor obtained from the actual
            /// consuming compilation.
            /// </summary>
            public ExternalSupportingSourceReconstructionPlan CreatePlan(
                ExternalAssemblyReferenceDescriptor descriptor,
                string? targetPath = null,
                string? pdbPath = null,
                IEnumerable<string>? referencePaths = null,
                IEnumerable<ExternalSourceReconstructionInput>? sourceInputs = null)
            {
                return new ExternalSupportingSourceReconstructionPlan(
                    descriptor,
                    targetPath ?? TargetPath,
                    pdbPath ?? PdbPath,
                    referencePaths ?? ReferencePaths,
                    sourceInputs ?? SourcePaths.Select((path, index) =>
                        new ExternalSourceReconstructionInput(
                            index,
                            UsesEmbeddedSources ? null : path)));
            }

            /// <summary>
            /// Creates a plan that keeps target, PDB, and source candidates
            /// explicit while opting reference ordinals into P7A discovery.
            /// </summary>
            public ExternalSupportingSourceReconstructionPlan CreateDiscoveryPlan(
                ExternalAssemblyReferenceDescriptor descriptor)
            {
                return ExternalSupportingSourceReconstructionPlan
                    .CreateWithLocalReferenceDiscovery(
                        descriptor,
                        TargetPath,
                        PdbPath,
                        SourcePaths.Select((path, index) =>
                            new ExternalSourceReconstructionInput(
                                index,
                                UsesEmbeddedSources ? null : path)));
            }

            /// <summary>
            /// Creates a P7A/P7B plan that discovers references and permits
            /// controlled acquisition for every source-tree ordinal.
            /// </summary>
            public ExternalSupportingSourceReconstructionPlan CreateAcquisitionPlan(
                ExternalAssemblyReferenceDescriptor descriptor)
            {
                return ExternalSupportingSourceReconstructionPlan
                    .CreateWithLocalReferenceDiscovery(
                        descriptor,
                        TargetPath,
                        PdbPath,
                        Enumerable.Range(0, SourcePaths.Length)
                            .Select(ExternalSourceReconstructionInput.CreateAcquirable));
            }
        }
    }
}
