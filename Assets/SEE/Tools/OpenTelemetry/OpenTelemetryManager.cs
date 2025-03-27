using System;
using System.IO;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UnityEngine;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Manages the initialization, operation, and shutdown of OpenTelemetry tracing.
    /// </summary>
    public class OpenTelemetryManager
    {
        private static TracerProvider _tracerProvider; // Holds the OpenTelemetry tracer provider instance.
        private static string _exportDirectoryPath; // Directory path for trace file export.

        /// <summary>
        /// Initializes the OpenTelemetry tracer provider and prepares trace export.
        /// If already initialized, a warning will be logged.
        /// </summary>
        public static void Initialize()
        {
            if (_tracerProvider != null)
            {
                Debug.LogWarning("TracerProvider is already initialized.");
                return;
            }

            try
            {
                // Generate a path for trace export directory.
                _exportDirectoryPath = Path.Combine(Application.dataPath, "StreamingAssets", "OpenTelemetryLogs", "TraceLogs");

                // Configure and build the OpenTelemetry tracer provider.
                _tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSource("SEE.Tracing") 
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("SEEOpenTelemetryTracking"))
                    .AddProcessor(
                        new SimpleActivityExportProcessor(
                            new TraceFileExporter(_exportDirectoryPath))) // Use the new exporter.
                    .Build();

                Debug.Log($"TracerProvider initialized. Trace export directory: {_exportDirectoryPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize OpenTelemetry: {ex.Message}");
            }
        }

        /// <summary>
        /// Shuts down the OpenTelemetry tracer provider.
        /// </summary>
        public static void Shutdown()
        {
            if (_tracerProvider == null)
            {
                Debug.LogWarning("TracerProvider is not initialized.");
                return;
            }

            try
            {
                // Dispose of the tracer provider.
                _tracerProvider.Dispose();
                _tracerProvider = null;
                Debug.Log("TracerProvider disposed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to shut down OpenTelemetry: {ex.Message}");
            }
        }
    }
}