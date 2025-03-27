using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenTelemetry;
using Debug = UnityEngine.Debug;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Custom exporter that writes activity trace data to a file.
    /// </summary>
    public class TraceFileExporter : BaseExporter<Activity>
    {
        private readonly string _filePath; // Path to the file where trace data will be written.

        public TraceFileExporter(string directoryPath)
        {
            // Ensure the directory exists, otherwise create it.
            Directory.CreateDirectory(directoryPath);

            // Generate a timestamped file path for the trace export.
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _filePath = Path.Combine(directoryPath, $"Traces-Session-{timestamp}.json");

            // Create the file and write the initial opening line (or headers) if needed.
            try
            {
                using (var writer = new StreamWriter(_filePath))
                {
                    writer.WriteLine("Trace Export Session Started...");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create trace file: {ex.Message}");
            }
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            try
            {
                using (var writer = new StreamWriter(_filePath, true, Encoding.UTF8))
                {
                    foreach (var activity in batch)
                    {
                        // Serialize activity details into a custom format (you can choose the format as needed).
                        var activityData = new
                        {
                            TraceId = activity.TraceId.ToString(),
                            DisplayName = activity.DisplayName,
                            StartTime = activity.StartTimeUtc,
                            Duration = activity.Duration,
                            Tags = activity.Tags
                        };

                        // Write activity data to the file.
                        writer.WriteLine(
                            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {JsonConvert.SerializeObject(activityData)}");
                    }
                }

                return ExportResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export traces: {ex.Message}");
                return ExportResult.Failure;
            }
        }
    }
}