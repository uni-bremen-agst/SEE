using System.Collections.Generic;
using System.IO;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;
using System;
using SEE.Utils.Config;
using SEE.Utils.Paths;

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
        /// Returns the file names (excluding their extension) of all saved metric-board configuration files.
        /// </summary>
        /// <returns>The file names of all saved metrics displays.</returns>
        internal static string[] GetSavedFileNames()
        {
            EnsureBoardsDirectoryExists();
            DirectoryInfo directoryInfo = new(metricsBoardsPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles($"*{Filenames.MetricBoardConfigExtension}");
            string[] fileNames = new string[fileInfos.Length];
            for (int i = 0; i < fileInfos.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(fileInfos[i].Name);
            }
            return fileNames;
        }

        /// <summary>
        /// Loads a metrics board from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the file which shall be loaded.</param>
        /// <returns>The GameObject that represents the metrics displays.</returns>
        internal static BoardConfig LoadBoard(DataPath path)
        {
            BoardConfig config = new();
            try
            {
                using ConfigReader stream = new(path.Path);
                Dictionary<string, object> attributes = stream.Read();
                if (!config.Restore(attributes))
                {
                    ShowNotification.Warn(
                        "Error loading board",
                        "Not all board attributes were loaded correctly.");
                }
            }
            catch (Exception e)
            {
                ShowNotification.Error("Error loading board", $"Could not load settings from {path.Path}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Loads a metrics board from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load.</param>
        /// <returns>The GameObject that represents the metrics displays.</returns>
        internal static BoardConfig LoadBoard(string fileName)
        {
            EnsureBoardsDirectoryExists();
            return LoadBoard(new DataPath()
            {
                Path = metricsBoardsPath + fileName + Filenames.MetricBoardConfigExtension
            });
        }

        /// <summary>
        /// Persists a metrics board to a file.
        /// </summary>
        /// <param name="widgetsManager">The metrics board to save.</param>
        /// <param name="fileName">The file name for the configuration.</param>
        internal static void SaveBoard(WidgetsManager widgetsManager, string fileName)
        {
            EnsureBoardsDirectoryExists();
            string filePath = metricsBoardsPath + fileName + Filenames.MetricBoardConfigExtension;
            using ConfigWriter writer = new(filePath);
            BoardConfig config = GetBoardConfig(widgetsManager);
            config.Save(writer);
            Debug.Log($"Saved metric-board configuration to file {filePath}.\n");
        }

        /// <summary>
        /// Deletes the <see cref="BoardConfig"/> file with the given <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to delete.</param>
        internal static void DeleteBoard(string filename)
        {
            EnsureBoardsDirectoryExists();
            File.Delete(metricsBoardsPath + filename + Filenames.MetricBoardConfigExtension);
        }

        /// <summary>
        /// Creates a new board config instance from the given board (the given widgets manager, actually).
        /// </summary>
        /// <param name="widgetsManager">The widgets manager representing the board to use for this.</param>
        /// <returns>The configuration from which the board could be created again.</returns>
        internal static BoardConfig GetBoardConfig(WidgetsManager widgetsManager)
        {
            Transform boardTransform = widgetsManager.transform;
            BoardConfig config = new()
            {
                Title = widgetsManager.GetTitle(),
                Position = boardTransform.localPosition,
                Rotation = boardTransform.rotation
            };
            foreach ((WidgetController, Metric) tuple in widgetsManager.Widgets)
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
        /// <param name="widgetController">The controller of the widget to save in the configuration.</param>
        /// <param name="metric">The metric that is being displayed on the widget to be saved.</param>
        /// <returns>A new widget config that contains all information needed for creating the widget again.</returns>
        internal static WidgetConfig GetWidgetConfig(WidgetController widgetController, Metric metric)
        {
            string widgetName = widgetController.gameObject.name;
            widgetName = widgetName.Substring(0, widgetName.Length - 7);
            WidgetConfig config = new()
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
