using System.IO;
using SEE.Game.HolisticMetrics.Metrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class can be used to load and save the holistic metric boards.
    /// </summary>
    public static class ConfigurationManager
    {
        /// <summary>
        /// The path to the folder containing the saved metrics displays. This is saved in a field because multiple
        /// methods of this class use it.
        /// </summary>
        private static readonly string metricsBoardsPath =
            Path.Combine(Application.persistentDataPath, "MetricsBoards");

        /// <summary>
        /// This method checks whether the directory for the saved metrics displays exists. If not, then it creates
        /// that directory.
        /// </summary>
        private static void EnsureDisplayDirectoryExists()
        {
            if (!Directory.Exists(metricsBoardsPath))
            {
                Directory.CreateDirectory(metricsBoardsPath);
            }
        }

        /// <summary>
        /// Returns the file names of all saved metrics displays.
        /// </summary>
        /// <returns>The file names of all saved metrics displays</returns>
        internal static string[] GetSavedFileNames()
        {
            EnsureDisplayDirectoryExists();
            string[] fileNames = Directory.GetFileSystemEntries(metricsBoardsPath);
            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];
                // Last 4 characters should be ".json". We do not want these.
                fileNames[i] = fileName.Substring(0, fileName.Length - 5);
            }

            return fileNames;
        }

        /// <summary>
        /// Loads all the metrics displays from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load</param>
        /// <returns>A list of the GameObjects that represent the metrics displays</returns>
        internal static GameObject LoadDisplay(string fileName)
        {
            EnsureDisplayDirectoryExists();
            string filePath = Path.Combine(metricsBoardsPath, fileName + ".json");
            MetricsBoardConfiguration metricsBoardConfiguration =
                JsonUtility.FromJson<MetricsBoardConfiguration>(filePath);
            return null;
        }

        /// <summary>
        /// Persist a metrics board to a file.
        /// </summary>
        /// <param name="boardController">The metrics board to save.</param>
        /// <param name="configurationName">The file name for the configuration.</param>
        internal static void SaveBoard(BoardController boardController, string configurationName)
        {
            EnsureDisplayDirectoryExists();
            MetricsBoardConfiguration metricsBoardConfiguration = new MetricsBoardConfiguration()
            {
                title = boardController.GetTitle(),
                Position = boardController.transform.localPosition
            };
            BoardController canvasController = boardController.GetComponent<BoardController>();
            foreach (Metric metric in canvasController.metrics)
            {
                WidgetConfiguration widget = new WidgetConfiguration()
                {
                    MetricType = metric.GetType(),
                    Position = metric.transform.localPosition,
                    WidgetName = metric.gameObject.name
                };
                metricsBoardConfiguration.WidgetConfigurations.Add(widget);
            }

            string configuration = JsonUtility.ToJson(metricsBoardConfiguration, true);
            string filePath = Path.Combine(metricsBoardsPath, configurationName + ".json");
            File.WriteAllText(filePath, configuration);
        }
    }
}