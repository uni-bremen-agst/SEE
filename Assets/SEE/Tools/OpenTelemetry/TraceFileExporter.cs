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
        /// <summary>
        /// Full path to the output trace file.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// Indicates whether the exporter has already been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes the trace file exporter and creates the output file with a session header.
        /// </summary>
        /// <param name="directoryPath">Directory where trace files will be stored. Must not be null.</param>
        public TraceFileExporter(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            filePath = Path.Combine(directoryPath, $"Traces-Session-{timestamp}.json");

            try
            {
                StreamWriter writer = new(filePath, false, Encoding.UTF8);
                writer.WriteLine("Trace Export Session Started...");
                writer.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to create trace file: {exception.Message}.\n");
            }
        }

        /// <summary>
        /// Writes a batch of activity traces to the export file.
        /// </summary>
        /// <param name="batch">The batch of activity traces to export. Must not be null.</param>
        /// <returns><see cref="ExportResult.Success"/> if export succeeds; otherwise, <see cref="ExportResult.Failure"/>.</returns>
        public override ExportResult Export(in Batch<Activity> batch)
        {
            try
            {
                StreamWriter writer = new(filePath, true, Encoding.UTF8);

                foreach (Activity activity in batch)
                {
                    object activityData = new
                    {
                        TraceId = activity.TraceId.ToString(),
                        DisplayName = activity.DisplayName,
                        StartTime = activity.StartTimeUtc,
                        Duration = activity.Duration,
                        Tags = activity.Tags
                    };

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    writer.WriteLine($"[{timestamp}] {JsonConvert.SerializeObject(activityData)}");
                }

                writer.Dispose();
                return ExportResult.Success;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to export traces: {exception.Message}\n");
                return ExportResult.Failure;
            }
        }

        /// <summary>
        /// Writes a shutdown message to the trace file if the exporter was not already disposed.
        /// </summary>
        public void CheckGracefulShutdown()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8);
                writer.WriteLine("Trace Export Session Ended. Tracer shut down cleanly.");
                writer.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to write trace shutdown message: {exception.Message}\n");
            }

            disposed = true;
        }

        /// <summary>
        /// Releases all resources used by the exporter and performs a graceful shutdown.
        /// </summary>
        public new void Dispose()
        {
            CheckGracefulShutdown();
            GC.SuppressFinalize(this);
        }
    }
}
