using NUnit.Framework;
using SEE.Tools.OpenTelemetry;

/// <summary>
/// User interface components and their tests.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Tests connection validation without a running network manager or persisted user settings.
    /// </summary>
    internal class TestOpeningMenuConnection
    {
        /// <summary>
        /// Accepts the full UDP port range documented by SEE, including both boundaries.
        /// </summary>
        /// <param name="port">A valid port.</param>
        [TestCase("0")]
        [TestCase("1")]
        [TestCase("7777")]
        [TestCase("65535")]
        public void AcceptsValidPort(string port)
        {
            OpeningMenuConnection connection = new()
            {
                Port = port
            };
            Assert.That(connection.TryValidate(out string error), Is.True);
            Assert.That(error, Is.Empty);
        }

        /// <summary>
        /// Rejects malformed and out-of-range ports before settings can be changed.
        /// </summary>
        /// <param name="port">An invalid port.</param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("-1")]
        [TestCase("65536")]
        [TestCase("2.5")]
        [TestCase("abc")]
        public void RejectsInvalidPort(string port)
        {
            OpeningMenuConnection connection = new()
            {
                Port = port
            };
            Assert.That(connection.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("port"));
        }

        /// <summary>
        /// Rejects addresses outside SEE's numeric IPv4 syntax.
        /// </summary>
        /// <param name="address">An invalid server address.</param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("localhost")]
        [TestCase("127.0.0")]
        [TestCase("256.0.0.1")]
        [TestCase("+1.0.0.1")]
        [TestCase("1.0.0.1.2")]
        public void RejectsInvalidAddress(string address)
        {
            OpeningMenuConnection connection = new()
            {
                ServerAddress = address
            };
            Assert.That(connection.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("IPv4"));
        }

        /// <summary>
        /// Preserves the optional backend configuration supported by the original settings dialog.
        /// </summary>
        [Test]
        public void AllowsEmptyBackend()
        {
            OpeningMenuConnection connection = new()
            {
                BackendAPI = string.Empty
            };
            Assert.That(connection.TryValidate(out _), Is.True);
        }

        /// <summary>
        /// Rejects unsupported protocols and relative backend paths.
        /// </summary>
        /// <param name="endpoint">An invalid endpoint.</param>
        [TestCase("ftp://example.org/api/")]
        [TestCase("api/v1/")]
        public void RejectsInvalidBackend(string endpoint)
        {
            OpeningMenuConnection connection = new()
            {
                BackendAPI = endpoint
            };
            Assert.That(connection.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("backend"));
        }

        /// <summary>
        /// Requires a valid telemetry endpoint only when remote telemetry is enabled.
        /// </summary>
        /// <param name="mode">The telemetry mode.</param>
        /// <param name="valid">Whether an empty endpoint should be accepted.</param>
        [TestCase(TelemetryMode.Disabled, true)]
        [TestCase(TelemetryMode.Local, true)]
        [TestCase(TelemetryMode.Remote, false)]
        public void RequiresEndpointOnlyForRemoteTelemetry(TelemetryMode mode, bool valid)
        {
            OpeningMenuConnection connection = new()
            {
                TelemetryMode = mode,
                TelemetryURL = string.Empty
            };
            Assert.That(connection.TryValidate(out _), Is.EqualTo(valid));
        }

        /// <summary>
        /// Accepts a remote HTTP telemetry endpoint.
        /// </summary>
        [Test]
        public void AcceptsRemoteTelemetryEndpoint()
        {
            OpeningMenuConnection connection = new()
            {
                TelemetryMode = TelemetryMode.Remote,
                TelemetryURL = "https://example.org/v1/traces"
            };
            Assert.That(connection.TryValidate(out _), Is.True);
        }

        /// <summary>
        /// Rejects an unsupported telemetry enum value rather than applying it.
        /// </summary>
        [Test]
        public void RejectsInvalidTelemetryMode()
        {
            OpeningMenuConnection connection = new()
            {
                TelemetryMode = (TelemetryMode)99
            };
            Assert.That(connection.TryValidate(out _), Is.False);
        }
    }
}
