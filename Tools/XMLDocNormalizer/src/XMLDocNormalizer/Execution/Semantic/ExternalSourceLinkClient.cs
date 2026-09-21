using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Resolves Source Link hosts for context-local network safety checks.
    /// </summary>
    internal interface IExternalSourceHostResolver
    {
        /// <summary>
        /// Resolves one host under the request timeout.
        /// </summary>
        /// <param name="host">The URI host without credentials.</param>
        /// <param name="cancellationToken">The request timeout token.</param>
        /// <returns>The resolved addresses.</returns>
        ValueTask<IPAddress[]> GetHostAddressesAsync(
            string host,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Retrieves bounded HTTPS Source Link candidate bytes under an explicit
    /// network policy. Source Link provenance remains a retrieval hint and is
    /// never a source-identity boundary.
    /// </summary>
    internal sealed class ExternalSourceLinkClient
    {
        /// <summary>
        /// The maximum accepted decompressed source response size: four mebibytes.
        /// </summary>
        public const int DefaultMaximumResponseBytes = 4 * 1024 * 1024;

        /// <summary>
        /// The maximum number of redirects followed per acquisition: three.
        /// </summary>
        public const int MaximumRedirects = 3;

        /// <summary>
        /// Stores the context-local reusable HTTP client.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Resolves hostnames before every initial or redirected request.
        /// </summary>
        private readonly IExternalSourceHostResolver hostResolver;

        /// <summary>
        /// Bounds DNS resolution, redirects, headers, and body streaming.
        /// </summary>
        private readonly TimeSpan timeout;

        /// <summary>
        /// Bounds the decompressed candidate bytes retained in memory.
        /// </summary>
        private readonly int maximumResponseBytes;

        /// <summary>
        /// Initializes a testable context-local Source Link client.
        /// </summary>
        /// <param name="handler">
        /// The reusable handler. Automatic redirects must remain disabled.
        /// </param>
        /// <param name="hostResolver">The timeout-aware hostname resolver.</param>
        /// <param name="timeout">The positive total acquisition timeout.</param>
        /// <param name="maximumResponseBytes">
        /// The positive decompressed response limit.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="handler"/> or
        /// <paramref name="hostResolver"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a limit is not positive.
        /// </exception>
        public ExternalSourceLinkClient(
            HttpMessageHandler handler,
            IExternalSourceHostResolver hostResolver,
            TimeSpan timeout,
            int maximumResponseBytes = DefaultMaximumResponseBytes)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(hostResolver);

            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResponseBytes);

            httpClient = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            this.hostResolver = hostResolver;
            this.timeout = timeout;
            this.maximumResponseBytes = maximumResponseBytes;
        }

        /// <summary>
        /// Creates the production HTTPS client with decompression, no proxy,
        /// no cookies, manual redirects, and address-pinned socket connects.
        /// </summary>
        /// <returns>A context-local production Source Link client.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively if runtime handler configuration is unavailable.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown transitively if a built-in positive limit is invalid.
        /// </exception>
        public static ExternalSourceLinkClient CreateDefault()
        {
            SocketsHttpHandler handler = new()
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip
                    | DecompressionMethods.Deflate
                    | DecompressionMethods.Brotli,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                Credentials = null,
                UseCookies = false,
                UseProxy = false,
                ConnectCallback = ConnectPublicAddressAsync
            };

            return new ExternalSourceLinkClient(
                handler,
                SystemHostResolver.Instance,
                TimeSpan.FromSeconds(15));
        }

        /// <summary>
        /// Tries to download exact decompressed bytes from a safe HTTPS URI.
        /// </summary>
        /// <param name="sourceUri">The Source Link retrieval hint.</param>
        /// <param name="image">The bounded candidate bytes when successful.</param>
        /// <returns>
        /// <see langword="true"/> only for an allowed target and successful
        /// bounded response; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Every redirect is revalidated. HTTP status, TLS, hostname, and URI
        /// do not establish source identity; the caller must invoke P5H.
        /// </remarks>
        public bool TryDownload(Uri sourceUri, out ImmutableArray<byte> image)
        {
            if (sourceUri == null)
            {
                image = default;
                return false;
            }

            try
            {
                using CancellationTokenSource timeoutSource = new(timeout);
                byte[]? bytes = TryDownloadAsync(sourceUri, timeoutSource.Token)
                    .GetAwaiter()
                    .GetResult();

                if (bytes == null)
                {
                    image = default;
                    return false;
                }

                image = ImmutableCollectionsMarshal.AsImmutableArray(bytes);
                return true;
            }
            catch (HttpRequestException)
            {
                image = default;
                return false;
            }
            catch (IOException)
            {
                image = default;
                return false;
            }
            catch (OperationCanceledException)
            {
                image = default;
                return false;
            }
            catch (SocketException)
            {
                image = default;
                return false;
            }
            catch (ArgumentException)
            {
                image = default;
                return false;
            }
        }

        /// <summary>
        /// Applies target policy, manual redirects, status checks, and bounded reads.
        /// </summary>
        /// <param name="initialUri">The initial Source Link URI.</param>
        /// <param name="cancellationToken">The total timeout token.</param>
        /// <returns>The exact response bytes, or <see langword="null"/>.</returns>
        private async Task<byte[]?> TryDownloadAsync(
            Uri initialUri,
            CancellationToken cancellationToken)
        {
            Uri currentUri = initialUri;

            for (int redirectCount = 0; redirectCount <= MaximumRedirects; redirectCount++)
            {
                if (!await IsAllowedTargetAsync(currentUri, cancellationToken))
                {
                    return null;
                }

                using HttpRequestMessage request = new(HttpMethod.Get, currentUri);
                using HttpResponseMessage response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (IsRedirect(response.StatusCode))
                {
                    if (redirectCount == MaximumRedirects
                        || !TryResolveRedirect(
                            currentUri,
                            response.Headers.Location,
                            out currentUri))
                    {
                        return null;
                    }

                    continue;
                }

                if (!response.IsSuccessStatusCode
                    || HasOversizedContentLength(response.Content.Headers))
                {
                    return null;
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(
                    cancellationToken);
                return await ReadBoundedAsync(stream, cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// Validates URI syntax and resolves every hostname to public addresses.
        /// </summary>
        /// <param name="uri">The initial or redirected URI.</param>
        /// <param name="cancellationToken">The total timeout token.</param>
        /// <returns><see langword="true"/> only for an allowed public target.</returns>
        private async ValueTask<bool> IsAllowedTargetAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            if (!uri.IsAbsoluteUri
                || !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || uri.Host.Length == 0
                || uri.UserInfo.Length != 0
                || uri.Fragment.Length != 0
                || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            IPAddress[] addresses = await hostResolver.GetHostAddressesAsync(
                uri.DnsSafeHost,
                cancellationToken);
            return addresses.Length != 0 && addresses.All(IsPublicAddress);
        }

        /// <summary>
        /// Creates a redirect URI while retaining query strings and rejecting fragments.
        /// </summary>
        /// <param name="currentUri">The response URI.</param>
        /// <param name="location">The response Location header.</param>
        /// <param name="redirectUri">The absolute redirect target.</param>
        /// <returns><see langword="true"/> when a target can be constructed.</returns>
        private static bool TryResolveRedirect(
            Uri currentUri,
            Uri? location,
            out Uri redirectUri)
        {
            if (location == null)
            {
                redirectUri = null!;
                return false;
            }

            try
            {
                redirectUri = location.IsAbsoluteUri
                    ? location
                    : new Uri(currentUri, location);
                return redirectUri.Fragment.Length == 0;
            }
            catch (UriFormatException)
            {
                redirectUri = null!;
                return false;
            }
        }

        /// <summary>
        /// Determines whether a response is one of the supported HTTP redirects.
        /// </summary>
        /// <param name="statusCode">The response status.</param>
        /// <returns><see langword="true"/> for 301, 302, 303, 307, or 308.</returns>
        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.MovedPermanently
                || statusCode == HttpStatusCode.Found
                || statusCode == HttpStatusCode.SeeOther
                || statusCode == HttpStatusCode.TemporaryRedirect
                || statusCode == HttpStatusCode.PermanentRedirect;
        }

        /// <summary>
        /// Checks a declared content length without trusting it as a read boundary.
        /// </summary>
        /// <param name="headers">The response content headers.</param>
        /// <returns><see langword="true"/> when the declaration exceeds the limit.</returns>
        private bool HasOversizedContentLength(HttpContentHeaders headers)
        {
            return headers.ContentLength > maximumResponseBytes;
        }

        /// <summary>
        /// Reads decompressed response bytes while enforcing the streaming limit.
        /// </summary>
        /// <param name="stream">The response content stream.</param>
        /// <param name="cancellationToken">The total timeout token.</param>
        /// <returns>The complete bytes, or <see langword="null"/> when oversized.</returns>
        private async Task<byte[]?> ReadBoundedAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            using MemoryStream buffer = new();
            byte[] chunk = new byte[81920];

            while (true)
            {
                int count = await stream.ReadAsync(chunk, cancellationToken);

                if (count == 0)
                {
                    return buffer.ToArray();
                }

                if (buffer.Length + count > maximumResponseBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, count);
            }
        }

        /// <summary>
        /// Resolves and connects the production socket only to a revalidated
        /// public address, preventing a second unchecked DNS lookup.
        /// </summary>
        /// <param name="context">The HTTP connection context.</param>
        /// <param name="cancellationToken">The connection timeout token.</param>
        /// <returns>The connected owning network stream.</returns>
        /// <exception cref="HttpRequestException">
        /// Thrown when DNS returns a blocked address or a socket cannot connect.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when DNS resolution or connection exceeds its cancellation bound.
        /// </exception>
        private static async ValueTask<Stream> ConnectPublicAddressAsync(
            SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(
                context.DnsEndPoint.Host,
                cancellationToken);
            IPAddress[] allowed = addresses.Where(IsPublicAddress).ToArray();

            if (allowed.Length == 0 || allowed.Length != addresses.Length)
            {
                throw new HttpRequestException("Source Link host resolved to a blocked address.");
            }

            Socket socket = new(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(
                    allowed,
                    context.DnsEndPoint.Port,
                    cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException exception)
            {
                socket.Dispose();
                throw new HttpRequestException(
                    "Source Link public address could not be reached.",
                    exception);
            }
            catch (OperationCanceledException exception)
            {
                socket.Dispose();
                throw new OperationCanceledException(
                    "Source Link connection was canceled.",
                    exception,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Classifies routable public addresses accepted by Source Link policy.
        /// </summary>
        /// <param name="address">The resolved or literal address.</param>
        /// <returns>
        /// <see langword="false"/> for unspecified, loopback, private,
        /// link-local, multicast, and other non-public ranges.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="address"/> is <see langword="null"/>.
        /// </exception>
        internal static bool IsPublicAddress(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.IsIPv4MappedToIPv6)
            {
                return IsPublicAddress(address.MapToIPv4());
            }

            if (IPAddress.IsLoopback(address)
                || address.Equals(IPAddress.Any)
                || address.Equals(IPAddress.IPv6Any)
                || address.Equals(IPAddress.None)
                || address.Equals(IPAddress.IPv6None))
            {
                return false;
            }

            byte[] bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return bytes[0] != 0
                    && bytes[0] != 10
                    && bytes[0] != 127
                    && !(bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                    && !(bytes[0] == 169 && bytes[1] == 254)
                    && !(bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    && !(bytes[0] == 192 && bytes[1] == 168)
                    && bytes[0] < 224;
            }

            return address.AddressFamily == AddressFamily.InterNetworkV6
                && !address.IsIPv6LinkLocal
                && !address.IsIPv6SiteLocal
                && bytes[0] != 0xff
                && (bytes[0] & 0xfe) != 0xfc;
        }

        /// <summary>
        /// Uses the runtime DNS APIs under caller cancellation.
        /// </summary>
        private sealed class SystemHostResolver : IExternalSourceHostResolver
        {
            /// <summary>
            /// Gets the stateless resolver instance.
            /// </summary>
            /// <value>The process-independent runtime DNS resolver.</value>
            public static SystemHostResolver Instance { get; } = new();

            /// <inheritdoc/>
            public ValueTask<IPAddress[]> GetHostAddressesAsync(
                string host,
                CancellationToken cancellationToken)
            {
                return new ValueTask<IPAddress[]>(
                    Dns.GetHostAddressesAsync(host, cancellationToken));
            }
        }
    }
}
