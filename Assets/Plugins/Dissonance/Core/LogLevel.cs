namespace Dissonance
{
    public enum LogLevel
    {
        /// <summary>
        /// Per-frame diagnostic events.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Significant diagnostic events.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Significant events which occur under normal operation.
        /// </summary>
        Info = 2,

        /// <summary>
        /// Non-critical errors, which deserve investigation.
        /// </summary>
        Warn = 3,

        /// <summary>
        /// Critical errors caused by external factors, outside of missuse or bugs.
        /// </summary>
        Error = 4
    }
}
