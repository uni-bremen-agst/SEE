using System.Net.Http;
using UnityEngine;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Provides a centralized service for tracing user and system actions.
    /// Holds a single instance of <see cref="TracingHelper"/> for the local player.
    /// </summary>
    public static class TracingHelperService
    {
        /// <summary>
        /// The tracing helper instance for the local player.
        /// Must be initialized once at application start.
        /// </summary>
        public static TracingHelper Instance { get; private set; }

        /// <summary>
        /// The OpenTelemetry manager responsible for exporter and provider setup.
        /// </summary>
        private static OpenTelemetryManager manager;

        /// <summary>
        /// Initializes the tracing service with the given player name.
        /// Should only be called once for the local player.
        /// </summary>
        /// <param name="playerName">The name of the player whose actions will be traced. Must not be null.</param>
        public static void Initialize(string playerName)
        {
            manager = new OpenTelemetryManager();
            manager.Initialize();
            Instance = new TracingHelper("SEE.Tracing", playerName);
        }
        /// <summary>
        /// Shuts down the tracing service and exporter cleanly.
        /// Should be called before application exit.
        /// </summary>
        public static void Shutdown(bool host)
        {
            if (manager != null)
            {
                manager.Shutdown(Instance,host);
                manager = null;
            }

            Instance = null;
        }
    }
}
