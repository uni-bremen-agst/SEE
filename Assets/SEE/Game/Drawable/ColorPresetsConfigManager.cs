using SEE.Game.HolisticMetrics;
using SEE.Game.UI.Notification;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class can be used to load and save the color presets of the color picker.
    /// </summary>
    public static class ColorPresetsConfigManager
    {
        /// <summary>
        /// The path to the file containing the saved color presets. This is saved in a field because multiple
        /// methods of this class use it.
        /// </summary>
        private static readonly string colorPresetsFile = DrawableConfigManager.drawablePath + "ColorPresets" + Filenames.ConfigExtension;

        /// <summary>
        /// This method checks whether the directory for the saved drawable exists. If not, then it creates
        /// that directory.
        /// </summary>
        private static void EnsureDrawableDirectoryExists()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists();
        }

        public static bool IsFileExists()
        {
            return File.Exists(colorPresetsFile);
        }

        /// <summary>
        /// Loads a metrics board from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the file which shall be loaded</param>
        /// <returns>The GameObject that represents the metrics displays</returns>
        internal static ColorsConfig LoadColors()
        {
            ColorsConfig config = new();
            try
            {
                using ConfigReader stream = new(colorPresetsFile);
                Dictionary<string, object> attributes = stream.Read();
                config.Restore(attributes);
            }
            catch (Exception e)
            {
                ShowNotification.Error("Error loading colors", $"Could not load settings from {colorPresetsFile}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Persist the color presets of the color picker to a file.
        /// </summary>
        /// <param name="colors">The colors to save.</param>
        internal static void SaveColors(Color[] colors)
        {
            EnsureDrawableDirectoryExists();
            using ConfigWriter writer = new(colorPresetsFile);
            ColorsConfig config = new() { Colors = colors };
            config.Save(writer);
            Debug.Log($"Saved color presets configuration to file {colorPresetsFile}.\n");
        }
    }
}