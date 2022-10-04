using System.IO;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
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
            DirectoryInfo directoryInfo = new DirectoryInfo(metricsBoardsPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            string[] fileNames = new string[fileInfos.Length];
            for (int i = 0; i < fileInfos.Length; i++)
            {
                string fileName = fileInfos[i].Name;
                // Last 4 characters should be ".json". We do not want these.
                fileNames[i] = fileName.Substring(0, fileName.Length - 5);
            }

            return fileNames;
        }

        /// <summary>
        /// Loads a metrics board from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load</param>
        /// <returns>The GameObject that represents the metrics displays</returns>
        internal static BoardConfiguration LoadBoard(string fileName)
        {
            EnsureDisplayDirectoryExists();
            string filePath = Path.Combine(metricsBoardsPath, fileName + ".json");
            string configuration = File.ReadAllText(filePath);
            BoardConfiguration boardConfiguration = JsonUtility.FromJson<BoardConfiguration>(configuration);
            return boardConfiguration;
        }

        /// <summary>
        /// Persist a metrics board to a file.
        /// </summary>
        /// <param name="boardController">The metrics board to save.</param>
        /// <param name="fileName">The file name for the configuration.</param>
        internal static void SaveBoard(BoardController boardController, string fileName)
        {
            EnsureDisplayDirectoryExists();
            BoardConfiguration metricsBoardConfiguration = new BoardConfiguration()
            {
                Title = boardController.GetTitle(),
                Position = boardController.transform.localPosition
            };
            BoardController canvasController = boardController.GetComponent<BoardController>();
            foreach ((WidgetController, Metric) tuple in canvasController.widgets)
            {
                string widgetName = tuple.Item1.gameObject.name;
                widgetName = widgetName.Substring(0, widgetName.Length - 7);
                WidgetConfiguration widget = new WidgetConfiguration()
                {
                    MetricType = tuple.Item2.GetType().Name,
                    Position = tuple.Item1.transform.localPosition,
                    WidgetName = widgetName
                };
                metricsBoardConfiguration.WidgetConfigurations.Add(widget);
            }

            string configuration = JsonUtility.ToJson(metricsBoardConfiguration, true);
            string filePath = Path.Combine(metricsBoardsPath, fileName + ".json");
            File.WriteAllText(filePath, configuration);
        }
    }
}
