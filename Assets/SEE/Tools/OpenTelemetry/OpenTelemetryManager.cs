using System;
using System.IO;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SEE.Controls;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Handles the setup, management, and shutdown of OpenTelemetry tracing for Unity applications.
    /// </summary>
    public class OpenTelemetryManager
    {
        /// <summary>
        /// The OpenTelemetry tracer provider instance.
        /// </summary>
        private TracerProvider tracerProvider;

        /// <summary>
        /// Exporter responsible for writing traces to a local file.
        /// </summary>
        private TraceFileExporter traceFileExporter;

        /// <summary>
        /// Path to the directory where trace logs are exported.
        /// </summary>
        private readonly string exportDirectoryPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryManager"/> class.
        /// </summary>
        /// <param name="exportFolderName">
        /// Optional name of the folder within StreamingAssets where local traces will be stored.
        /// Defaults to "OpenTelemetryLogs".
        /// </param>
        public OpenTelemetryManager(string exportFolderName = "OpenTelemetryLogs")
        {
            exportDirectoryPath = Path.Combine(Application.dataPath, "StreamingAssets", exportFolderName, "TraceLogs");
        }

        /// <summary>
        /// Initializes the OpenTelemetry system based on the current telemetry mode.
        /// Logs a warning if already initialized.
        /// </summary>
        public void Initialize()
        {
            if (tracerProvider != null)
            {
                Debug.LogWarning("OpenTelemetry is already initialized.");
                return;
            }

            switch (User.UserSettings.Instance?.Telemetry.Mode)
            {
                case TelemetryMode.Disabled:
                    Debug.Log("Telemetry is disabled. Skipping OpenTelemetry initialization.");
                    return;

                case TelemetryMode.Local:
                    InitializeLocalExporter();
                    break;

                case TelemetryMode.Remote:
                    InitializeRemoteExporter(User.UserSettings.Instance?.Telemetry.ServerURL);
                    break;
            }
        }

        /// <summary>
        /// Initializes the OpenTelemetry system with a remote OTLP exporter using HTTP/Protobuf.
        /// Internally uses a <see cref="BatchActivityExportProcessor"/> with default settings.
        ///
        /// Export behavior:
        /// - Traces are buffered in memory.
        /// - Up to 512 spans are exported in a batch every 5 seconds.
        /// - All remaining spans are exported on shutdown or disposal.
        /// - The export queue can hold up to 2048 spans before forced flush.
        /// </summary>
        /// <param name="serverUrl">
        /// The OTLP endpoint that accepts HTTP/Protobuf (e.g. http://localhost:4318).
        /// </param>
        private void InitializeRemoteExporter(string serverUrl)
        {
            try
            {
                tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("SEE.Tracing")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("SEEOpenTelemetryTracking"))
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(serverUrl);
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        // Do not add custom headers – OTLP/HTTP expects standard requests
                    })
                    .Build();

                Debug.Log($"OpenTelemetry initialized for HTTP/Protobuf export to {serverUrl} (batching every ~5s).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize remote OpenTelemetry exporter: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes the OpenTelemetry system with a local file-based trace exporter.
        /// </summary>
        private void InitializeLocalExporter()
        {
            try
            {
                traceFileExporter = new TraceFileExporter(exportDirectoryPath);

                tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("SEE.Tracing")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SEEOpenTelemetryTracking"))
                    .AddProcessor(new SimpleActivityExportProcessor(traceFileExporter))
                    .Build();

                Debug.Log($"OpenTelemetry (local) initialized. Logs in: {exportDirectoryPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Local OpenTelemetry initialization failed: {exception.Message}");
            }
        }

        /// <summary>
        /// Properly shuts down the OpenTelemetry tracer provider and its associated exporter.
        /// Logs a warning if not previously initialized.
        /// </summary>
        public void Shutdown(TracingHelper helper,bool host)
        {
            if (host)
            {
                TracingHelperService.Instance?.TrackSessionEnd();
            }
            if (tracerProvider == null)
            {
                Debug.LogWarning("OpenTelemetry is not initialized.");
                return;
            }

            tracerProvider.Dispose();
            tracerProvider = null;

            traceFileExporter?.CheckGracefulShutdown();
            traceFileExporter = null;

            Debug.Log("OpenTelemetry shutdown complete.");
        }
    }
}
