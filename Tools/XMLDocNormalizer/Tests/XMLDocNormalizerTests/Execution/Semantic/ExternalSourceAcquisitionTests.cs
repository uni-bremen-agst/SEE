using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests controlled local and Source Link candidate acquisition while P5H
    /// remains the only source-content identity boundary.
    /// </summary>
    public sealed class ExternalSourceAcquisitionTests
    {
        private static readonly Guid Sha256DocumentHashAlgorithm =
            new("8829d00f-11b8-4213-878b-770e8597ac16");

        private static readonly Guid CSharpLanguage =
            new("3f5162f8-07c6-11d3-9053-00c04fa302a1");

        [Fact]
        public void MappingConfiguration_IsAbsoluteImmutableAndIdempotent()
        {
            using SourceWorkspace workspace = new();
            List<ExternalSourcePathMapping> mappings =
            [new("/_/", workspace.Root)];
            ExternalSourceAcquisition acquisition = new();

            Assert.True(acquisition.TryConfigureMappings(mappings));
            mappings.Clear();
            Assert.True(acquisition.TryConfigureMappings(
                [new ExternalSourcePathMapping("/_/", workspace.Root + Path.DirectorySeparatorChar)]));
            Assert.False(new ExternalSourceAcquisition().TryConfigureMappings(
                [new ExternalSourcePathMapping("/_/", "relative")]));
        }

        [Fact]
        public void LocalProjection_ValidBytesCreateP5HMaterial()
        {
            using SourceWorkspace workspace = new();
            byte[] bytes = "public sealed class Local { }"u8.ToArray();
            string path = workspace.Write(Path.Combine("src", "Local.cs"), bytes);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("/_/", workspace.Root));

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/src/Local.cs", bytes),
                sourceLink: null,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(bytes, material.Image);
            Assert.Equal(path, material.FilePath);
            Assert.Equal(ExternalSourceMaterialOrigin.LocalMapping, material.Origin);
        }

        [Fact]
        public void LocalProjection_MixedSeparatorsRemainInsideRoot()
        {
            using SourceWorkspace workspace = new();
            byte[] bytes = "mixed"u8.ToArray();
            _ = workspace.Write(Path.Combine("src", "nested", "Mixed.cs"), bytes);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("C:\\build\\", workspace.Root));

            Assert.True(acquisition.TryAcquire(
                CreateDocument("C:\\build\\src/nested\\Mixed.cs", bytes),
                sourceLink: null,
                out _));
        }

        [Theory]
        [InlineData("/_/../Outside.cs")]
        [InlineData("/_/src/../../Outside.cs")]
        [InlineData("/_/C:/Outside.cs")]
        public void LocalProjection_PathTraversalFailsClosed(string documentName)
        {
            using SourceWorkspace workspace = new();
            byte[] bytes = "outside"u8.ToArray();
            _ = workspace.Write("Outside.cs", bytes);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("/_/", workspace.Root));

            Assert.False(acquisition.TryAcquire(
                CreateDocument(documentName, bytes),
                sourceLink: null,
                out _));
        }

        [Fact]
        public void LocalProjection_LongestPrefixAndMultipleRootsFindExactCandidate()
        {
            using SourceWorkspace workspace = new();
            string broad = workspace.CreateDirectory("broad");
            string firstSpecific = workspace.CreateDirectory("specific-a");
            string secondSpecific = workspace.CreateDirectory("specific-b");
            byte[] expected = "expected"u8.ToArray();
            _ = workspace.Write(broad, "Value.cs", expected);
            _ = workspace.Write(firstSpecific, "Value.cs", "wrong"u8.ToArray());
            string selected = workspace.Write(secondSpecific, "Value.cs", expected);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("/_/", broad),
                new ExternalSourcePathMapping("/_/src/", firstSpecific),
                new ExternalSourcePathMapping("/_/src/", secondSpecific));

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/src/Value.cs", expected),
                sourceLink: null,
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(selected, material.FilePath);
        }

        [Fact]
        public void LocalProjection_RenamedSourceRequiresExplicitCandidate()
        {
            using SourceWorkspace workspace = new();
            byte[] bytes = "renamed"u8.ToArray();
            string renamed = workspace.Write("DifferentName.cs", bytes);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Expected.cs", bytes);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("/_/", workspace.Root));

            Assert.False(acquisition.TryAcquire(document, sourceLink: null, out _));
            Assert.True(ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                document,
                renamed,
                out _));
        }

        [Fact]
        public void LocalProjection_OversizedCandidateFailsClosed()
        {
            using SourceWorkspace workspace = new();
            byte[] oversized = new byte[
                ExternalSourceLinkClient.DefaultMaximumResponseBytes + 1];
            _ = workspace.Write("Large.cs", oversized);
            ExternalSourceAcquisition acquisition = ConfigureLocal(
                new ExternalSourcePathMapping("/_/", workspace.Root));

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Large.cs", oversized),
                sourceLink: null,
                out _));
        }

        [Fact]
        public void ConfigurationCannotChangeAfterAcquisitionStarts()
        {
            byte[] bytes = "immutable policy"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                sourceLink: null,
                out _));

            Assert.False(acquisition.TryConfigureMappings(
                [new ExternalSourcePathMapping("/_/", Path.GetTempPath())]));
            Assert.False(acquisition.TryConfigureSourceLink(
                new ExternalSourceLinkClient(
                    FakeHandler.Success(bytes),
                    new FakeResolver(IPAddress.Parse("8.8.8.8")),
                    TimeSpan.FromSeconds(5))));
        }

        [Fact]
        public void LocalProjection_WrongCandidateMayContinueToSourceLink()
        {
            using SourceWorkspace workspace = new();
            byte[] expected = "network exact"u8.ToArray();
            _ = workspace.Write("Value.cs", "local wrong"u8.ToArray());
            FakeHandler handler = FakeHandler.Success(expected);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            Assert.True(acquisition.TryConfigureMappings(
                [new ExternalSourcePathMapping("/_/", workspace.Root)]));

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", expected),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(ExternalSourceMaterialOrigin.SourceLink, material.Origin);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLinkDisabled_PerformsNoRequest()
        {
            byte[] bytes = "disabled"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = new();

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Disabled.cs", bytes),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out _));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_NoDocumentMappingPerformsNoRequest()
        {
            byte[] bytes = "unmapped"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/other/*", "https://public.test/*"),
                out _));
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_UriEscapingPreservesUnicodeAndExistingEscapes()
        {
            byte[] bytes = "escaped"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/space ü.cs", bytes),
                CreateSourceLink(
                    "/_/*",
                    "https://public.test/root/*?token=already%20escaped"),
                out _));
            string absoluteUri = handler.LastRequestUri!.AbsoluteUri;
            Assert.Contains("space%20%C3%BC.cs", absoluteUri, StringComparison.Ordinal);
            Assert.Contains("token=already%20escaped", absoluteUri, StringComparison.Ordinal);
            Assert.DoesNotContain("%2520", absoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("http://public.test/Value.cs")]
        [InlineData("file:///Value.cs")]
        [InlineData("ftp://public.test/Value.cs")]
        [InlineData("https://user:secret@public.test/Value.cs")]
        [InlineData("https://public.test/Value.cs#fragment")]
        public void SourceLink_UnsafeUriFailsBeforeRequest(string locator)
        {
            byte[] bytes = "unsafe"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/Value.cs", locator),
                out _));
            Assert.Equal(0, handler.RequestCount);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("10.0.0.1")]
        [InlineData("169.254.1.1")]
        [InlineData("192.168.1.1")]
        [InlineData("0.0.0.0")]
        [InlineData("::1")]
        [InlineData("fe80::1")]
        [InlineData("fc00::1")]
        public void SourceLink_BlockedAddressFailsBeforeRequest(string addressText)
        {
            byte[] bytes = "blocked"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            FakeResolver resolver = new(IPAddress.Parse(addressText));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler, resolver);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/*", "https://blocked.test/*"),
                out _));
            Assert.Equal(0, handler.RequestCount);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("service.localhost")]
        public void SourceLink_LocalhostFailsWithoutDnsOrRequest(string host)
        {
            byte[] bytes = "localhost"u8.ToArray();
            FakeHandler handler = FakeHandler.Success(bytes);
            FakeResolver resolver = new(IPAddress.Parse("8.8.8.8"));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler, resolver);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/*", $"https://{host}/*"),
                out _));
            Assert.Equal(0, resolver.RequestCount);
            Assert.Equal(0, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_AllowedRedirectRevalidatesTarget()
        {
            byte[] bytes = "redirect"u8.ToArray();
            FakeHandler handler = new((request, count) => count == 1
                ? Redirect("https://second.test/final.cs")
                : Response(HttpStatusCode.OK, bytes));
            FakeResolver resolver = new(IPAddress.Parse("8.8.8.8"));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler, resolver);

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/*", "https://first.test/*"),
                out _));
            Assert.Equal(2, handler.RequestCount);
            Assert.Equal(2, resolver.RequestCount);
        }

        [Fact]
        public void SourceLink_RedirectToPrivateTargetIsBlocked()
        {
            byte[] bytes = "redirect blocked"u8.ToArray();
            FakeHandler handler = new((_, _) => Redirect("https://private.test/final.cs"));
            FakeResolver resolver = new(host => host == "private.test"
                ? [IPAddress.Parse("10.0.0.1")]
                : [IPAddress.Parse("8.8.8.8")]);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler, resolver);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_TooManyRedirectsFailClosed()
        {
            byte[] bytes = "redirect loop"u8.ToArray();
            FakeHandler handler = new((_, _) => Redirect("https://public.test/next.cs"));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", bytes),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out _));
            Assert.Equal(ExternalSourceLinkClient.MaximumRedirects + 1, handler.RequestCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void SourceLink_NonSuccessStatusIsNegativeCached(HttpStatusCode status)
        {
            byte[] bytes = "status"u8.ToArray();
            FakeHandler handler = new((_, _) => Response(status, []));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", bytes);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*");

            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_DeclaredOversizeFailsBeforeReadingBody()
        {
            byte[] expected = "small"u8.ToArray();
            FakeHandler handler = new((_, _) =>
            {
                HttpResponseMessage response = Response(HttpStatusCode.OK, expected);
                response.Content.Headers.ContentLength =
                    ExternalSourceLinkClient.DefaultMaximumResponseBytes + 1;
                return response;
            });
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", expected),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out _));
        }

        [Fact]
        public void SourceLink_StreamingOversizeFailsClosed()
        {
            byte[] oversized = new byte[
                ExternalSourceLinkClient.DefaultMaximumResponseBytes + 1];
            FakeHandler handler = FakeHandler.Success(oversized);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);

            Assert.False(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", "different"u8.ToArray()),
                CreateSourceLink("/_/*", "https://public.test/*"),
                out _));
        }

        [Fact]
        public void SourceLink_TimeoutFailsAndIsNegativeCached()
        {
            byte[] bytes = "timeout"u8.ToArray();
            FakeHandler handler = new(async (_, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return Response(HttpStatusCode.OK, bytes);
            });
            ExternalSourceAcquisition acquisition = ConfigureNetwork(
                handler,
                timeout: TimeSpan.FromMilliseconds(30));
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", bytes);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*");

            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public void SourceLink_WrongBytesAreRejectedAndNegativeCached()
        {
            byte[] expected = "expected"u8.ToArray();
            FakeHandler handler = FakeHandler.Success("wrong"u8.ToArray());
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", expected);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*");

            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            Assert.Equal(1, handler.RequestCount);
        }

        public static IEnumerable<object[]> ExactByteSequences()
        {
            yield return [Encoding.UTF8.GetPreamble().Concat("BOM"u8.ToArray()).ToArray()];
            yield return ["line1\r\nline2\r\n"u8.ToArray()];
            yield return ["line1\nline2\n"u8.ToArray()];
            yield return [Array.Empty<byte>()];
        }

        [Theory]
        [MemberData(nameof(ExactByteSequences))]
        public void SourceLink_ExactBytesArePreservedAndPositiveCached(byte[] bytes)
        {
            FakeHandler handler = FakeHandler.Success(bytes);
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", bytes);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*?raw=1");

            Assert.True(acquisition.TryAcquire(document, sourceLink, out var first));
            Assert.True(acquisition.TryAcquire(document, sourceLink, out var second));
            Assert.Equal(bytes, first.Image);
            Assert.Same(first, second);
            Assert.Equal(1, handler.RequestCount);
            Assert.Equal("?raw=1", handler.LastRequestUri!.Query);
        }

        [Fact]
        public void SourceLink_SameUrlDifferentHashesDoesNotReuseMaterial()
        {
            byte[] firstBytes = "first"u8.ToArray();
            byte[] secondBytes = "second"u8.ToArray();
            FakeHandler handler = new((_, count) => Response(
                HttpStatusCode.OK,
                count == 1 ? firstBytes : secondBytes));
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/Value.cs", "https://public.test/shared.cs");

            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", firstBytes), sourceLink, out _));
            Assert.True(acquisition.TryAcquire(
                CreateDocument("/_/Value.cs", secondBytes), sourceLink, out _));
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public void AcquisitionCachesAreContextLocal()
        {
            byte[] bytes = "isolated"u8.ToArray();
            FakeHandler firstHandler = FakeHandler.Success(bytes);
            FakeHandler secondHandler = FakeHandler.Success(bytes);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", bytes);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*");

            Assert.True(ConfigureNetwork(firstHandler).TryAcquire(document, sourceLink, out _));
            Assert.True(ConfigureNetwork(secondHandler).TryAcquire(document, sourceLink, out _));
            Assert.Equal(1, firstHandler.RequestCount);
            Assert.Equal(1, secondHandler.RequestCount);
        }

        [Fact]
        public async Task ConcurrentAcquisition_PerformsOneNetworkRequest()
        {
            byte[] bytes = "concurrent"u8.ToArray();
            TaskCompletionSource entered = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            FakeHandler handler = new(async (_, _, cancellationToken) =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(cancellationToken);
                return Response(HttpStatusCode.OK, bytes);
            });
            ExternalSourceAcquisition acquisition = ConfigureNetwork(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument("/_/Value.cs", bytes);
            ExternalSourceLinkDescriptor sourceLink =
                CreateSourceLink("/_/*", "https://public.test/*");
            Task<bool> first = Task.Run(() => acquisition.TryAcquire(
                document,
                sourceLink,
                out _));
            await entered.Task;

            Assert.False(acquisition.TryAcquire(document, sourceLink, out _));
            release.SetResult();
            Assert.True(await first);
            Assert.Equal(1, handler.RequestCount);
        }

        private static ExternalSourceAcquisition ConfigureLocal(
            params ExternalSourcePathMapping[] mappings)
        {
            ExternalSourceAcquisition acquisition = new();
            Assert.True(acquisition.TryConfigureMappings(mappings));
            return acquisition;
        }

        private static ExternalSourceAcquisition ConfigureNetwork(
            FakeHandler handler,
            FakeResolver? resolver = null,
            TimeSpan? timeout = null)
        {
            ExternalSourceAcquisition acquisition = new();
            Assert.True(acquisition.TryConfigureSourceLink(
                new ExternalSourceLinkClient(
                    handler,
                    resolver ?? new FakeResolver(IPAddress.Parse("8.8.8.8")),
                    timeout ?? TimeSpan.FromSeconds(5))));
            return acquisition;
        }

        private static ExternalSourceDocumentDescriptor CreateDocument(
            string name,
            byte[] bytes)
        {
            return new ExternalSourceDocumentDescriptor(
                name,
                Sha256DocumentHashAlgorithm,
                ImmutableArray.Create(SHA256.HashData(bytes)),
                CSharpLanguage,
                embeddedSource: null);
        }

        private static ExternalSourceLinkDescriptor CreateSourceLink(
            string pattern,
            string target)
        {
            string json = "{\"documents\":{\""
                + pattern.Replace("\\", "\\\\", StringComparison.Ordinal)
                + "\":\""
                + target.Replace("\\", "\\\\", StringComparison.Ordinal)
                + "\"}}";
            Assert.True(ExternalSourceLinkDescriptor.TryCreate(
                ImmutableArray.Create(Encoding.UTF8.GetBytes(json)),
                out ExternalSourceLinkDescriptor descriptor));
            return descriptor;
        }

        private static HttpResponseMessage Response(HttpStatusCode status, byte[] bytes)
        {
            return new HttpResponseMessage(status)
            {
                Content = new ByteArrayContent(bytes)
            };
        }

        private static HttpResponseMessage Redirect(string location)
        {
            return new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri(location) }
            };
        }

        private sealed class FakeResolver : IExternalSourceHostResolver
        {
            private readonly Func<string, IPAddress[]> resolve;

            public FakeResolver(IPAddress address)
                : this(_ => [address])
            {
            }

            public FakeResolver(Func<string, IPAddress[]> resolve)
            {
                this.resolve = resolve;
            }

            public int RequestCount { get; private set; }

            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequestCount++;
                return ValueTask.FromResult(resolve(host));
            }
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, int, CancellationToken,
                Task<HttpResponseMessage>> responseFactory;

            public FakeHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory)
                : this((request, count, _) => Task.FromResult(responseFactory(request, count)))
            {
            }

            public FakeHandler(
                Func<HttpRequestMessage, int, CancellationToken,
                    Task<HttpResponseMessage>> responseFactory)
            {
                this.responseFactory = responseFactory;
            }

            public int RequestCount { get; private set; }

            public Uri? LastRequestUri { get; private set; }

            public static FakeHandler Success(byte[] bytes)
            {
                return new FakeHandler((_, _) => Response(HttpStatusCode.OK, bytes));
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestCount++;
                LastRequestUri = request.RequestUri;
                return responseFactory(request, RequestCount, cancellationToken);
            }
        }

        private sealed class SourceWorkspace : IDisposable
        {
            public SourceWorkspace()
            {
                Root = Path.Combine(
                    Path.GetTempPath(),
                    "XMLDocNormalizerTests",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            public string Root { get; }

            public string CreateDirectory(string name)
            {
                string path = Path.Combine(Root, name);
                Directory.CreateDirectory(path);
                return path;
            }

            public string Write(string relativePath, byte[] bytes)
            {
                return Write(Root, relativePath, bytes);
            }

            public string Write(string root, string relativePath, byte[] bytes)
            {
                string path = Path.Combine(root, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, bytes);
                return path;
            }

            public void Dispose()
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
