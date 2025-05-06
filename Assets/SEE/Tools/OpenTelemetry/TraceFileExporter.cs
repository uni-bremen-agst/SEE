using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using OpenTelemetry;
using Debug = UnityEngine.Debug;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Custom OpenTelemetry exporter that writes activity trace data to a JSON file.
    /// </summary>
    public class TraceFileExporter : BaseExporter<Activity>, IDisposable
    {
        private readonly string filePath; // Full path to the output trace file.
        private bool disposed = false;

        /// <summary>
        /// Initializes the trace file exporter and creates the output file with a session header.
        /// </summary>
        /// <param name="directoryPath">Directory where trace files will be stored.</param>
        public TraceFileExporter(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            filePath = Path.Combine(directoryPath, $"Traces-Session-{timestamp}.json");

            try
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Trace Export Session Started...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create trace file: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a batch of activity traces to the export file.
        /// </summary>
        /// <param name="batch">The batch of activity traces.</param>
        /// <returns>ExportResult.Success if export succeeds; otherwise, ExportResult.Failure.</returns>
        public override ExportResult Export(in Batch<Activity> batch)
        {
            try
            {
                using var writer = new StreamWriter(filePath, true, Encoding.UTF8);

                foreach (var activity in batch)
                {
                    var activityData = new
                    {
                        TraceId = activity.TraceId.ToString(),
                        DisplayName = activity.DisplayName,
                        StartTime = activity.StartTimeUtc,
                        Duration = activity.Duration,
                        Tags = activity.Tags
                    };

                    writer.WriteLine(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {JsonConvert.SerializeObject(activityData)}");
                }

                return ExportResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export traces: {ex.Message}");
                return ExportResult.Failure;
            }
        }

        /// <summary>
        /// Writes a shutdown message to the trace file if the exporter was not already disposed.
        /// </summary>
        public void CheckGracefulShutdown()
        {
            if (disposed) return;

            try
            {
                using var writer = new StreamWriter(filePath, true, Encoding.UTF8);
                writer.WriteLine("Trace Export Session Ended. Tracer shut down cleanly.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write trace shutdown message: {ex.Message}");
            }

            disposed = true;
        }

        /// <summary>
        /// Releases all resources used by the exporter.
        /// </summary>
        public void Dispose()
        {
            CheckGracefulShutdown();
            GC.SuppressFinalize(this);
        }
    }
}