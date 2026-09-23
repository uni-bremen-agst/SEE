using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using PreparedDependency = XMLDocNormalizerTests.Helpers.ExternalReconstructionPlanTestWorkspace.PreparedDependency;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>Tests bounded remote artifact discovery without weakening P5.</summary>
    public sealed class ExternalRemoteReferenceAcquisitionTests
    {
        private static readonly Uri ServiceIndex = new("https://feed.test/v3/index.json");
        private const string PackageBase = "https://feed.test/flat/";

        [Fact]
        public void LocalExact_WinsWithoutRemoteRequest()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            string root = Path.Combine(fixture.Workspace.DirectoryPath, "local");
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, fixture.Expected.Name), fixture.Candidate.PeImage);
            RecordingHandler handler = new(new Dictionary<string, byte[]>());
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);
            ExternalBinaryCandidateDiscovery discovery = new();
            Assert.True(discovery.TryConfigure([root]));

            Assert.True(discovery.TryAcquireReferenceMaterial(
                fixture.Expected,
                expectedReferenceOrdinal: 0,
                remote,
                out _,
                out ExternalReferenceArtifactSourceKind source,
                out ExternalRemoteReferenceProvenance? provenance));

            Assert.Equal(ExternalReferenceArtifactSourceKind.ExplicitRoot, source);
            Assert.Null(provenance);
            Assert.Equal(0, handler.RequestCount);
            Assert.Equal(0, remote.GetStatistics().RemoteRequestCount);
        }

        [Fact]
        public void KnownPackageCoordinate_ExactEntryBecomesRemoteExactAndIsCached()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Renamed.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);
            ExternalBinaryCandidateDiscovery discovery = new();

            Assert.True(discovery.TryAcquireReferenceMaterial(
                fixture.Expected,
                expectedReferenceOrdinal: 7,
                remote,
                out ValidatedExternalMetadataReferenceMaterial material,
                out ExternalReferenceArtifactSourceKind source,
                out ExternalRemoteReferenceProvenance? provenance));
            Assert.True(discovery.TryAcquireReferenceMaterial(
                fixture.Expected,
                expectedReferenceOrdinal: 9,
                remote,
                out _,
                out _,
                out ExternalRemoteReferenceProvenance? cachedProvenance));

            Assert.Equal(fixture.Candidate.PeImage, material.Image.ToArray());
            Assert.Equal(ExternalReferenceArtifactSourceKind.RemoteNuGetPackage, source);
            Assert.NotNull(provenance);
            Assert.Equal(7, provenance.ExpectedReferenceOrdinal);
            Assert.NotNull(cachedProvenance);
            Assert.Equal(9, cachedProvenance.ExpectedReferenceOrdinal);
            Assert.Equal("lib/net8.0/Renamed.dll", provenance.ArchiveEntry);
            Assert.True(provenance.P5ValidationSucceeded);
            Assert.Equal(4, handler.RequestCount);
            Assert.Equal(1, remote.GetStatistics().RemoteExactCount);
            Assert.True(remote.GetStatistics().CacheHits >= 1);
        }

        [Fact]
        public void PackageSearch_WrongFirstCandidateIsRejectedAndLaterExactCandidateWins()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            using ReferenceFixture wrong = ReferenceFixture.Create("G4B.Wrong");
            byte[] wrongPackage = CreatePackage(("lib/net8.0/Wrong.dll", wrong.Candidate.PeImage));
            byte[] exactPackage = CreatePackage(("ref/net8.0/Exact.dll", fixture.Candidate.PeImage));
            string searchJson = """
                {"data":[
                  {"id":"A.Wrong","versions":[{"version":"1.0.0"}]},
                  {"id":"B.Right","versions":[{"version":"2.0.0"}]}
                ]}
                """;
            RecordingHandler handler = CreateFeedHandler(
                [("a.wrong", "1.0.0", wrongPackage), ("b.right", "2.0.0", exactPackage)],
                searchJson);
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [],
                enableSearch: true);

            Assert.True(remote.TryAcquire(
                fixture.Expected,
                out _,
                out ExternalRemoteReferenceProvenance provenance));

            Assert.Contains("search", provenance.DiscoveryHint, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("b.right", provenance.ArtifactIdentity);
            Assert.Equal(1, remote.GetStatistics().RemoteSearchCount);
            Assert.True(remote.GetStatistics().Providers.Single().RejectedCount >= 1);
        }

        [Fact]
        public void DotNetPack_WrongVersionIsRejectedBeforeExactVersion()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            using ReferenceFixture wrong = ReferenceFixture.Create("G4B.Pack.Wrong");
            byte[] wrongPackage = CreatePackage(("ref/net6.0/RemoteExpected.dll", wrong.Candidate.PeImage));
            byte[] exactPackage = CreatePackage(("ref/net6.0/RemoteExpected.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                [("microsoft.netcore.app.ref", "6.0.15", wrongPackage),
                 ("microsoft.netcore.app.ref", "6.0.14", exactPackage)]);
            ExternalRemoteReferencePackageHint hint = new(
                Path.GetFileNameWithoutExtension(fixture.Expected.Name),
                "Microsoft.NETCore.App.Ref",
                ["6.0.15", "6.0.14"],
                ExternalRemoteArtifactProviderKind.DotNetReferencePack,
                "bounded historical net6.0 pack candidates");
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(handler, [hint]);

            Assert.True(remote.TryAcquire(fixture.Expected, out _, out ExternalRemoteReferenceProvenance provenance));

            Assert.Equal(ExternalRemoteArtifactProviderKind.DotNetReferencePack, provenance.ProviderKind);
            Assert.Equal("6.0.14", provenance.ArtifactVersion);
            Assert.Equal(2, remote.GetStatistics().RemoteArtifactRequestCount);
            Assert.True(remote.GetStatistics().Providers.Single().RejectedCount >= 1);
        }

        [Fact]
        public void TransportHashMismatch_FailsClosedBeforeArchiveInspection()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package),
                corruptHash: true);
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(0, remote.GetStatistics().RemoteP5ValidationAttemptCount);
            Assert.True(remote.GetStatistics().Providers.Single().RejectedCount >= 1);
        }

        [Fact]
        public void MalformedArchive_FailsClosedAndNegativeResultIsCached()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] malformed = Encoding.UTF8.GetBytes("not a zip archive");
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", malformed));
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));

            Assert.Equal(4, handler.RequestCount);
            Assert.True(remote.GetStatistics().CacheHits >= 1);
        }

        [Fact]
        public void TraversalEntry_RejectsWholeArchive()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(
                ("../escape.dll", fixture.Candidate.PeImage),
                ("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(0, remote.GetStatistics().RemoteExactCount);
        }

        [Fact]
        public void OversizedEntry_StopsAtConfiguredLimit()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default with
            {
                MaxArchiveEntryBytes = fixture.Candidate.PeImage.Length - 1
            };
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")],
                limits: limits);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(1, remote.GetStatistics().LimitHitCount);
        }

        [Fact]
        public void PackageLimit_DoesNotEscalateSearchBreadth()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            using ReferenceFixture wrong = ReferenceFixture.Create("G4B.Limit.Wrong");
            byte[] wrongPackage = CreatePackage(("lib/net8.0/Wrong.dll", wrong.Candidate.PeImage));
            byte[] exactPackage = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                [("candidate.package", "1.0.0", wrongPackage),
                 ("candidate.package", "2.0.0", exactPackage)]);
            ExternalRemoteReferencePackageHint hint = new(
                Path.GetFileNameWithoutExtension(fixture.Expected.Name),
                "candidate.package",
                ["1.0.0", "2.0.0"],
                ExternalRemoteArtifactProviderKind.NuGetPackage,
                "test range");
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default with
            {
                MaxPackagesDownloadedPerReference = 1
            };
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(handler, [hint], limits: limits);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));

            Assert.Equal(1, remote.GetStatistics().RemoteArtifactRequestCount);
            Assert.Equal(1, remote.GetStatistics().LimitHitCount);
        }

        [Fact]
        public void RequestLimit_StopsBeforeArtifactDownload()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default with
            {
                MaxRequestsPerReference = 2
            };
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")],
                limits: limits);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(2, handler.RequestCount);
            Assert.Equal(0, remote.GetStatistics().RemoteArtifactRequestCount);
            Assert.Equal(1, remote.GetStatistics().LimitHitCount);
        }

        [Fact]
        public void ConflictingDuplicateArchivePath_RejectsWholeArchive()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(
                ("lib/net8.0/Exact.dll", fixture.Candidate.PeImage),
                ("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(0, remote.GetStatistics().RemoteP5ValidationAttemptCount);
        }

        [Fact]
        public void ParallelDuplicateReference_PerformsOneArtifactDownload()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler handler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferenceAcquisition remote = ConfigureRemote(
                handler,
                [CreateHint(fixture, "candidate.package", "1.0.0")]);
            bool[] results = new bool[8];

            Parallel.For(0, results.Length, index =>
            {
                results[index] = remote.TryAcquire(fixture.Expected, out _, out _);
            });

            Assert.All(results, Assert.True);
            Assert.Equal(4, handler.RequestCount);
            Assert.Equal(1, remote.GetStatistics().RemoteArtifactRequestCount);
        }

        [Fact]
        public void PrivateFeedAddress_IsRejectedBeforeHttpRequest()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            RecordingHandler handler = new(new Dictionary<string, byte[]>());
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default;
            Assert.True(ExternalRemoteReferenceAcquisitionConfiguration.TryCreate(
                ExternalReferenceAcquisitionPolicy.BoundedRemoteArtifacts,
                ServiceIndex,
                [CreateHint(fixture, "candidate.package", "1.0.0")],
                enablePackageSearch: false,
                limits,
                out ExternalRemoteReferenceAcquisitionConfiguration configuration));
            ExternalRemoteReferenceAcquisition remote = new();
            Assert.True(remote.TryConfigure(
                configuration,
                new ExternalSourceLinkClient(
                    handler,
                    PrivateResolver.Instance,
                    limits.Timeout,
                    limits.MaxArtifactBytes)));

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void RequestTimeout_FailsClosed()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default with
            {
                Timeout = TimeSpan.FromMilliseconds(20)
            };
            Assert.True(ExternalRemoteReferenceAcquisitionConfiguration.TryCreate(
                ExternalReferenceAcquisitionPolicy.BoundedRemoteArtifacts,
                ServiceIndex,
                [CreateHint(fixture, "candidate.package", "1.0.0")],
                enablePackageSearch: false,
                limits,
                out ExternalRemoteReferenceAcquisitionConfiguration configuration));
            ExternalRemoteReferenceAcquisition remote = new();
            Assert.True(remote.TryConfigure(
                configuration,
                new ExternalSourceLinkClient(
                    new TimeoutHandler(),
                    PublicResolver.Instance,
                    limits.Timeout,
                    limits.MaxArtifactBytes)));

            Assert.False(remote.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(1, remote.GetStatistics().RemoteRequestCount);
        }

        [Fact]
        public void LocalOnlyAndRemoteEnabledContextsRemainIsolatedAndDemandDriven()
        {
            using ReferenceFixture fixture = ReferenceFixture.Create();
            byte[] package = CreatePackage(("lib/net8.0/Exact.dll", fixture.Candidate.PeImage));
            RecordingHandler localHandler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            RecordingHandler remoteHandler = CreateFeedHandler(
                ("candidate.package", "1.0.0", package));
            ExternalRemoteReferencePackageHint hint = CreateHint(
                fixture,
                "candidate.package",
                "1.0.0");
            ExternalRemoteReferenceAcquisition localOnly = ConfigureRemote(
                localHandler,
                [hint],
                policy: ExternalReferenceAcquisitionPolicy.LocalOnly);
            ExternalRemoteReferenceAcquisition enabled = ConfigureRemote(remoteHandler, [hint]);

            Assert.False(localOnly.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(0, localHandler.RequestCount);
            Assert.Equal(0, remoteHandler.RequestCount);
            Assert.True(enabled.TryAcquire(fixture.Expected, out _, out _));
            Assert.Equal(4, remoteHandler.RequestCount);
        }

        private static ExternalRemoteReferencePackageHint CreateHint(
            ReferenceFixture fixture,
            string packageId,
            string version)
        {
            return new ExternalRemoteReferencePackageHint(
                Path.GetFileNameWithoutExtension(fixture.Expected.Name),
                packageId,
                [version],
                ExternalRemoteArtifactProviderKind.NuGetPackage,
                "known package coordinate");
        }

        private static ExternalRemoteReferenceAcquisition ConfigureRemote(
            RecordingHandler handler,
            IEnumerable<ExternalRemoteReferencePackageHint> hints,
            bool enableSearch = false,
            ExternalReferenceAcquisitionPolicy policy = ExternalReferenceAcquisitionPolicy.BoundedRemoteArtifacts,
            ExternalRemoteReferenceAcquisitionLimits? limits = null)
        {
            ExternalRemoteReferenceAcquisitionLimits actualLimits = limits
                ?? ExternalRemoteReferenceAcquisitionLimits.Default;
            Assert.True(ExternalRemoteReferenceAcquisitionConfiguration.TryCreate(
                policy,
                ServiceIndex,
                hints,
                enableSearch,
                actualLimits,
                out ExternalRemoteReferenceAcquisitionConfiguration configuration));
            ExternalSourceLinkClient client = new(
                handler,
                PublicResolver.Instance,
                actualLimits.Timeout,
                actualLimits.MaxArtifactBytes);
            ExternalRemoteReferenceAcquisition acquisition = new();
            Assert.True(acquisition.TryConfigure(configuration, client));
            return acquisition;
        }

        private static RecordingHandler CreateFeedHandler(
            (string PackageId, string Version, byte[] Bytes) package,
            bool corruptHash = false)
        {
            return CreateFeedHandler([package], searchJson: null, corruptHash);
        }

        private static RecordingHandler CreateFeedHandler(
            IReadOnlyCollection<(string PackageId, string Version, byte[] Bytes)> packages,
            string? searchJson = null,
            bool corruptHash = false)
        {
            Dictionary<string, byte[]> responses = new(StringComparer.OrdinalIgnoreCase)
            {
                [ServiceIndex.AbsoluteUri] = Encoding.UTF8.GetBytes(
                    "{\"resources\":["
                    + "{\"@id\":\"" + PackageBase + "\",\"@type\":\"PackageBaseAddress/3.0.0\"},"
                    + "{\"@id\":\"https://feed.test/search\",\"@type\":\"SearchQueryService/3.5.0\"},"
                    + "{\"@id\":\"https://feed.test/registration/\",\"@type\":\"RegistrationsBaseUrl/3.6.0\"}]}" )
            };
            if (searchJson != null)
            {
                responses["https://feed.test/search"] = Encoding.UTF8.GetBytes(searchJson);
            }

            foreach ((string packageId, string version, byte[] bytes) in packages)
            {
                string id = packageId.ToLowerInvariant();
                string normalizedVersion = version.ToLowerInvariant();
                string fileName = $"{id}.{normalizedVersion}.nupkg";
                string root = $"{PackageBase}{id}/{normalizedVersion}/{fileName}";
                string catalog = $"https://feed.test/catalog/{id}.{normalizedVersion}.json";
                string registration = $"https://feed.test/registration/{id}/{normalizedVersion}.json";
                byte[] hash = corruptHash ? new byte[64] : SHA512.HashData(bytes);
                responses[registration] = Encoding.UTF8.GetBytes(
                    "{\"catalogEntry\":\"" + catalog + "\",\"packageContent\":\"" + root + "\"}");
                responses[catalog] = Encoding.UTF8.GetBytes(
                    "{\"packageHashAlgorithm\":\"SHA512\",\"packageHash\":\""
                    + Convert.ToBase64String(hash) + "\",\"packageSize\":" + bytes.Length + "}");
                responses[root] = bytes;
            }

            return new RecordingHandler(responses);
        }

        private static byte[] CreatePackage(params (string Path, byte[] Bytes)[] entries)
        {
            using MemoryStream stream = new();
            using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach ((string path, byte[] bytes) in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
                    using Stream target = entry.Open();
                    target.Write(bytes, 0, bytes.Length);
                }
            }

            return stream.ToArray();
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly IReadOnlyDictionary<string, byte[]> responses;

            public RecordingHandler(IReadOnlyDictionary<string, byte[]> responses)
            {
                this.responses = responses;
            }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestCount++;
                string key = request.RequestUri!.GetLeftPart(UriPartial.Path);
                HttpResponseMessage response = responses.TryGetValue(key, out byte[]? bytes)
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
                    : new HttpResponseMessage(HttpStatusCode.NotFound);
                return Task.FromResult(response);
            }
        }

        private sealed class PublicResolver : IExternalSourceHostResolver
        {
            public static PublicResolver Instance { get; } = new();

            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(new[] { IPAddress.Parse("93.184.216.34") });
            }
        }

        private sealed class PrivateResolver : IExternalSourceHostResolver
        {
            public static PrivateResolver Instance { get; } = new();

            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(new[] { IPAddress.Loopback });
            }
        }

        private sealed class TimeoutHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private sealed class ReferenceFixture : IDisposable
        {
            private ReferenceFixture(
                ExternalReconstructionPlanTestWorkspace workspace,
                PreparedDependency candidate,
                ExternalCompilationMetadataReferenceDescriptor expected)
            {
                Workspace = workspace;
                Candidate = candidate;
                Expected = expected;
            }

            public ExternalReconstructionPlanTestWorkspace Workspace { get; }
            public PreparedDependency Candidate { get; }
            public ExternalCompilationMetadataReferenceDescriptor Expected { get; }

            public static ReferenceFixture Create(string assemblyName = "G4B.Expected")
            {
                ExternalReconstructionPlanTestWorkspace workspace = new();
                try
                {
                    PreparedDependency candidate = workspace.Prepare(
                        assemblyName,
                        [new SourceInput("/_/Candidate.cs", "public sealed class CandidateApi { }")]);
                    PortableExecutableReference reference = MetadataReference.CreateFromImage(
                        ImmutableArray.Create(candidate.PeImage),
                        filePath: "RemoteExpected.dll");
                    PreparedDependency owner = workspace.Prepare(
                        assemblyName + ".Owner",
                        [new SourceInput("/_/Owner.cs", "public sealed class Owner { }")],
                        additionalReferences: [reference]);
                    return new ReferenceFixture(workspace, candidate, GetExpected(owner));
                }
                catch
                {
                    workspace.Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                Workspace.Dispose();
            }

            private static ExternalCompilationMetadataReferenceDescriptor GetExpected(
                PreparedDependency owner)
            {
                CSharpCompilation host = CSharpCompilation.Create(
                    "G4B.Provenance.Host",
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
                return Assert.IsType<ExternalCompilationMetadataReferencesDescriptor>(
                    provenance.MetadataReferences).References[^1];
            }
        }
    }
}
