namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Enumeration for the different telemetry modes.
    /// </summary>
    public enum TelemetryMode
    {
        /// <summary>
        /// Telemetry is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// Telemetry data is stored locally.
        /// </summary>
        Local,
        /// <summary>
        /// Telemetry data is sent to a remote server.
        /// </summary>
        Remote
    }
}
