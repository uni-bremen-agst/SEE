using System;
using System.IO;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SEE.Controls;
using UnityEngine;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Handles the setup, management, and shutdown of OpenTelemetry tracing for Unity applications.
    /// </summary>
    public class OpenTelemetryManager : IDisposable
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

            switch (SceneSettings.telemetryMode)
            {
                case TelemetryMode.Disabled:
                    Debug.Log("Telemetry is disabled. Skipping OpenTelemetry initialization.");
                    return;

                case TelemetryMode.Local:
                    InitializeLocalExporter();
                    break;

                case TelemetryMode.Remote:
                    InitializeRemoteExporter(SceneSettings.CustomTelemetryServerURL);
                    break;
            }
        }

        /// <summary>
        /// Initializes the OpenTelemetry system with a remote OTLP exporter.
        /// </summary>
        /// <param name="serverUrl">The URL of the remote telemetry endpoint. Must not be null or empty.</param>
        private void InitializeRemoteExporter(string serverUrl)
        {
            try
            {
                // Uncomment and configure this block when using remote OTLP exporter.
                /*
                tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("SEE.Tracing")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SEEOpenTelemetryTracking"))
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(serverUrl);
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    })
                    .Build();
                */

                Debug.Log($"OpenTelemetry (remote) initialized. Sending to: {serverUrl}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Remote OpenTelemetry initialization failed: {exception.Message}");
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
        public void Shutdown()
        {
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

        /// <summary>
        /// Disposes the manager and cleans up the provider and exporter.
        /// </summary>
        public void Dispose()
        {
            Shutdown();
        }
    }
}
