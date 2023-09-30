using SEE.Game;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
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
     /// This class can be used to load and save the drawables.
     /// </summary>
    public static class DrawableConfigManager
    {
        /// <summary>
        /// The path to the folder containing the saved drawables. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string drawablePath = Application.persistentDataPath + "/Drawable/";

        /// <summary>
        /// This method checks whether the directory for the saved drawable exists. If not, then it creates
        /// that directory.
        /// </summary>
        public static void EnsureDrawableDirectoryExists()
        {
            if (!Directory.Exists(drawablePath))
            {
                Directory.CreateDirectory(drawablePath);
            }
        }

        /// <summary>
        /// Loads a drawable from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the file which shall be loaded</param>
        /// <returns>The GameObject that represents the drawable displays</returns>
        internal static DrawableConfig LoadDrawable(FilePath path)
        {
            DrawableConfig config = new();
            try
            {
                using ConfigReader stream = new(path.Path);
                Dictionary<string, object> attributes = stream.Read();
                if (!config.Restore(attributes))
                {
                    ShowNotification.Warn(
                        "Error loading drawable",
                        "Not all drawable attributes were loaded correctly.");
                }
            }
            catch (Exception e)
            {
                ShowNotification.Error("Error loading drawable", $"Could not load settings from {path.Path}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Loads all drawables from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the file which shall be loaded</param>
        /// <returns>The GameObject that represents the drawable displays</returns>
        internal static DrawablesConfigs LoadDrawables(FilePath path)
        {
            DrawablesConfigs config = new();
            try
            {
                using ConfigReader stream = new(path.Path);
                Dictionary<string, object> attributes = stream.Read();
                if (!config.Restore(attributes))
                {
                    ShowNotification.Warn(
                        "Error loading drawables",
                        "Not all drawables attributes were loaded correctly.");
                }
            }
            catch (Exception e)
            {
                ShowNotification.Error("Error loading drawable", $"Could not load settings from {path.Path}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Loads a drawable from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load</param>
        /// <returns>The GameObject that represents the metrics displays</returns>
        internal static DrawableConfig LoadDrawable(string fileName)
        {
            EnsureDrawableDirectoryExists();
            return LoadDrawable(new FilePath(drawablePath + fileName + Filenames.ConfigExtension));
        }

        /// <summary>
        /// Loads all drawables from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load</param>
        /// <returns>The GameObject that represents the metrics displays</returns>
        internal static DrawablesConfigs LoadDrawables(string fileName)
        {
            EnsureDrawableDirectoryExists();
            return LoadDrawables(new FilePath(drawablePath + fileName + Filenames.ConfigExtension));
        }

        /// <summary>
        /// Persist a drawable to a file.
        /// </summary>
        /// <param name="GameObject">The drawable to save.</param>
        /// <param name="fileName">The file name for the configuration.</param>
        internal static void SaveDrawable(GameObject drawable, string fileName)
        {
            EnsureDrawableDirectoryExists();
            string filePath = drawablePath + fileName + Filenames.ConfigExtension;
            using ConfigWriter writer = new(filePath);
            DrawableConfig config = GetDrawableConfig(drawable);
            config.Save(writer);
            Debug.Log($"Saved drawable configuration to file {filePath}.\n");
        }

        /// <summary>
        /// Persist a drawable to a file.
        /// </summary>
        /// <param name="GameObject">The drawable to save.</param>
        /// <param name="fileName">The file name for the configuration.</param>
        internal static void SaveDrawables(GameObject[] drawables, string fileName)
        {
            EnsureDrawableDirectoryExists();
            string filePath = drawablePath + fileName + Filenames.ConfigExtension;
            using ConfigWriter writer = new(filePath);
            DrawablesConfigs configs = GetDrawablesConfigs(drawables);
            configs.Save(writer);
            Debug.Log($"Saved drawables configurations to file {filePath}.\n");
        }

        /// <summary>
        /// Deletes the <see cref="DrawableConfig"/> or <see cref="DrawablesConfigs"/> file with the given <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to delete</param>
        internal static void DeleteDrawables(string filename)
        {
            EnsureDrawableDirectoryExists();
            string filePath = drawablePath + filename + Filenames.ConfigExtension;
            File.Delete(filePath);
        }

        /// <summary>
        /// Creates a new drawable config instance from the given drawable. 
        /// </summary>
        /// <param name="GameObject">The game object representing the drawable to use for this</param>
        /// <returns>The configuration from which the drawable could be created again</returns>
        internal static DrawableConfig GetDrawableConfig(GameObject drawable)
        {
            Transform transform = drawable.transform;
            if (GameDrawableFinder.GetDrawableParentName(drawable) != "")
            {
                transform = drawable.transform.parent;
            }
            DrawableConfig config = new()
            {
                DrawableName = drawable.name,
                DrawableParentName = GameDrawableFinder.GetDrawableParentName(drawable),
                Position = transform.position,
                Rotation = transform.eulerAngles
            };
            GameObject attachedObjects = GameDrawableFinder.GetAttachedObjectsObject(drawable);
            if (attachedObjects != null)
            {
                GameObject[] lines = GameDrawableFinder.FindAllChildrenWithTag(attachedObjects, Tags.Line).ToArray();
                foreach (GameObject line in lines)
                {
                    Line lineConfig = Line.GetLine(line);
                    config.LineConfigs.Add(lineConfig);
                }
            }
            return config;
        }

        /// <summary>
        /// Creates a new DrawablesConfigs instance from the given drawables. 
        /// </summary>
        /// <param name="drawable<">The game objects representing the drawables to use for this</param>
        /// <returns>The configuration from which the drawables could be created again</returns>
        internal static DrawablesConfigs GetDrawablesConfigs(GameObject[] drawables)
        {
            DrawablesConfigs config = new();
            foreach (GameObject drawable in drawables)
            {
                DrawableConfig drawableConfig = GetDrawableConfig(drawable);
                config.Drawables.Add(drawableConfig);
            }

            return config;
        }
    }
}