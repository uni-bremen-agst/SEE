using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class can be used to load and save the color presets of the color picker.
    /// </summary>
    public static class ColorPresetsConfigManager
    {
        /// <summary>
        /// The path to the file containing the saved color presets.
        /// This is saved in a field because multiple methods of this class use it.
        /// </summary>
        private static readonly string colorPresetsFile = DrawableConfigManager.ConfigurationPath
            + "ColorPresets" + Filenames.DrawableConfigExtension;

        /// <summary>
        /// This method checks whether the directory for the saved drawable exists.
        /// If not, it creates that directory.
        /// </summary>
        private static void EnsureDrawableDirectoryExists()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.ConfigurationPath);
        }

        /// <summary>
        /// True if the color-preset file exists
        /// </summary>
        /// <returns>Whether the file exists or not.</returns>
        public static bool IsFileExists()
        {
            return File.Exists(colorPresetsFile);
        }

        /// <summary>
        /// Loads the colors of the file.
        /// </summary>
        /// <returns>The created colors configuration.</returns>
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
                ShowNotification.Error("Error loading colors",
                    $"Could not load settings from {colorPresetsFile}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        // Saves the color presets of the color picker to a file.
        /// </summary>
        /// <param name="colors">The colors to save.</param>
        internal static void SaveColors(Color[] colors)
        {
            EnsureDrawableDirectoryExists();
            using ConfigWriter writer = new(colorPresetsFile);
            ColorsConfig config = new() { Colors = colors };
            config.Save(writer);
        }
    }
}
