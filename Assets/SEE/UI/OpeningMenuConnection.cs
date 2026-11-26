using System;
using System.Globalization;
using System.Linq;
using SEE.Tools.OpenTelemetry;
using SEE.UserSettings;

/// <summary>
/// User interface components for SEE.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// A connection draft that can be validated before changing the active user settings.
    /// </summary>
    internal sealed class OpeningMenuConnection
    {
        /// <summary>
        /// The numeric IPv4 address of the server.
        /// </summary>
        internal string ServerAddress { get; set; } = "127.0.0.1";
        /// <summary>
        /// The UDP port as entered by the user, before integer validation.
        /// </summary>
        internal string Port { get; set; } = "7777";
        /// <summary>
        /// The room password; an empty string permits a room without a password.
        /// </summary>
        internal string RoomPassword { get; set; } = string.Empty;
        /// <summary>
        /// The optional HTTP or HTTPS backend API endpoint.
        /// </summary>
        internal string BackendAPI { get; set; } = "http://localhost:8080/api/v1/";
        /// <summary>
        /// The requested telemetry mode.
        /// </summary>
        internal TelemetryMode TelemetryMode { get; set; } = TelemetryMode.Disabled;
        /// <summary>
        /// The HTTP or HTTPS telemetry endpoint, required only for remote telemetry.
        /// </summary>
        internal string TelemetryURL { get; set; } = string.Empty;

        /// <summary>
        /// Copies the active configuration into an independently editable draft.
        /// </summary>
        /// <param name="settings">The non-null user settings with an initialized network manager.</param>
        /// <returns>A draft containing the current connection and telemetry settings.</returns>
        /// <exception cref="ArgumentNullException">The settings are null.</exception>
        internal static OpeningMenuConnection FromSettings(UserSetting settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return new()
            {
                ServerAddress = settings.Network.ServerIP4Address,
                Port = settings.Network.ServerPort.ToString(CultureInfo.InvariantCulture),
                RoomPassword = settings.Network.RoomPassword,
                BackendAPI = settings.Network.BackendServerAPI,
                TelemetryMode = settings.Telemetry.Mode,
                TelemetryURL = settings.Telemetry.ServerURL
            };
        }

        /// <summary>
        /// Validates the complete draft without modifying settings or starting a connection.
        /// </summary>
        /// <param name="error">The validation message, or an empty string when the draft is valid.</param>
        /// <returns>Whether all required values are valid.</returns>
        internal bool TryValidate(out string error)
        {
            string[] octets = (ServerAddress ?? string.Empty).Trim().Split('.');
            if (octets.Length != 4 || !octets.All(IsIPv4Octet))
            {
                error = "Enter a valid IPv4 address, for example 127.0.0.1.";
            }
            else if (!int.TryParse(Port, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) || port < 0 || port > 65535)
            {
                error = "The port must be a number between 0 and 65535.";
            }
            else if (!string.IsNullOrWhiteSpace(BackendAPI) && !IsHttpURL(BackendAPI))
            {
                error = "Enter a valid HTTP or HTTPS backend API URL.";
            }
            else if (!Enum.IsDefined(typeof(TelemetryMode), TelemetryMode))
            {
                error = "Select a valid telemetry mode.";
            }
            else if (TelemetryMode == TelemetryMode.Remote && !IsHttpURL(TelemetryURL))
            {
                error = "Enter a valid HTTP or HTTPS telemetry URL.";
            }
            else
            {
                error = string.Empty;
            }

            return error.Length == 0;
        }

        /// <summary>
        /// Applies a validated draft to the current configuration without saving it or connecting.
        /// </summary>
        /// <param name="settings">The non-null destination with an initialized network manager.</param>
        /// <exception cref="ArgumentNullException">The destination is null.</exception>
        /// <exception cref="InvalidOperationException">The draft is invalid.</exception>
        internal void ApplyTo(UserSetting settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (!TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            settings.Network.ServerIP4Address = ServerAddress.Trim();
            settings.Network.ServerPort = int.Parse(Port, CultureInfo.InvariantCulture);
            settings.Network.RoomPassword = RoomPassword ?? string.Empty;
            settings.Network.BackendServerAPI = string.IsNullOrWhiteSpace(BackendAPI) ? string.Empty : BackendAPI.Trim().TrimEnd('/') + "/";
            settings.Telemetry.Mode = TelemetryMode;
            settings.Telemetry.ServerURL = TelemetryURL?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Checks one unsigned decimal IPv4 octet.
        /// </summary>
        /// <param name="value">The octet to check; may be null.</param>
        /// <returns>Whether the value consists of decimal digits representing a byte.</returns>
        private static bool IsIPv4Octet(string value)
        {
            return !string.IsNullOrEmpty(value) && value.All(character => character >= '0' && character <= '9') && byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// Checks an absolute HTTP or HTTPS endpoint.
        /// </summary>
        /// <param name="value">The endpoint to check; may be null.</param>
        /// <returns>Whether the endpoint is an absolute HTTP or HTTPS URL with a host.</returns>
        private static bool IsHttpURL(string value)
        {
            return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri uri) && !string.IsNullOrEmpty(uri.Host) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
