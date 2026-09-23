using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Acquires bounded NuGet package candidates after local discovery fails
    /// and accepts binary bytes only through the unchanged P5 boundary.
    /// </summary>
    internal sealed class ExternalRemoteReferenceAcquisition
    {
        /// <summary>Serializes mutable context-local acquisition state.</summary>
        private readonly object gate = new();
        /// <summary>Caches exact and unavailable outcomes by authoritative P5 identity.</summary>
        private readonly Dictionary<ReferenceKey, ReferenceEntry> referenceCache = new();
        /// <summary>Caches hash-verified package artifacts by exact coordinate.</summary>
        private readonly Dictionary<ArtifactKey, ArtifactEntry> artifactCache = new();
        /// <summary>Caches bounded package-search results by assembly simple name.</summary>
        private readonly Dictionary<string, ImmutableArray<PackageSearchResult>> searchCache =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Accumulates context-local work by provider.</summary>
        private readonly Dictionary<ExternalRemoteArtifactProviderKind, MutableStatistics> providerStatistics = new();
        /// <summary>Stores the immutable configuration after successful setup.</summary>
        private ExternalRemoteReferenceAcquisitionConfiguration? configuration;
        /// <summary>Stores the bounded hardened HTTPS client after setup.</summary>
        private ExternalSourceLinkClient? client;
        /// <summary>Caches the parsed NuGet V3 endpoints.</summary>
        private NuGetEndpoints? endpoints;
        /// <summary>Tracks whether service-index acquisition has already been attempted.</summary>
        private bool endpointsAttempted;
        /// <summary>Tracks whether the context has accepted a configuration.</summary>
        private bool configured;
        /// <summary>Tracks whether any acquisition attempt has started.</summary>
        private bool acquisitionStarted;
        /// <summary>Counts all context-local remote requests.</summary>
        private long requestCount;
        /// <summary>Counts all context-local downloaded response bytes.</summary>
        private long downloadedBytes;

        /// <summary>Configures this context without performing network I/O.</summary>
        /// <param name="remoteConfiguration">The validated immutable configuration.</param>
        /// <param name="remoteClient">The bounded hardened HTTPS client.</param>
        /// <returns>
        /// <see langword="true"/> for the first configuration or an equivalent repeated one.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is <see langword="null"/>.
        /// </exception>
        public bool TryConfigure(
            ExternalRemoteReferenceAcquisitionConfiguration remoteConfiguration,
            ExternalSourceLinkClient remoteClient)
        {
            ArgumentNullException.ThrowIfNull(remoteConfiguration);
            ArgumentNullException.ThrowIfNull(remoteClient);

            lock (gate)
            {
                if (configured)
                {
                    return configuration!.IsEquivalentTo(remoteConfiguration);
                }

                if (acquisitionStarted)
                {
                    return false;
                }

                configuration = remoteConfiguration;
                client = remoteClient;
                configured = true;
                return true;
            }
        }

        /// <summary>Tries to acquire one exact remote PE material after local failure.</summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="material">The exact P5-validated material when successful.</param>
        /// <param name="provenance">The complete remote acquisition provenance.</param>
        /// <returns><see langword="true"/> only when unchanged P5 validation succeeds.</returns>
        public bool TryAcquire(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalRemoteReferenceProvenance provenance)
        {
            return TryAcquire(
                expectedReference,
                expectedReferenceOrdinal: 0,
                out material,
                out provenance);
        }

        /// <summary>Tries to acquire one exact remote PE material for its expected ordinal.</summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="expectedReferenceOrdinal">The non-negative expected reference ordinal.</param>
        /// <param name="material">The exact P5-validated material when successful.</param>
        /// <param name="provenance">The complete ordinal-specific remote provenance.</param>
        /// <returns><see langword="true"/> only when unchanged P5 validation succeeds.</returns>
        public bool TryAcquire(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            int expectedReferenceOrdinal,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalRemoteReferenceProvenance provenance)
        {
            if (expectedReference == null || expectedReferenceOrdinal < 0)
            {
                material = null!;
                provenance = null!;
                return false;
            }

            lock (gate)
            {
                acquisitionStarted = true;
                ReferenceKey key = ReferenceKey.Create(expectedReference);
                if (referenceCache.TryGetValue(key, out ReferenceEntry? cached))
                {
                    GetStatisticsFor(cached.ProviderKind).CacheHits++;
                    material = cached.Material!;
                    provenance = cached.Provenance == null
                        ? null!
                        : cached.Provenance with
                        {
                            ExpectedReference = expectedReference,
                            ExpectedReferenceOrdinal = expectedReferenceOrdinal
                        };
                    return cached.Material != null;
                }

                if (!configured
                    || configuration!.Policy != ExternalReferenceAcquisitionPolicy.BoundedRemoteArtifacts)
                {
                    referenceCache.Add(key, ReferenceEntry.Unavailable(null));
                    material = null!;
                    provenance = null!;
                    return false;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                ReferenceBudget budget = new();
                bool acquired = TryAcquireCore(
                    expectedReference,
                    budget,
                    out material,
                    out provenance,
                    out ExternalRemoteArtifactProviderKind? lastProvider);
                stopwatch.Stop();

                if (acquired)
                {
                    provenance = provenance with
                    {
                        ExpectedReferenceOrdinal = expectedReferenceOrdinal
                    };
                }

                if (lastProvider.HasValue)
                {
                    MutableStatistics statistics = GetStatisticsFor(lastProvider.Value);
                    statistics.DurationTicks += stopwatch.ElapsedTicks;
                    if (!acquired)
                    {
                        statistics.UnavailableCount++;
                    }
                }

                referenceCache.Add(
                    key,
                    acquired
                        ? ReferenceEntry.Found(material, provenance)
                        : ReferenceEntry.Unavailable(lastProvider));
                return acquired;
            }
        }

        /// <summary>Returns an atomic snapshot of all context-local provider work.</summary>
        /// <returns>The immutable aggregate and provider-level statistics.</returns>
        public ExternalRemoteReferenceAcquisitionStatistics GetStatistics()
        {
            lock (gate)
            {
                ImmutableArray<ExternalRemoteArtifactProviderStatistics> providers =
                    providerStatistics
                        .OrderBy(static pair => pair.Key)
                        .Select(static pair => pair.Value.Snapshot(pair.Key))
                        .ToImmutableArray();
                return new ExternalRemoteReferenceAcquisitionStatistics(
                    providers.Sum(static value => value.SearchCount),
                    requestCount,
                    providers.Sum(static value => value.ArtifactRequestCount),
                    downloadedBytes,
                    providers.Sum(static value => value.CandidateBinaryCount),
                    providers.Sum(static value => value.P5ValidationAttemptCount),
                    providers.Sum(static value => value.RemoteExactCount),
                    providers.Sum(static value => value.UnavailableCount),
                    providers.Sum(static value => value.LimitHits),
                    providers.Sum(static value => value.CacheHits),
                    providers.Sum(static value => value.DurationTicks),
                    providers);
            }
        }

        /// <summary>Runs bounded configured hints followed by optional bounded search.</summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="material">The exact P5-validated material when successful.</param>
        /// <param name="provenance">The accepted remote artifact provenance.</param>
        /// <param name="lastProvider">The last provider consulted for statistics.</param>
        /// <returns><see langword="true"/> only when an exact remote material is found.</returns>
        private bool TryAcquireCore(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            ReferenceBudget budget,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalRemoteReferenceProvenance provenance,
            out ExternalRemoteArtifactProviderKind? lastProvider)
        {
            string? simpleName = TryGetSimpleName(expectedReference.Name);
            if (simpleName == null)
            {
                material = null!;
                provenance = null!;
                lastProvider = null;
                return false;
            }

            ImmutableArray<ExternalRemoteReferencePackageHint> hints = configuration!.PackageHints
                .Where(hint => hint.AssemblySimpleName == "*"
                    || hint.AssemblySimpleName.Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                .ToImmutableArray();

            foreach (ExternalRemoteReferencePackageHint hint in hints)
            {
                lastProvider = hint.ProviderKind;
                foreach (string version in hint.CandidateVersions
                             .Take(configuration.Limits.MaxPackageVersionsPerPackage))
                {
                    if (TryPackage(
                        expectedReference,
                        hint.PackageId,
                        version,
                        hint.ProviderKind,
                        hint.Evidence,
                        budget,
                        out material,
                        out provenance))
                    {
                        return true;
                    }

                    if (budget.Stopped)
                    {
                        material = null!;
                        provenance = null!;
                        return false;
                    }
                }
            }

            if (configuration.EnablePackageSearch
                && TrySearch(simpleName, budget, out ImmutableArray<PackageSearchResult> results))
            {
                lastProvider = ExternalRemoteArtifactProviderKind.NuGetPackage;
                foreach (PackageSearchResult result in results)
                {
                    foreach (string version in result.Versions
                                 .Take(configuration.Limits.MaxPackageVersionsPerPackage))
                    {
                        if (TryPackage(
                            expectedReference,
                            result.PackageId,
                            version,
                            ExternalRemoteArtifactProviderKind.NuGetPackage,
                            $"NuGet search for assembly name '{simpleName}'",
                            budget,
                            out material,
                            out provenance))
                        {
                            return true;
                        }

                        if (budget.Stopped)
                        {
                            material = null!;
                            provenance = null!;
                            return false;
                        }
                    }
                }
            }

            material = null!;
            provenance = null!;
            lastProvider = hints.Length == 0
                ? ExternalRemoteArtifactProviderKind.NuGetPackage
                : hints[^1].ProviderKind;
            return false;
        }

        /// <summary>Obtains and inspects one exact package coordinate under the current budget.</summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="packageId">The safe normalized package identifier.</param>
        /// <param name="version">The safe exact package version.</param>
        /// <param name="providerKind">The provider classification.</param>
        /// <param name="discoveryHint">The non-authoritative discovery evidence.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="material">The exact P5-validated material when successful.</param>
        /// <param name="provenance">The accepted remote artifact provenance.</param>
        /// <returns><see langword="true"/> only when the package contains exact material.</returns>
        private bool TryPackage(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            string packageId,
            string version,
            ExternalRemoteArtifactProviderKind providerKind,
            string discoveryHint,
            ReferenceBudget budget,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalRemoteReferenceProvenance provenance)
        {
            if (budget.PackagesDownloaded >= configuration!.Limits.MaxPackagesDownloadedPerReference)
            {
                StopForLimit(providerKind, budget);
                material = null!;
                provenance = null!;
                return false;
            }

            if (!TryGetEndpoints(providerKind, budget, out NuGetEndpoints serviceEndpoints))
            {
                material = null!;
                provenance = null!;
                return false;
            }

            ArtifactKey key = new(
                providerKind,
                packageId.ToLowerInvariant(),
                version.ToLowerInvariant());
            if (!artifactCache.TryGetValue(key, out ArtifactEntry? artifact))
            {
                budget.PackagesDownloaded++;
                artifact = DownloadPackage(serviceEndpoints, key, budget);
                artifactCache.Add(key, artifact);
            }
            else
            {
                GetStatisticsFor(providerKind).CacheHits++;
            }

            if (artifact.Bytes.IsDefaultOrEmpty)
            {
                material = null!;
                provenance = null!;
                return false;
            }

            return TryReadPackage(
                expectedReference,
                key,
                artifact,
                discoveryHint,
                budget,
                out material,
                out provenance);
        }

        /// <summary>Downloads one package only after obtaining authoritative catalog hash metadata.</summary>
        /// <param name="serviceEndpoints">The validated NuGet V3 endpoints.</param>
        /// <param name="key">The exact provider and package coordinate.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <returns>The hash-verified artifact or an unavailable cache entry.</returns>
        private ArtifactEntry DownloadPackage(
            NuGetEndpoints serviceEndpoints,
            ArtifactKey key,
            ReferenceBudget budget)
        {
            MutableStatistics statistics = GetStatisticsFor(key.ProviderKind);
            Uri registrationUri = new(
                serviceEndpoints.RegistrationBaseAddress.AbsoluteUri.TrimEnd('/')
                + $"/{key.PackageId}/{key.Version}.json");
            if (!TryRequest(
                    registrationUri,
                    key.ProviderKind,
                    budget,
                    artifactRequest: false,
                    out ImmutableArray<byte> registrationBytes)
                || !TryParseRegistrationLeaf(
                    registrationBytes,
                    out Uri catalogEntryUri,
                    out Uri packageUri)
                || !TryRequest(
                    catalogEntryUri,
                    key.ProviderKind,
                    budget,
                    artifactRequest: false,
                    out ImmutableArray<byte> catalogBytes)
                || !TryParseCatalogHash(
                    catalogBytes,
                    configuration!.Limits.MaxArtifactBytes,
                    out byte[] expectedHash)
                || !TryRequest(packageUri, key.ProviderKind, budget, artifactRequest: true, out ImmutableArray<byte> packageBytes))
            {
                return ArtifactEntry.Unavailable;
            }

            byte[] actualHash = SHA512.HashData(packageBytes.AsSpan());
            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            {
                statistics.RejectedCount++;
                return ArtifactEntry.Unavailable;
            }

            return new ArtifactEntry(packageBytes, Convert.ToHexString(actualHash));
        }

        /// <summary>Safely streams candidate PE entries through unchanged P5 validation.</summary>
        /// <param name="expectedReference">The authoritative expected P5 reference.</param>
        /// <param name="artifactKey">The exact provider and package coordinate.</param>
        /// <param name="artifact">The hash-verified bounded package bytes.</param>
        /// <param name="discoveryHint">The non-authoritative discovery evidence.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="material">The unique exact P5 material when successful.</param>
        /// <param name="provenance">The accepted remote artifact provenance.</param>
        /// <returns><see langword="true"/> only for one unambiguous exact binary.</returns>
        private bool TryReadPackage(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            ArtifactKey artifactKey,
            ArtifactEntry artifact,
            string discoveryHint,
            ReferenceBudget budget,
            out ValidatedExternalMetadataReferenceMaterial material,
            out ExternalRemoteReferenceProvenance provenance)
        {
            MutableStatistics statistics = GetStatisticsFor(artifactKey.ProviderKind);
            try
            {
                using MemoryStream packageStream = new(artifact.Bytes.ToArray(), writable: false);
                using ZipArchive archive = new(packageStream, ZipArchiveMode.Read, leaveOpen: false);
                HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
                List<ZipArchiveEntry> candidates = new();
                long expandedBytes = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string path = entry.FullName.Replace('\\', '/');
                    if (!IsSafeArchivePath(path) || !paths.Add(path))
                    {
                        statistics.RejectedCount++;
                        material = null!;
                        provenance = null!;
                        return false;
                    }

                    expandedBytes = checked(expandedBytes + entry.Length);
                    if (entry.Length > configuration!.Limits.MaxArchiveEntryBytes
                        || expandedBytes > configuration.Limits.MaxDownloadedBytesPerReference)
                    {
                        StopForLimit(artifactKey.ProviderKind, budget);
                        material = null!;
                        provenance = null!;
                        return false;
                    }

                    if (IsCandidateEntry(path, expectedReference.Kind))
                    {
                        candidates.Add(entry);
                    }
                }

                string expectedFileName = Path.GetFileName(expectedReference.Name);
                ZipArchiveEntry[] ordered = candidates
                    .OrderByDescending(entry => Path.GetFileName(entry.FullName)
                        .Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(static entry => entry.FullName, StringComparer.Ordinal)
                    .Take(configuration!.Limits.MaxBinaryCandidatesPerArtifact)
                    .ToArray();
                if (candidates.Count > ordered.Length)
                {
                    StopForLimit(artifactKey.ProviderKind, budget);
                    material = null!;
                    provenance = null!;
                    return false;
                }

                ValidatedExternalMetadataReferenceMaterial? selected = null;
                string? selectedEntry = null;

                foreach (ZipArchiveEntry entry in ordered)
                {
                    statistics.CandidateBinaryCount++;
                    statistics.P5ValidationAttemptCount++;
                    using Stream entryStream = entry.Open();
                    using MemoryStream imageStream = new();
                    if (!TryCopyBounded(
                            entryStream,
                            imageStream,
                            configuration.Limits.MaxArchiveEntryBytes))
                    {
                        StopForLimit(artifactKey.ProviderKind, budget);
                        material = null!;
                        provenance = null!;
                        return false;
                    }

                    imageStream.Position = 0;
                    if (!ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(
                            expectedReference,
                            imageStream,
                            out ValidatedExternalMetadataReferenceMaterial candidate))
                    {
                        statistics.RejectedCount++;
                        continue;
                    }

                    if (selected != null
                        && !selected.Image.AsSpan().SequenceEqual(candidate.Image.AsSpan()))
                    {
                        statistics.RejectedCount++;
                        material = null!;
                        provenance = null!;
                        return false;
                    }

                    selected = candidate;
                    selectedEntry = entry.FullName;
                }

                if (selected == null)
                {
                    material = null!;
                    provenance = null!;
                    return false;
                }

                statistics.RemoteExactCount++;
                material = selected;
                provenance = new ExternalRemoteReferenceProvenance(
                    expectedReference,
                    ExpectedReferenceOrdinal: 0,
                    artifactKey.ProviderKind,
                    configuration.ServiceIndexUri.GetLeftPart(UriPartial.Authority),
                    discoveryHint,
                    artifactKey.PackageId,
                    artifactKey.Version,
                    artifact.Sha512,
                    selectedEntry!,
                    P5ValidationSucceeded: true);
                return true;
            }
            catch (Exception exception) when (exception is InvalidDataException
                or IOException
                or ArgumentException
                or OverflowException)
            {
                statistics.RejectedCount++;
                material = null!;
                provenance = null!;
                return false;
            }
        }

        /// <summary>Performs and caches one bounded assembly-name package search.</summary>
        /// <param name="simpleName">The expected assembly simple name.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="results">The deterministic bounded search results.</param>
        /// <returns><see langword="true"/> when at least one safe result exists.</returns>
        private bool TrySearch(
            string simpleName,
            ReferenceBudget budget,
            out ImmutableArray<PackageSearchResult> results)
        {
            MutableStatistics statistics = GetStatisticsFor(
                ExternalRemoteArtifactProviderKind.NuGetPackage);
            if (searchCache.TryGetValue(simpleName, out results))
            {
                statistics.CacheHits++;
                return !results.IsDefaultOrEmpty;
            }

            statistics.SearchCount++;
            if (!TryGetEndpoints(
                    ExternalRemoteArtifactProviderKind.NuGetPackage,
                    budget,
                    out NuGetEndpoints serviceEndpoints)
                || serviceEndpoints.SearchQueryService == null)
            {
                results = ImmutableArray<PackageSearchResult>.Empty;
                searchCache[simpleName] = results;
                return false;
            }

            Uri uri = new(
                serviceEndpoints.SearchQueryService.AbsoluteUri.TrimEnd('/')
                + "?q=" + Uri.EscapeDataString(simpleName)
                + "&prerelease=false&semVerLevel=2.0.0&skip=0&take="
                + configuration!.Limits.MaxSearchResultsPerReference);
            if (!TryRequest(
                    uri,
                    ExternalRemoteArtifactProviderKind.NuGetPackage,
                    budget,
                    artifactRequest: false,
                    out ImmutableArray<byte> bytes))
            {
                results = ImmutableArray<PackageSearchResult>.Empty;
                searchCache[simpleName] = results;
                return false;
            }

            results = ParseSearchResults(simpleName, bytes);
            searchCache[simpleName] = results;
            return !results.IsDefaultOrEmpty;
        }

        /// <summary>Parses, validates, orders, and bounds NuGet search results.</summary>
        /// <param name="simpleName">The expected assembly simple name.</param>
        /// <param name="bytes">The bounded response bytes.</param>
        /// <returns>The immutable safe result sequence, or an empty sequence.</returns>
        private ImmutableArray<PackageSearchResult> ParseSearchResults(
            string simpleName,
            ImmutableArray<byte> bytes)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(bytes.ToArray());
                List<PackageSearchResult> results = new();
                foreach (JsonElement item in document.RootElement.GetProperty("data").EnumerateArray())
                {
                    string? packageId = item.GetProperty("id").GetString();
                    if (packageId == null || !IsSafePackageComponent(packageId))
                    {
                        continue;
                    }

                    ImmutableArray<string> versions = item.TryGetProperty("versions", out JsonElement versionItems)
                        ? versionItems.EnumerateArray()
                            .Select(static version => version.GetProperty("version").GetString())
                            .Where(static version => version != null)
                            .Select(static version => version!)
                            .Where(IsSafePackageComponent)
                            .Reverse()
                            .Take(configuration!.Limits.MaxPackageVersionsPerPackage)
                            .ToImmutableArray()
                        : ImmutableArray<string>.Empty;
                    if (versions.Length != 0)
                    {
                        results.Add(new PackageSearchResult(packageId, versions));
                    }
                }

                return results
                    .OrderByDescending(result => result.PackageId.Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(static result => result.PackageId, StringComparer.OrdinalIgnoreCase)
                    .Take(configuration!.Limits.MaxSearchResultsPerReference)
                    .ToImmutableArray();
            }
            catch (Exception exception) when (exception is JsonException
                or InvalidOperationException
                or KeyNotFoundException)
            {
                return ImmutableArray<PackageSearchResult>.Empty;
            }
        }

        /// <summary>Obtains and caches required NuGet V3 service endpoints.</summary>
        /// <param name="providerKind">The requesting provider classification.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="serviceEndpoints">The parsed service endpoints when successful.</param>
        /// <returns><see langword="true"/> when required safe endpoints exist.</returns>
        private bool TryGetEndpoints(
            ExternalRemoteArtifactProviderKind providerKind,
            ReferenceBudget budget,
            out NuGetEndpoints serviceEndpoints)
        {
            if (endpoints != null)
            {
                serviceEndpoints = endpoints;
                return true;
            }

            if (endpointsAttempted)
            {
                serviceEndpoints = null!;
                return false;
            }

            endpointsAttempted = true;
            if (!TryRequest(
                    configuration!.ServiceIndexUri,
                    providerKind,
                    budget,
                    artifactRequest: false,
                    out ImmutableArray<byte> bytes))
            {
                serviceEndpoints = null!;
                return false;
            }

            endpoints = ParseServiceIndex(bytes);
            serviceEndpoints = endpoints!;
            return endpoints != null;
        }

        /// <summary>Parses the required package, registration, and optional search endpoints.</summary>
        /// <param name="bytes">The bounded NuGet V3 service-index bytes.</param>
        /// <returns>The parsed endpoints, or <see langword="null"/> on invalid input.</returns>
        private static NuGetEndpoints? ParseServiceIndex(ImmutableArray<byte> bytes)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(bytes.ToArray());
                Uri? packageBase = null;
                Uri? search = null;
                Uri? registration = null;
                foreach (JsonElement resource in document.RootElement.GetProperty("resources").EnumerateArray())
                {
                    string? type = resource.GetProperty("@type").GetString();
                    string? id = resource.GetProperty("@id").GetString();
                    if (type == null || id == null || !Uri.TryCreate(id, UriKind.Absolute, out Uri? uri))
                    {
                        continue;
                    }

                    if (type.StartsWith("PackageBaseAddress/", StringComparison.Ordinal))
                    {
                        packageBase ??= uri;
                    }
                    else if (type.StartsWith("SearchQueryService/", StringComparison.Ordinal))
                    {
                        search ??= uri;
                    }
                    else if (type.StartsWith("RegistrationsBaseUrl/", StringComparison.Ordinal))
                    {
                        registration ??= uri;
                    }
                }

                return packageBase == null || registration == null
                    ? null
                    : new NuGetEndpoints(packageBase, registration, search);
            }
            catch (Exception exception) when (exception is JsonException
                or InvalidOperationException
                or KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>Performs one request only when all per-reference and run limits permit it.</summary>
        /// <param name="uri">The untrusted remote URI passed to the hardened transport.</param>
        /// <param name="providerKind">The provider classification.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        /// <param name="artifactRequest">Whether the response is a package artifact.</param>
        /// <param name="bytes">The bounded response bytes when successful.</param>
        /// <returns><see langword="true"/> only for a complete safe bounded response.</returns>
        private bool TryRequest(
            Uri uri,
            ExternalRemoteArtifactProviderKind providerKind,
            ReferenceBudget budget,
            bool artifactRequest,
            out ImmutableArray<byte> bytes)
        {
            ExternalRemoteReferenceAcquisitionLimits limits = configuration!.Limits;
            int remainingReferenceBytes = limits.MaxDownloadedBytesPerReference - budget.DownloadedBytes;
            int remainingRunBytes = limits.MaxDownloadedBytesPerRun - checked((int)downloadedBytes);
            if (budget.Requests >= limits.MaxRequestsPerReference
                || requestCount >= limits.MaxRequestsPerRun
                || remainingReferenceBytes <= 0
                || remainingRunBytes <= 0)
            {
                StopForLimit(providerKind, budget);
                bytes = default;
                return false;
            }

            int responseLimit = Math.Min(
                limits.MaxArtifactBytes,
                Math.Min(remainingReferenceBytes, remainingRunBytes));
            budget.Requests++;
            requestCount++;
            MutableStatistics statistics = GetStatisticsFor(providerKind);
            if (artifactRequest)
            {
                statistics.ArtifactRequestCount++;
            }
            else
            {
                statistics.MetadataRequestCount++;
            }

            if (!client!.TryDownload(uri, responseLimit, out bytes))
            {
                return false;
            }

            budget.DownloadedBytes += bytes.Length;
            downloadedBytes += bytes.Length;
            statistics.DownloadedBytes += bytes.Length;
            return true;
        }

        /// <summary>Stops one reference budget and records its first hard-limit hit.</summary>
        /// <param name="providerKind">The provider at which the limit was reached.</param>
        /// <param name="budget">The mutable per-reference hard-limit budget.</param>
        private void StopForLimit(
            ExternalRemoteArtifactProviderKind providerKind,
            ReferenceBudget budget)
        {
            if (!budget.Stopped)
            {
                budget.Stopped = true;
                GetStatisticsFor(providerKind).LimitHits++;
            }
        }

        /// <summary>Gets or creates the mutable counter set for one provider.</summary>
        /// <param name="providerKind">The provider, or the package-provider fallback.</param>
        /// <returns>The context-local mutable counter set.</returns>
        private MutableStatistics GetStatisticsFor(ExternalRemoteArtifactProviderKind? providerKind)
        {
            ExternalRemoteArtifactProviderKind key = providerKind
                ?? ExternalRemoteArtifactProviderKind.NuGetPackage;
            if (!providerStatistics.TryGetValue(key, out MutableStatistics? statistics))
            {
                statistics = new MutableStatistics();
                providerStatistics.Add(key, statistics);
            }

            return statistics;
        }

        /// <summary>Extracts a supported PE simple name from an expected reference name.</summary>
        /// <param name="expectedName">The expected reference file name.</param>
        /// <returns>The simple name, or <see langword="null"/> for an unsupported extension.</returns>
        private static string? TryGetSimpleName(string expectedName)
        {
            string fileName = Path.GetFileName(expectedName);
            string extension = Path.GetExtension(fileName);
            return extension is ".dll" or ".exe" or ".netmodule"
                ? Path.GetFileNameWithoutExtension(fileName)
                : null;
        }

        /// <summary>Determines whether a package identifier or version has safe bounded syntax.</summary>
        /// <param name="value">The candidate package component.</param>
        /// <returns><see langword="true"/> when the component is safe.</returns>
        private static bool IsSafePackageComponent(string value)
        {
            return value.Length is > 0 and <= 256
                && value.All(static character => char.IsAsciiLetterOrDigit(character)
                    || character is '.' or '-' or '_' or '+');
        }

        /// <summary>Rejects empty, absolute, drive-qualified, and traversing archive paths.</summary>
        /// <param name="path">The normalized archive entry path.</param>
        /// <returns><see langword="true"/> only for a safe relative path.</returns>
        private static bool IsSafeArchivePath(string path)
        {
            if (path.Length == 0
                || path[0] == '/'
                || path.Contains(':'))
            {
                return false;
            }

            return path.Split('/').All(static part => part is not "." and not "..");
        }

        /// <summary>Determines whether an archive entry can contain the expected PE kind.</summary>
        /// <param name="path">The safe normalized archive entry path.</param>
        /// <param name="kind">The authoritative expected metadata image kind.</param>
        /// <returns><see langword="true"/> for a supported candidate location and extension.</returns>
        private static bool IsCandidateEntry(string path, Microsoft.CodeAnalysis.MetadataImageKind kind)
        {
            if (!(path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            string extension = Path.GetExtension(path);
            return kind == Microsoft.CodeAnalysis.MetadataImageKind.Module
                ? extension.Equals(".netmodule", StringComparison.OrdinalIgnoreCase)
                : extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Copies one archive entry while enforcing its decompressed byte limit.</summary>
        /// <param name="input">The archive entry stream.</param>
        /// <param name="output">The bounded destination stream.</param>
        /// <param name="maximumBytes">The positive decompressed byte limit.</param>
        /// <returns><see langword="true"/> when the complete entry fits the limit.</returns>
        private static bool TryCopyBounded(Stream input, Stream output, int maximumBytes)
        {
            byte[] buffer = new byte[81920];
            int total = 0;
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    return true;
                }

                total = checked(total + read);
                if (total > maximumBytes)
                {
                    return false;
                }

                output.Write(buffer, 0, read);
            }
        }

        /// <summary>Parses the catalog and package-content URIs from a registration leaf.</summary>
        /// <param name="bytes">The bounded registration-leaf bytes.</param>
        /// <param name="catalogEntry">The absolute catalog-entry URI.</param>
        /// <param name="packageContent">The absolute package-content URI.</param>
        /// <returns><see langword="true"/> when both absolute URIs are present.</returns>
        private static bool TryParseRegistrationLeaf(
            ImmutableArray<byte> bytes,
            out Uri catalogEntry,
            out Uri packageContent)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(bytes.ToArray());
                string? catalog = document.RootElement.GetProperty("catalogEntry").GetString();
                string? package = document.RootElement.GetProperty("packageContent").GetString();
                bool catalogCreated = Uri.TryCreate(
                    catalog,
                    UriKind.Absolute,
                    out catalogEntry!);
                bool packageCreated = Uri.TryCreate(
                    package,
                    UriKind.Absolute,
                    out packageContent!);
                return catalogCreated && packageCreated;
            }
            catch (Exception exception) when (exception is JsonException
                or InvalidOperationException
                or KeyNotFoundException)
            {
                catalogEntry = null!;
                packageContent = null!;
                return false;
            }
        }

        /// <summary>Parses an authoritative SHA-512 hash and bounded package size.</summary>
        /// <param name="bytes">The bounded catalog-entry bytes.</param>
        /// <param name="maximumArtifactBytes">The configured artifact byte limit.</param>
        /// <param name="hash">The 64-byte SHA-512 digest when successful.</param>
        /// <returns><see langword="true"/> for valid SHA-512 metadata within the size limit.</returns>
        private static bool TryParseCatalogHash(
            ImmutableArray<byte> bytes,
            int maximumArtifactBytes,
            out byte[] hash)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(bytes.ToArray());
                JsonElement root = document.RootElement;
                if (!root.GetProperty("packageHashAlgorithm").GetString()!
                        .Equals("SHA512", StringComparison.OrdinalIgnoreCase)
                    || root.GetProperty("packageSize").GetInt64() > maximumArtifactBytes)
                {
                    hash = [];
                    return false;
                }

                hash = Convert.FromBase64String(root.GetProperty("packageHash").GetString()!);
                return hash.Length == 64;
            }
            catch (Exception exception) when (exception is JsonException
                or InvalidOperationException
                or KeyNotFoundException
                or FormatException
                or NullReferenceException)
            {
                hash = [];
                return false;
            }
        }

        /// <summary>Identifies one authoritative expected P5 binary identity.</summary>
        /// <param name="Kind">The expected metadata image kind.</param>
        /// <param name="Timestamp">The expected PE timestamp.</param>
        /// <param name="ImageSize">The expected PE image size.</param>
        /// <param name="ModuleVersionId">The expected module version identifier.</param>
        private readonly record struct ReferenceKey(
            Microsoft.CodeAnalysis.MetadataImageKind Kind,
            int Timestamp,
            int ImageSize,
            Guid ModuleVersionId)
        {
            /// <summary>Creates a cache key from authoritative expected provenance.</summary>
            /// <param name="value">The expected P5 reference descriptor.</param>
            /// <returns>The exact binary identity cache key.</returns>
            public static ReferenceKey Create(ExternalCompilationMetadataReferenceDescriptor value)
            {
                return new ReferenceKey(value.Kind, value.Timestamp, value.ImageSize, value.ModuleVersionId);
            }
        }

        /// <summary>Caches an exact material or an unavailable provider outcome.</summary>
        /// <param name="Material">The exact P5 material, if found.</param>
        /// <param name="Provenance">The remote provenance, if found.</param>
        /// <param name="ProviderKind">The provider associated with the outcome.</param>
        private sealed record ReferenceEntry(
            ValidatedExternalMetadataReferenceMaterial? Material,
            ExternalRemoteReferenceProvenance? Provenance,
            ExternalRemoteArtifactProviderKind? ProviderKind)
        {
            /// <summary>Creates a positive exact-material cache entry.</summary>
            /// <param name="material">The exact P5 material.</param>
            /// <param name="provenance">The remote artifact provenance.</param>
            /// <returns>The positive cache entry.</returns>
            public static ReferenceEntry Found(
                ValidatedExternalMetadataReferenceMaterial material,
                ExternalRemoteReferenceProvenance provenance)
            {
                return new ReferenceEntry(material, provenance, provenance.ProviderKind);
            }

            /// <summary>Creates a negative unavailable cache entry.</summary>
            /// <param name="providerKind">The last provider consulted, if any.</param>
            /// <returns>The negative cache entry.</returns>
            public static ReferenceEntry Unavailable(ExternalRemoteArtifactProviderKind? providerKind)
            {
                return new ReferenceEntry(null, null, providerKind);
            }
        }

        /// <summary>Identifies one exact provider package coordinate.</summary>
        /// <param name="ProviderKind">The artifact provider classification.</param>
        /// <param name="PackageId">The normalized package identifier.</param>
        /// <param name="Version">The normalized exact package version.</param>
        private readonly record struct ArtifactKey(
            ExternalRemoteArtifactProviderKind ProviderKind,
            string PackageId,
            string Version);

        /// <summary>Caches bounded hash-verified artifact bytes.</summary>
        /// <param name="Bytes">The verified package bytes, or an empty value.</param>
        /// <param name="Sha512">The verified SHA-512 digest, or an empty value.</param>
        private sealed record ArtifactEntry(ImmutableArray<byte> Bytes, string Sha512)
        {
            /// <summary>Gets the reusable unavailable artifact entry.</summary>
            /// <value>An entry without bytes or a digest.</value>
            public static ArtifactEntry Unavailable { get; } = new(default, string.Empty);
        }

        /// <summary>Stores required and optional NuGet V3 resource endpoints.</summary>
        /// <param name="PackageBaseAddress">The package-base endpoint.</param>
        /// <param name="RegistrationBaseAddress">The registration endpoint.</param>
        /// <param name="SearchQueryService">The optional search endpoint.</param>
        private sealed record NuGetEndpoints(
            Uri PackageBaseAddress,
            Uri RegistrationBaseAddress,
            Uri? SearchQueryService);

        /// <summary>Stores one safe bounded package-search result.</summary>
        /// <param name="PackageId">The safe package identifier.</param>
        /// <param name="Versions">The deterministic bounded version sequence.</param>
        private sealed record PackageSearchResult(
            string PackageId,
            ImmutableArray<string> Versions);

        /// <summary>Tracks mutable hard-limit usage for one expected reference.</summary>
        private sealed class ReferenceBudget
        {
            /// <summary>Gets or sets the requests used by this reference.</summary>
            /// <value>The request count.</value>
            public int Requests { get; set; }
            /// <summary>Gets or sets the package downloads used by this reference.</summary>
            /// <value>The package-download count.</value>
            public int PackagesDownloaded { get; set; }
            /// <summary>Gets or sets the response bytes used by this reference.</summary>
            /// <value>The downloaded byte count.</value>
            public int DownloadedBytes { get; set; }
            /// <summary>Gets or sets whether a hard limit stopped this reference.</summary>
            /// <value><see langword="true"/> after the first hard-limit stop.</value>
            public bool Stopped { get; set; }
        }

        /// <summary>Accumulates mutable work counters for one provider.</summary>
        private sealed class MutableStatistics
        {
            /// <summary>Gets or sets the bounded search count.</summary>
            /// <value>The search count.</value>
            public long SearchCount { get; set; }
            /// <summary>Gets or sets the metadata request count.</summary>
            /// <value>The metadata request count.</value>
            public long MetadataRequestCount { get; set; }
            /// <summary>Gets or sets the artifact request count.</summary>
            /// <value>The artifact request count.</value>
            public long ArtifactRequestCount { get; set; }
            /// <summary>Gets or sets the downloaded response byte count.</summary>
            /// <value>The downloaded response bytes.</value>
            public long DownloadedBytes { get; set; }
            /// <summary>Gets or sets the candidate binary count.</summary>
            /// <value>The candidate binary count.</value>
            public long CandidateBinaryCount { get; set; }
            /// <summary>Gets or sets the unchanged P5 validation attempt count.</summary>
            /// <value>The P5 validation attempt count.</value>
            public long P5ValidationAttemptCount { get; set; }
            /// <summary>Gets or sets the accepted exact remote count.</summary>
            /// <value>The exact remote count.</value>
            public long RemoteExactCount { get; set; }
            /// <summary>Gets or sets the rejected candidate or artifact count.</summary>
            /// <value>The rejection count.</value>
            public long RejectedCount { get; set; }
            /// <summary>Gets or sets the unavailable expected-reference count.</summary>
            /// <value>The unavailable count.</value>
            public long UnavailableCount { get; set; }
            /// <summary>Gets or sets the positive and negative cache-hit count.</summary>
            /// <value>The cache-hit count.</value>
            public long CacheHits { get; set; }
            /// <summary>Gets or sets the hard-limit hit count.</summary>
            /// <value>The hard-limit hit count.</value>
            public long LimitHits { get; set; }
            /// <summary>Gets or sets the acquisition duration in stopwatch ticks.</summary>
            /// <value>The duration in stopwatch ticks.</value>
            public long DurationTicks { get; set; }

            /// <summary>Creates an immutable provider-level statistics snapshot.</summary>
            /// <param name="providerKind">The provider represented by this counter set.</param>
            /// <returns>The immutable provider-level snapshot.</returns>
            public ExternalRemoteArtifactProviderStatistics Snapshot(
                ExternalRemoteArtifactProviderKind providerKind)
            {
                return new ExternalRemoteArtifactProviderStatistics(
                    providerKind,
                    SearchCount,
                    MetadataRequestCount,
                    ArtifactRequestCount,
                    DownloadedBytes,
                    CandidateBinaryCount,
                    P5ValidationAttemptCount,
                    RemoteExactCount,
                    RejectedCount,
                    UnavailableCount,
                    CacheHits,
                    LimitHits,
                    DurationTicks);
            }
        }
    }
}
