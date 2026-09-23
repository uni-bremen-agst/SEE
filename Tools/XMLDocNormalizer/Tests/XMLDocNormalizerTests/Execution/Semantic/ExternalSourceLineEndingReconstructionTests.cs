using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests deterministic source candidates whose exact reconstructed bytes
    /// must still pass the existing P5H checksum boundary.
    /// </summary>
    public sealed class ExternalSourceLineEndingReconstructionTests
    {
        private static readonly Guid Sha256DocumentHashAlgorithm =
            new("8829d00f-11b8-4213-878b-770e8597ac16");

        private static readonly Guid CSharpLanguage =
            new("3f5162f8-07c6-11d3-9053-00c04fa302a1");

        [Fact]
        public void StrictPolicy_LfCandidateForCrlfDocumentFailsClosed()
        {
            byte[] downloaded = "class C\n{\n}\n"u8.ToArray();
            byte[] expected = "class C\r\n{\r\n}\r\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = Configure(downloaded);

            Assert.False(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out _));

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(0, statistics.ReconstructionAttemptCount);
            Assert.Equal(1, statistics.P5HValidationAttempts);
        }

        [Fact]
        public void VerifiedPolicy_LfToCrlf_PreservesProvenanceAndPassesP5H()
        {
            byte[] downloaded = "class C\n{\n}\n"u8.ToArray();
            byte[] expected = "class C\r\n{\r\n}\r\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.True(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(expected, material.Image);
            Assert.Equal(ExternalSourceMaterialOrigin.SourceLink, material.Origin);
            Assert.Equal("https://public.test/Value.cs", material.SourceIdentity);
            Assert.Equal(ExternalSourceMaterialExactness.ReconstructedExact, material.Exactness);
            Assert.Equal(ExternalSourceLineEndingTransformation.LfToCrlf, material.Transformation);

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.ReconstructionAttemptCount);
            Assert.Equal(1, statistics.ReconstructionSuccessCount);
            Assert.Equal(1, statistics.LfToCrlfSuccessCount);
            Assert.Equal(2, statistics.P5HValidationAttempts);
        }

        [Fact]
        public void VerifiedPolicy_LocalMappingCandidateUsesSameP5HReconstruction()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"));
            string path = Path.Combine(root, "Value.cs");
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            Directory.CreateDirectory(root);
            File.WriteAllBytes(path, downloaded);

            try
            {
                ExternalSourceAcquisition acquisition = new();
                Assert.True(acquisition.TryConfigureMappings(
                    [new ExternalSourcePathMapping("/_/", root)]));
                Assert.True(acquisition.TryConfigureReconstructionPolicy(
                    ExternalSourceReconstructionPolicy.VerifiedLineEndings));

                Assert.True(acquisition.TryAcquire(
                    CreateDocument(expected),
                    sourceLink: null,
                    CreateConfiguration(),
                    out ValidatedExternalSourceMaterial material));
                Assert.Equal(ExternalSourceMaterialOrigin.LocalMapping, material.Origin);
                Assert.Equal(path, material.SourceIdentity);
                Assert.Equal(ExternalSourceMaterialExactness.ReconstructedExact, material.Exactness);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void VerifiedPolicy_CrlfToLf_PassesP5H()
        {
            byte[] downloaded = "class C\r\n{\r\n}\r\n"u8.ToArray();
            byte[] expected = "class C\n{\n}\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.True(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(expected, material.Image);
            Assert.Equal(ExternalSourceMaterialExactness.ReconstructedExact, material.Exactness);
            Assert.Equal(ExternalSourceLineEndingTransformation.CrlfToLf, material.Transformation);

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.CrlfToLfSuccessCount);
            Assert.Equal(2, statistics.P5HValidationAttempts);
        }

        [Fact]
        public void VerifiedPolicy_OriginalExactBytesWinWithoutReconstruction()
        {
            byte[] downloaded = "class C\n{\n}\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.True(acquisition.TryAcquire(
                CreateDocument(downloaded),
                CreateSourceLink(),
                CreateConfiguration(),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(ExternalSourceMaterialExactness.DirectExact, material.Exactness);
            Assert.Equal(ExternalSourceLineEndingTransformation.None, material.Transformation);

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.DirectExactSourceCount);
            Assert.Equal(0, statistics.ReconstructionAttemptCount);
            Assert.Equal(1, statistics.P5HValidationAttempts);
        }

        [Theory]
        [InlineData("return 1;\n", "return 2;\r\n")]
        [InlineData("\treturn 1;\n", "    return 1;\r\n")]
        [InlineData("return 1;", "return 1;\n")]
        public void VerifiedPolicy_NonLineEndingDifferencesFailClosed(
            string downloadedText,
            string expectedText)
        {
            byte[] downloaded = Encoding.UTF8.GetBytes(downloadedText);
            byte[] expected = Encoding.UTF8.GetBytes(expectedText);
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.False(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out _));
            Assert.Equal(1, acquisition.GetStatistics().ReconstructionFailureCount);
        }

        [Fact]
        public void VerifiedPolicy_MixedEndings_TransformsOnlyBareLf()
        {
            byte[] downloaded = "a\r\nb\nc\rd\n"u8.ToArray();
            byte[] expected = "a\r\nb\r\nc\rd\r\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.True(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(expected, material.Image);
            Assert.Equal(ExternalSourceLineEndingTransformation.LfToCrlf, material.Transformation);
        }

        [Fact]
        public void VerifiedPolicy_Utf8BomIsPreservedExactly()
        {
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] downloaded = bom.Concat("class C\n"u8.ToArray()).ToArray();
            byte[] expected = bom.Concat("class C\r\n"u8.ToArray()).ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.True(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out ValidatedExternalSourceMaterial material));
            Assert.Equal(expected, material.Image);
            Assert.True(material.Image.AsSpan()[..bom.Length].SequenceEqual(bom));
        }

        [Theory]
        [InlineData("")]
        [InlineData("class C { }")]
        public void VerifiedPolicy_NoLineEndingCandidateFailsClosed(string downloadedText)
        {
            byte[] downloaded = Encoding.UTF8.GetBytes(downloadedText);
            byte[] expected = "different"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.False(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration(),
                out _));

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.ReconstructionAttemptCount);
            Assert.Equal(0, statistics.ReconstructionBytesProduced);
            Assert.Equal(1, statistics.P5HValidationAttempts);
        }

        [Fact]
        public void VerifiedPolicy_Utf16ConfigurationDoesNotTransformBytes()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = ConfigureVerified(downloaded);

            Assert.False(acquisition.TryAcquire(
                CreateDocument(expected),
                CreateSourceLink(),
                CreateConfiguration("utf-16"),
                out _));

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(0, statistics.ReconstructionBytesProduced);
            Assert.Equal(1, statistics.P5HValidationAttempts);
        }

        [Fact]
        public void ReconstructedSuccess_IsPositiveCachedWithoutMoreWorkOrTraffic()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            CountingHandler handler = new(downloaded);
            ExternalSourceAcquisition acquisition = ConfigureVerified(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            ExternalSourceLinkDescriptor sourceLink = CreateSourceLink();
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();

            Assert.True(acquisition.TryAcquire(document, sourceLink, configuration, out ValidatedExternalSourceMaterial first));
            Assert.True(acquisition.TryAcquire(document, sourceLink, configuration, out ValidatedExternalSourceMaterial second));
            Assert.Same(first, second);
            Assert.Equal(1, handler.RequestCount);

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.ReconstructionAttemptCount);
            Assert.Equal(1, statistics.PositiveCacheHits);
        }

        [Fact]
        public void FailedReconstruction_IsNegativeCachedWithoutMoreWorkOrTraffic()
        {
            byte[] downloaded = "wrong\n"u8.ToArray();
            CountingHandler handler = new(downloaded);
            ExternalSourceAcquisition acquisition = ConfigureVerified(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument("expected\r\n"u8.ToArray());
            ExternalSourceLinkDescriptor sourceLink = CreateSourceLink();
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();

            Assert.False(acquisition.TryAcquire(document, sourceLink, configuration, out _));
            Assert.False(acquisition.TryAcquire(document, sourceLink, configuration, out _));
            Assert.Equal(1, handler.RequestCount);

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            Assert.Equal(1, statistics.ReconstructionAttemptCount);
            Assert.Equal(1, statistics.NegativeCacheHits);
        }

        [Fact]
        public void StrictAndVerifiedPolicies_AreContextLocal()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            ExternalSourceLinkDescriptor sourceLink = CreateSourceLink();
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();
            ExternalSourceAcquisition strict = Configure(downloaded);
            ExternalSourceAcquisition verified = ConfigureVerified(downloaded);

            Assert.False(strict.TryAcquire(document, sourceLink, configuration, out _));
            Assert.True(verified.TryAcquire(document, sourceLink, configuration, out _));
            Assert.Equal(0, strict.GetStatistics().ReconstructionAttemptCount);
            Assert.Equal(1, verified.GetStatistics().ReconstructionSuccessCount);
        }

        [Fact]
        public void EncodingConfiguration_IsPartOfAcquisitionCacheKey()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            CountingHandler handler = new(downloaded);
            ExternalSourceAcquisition acquisition = ConfigureVerified(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            ExternalSourceLinkDescriptor sourceLink = CreateSourceLink();

            Assert.False(acquisition.TryAcquire(
                document,
                sourceLink,
                CreateConfiguration("utf-16"),
                out _));
            Assert.True(acquisition.TryAcquire(
                document,
                sourceLink,
                CreateConfiguration("utf-8"),
                out _));
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task ConcurrentReconstruction_PerformsOneRequestAndOneAttempt()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            byte[] expected = "class C\r\n"u8.ToArray();
            TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            CountingHandler handler = new(async cancellationToken =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(cancellationToken);
                return downloaded;
            });
            ExternalSourceAcquisition acquisition = ConfigureVerified(handler);
            ExternalSourceDocumentDescriptor document = CreateDocument(expected);
            ExternalSourceLinkDescriptor sourceLink = CreateSourceLink();
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration();
            Task<bool> first = Task.Run(() => acquisition.TryAcquire(
                document,
                sourceLink,
                configuration,
                out _));
            await entered.Task;

            Assert.False(acquisition.TryAcquire(document, sourceLink, configuration, out _));
            release.SetResult();
            Assert.True(await first);
            Assert.Equal(1, handler.RequestCount);
            Assert.Equal(1, acquisition.GetStatistics().ReconstructionAttemptCount);
        }

        [Fact]
        public void ReconstructionPolicy_BecomesImmutableAfterAcquisitionStarts()
        {
            byte[] downloaded = "class C\n"u8.ToArray();
            ExternalSourceAcquisition acquisition = Configure(downloaded);
            Assert.False(acquisition.TryAcquire(
                CreateDocument("class C\r\n"u8.ToArray()),
                CreateSourceLink(),
                CreateConfiguration(),
                out _));

            Assert.False(acquisition.TryConfigureReconstructionPolicy(
                ExternalSourceReconstructionPolicy.VerifiedLineEndings));
        }

        private static ExternalSourceAcquisition Configure(byte[] bytes)
        {
            return Configure(new CountingHandler(bytes));
        }

        private static ExternalSourceAcquisition Configure(CountingHandler handler)
        {
            ExternalSourceAcquisition acquisition = new();
            Assert.True(acquisition.TryConfigureSourceLink(
                new ExternalSourceLinkClient(
                    handler,
                    new PublicResolver(),
                    TimeSpan.FromSeconds(5))));
            return acquisition;
        }

        private static ExternalSourceAcquisition ConfigureVerified(byte[] bytes)
        {
            return ConfigureVerified(new CountingHandler(bytes));
        }

        private static ExternalSourceAcquisition ConfigureVerified(CountingHandler handler)
        {
            ExternalSourceAcquisition acquisition = Configure(handler);
            Assert.True(acquisition.TryConfigureReconstructionPolicy(
                ExternalSourceReconstructionPolicy.VerifiedLineEndings));
            return acquisition;
        }

        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            string? defaultEncoding = "utf-8")
        {
            return new ExternalCSharpCompilationConfiguration(
                new CSharpParseOptions(LanguageVersion.CSharp12),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                "test",
                "test",
                1,
                defaultEncoding,
                fallbackEncodingWebName: null);
        }

        private static ExternalSourceDocumentDescriptor CreateDocument(byte[] expected)
        {
            return new ExternalSourceDocumentDescriptor(
                "/_/Value.cs",
                Sha256DocumentHashAlgorithm,
                ImmutableArray.Create(SHA256.HashData(expected)),
                CSharpLanguage,
                embeddedSource: null);
        }

        private static ExternalSourceLinkDescriptor CreateSourceLink()
        {
            Assert.True(ExternalSourceLinkDescriptor.TryCreate(
                ImmutableArray.Create(
                    Encoding.UTF8.GetBytes(
                        "{\"documents\":{\"/_/*\":\"https://public.test/*\"}}")),
                out ExternalSourceLinkDescriptor descriptor));
            return descriptor;
        }

        private sealed class PublicResolver : IExternalSourceHostResolver
        {
            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new[] { IPAddress.Parse("8.8.8.8") });
            }
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            private readonly Func<CancellationToken, Task<byte[]>> getBytes;

            public CountingHandler(byte[] bytes)
                : this(_ => Task.FromResult(bytes))
            {
            }

            public CountingHandler(Func<CancellationToken, Task<byte[]>> getBytes)
            {
                this.getBytes = getBytes;
            }

            public int RequestCount { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestCount++;
                byte[] bytes = await getBytes(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(bytes)
                };
            }
        }
    }
}
