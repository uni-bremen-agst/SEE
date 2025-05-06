using System;
using System.IO;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UnityEngine;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Handles the setup, management, and shutdown of OpenTelemetry tracing for Unity applications.
    /// </summary>
    public class OpenTelemetryManager : IDisposable
    {
        private TracerProvider tracerProvider;
        private TraceFileExporter traceFileExporter;
        private readonly string exportDirectoryPath;

        public OpenTelemetryManager(string exportFolderName = "OpenTelemetryLogs")
        {
            exportDirectoryPath = Path.Combine(Application.dataPath, "StreamingAssets", exportFolderName, "TraceLogs");
        }

        /// <summary>
        /// Initializes the OpenTelemetry tracer provider and sets up trace export to a file.
        /// Logs a warning if already initialized.
        /// </summary>
        public void Initialize()
        {
            if (tracerProvider != null)
            {
                Debug.LogWarning("OpenTelemetry is already initialized.");
                return;
            }

            try
            {
                traceFileExporter = new TraceFileExporter(exportDirectoryPath);

                tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("SEE.Tracing")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("SEEOpenTelemetryTracking"))
                    .AddProcessor(new SimpleActivityExportProcessor(traceFileExporter))
                    .Build();

                Debug.Log($"OpenTelemetry initialized. Logs in: {exportDirectoryPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenTelemetry initialization failed: {ex.Message}");
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