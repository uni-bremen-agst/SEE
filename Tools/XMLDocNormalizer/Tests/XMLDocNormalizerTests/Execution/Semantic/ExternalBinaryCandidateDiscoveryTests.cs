using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using FidelityWorkspace = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.FidelityWorkspace;
using PreparedDependency = XMLDocNormalizerTests.Helpers.ExternalReconstructionPlanTestWorkspace.PreparedDependency;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests deterministic local binary discovery while retaining P5 as the
    /// sole candidate-validation boundary.
    /// </summary>
    public sealed class ExternalBinaryCandidateDiscoveryTests
    {
        /// <summary>
        /// Returns no candidate when no roots were configured.
        /// </summary>
        [Fact]
        public void NoRoots_ReturnsNoCandidate()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            ExternalBinaryCandidateDiscovery discovery = new();

            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Normalizes and deduplicates absolute roots while rejecting relative roots.
        /// </summary>
        [Fact]
        public void RootConfiguration_IsImmutableIdempotentAndAbsolute()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            ExternalBinaryCandidateDiscovery discovery = new();
            string duplicate = fixture.Root + Path.DirectorySeparatorChar;

            Assert.True(discovery.TryConfigure([fixture.Root, duplicate]));
            Assert.True(discovery.TryConfigure([duplicate]));
            Assert.False(discovery.TryConfigure(["relative-root"]));
        }

        /// <summary>
        /// Retains an immutable normalized root snapshot after caller mutation.
        /// </summary>
        [Fact]
        public void RootConfiguration_DefensivelySnapshotsCallerList()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", fixture.Image);
            List<string> roots = [fixture.Root];
            ExternalBinaryCandidateDiscovery discovery = new();
            Assert.True(discovery.TryConfigure(roots));
            roots.Clear();

            Assert.True(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Uses the expected filename without enumerating renamed fallback candidates.
        /// </summary>
        [Fact]
        public void ExpectedFilenameFastPath_UsesExactValidatedCandidate()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string expectedPath = fixture.Write("Expected.dll", fixture.Image);
            byte[] ambiguousFallback = fixture.Image.Append((byte)0).ToArray();
            _ = fixture.Write("renamed.dll", ambiguousFallback);
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);

            Assert.True(discovery.TryFindReferenceCandidate(
                fixture.Expected,
                out string candidatePath));
            Assert.Equal(expectedPath, candidatePath);
        }

        /// <summary>
        /// Finds an exact binary under a different plausible filename via fallback.
        /// </summary>
        [Fact]
        public void RenamedValidCandidate_IsFoundByFallback()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string renamed = fixture.Write("random-name.dll", fixture.Image);
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);

            Assert.True(discovery.TryFindReferenceCandidate(fixture.Expected, out string path));
            Assert.Equal(renamed, path);
        }

        /// <summary>
        /// Rejects a candidate from another build with a different MVID.
        /// </summary>
        [Fact]
        public void WrongMvid_IsRejected()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            PreparedDependency wrong = fixture.Workspace.Prepare(
                fixture.CandidateAssemblyName,
                [new SourceInput("/_/Wrong.cs", "public sealed class WrongBuild { }")]);
            _ = fixture.Write("Expected.dll", wrong.PeImage);
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);

            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Leaves timestamp comparison to existing P5 validation.
        /// </summary>
        [Fact]
        public void WrongTimestamp_IsRejected()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", fixture.Image);
            ExternalCompilationMetadataReferenceDescriptor expected = fixture.CloneExpected(
                timestamp: fixture.Expected.Timestamp + 1);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(expected, out _));
        }

        /// <summary>
        /// Leaves PE image-size comparison to existing P5 validation.
        /// </summary>
        [Fact]
        public void WrongImageSize_IsRejected()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", fixture.Image);
            ExternalCompilationMetadataReferenceDescriptor expected = fixture.CloneExpected(
                imageSize: fixture.Expected.ImageSize + 1);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(expected, out _));
        }

        /// <summary>
        /// Rejects an assembly when P5A expects a standalone module.
        /// </summary>
        [Fact]
        public void WrongMetadataKind_IsRejected()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", fixture.Image);
            ExternalCompilationMetadataReferenceDescriptor expected = fixture.CloneExpected(
                kind: MetadataImageKind.Module);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(expected, out _));
        }

        /// <summary>
        /// Rejects another assembly identity through its mismatching concrete build provenance.
        /// </summary>
        [Fact]
        public void DifferentAssemblyIdentity_IsRejectedByP5BuildProvenance()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            PreparedDependency wrong = fixture.Workspace.Prepare(
                "P7A.Different.Identity",
                [new SourceInput("/_/Different.cs", "public sealed class Different { }")]);
            _ = fixture.Write("Expected.dll", wrong.PeImage);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Skips a malformed file with a plausible extension.
        /// </summary>
        [Fact]
        public void MalformedPe_IsSkipped()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", [1, 2, 3, 4]);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Skips an MZ-shaped native/non-managed candidate.
        /// </summary>
        [Fact]
        public void NativeDll_IsSkipped()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            byte[] native = new byte[256];
            native[0] = (byte)'M';
            native[1] = (byte)'Z';
            _ = fixture.Write("Expected.dll", native);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Skips a truncated managed PE candidate.
        /// </summary>
        [Fact]
        public void TruncatedPe_IsSkipped()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("Expected.dll", fixture.Image.Take(128).ToArray());

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Discovers a standalone netmodule without assigning assembly identity.
        /// </summary>
        [Fact]
        public void StandaloneNetModule_IsDiscoveredByP5KindAndMvid()
        {
            using FidelityWorkspace moduleWorkspace = new();
            PortableExecutableReference module = moduleWorkspace.CreateReference(
                "P7A.Module",
                "public sealed class ModuleType { }",
                OutputKind.NetModule);
            using FileStream stream = File.OpenRead(module.FilePath!);
            using PEReader reader = new(stream);
            MetadataReader metadata = reader.GetMetadataReader();
            ModuleDefinition definition = metadata.GetModuleDefinition();
            ExternalCompilationMetadataReferenceDescriptor expected = new(
                Path.GetFileName(module.FilePath!)!,
                ImmutableArray<string>.Empty,
                MetadataImageKind.Module,
                embedInteropTypes: false,
                reader.PEHeaders.CoffHeader.TimeDateStamp,
                reader.PEHeaders.PEHeader!.SizeOfImage,
                metadata.GetGuid(definition.Mvid));
            ExternalBinaryCandidateDiscovery discovery = Configure(moduleWorkspace.DirectoryPath);

            Assert.Equal(MetadataImageKind.Module, expected.Kind);
            Assert.True(discovery.TryFindReferenceCandidate(expected, out string path));
            Assert.Equal(module.FilePath, path);
        }

        /// <summary>
        /// Selects the original reference-assembly build rather than a same-name implementation.
        /// </summary>
        [Fact]
        public void ReferenceAssemblyAndImplementationAssembly_SelectExactBuild()
        {
            using CandidateFixture fixture = CandidateFixture.Create(
                candidateSource: "using System.Runtime.CompilerServices; "
                    + "[assembly: ReferenceAssembly] public sealed class Api { }");
            string referenceRoot = fixture.CreateRoot("ref");
            string runtimeRoot = fixture.CreateRoot("runtime");
            string exactPath = fixture.Write(referenceRoot, "Expected.dll", fixture.Image);
            PreparedDependency implementation = fixture.Workspace.Prepare(
                fixture.CandidateAssemblyName,
                [new SourceInput("/_/Implementation.cs", "public sealed class Api { }")]);
            _ = fixture.Write(runtimeRoot, "Expected.dll", implementation.PeImage);
            ExternalBinaryCandidateDiscovery discovery = Configure(referenceRoot, runtimeRoot);

            Assert.True(discovery.TryFindReferenceCandidate(fixture.Expected, out string path));
            Assert.Equal(exactPath, path);
        }

        /// <summary>
        /// Collapses identical binary snapshots at multiple paths.
        /// </summary>
        [Fact]
        public void SameBinaryAtTwoRenamedPaths_IsNotAmbiguous()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string second = fixture.Write("z-copy.dll", fixture.Image);
            string first = fixture.Write("a-copy.dll", fixture.Image);

            Assert.True(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out string path));
            Assert.Equal(first, path);
            Assert.NotEqual(second, path);
        }

        /// <summary>
        /// Chooses the exact build among two equal AssemblyIdentity values with different MVIDs.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentMvid_SelectsExpectedBuild()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string exact = fixture.Write("exact.dll", fixture.Image);
            PreparedDependency rebuilt = fixture.Workspace.Prepare(
                fixture.CandidateAssemblyName,
                [new SourceInput("/_/Rebuilt.cs", "public sealed class Rebuilt { }")]);
            _ = fixture.Write("rebuilt.dll", rebuilt.PeImage);

            Assert.True(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out string path));
            Assert.Equal(exact, path);
        }

        /// <summary>
        /// Evaluates same-name candidates across roots without treating root order as identity.
        /// </summary>
        [Fact]
        public void SameFilenameDifferentBuilds_SelectsOnlyExactBuild()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string firstRoot = fixture.CreateRoot("first");
            string secondRoot = fixture.CreateRoot("second");
            PreparedDependency wrong = fixture.Workspace.Prepare(
                fixture.CandidateAssemblyName,
                [new SourceInput("/_/Wrong.cs", "public sealed class Wrong { }")]);
            _ = fixture.Write(firstRoot, "Expected.dll", wrong.PeImage);
            string exact = fixture.Write(secondRoot, "Expected.dll", fixture.Image);

            Assert.True(Configure(firstRoot, secondRoot).TryFindReferenceCandidate(
                fixture.Expected,
                out string path));
            Assert.Equal(exact, path);
        }

        /// <summary>
        /// Returns no match when plausible candidates all fail P5 validation.
        /// </summary>
        [Fact]
        public void ZeroExactMatches_FailsClosed()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("wrong.dll", [1, 2, 3]);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Rejects differing byte snapshots that both satisfy all P5B provenance fields.
        /// </summary>
        [Fact]
        public void DistinctExactMatches_AreAmbiguous()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            _ = fixture.Write("a.dll", fixture.Image);
            _ = fixture.Write("b.dll", fixture.Image.Append((byte)0).ToArray());

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Produces the same candidate regardless of configured root order.
        /// </summary>
        [Fact]
        public void RootOrder_DoesNotChangeDeterministicResult()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string firstRoot = fixture.CreateRoot("a-root");
            string secondRoot = fixture.CreateRoot("z-root");
            string first = fixture.Write(firstRoot, "copy.dll", fixture.Image);
            _ = fixture.Write(secondRoot, "copy.dll", fixture.Image);

            Assert.True(Configure(secondRoot, firstRoot).TryFindReferenceCandidate(
                fixture.Expected,
                out string reversed));
            Assert.True(Configure(firstRoot, secondRoot).TryFindReferenceCandidate(
                fixture.Expected,
                out string forward));
            Assert.Equal(first, reversed);
            Assert.Equal(first, forward);
        }

        /// <summary>
        /// Treats a missing root as an empty root without aborting discovery.
        /// </summary>
        [Fact]
        public void MissingRoot_IsSkipped()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string missing = Path.Combine(fixture.Workspace.DirectoryPath, "missing-root");

            Assert.False(Configure(missing).TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Skips a candidate that cannot be opened without aborting analysis.
        /// </summary>
        [Fact]
        public void InaccessibleCandidate_IsSkipped()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string path = fixture.Write("Expected.dll", fixture.Image);
            using FileStream exclusive = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

            Assert.False(Configure(fixture.Root).TryFindReferenceCandidate(
                fixture.Expected,
                out _));
        }

        /// <summary>
        /// Does not recurse into nested directories.
        /// </summary>
        [Fact]
        public void NestedCandidate_IsOutsideSearchDepth()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string nested = fixture.CreateRoot(Path.Combine("root", "nested"));
            _ = fixture.Write(nested, "renamed.dll", fixture.Image);
            string root = Path.GetDirectoryName(nested)!;

            Assert.False(Configure(root).TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Caches a negative result rather than rescanning after filesystem changes.
        /// </summary>
        [Fact]
        public void NegativeResult_IsCachedExactlyOnce()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);
            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
            _ = fixture.Write("Expected.dll", fixture.Image);

            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
        }

        /// <summary>
        /// Caches a positive deterministic path without reopening the file.
        /// </summary>
        [Fact]
        public void PositiveResult_IsCachedExactlyOnce()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string expectedPath = fixture.Write("Expected.dll", fixture.Image);
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);
            Assert.True(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
            File.Delete(expectedPath);

            Assert.True(discovery.TryFindReferenceCandidate(
                fixture.Expected,
                out string cachedPath));
            Assert.Equal(expectedPath, cachedPath);
        }

        /// <summary>
        /// Reuses binary discovery across duplicate expected entries whose
        /// aliases and EmbedInteropTypes properties differ.
        /// </summary>
        [Fact]
        public void ReferenceOnlyProperties_DoNotSplitBinaryCache()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string path = fixture.Write("Expected.dll", fixture.Image);
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);
            Assert.True(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
            File.Delete(path);
            ExternalCompilationMetadataReferenceDescriptor aliased = new(
                "renamed-hint.dll",
                ImmutableArray.Create("alias"),
                fixture.Expected.Kind,
                embedInteropTypes: true,
                fixture.Expected.Timestamp,
                fixture.Expected.ImageSize,
                fixture.Expected.ModuleVersionId);

            Assert.True(discovery.TryFindReferenceCandidate(aliased, out string cached));
            Assert.Equal(path, cached);
        }

        /// <summary>
        /// Caches ambiguity rather than accepting a later filesystem change.
        /// </summary>
        [Fact]
        public void AmbiguousResult_IsCachedExactlyOnce()
        {
            using CandidateFixture fixture = CandidateFixture.Create();
            string first = fixture.Write("a.dll", fixture.Image);
            string second = fixture.Write("b.dll", fixture.Image.Append((byte)0).ToArray());
            ExternalBinaryCandidateDiscovery discovery = Configure(fixture.Root);
            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
            File.Delete(second);
            Assert.True(File.Exists(first));

            Assert.False(discovery.TryFindReferenceCandidate(fixture.Expected, out _));
        }

        private static ExternalBinaryCandidateDiscovery Configure(params string[] roots)
        {
            ExternalBinaryCandidateDiscovery discovery = new();
            Assert.True(discovery.TryConfigure(roots));
            return discovery;
        }

        private static ExternalCompilationMetadataReferenceDescriptor GetLastExpected(
            PreparedDependency owner)
        {
            return GetExpected(owner, kind: null);
        }

        private static ExternalCompilationMetadataReferenceDescriptor GetExpected(
            PreparedDependency owner,
            MetadataImageKind? kind)
        {
            CSharpCompilation host = CSharpCompilation.Create(
                "P7A.Provenance.Host",
                references: MetadataReferences.Default.Append(owner.Reference),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                host.GetAssemblyOrModuleSymbol(owner.Reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                host,
                assembly,
                out ExternalAssemblyReferenceDescriptor target));
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                target,
                owner.TargetPath,
                out ExternalPeDebugDirectoryDescriptor debug));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(
                debug,
                owner.PdbPath,
                out ExternalCompilationProvenanceDescriptor provenance));
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> references =
                Assert.IsType<ExternalCompilationMetadataReferencesDescriptor>(
                    provenance.MetadataReferences).References;
            return kind.HasValue
                ? Assert.Single(references, reference => reference.Kind == kind.Value)
                : references[^1];
        }

        private sealed class CandidateFixture : IDisposable
        {
            private CandidateFixture(
                ExternalReconstructionPlanTestWorkspace workspace,
                PreparedDependency candidate,
                ExternalCompilationMetadataReferenceDescriptor expected,
                string root)
            {
                Workspace = workspace;
                Candidate = candidate;
                Expected = expected;
                Root = root;
            }

            public ExternalReconstructionPlanTestWorkspace Workspace { get; }

            public PreparedDependency Candidate { get; }

            public ExternalCompilationMetadataReferenceDescriptor Expected { get; }

            public string Root { get; }

            public string CandidateAssemblyName => Candidate.Original.AssemblyName!;

            public byte[] Image => Candidate.PeImage;

            public static CandidateFixture Create(
                string candidateSource = "public sealed class CandidateApi { }")
            {
                ExternalReconstructionPlanTestWorkspace workspace = new();

                try
                {
                    PreparedDependency candidate = workspace.Prepare(
                        "P7A.Expected.Assembly",
                        [new SourceInput("/_/Candidate.cs", candidateSource)]);
                    PortableExecutableReference reference = MetadataReference.CreateFromImage(
                        ImmutableArray.Create(candidate.PeImage),
                        filePath: "Expected.dll");
                    PreparedDependency owner = workspace.Prepare(
                        "P7A.Owner",
                        [new SourceInput("/_/Owner.cs", "public sealed class Owner { }")],
                        additionalReferences: [reference]);
                    string root = Path.Combine(workspace.DirectoryPath, "search-root");
                    Directory.CreateDirectory(root);
                    return new CandidateFixture(
                        workspace,
                        candidate,
                        GetLastExpected(owner),
                        root);
                }
                catch
                {
                    workspace.Dispose();
                    throw;
                }
            }

            public ExternalCompilationMetadataReferenceDescriptor CloneExpected(
                MetadataImageKind? kind = null,
                int? timestamp = null,
                int? imageSize = null)
            {
                return new ExternalCompilationMetadataReferenceDescriptor(
                    Expected.Name,
                    Expected.Aliases,
                    kind ?? Expected.Kind,
                    Expected.EmbedInteropTypes,
                    timestamp ?? Expected.Timestamp,
                    imageSize ?? Expected.ImageSize,
                    Expected.ModuleVersionId);
            }

            public string CreateRoot(string relativePath)
            {
                string path = Path.Combine(Workspace.DirectoryPath, relativePath);
                Directory.CreateDirectory(path);
                return path;
            }

            public string Write(string fileName, byte[] image)
            {
                return Write(Root, fileName, image);
            }

            public string Write(string root, string fileName, byte[] image)
            {
                string path = Path.Combine(root, fileName);
                File.WriteAllBytes(path, image);
                return path;
            }

            public void Dispose()
            {
                Workspace.Dispose();
            }
        }
    }
}
