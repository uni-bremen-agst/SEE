using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>Verifies bounded local exact Portable PDB acquisition.</summary>
    public sealed class ExternalPortablePdbAcquisitionTests
    {
        /// <summary>Configuration snapshots paths without touching artifacts.</summary>
        [Fact]
        public void Configuration_PerformsNoArtifactIo()
        {
            string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Assert.True(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                [Path.Combine(missing, "known.pdb")],
                [Path.Combine(missing, "root")],
                [Path.Combine(missing, "symbols.snupkg")],
                out ExternalPortablePdbAcquisitionConfiguration configuration));
            Assert.Single(configuration.KnownPdbPaths);
            Assert.Single(configuration.SearchRoots);
            Assert.Single(configuration.PackageArchivePaths);
        }

        /// <summary>Relative configuration paths fail closed.</summary>
        [Fact]
        public void Configuration_RequiresAbsolutePaths()
        {
            Assert.False(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                ["relative.pdb"],
                [],
                [],
                out _));
        }

        /// <summary>Only NuGet package container extensions are accepted.</summary>
        [Fact]
        public void Configuration_RejectsArbitraryArchiveTypes()
        {
            string archive = Path.Combine(Path.GetTempPath(), "symbols.zip");

            Assert.False(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                [],
                [],
                [archive],
                out _));
        }

        /// <summary>An embedded Portable PDB is the zero-search fast path.</summary>
        [Fact]
        public void EmbeddedPortablePdb_IsValidatedFromTargetPe()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create(embedPortablePdb: true);
            ExternalPortablePdbAcquisition acquisition = new();

            Assert.True(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out ExternalCompilationProvenanceDescriptor provenance,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.Embedded, origin);
            Assert.NotEmpty(provenance.PortablePdb.Documents);
            Assert.Equal(0, acquisition.GetStatistics().CandidatesConsidered);
            Assert.Equal(0, acquisition.GetStatistics().CandidatesOpened);
            Assert.Equal(1, acquisition.GetStatistics().ValidationAttempts);
            Assert.Equal(0, acquisition.GetStatistics().LocalSymbolPackagesInspected);
        }

        /// <summary>An exact sibling candidate is accepted by P4B.</summary>
        [Fact]
        public void ExactSiblingPdb_IsFound()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            File.WriteAllBytes(fixture.SiblingPath, fixture.PdbImage);

            Assert.True(new ExternalPortablePdbAcquisition().TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.Sibling, origin);
        }

        /// <summary>A same-name wrong sibling never establishes identity.</summary>
        [Fact]
        public void WrongSiblingPdb_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            using AcquisitionFixture wrong = AcquisitionFixture.Create(
                "G2.Wrong.Sibling",
                source: "public sealed class WrongSibling { public void M() { } }");
            File.WriteAllBytes(fixture.SiblingPath, wrong.PdbImage);

            Assert.False(new ExternalPortablePdbAcquisition().TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>An explicitly known local path is probed before sibling discovery.</summary>
        [Fact]
        public void ExactKnownPath_IsFound()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition acquisition = fixture.Configure(
                knownPaths: [fixture.Dependency.PdbPath]);

            Assert.True(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.KnownPath, origin);
        }

        /// <summary>An exact expected filename in a configured root is found directly.</summary>
        [Fact]
        public void ExactConfiguredRootPdb_IsFound()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string root = fixture.CreateDirectory("pdb-root");
            File.WriteAllBytes(Path.Combine(root, fixture.PdbName), fixture.PdbImage);
            ExternalPortablePdbAcquisition acquisition = fixture.Configure(searchRoots: [root]);

            Assert.True(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.ConfiguredRoot, origin);
        }

        /// <summary>An exact Portable PDB can be read from a configured nupkg.</summary>
        [Fact]
        public void ExactNuGetPackagePdb_IsFound()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string archive = fixture.CreateArchive(
                "candidate.nupkg",
                [("lib/net8.0/" + fixture.PdbName, fixture.PdbImage)]);

            Assert.True(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.NuGetPackage, origin);
        }

        /// <summary>An exact Portable PDB can be read from a configured snupkg.</summary>
        [Fact]
        public void ExactSymbolPackagePdb_IsFound()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string archive = fixture.CreateArchive(
                "candidate.snupkg",
                [("lib/net8.0/" + fixture.PdbName, fixture.PdbImage)]);

            Assert.True(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.SymbolPackage, origin);
        }

        /// <summary>A wrong PDB in the expected package location is rejected.</summary>
        [Fact]
        public void WrongSymbolPackagePdb_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            using AcquisitionFixture wrong = AcquisitionFixture.Create(
                "G2.Wrong.Package",
                source: "public sealed class WrongPackage { public void M() { } }");
            string archive = fixture.CreateArchive(
                "wrong.snupkg",
                [("lib/net8.0/" + fixture.PdbName, wrong.PdbImage)]);

            Assert.False(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Malformed PDB bytes are rejected without throwing.</summary>
        [Fact]
        public void MalformedPdb_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string malformed = fixture.Write("malformed.pdb", [1, 2, 3, 4]);

            Assert.False(fixture.Configure(knownPaths: [malformed]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>A malformed archive is skipped without throwing.</summary>
        [Fact]
        public void MalformedArchive_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string malformed = fixture.Write("malformed.snupkg", [1, 2, 3, 4]);

            Assert.False(fixture.Configure(archives: [malformed]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Archive traversal entries invalidate the archive.</summary>
        [Fact]
        public void ArchiveTraversal_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string archive = fixture.CreateArchive(
                "traversal.snupkg",
                [("../" + fixture.PdbName, fixture.PdbImage)]);

            Assert.False(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Rooted archive entries invalidate the entire archive.</summary>
        [Fact]
        public void RootedArchiveEntry_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string archive = fixture.CreateArchive(
                "rooted.snupkg",
                [("/absolute/" + fixture.PdbName, fixture.PdbImage)]);

            Assert.False(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>An oversized declared archive entry is rejected before reading.</summary>
        [Fact]
        public void OversizedArchiveEntry_FailsClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string archive = fixture.CreateArchive(
                "oversized.snupkg",
                [(fixture.PdbName, fixture.PdbImage)]);
            fixture.SetCentralDirectoryUncompressedSize(
                archive,
                (uint)ExternalPortablePdbAcquisition.MaximumPortablePdbBytes + 1U);

            Assert.False(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Duplicate archive entry names invalidate the archive.</summary>
        [Fact]
        public void DuplicateArchiveEntries_FailClosed()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string entry = "lib/net8.0/" + fixture.PdbName;
            string archive = fixture.CreateArchive(
                "duplicate.snupkg",
                [(entry, fixture.PdbImage), (entry, fixture.PdbImage)]);

            Assert.False(fixture.Configure(archives: [archive]).TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Identical exact files in one source tier are not ambiguous.</summary>
        [Fact]
        public void DuplicateExactKnownCandidates_AreNotAmbiguous()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            string duplicate = fixture.Write("duplicate.pdb", fixture.PdbImage);

            Assert.True(fixture.Configure(
                knownPaths: [fixture.Dependency.PdbPath, duplicate]).TryAcquire(
                    fixture.DebugDirectory,
                    fixture.Dependency.TargetPath,
                    out _,
                    out ExternalPortablePdbOrigin origin));
            Assert.Equal(ExternalPortablePdbOrigin.KnownPath, origin);
        }

        /// <summary>A positive result is reused without reopening candidates.</summary>
        [Fact]
        public void PositiveResult_IsCached()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition acquisition = fixture.Configure(
                knownPaths: [fixture.Dependency.PdbPath]);

            Assert.True(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            long opened = acquisition.GetStatistics().CandidatesOpened;
            Assert.True(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            Assert.Equal(opened, acquisition.GetStatistics().CandidatesOpened);
            Assert.Equal(1, acquisition.GetStatistics().PositiveCacheHits);
        }

        /// <summary>Concurrent identical requests share one completed lookup.</summary>
        [Fact]
        public async Task ConcurrentRequests_ShareOneLookup()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition acquisition = fixture.Configure(
                knownPaths: [fixture.Dependency.PdbPath]);
            using ManualResetEventSlim start = new(false);
            Task<bool>[] requests = Enumerable.Range(0, 16)
                .Select(index => Task.Run(() =>
                {
                    _ = index;
                    start.Wait();
                    return acquisition.TryAcquire(
                        fixture.DebugDirectory,
                        fixture.Dependency.TargetPath,
                        out _,
                        out _);
                }))
                .ToArray();

            start.Set();
            bool[] results = await Task.WhenAll(requests);

            Assert.All(results, Assert.True);
            Assert.Equal(1, acquisition.GetStatistics().ValidationAttempts);
            Assert.Equal(15, acquisition.GetStatistics().PositiveCacheHits);
        }

        /// <summary>A negative result is reused without repeating source probes.</summary>
        [Fact]
        public void NegativeResult_IsCached()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition acquisition = fixture.Configure();

            Assert.False(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            long opened = acquisition.GetStatistics().CandidatesOpened;
            Assert.False(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            Assert.Equal(opened, acquisition.GetStatistics().CandidatesOpened);
            Assert.Equal(1, acquisition.GetStatistics().NegativeCacheHits);
        }

        /// <summary>Independent contexts observe independent local source snapshots.</summary>
        [Fact]
        public void Acquisition_IsContextLocal()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition first = fixture.Configure(
                knownPaths: [fixture.Dependency.PdbPath]);
            ExternalPortablePdbAcquisition second = fixture.Configure();

            Assert.True(first.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            Assert.False(second.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
        }

        /// <summary>Configuration becomes immutable after the first lookup.</summary>
        [Fact]
        public void Configuration_IsImmutableAfterLookup()
        {
            using AcquisitionFixture fixture = AcquisitionFixture.Create();
            ExternalPortablePdbAcquisition acquisition = fixture.Configure();
            Assert.False(acquisition.TryAcquire(
                fixture.DebugDirectory,
                fixture.Dependency.TargetPath,
                out _,
                out _));
            Assert.True(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                [fixture.Dependency.PdbPath],
                [],
                [],
                out ExternalPortablePdbAcquisitionConfiguration changed));

            Assert.False(acquisition.TryConfigure(changed));
        }

        /// <summary>Creates exact PE/PDB fixtures and bounded local sources.</summary>
        private sealed class AcquisitionFixture : IDisposable
        {
            /// <summary>Initializes one fixture.</summary>
            /// <param name="workspace">The isolated workspace.</param>
            /// <param name="dependency">The emitted dependency.</param>
            /// <param name="debugDirectory">The exact P4A descriptor.</param>
            private AcquisitionFixture(
                ExternalReconstructionPlanTestWorkspace workspace,
                ExternalReconstructionPlanTestWorkspace.PreparedDependency dependency,
                ExternalPeDebugDirectoryDescriptor debugDirectory)
            {
                Workspace = workspace;
                Dependency = dependency;
                DebugDirectory = debugDirectory;
                ExternalCodeViewPdbReference codeView = Assert.Single(
                    debugDirectory.CodeViewPdbReferences,
                    static reference => reference.IsPortable);
                PdbName = GetFileName(codeView.Path);
                SiblingPath = Path.Combine(
                    Path.GetDirectoryName(dependency.TargetPath)!,
                    PdbName);
            }

            /// <summary>Gets the isolated workspace.</summary>
            private ExternalReconstructionPlanTestWorkspace Workspace { get; }

            /// <summary>Gets the emitted dependency.</summary>
            public ExternalReconstructionPlanTestWorkspace.PreparedDependency Dependency { get; }

            /// <summary>Gets exact PE debug provenance.</summary>
            public ExternalPeDebugDirectoryDescriptor DebugDirectory { get; }

            /// <summary>Gets the safe CodeView-derived PDB filename.</summary>
            public string PdbName { get; }

            /// <summary>Gets the candidate sibling path.</summary>
            public string SiblingPath { get; }

            /// <summary>Gets the standalone Portable PDB bytes.</summary>
            public byte[] PdbImage => File.ReadAllBytes(Dependency.PdbPath);

            /// <summary>Creates one portable or embedded PDB fixture.</summary>
            /// <param name="assemblyName">The emitted assembly name.</param>
            /// <param name="embedPortablePdb">Whether the PDB is embedded.</param>
            /// <returns>The prepared fixture.</returns>
            public static AcquisitionFixture Create(
                string assemblyName = "G2.Expected",
                bool embedPortablePdb = false,
                string source = "public sealed class Expected { }")
            {
                ExternalReconstructionPlanTestWorkspace workspace = new();
                try
                {
                    ExternalReconstructionPlanTestWorkspace.PreparedDependency dependency =
                        workspace.Prepare(
                            assemblyName,
                            [new SourceInput("/_/Expected.cs", source)],
                            embedPortablePdb: embedPortablePdb);
                    CSharpCompilation host = CSharpCompilation.Create(
                        "G2.Host",
                        references: MetadataReferences.Default.Append(dependency.Reference),
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                    IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                        host.GetAssemblyOrModuleSymbol(dependency.Reference));
                    Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                        host,
                        assembly,
                        out ExternalAssemblyReferenceDescriptor target));
                    Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                        target,
                        dependency.TargetPath,
                        out ExternalPeDebugDirectoryDescriptor debugDirectory));
                    return new AcquisitionFixture(workspace, dependency, debugDirectory);
                }
                catch
                {
                    workspace.Dispose();
                    throw;
                }
            }

            /// <summary>Creates and configures a context-local acquisition catalog.</summary>
            /// <param name="knownPaths">The known candidate paths.</param>
            /// <param name="searchRoots">The configured roots.</param>
            /// <param name="archives">The configured package archives.</param>
            /// <returns>The configured catalog.</returns>
            public ExternalPortablePdbAcquisition Configure(
                IEnumerable<string>? knownPaths = null,
                IEnumerable<string>? searchRoots = null,
                IEnumerable<string>? archives = null)
            {
                Assert.True(ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                    knownPaths ?? [],
                    searchRoots ?? [],
                    archives ?? [],
                    out ExternalPortablePdbAcquisitionConfiguration configuration));
                ExternalPortablePdbAcquisition acquisition = new();
                Assert.True(acquisition.TryConfigure(configuration));
                return acquisition;
            }

            /// <summary>Creates a child directory.</summary>
            /// <param name="name">The directory name.</param>
            /// <returns>The absolute directory path.</returns>
            public string CreateDirectory(string name)
            {
                string path = Path.Combine(Workspace.DirectoryPath, name);
                Directory.CreateDirectory(path);
                return path;
            }

            /// <summary>Writes one local fixture file.</summary>
            /// <param name="name">The local filename.</param>
            /// <param name="content">The file content.</param>
            /// <returns>The absolute path.</returns>
            public string Write(string name, byte[] content)
            {
                string path = Path.Combine(Workspace.DirectoryPath, name);
                File.WriteAllBytes(path, content);
                return path;
            }

            /// <summary>Creates one package archive without extracting it.</summary>
            /// <param name="name">The archive filename.</param>
            /// <param name="entries">The entry names and bytes.</param>
            /// <returns>The absolute archive path.</returns>
            public string CreateArchive(
                string name,
                IReadOnlyList<(string Name, byte[] Content)> entries)
            {
                string path = Path.Combine(Workspace.DirectoryPath, name);
                using FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write);
                using ZipArchive archive = new(stream, ZipArchiveMode.Create);
                foreach ((string entryName, byte[] content) in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(entryName);
                    using Stream entryStream = entry.Open();
                    entryStream.Write(content);
                }

                return path;
            }

            /// <summary>Changes the first central-directory declared size.</summary>
            /// <param name="path">The ZIP archive path.</param>
            /// <param name="size">The replacement uncompressed size.</param>
            public void SetCentralDirectoryUncompressedSize(string path, uint size)
            {
                byte[] image = File.ReadAllBytes(path);
                ReadOnlySpan<byte> signature = [0x50, 0x4B, 0x01, 0x02];
                int offset = image.AsSpan().IndexOf(signature);
                Assert.True(offset >= 0);
                BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(offset + 24, 4), size);
                File.WriteAllBytes(path, image);
            }

            /// <summary>Disposes the isolated workspace.</summary>
            public void Dispose()
            {
                Workspace.Dispose();
            }

            /// <summary>Extracts a filename from either slash convention.</summary>
            /// <param name="path">The CodeView path.</param>
            /// <returns>The final path component.</returns>
            private static string GetFileName(string path)
            {
                int separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                return separator < 0 ? path : path[(separator + 1)..];
            }
        }
    }
}
