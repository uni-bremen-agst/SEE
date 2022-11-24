using System;
using System.IO;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class can be used to load and save the holistic metric boards.
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// The path to the folder containing the saved metrics displays. This is saved in a field because multiple
        /// methods of this class use it.
        /// </summary>
        private static readonly string metricsBoardsPath = Application.persistentDataPath + "/MetricsBoards/";

        private const string fileNameExtension = ".json";

        /// <summary>
        /// This method checks whether the directory for the saved metrics boards exists. If not, then it creates
        /// that directory.
        /// </summary>
        private static void EnsureBoardsDirectoryExists()
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
            EnsureBoardsDirectoryExists();
            DirectoryInfo directoryInfo = new DirectoryInfo(metricsBoardsPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            string[] fileNames = new string[fileInfos.Length];
            for (int i = 0; i < fileInfos.Length; i++)
            {
                string fileName = fileInfos[i].Name;
                // Last 4 characters should be ".json". We do not want these.
                fileNames[i] = fileName.Substring(0, fileName.Length - fileNameExtension.Length);
            }

            return fileNames;
        }

        /// <summary>
        /// Loads a metrics board from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load</param>
        /// <returns>The GameObject that represents the metrics displays</returns>
        internal static BoardConfig LoadBoard(string fileName)
        {
            EnsureBoardsDirectoryExists();
            string configuration = File.ReadAllText(metricsBoardsPath + fileName + fileNameExtension);
            BoardConfig boardConfiguration = JsonUtility.FromJson<BoardConfig>(configuration);
            foreach (WidgetConfig config in boardConfiguration.WidgetConfigs)
            {
                config.ID = Guid.NewGuid();
            }
            return boardConfiguration;
        }
        
        /// <summary>
        /// Persist a metrics board to a file.
        /// </summary>
        /// <param name="widgetsManager">The metrics board to save.</param>
        /// <param name="fileName">The file name for the configuration.</param>
        internal static void SaveBoard(WidgetsManager widgetsManager, string fileName)
        {
            EnsureBoardsDirectoryExists();

            BoardConfig config = GetBoardConfig(widgetsManager);

            string configuration = JsonUtility.ToJson(config, true);
            string filePath = metricsBoardsPath + fileName + fileNameExtension;
            File.WriteAllText(filePath, configuration);
        }

        /// <summary>
        /// Creates a new board config instance from the given board (the given widgets manager, actually).
        /// </summary>
        /// <param name="widgetsManager">The widgets manager representing the board to use for this</param>
        /// <returns>The configuration from which the board could be created again</returns>
        internal static BoardConfig GetBoardConfig(WidgetsManager widgetsManager)
        {
            Transform boardTransform = widgetsManager.transform;
            BoardConfig config = new BoardConfig()
            {
                Title = widgetsManager.GetTitle(),
                Position = boardTransform.localPosition,
                Rotation = boardTransform.rotation
            };
            foreach ((WidgetController, Metric) tuple in widgetsManager.widgets)
            {
                WidgetConfig widgetConfig = GetWidgetConfig(tuple.Item1, tuple.Item2);
                config.WidgetConfigs.Add(widgetConfig);
            }

            return config;
        }

        /// <summary>
        /// For a widget controller and a correlating metric, returns a new widget config from which the corresponding
        /// widget could be created again.
        /// </summary>
        /// <param name="widgetController">The controller of the widget to save in the configuration</param>
        /// <param name="metric">The metric that is being displayed on the widget to be saved</param>
        /// <returns>A new widget config that contains all information needed for creating the widget again</returns>
        internal static WidgetConfig GetWidgetConfig(WidgetController widgetController, Metric metric)
        {
            string widgetName = widgetController.gameObject.name;
            widgetName = widgetName.Substring(0, widgetName.Length - 7);
            WidgetConfig config = new WidgetConfig()
            {
                ID = widgetController.ID,
                MetricType = metric.GetType().Name,
                Position = widgetController.transform.localPosition,
                WidgetName = widgetName
            };
            return config;
        }
    }
}
