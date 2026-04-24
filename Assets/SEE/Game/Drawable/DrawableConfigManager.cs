using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class can be used to load and save the drawables.
    /// </summary>
    public static class DrawableConfigManager
    {
        /// <summary>
        /// The path to the configuration folder of the saved drawables.
        /// This is saved in a field because multiple methods of this class and other classes use it.
        /// </summary>
        public static readonly string ConfigurationPath = ValueHolder.DrawablePath + "Configuration/";

        /// <summary>
        /// The path to the folder of saved drawable (single).
        /// </summary>
        public static readonly string SingleConfPath = ConfigurationPath + "1_Single_Drawable/";

        /// <summary>
        /// The path to the folder of saved drawables (multiple).
        /// </summary>
        public static readonly string MultipleConfPath = ConfigurationPath + "2_Multiple_Drawables/";

        /// <summary>
        /// This method checks whether the directory for the saved drawable exists.
        /// If not, then it creates that directory.
        /// </summary>
        public static void EnsureDrawableDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Loads a single drawable from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the file which shall be loaded.</param>
        /// <returns>The drawable configuration of the loaded file.</returns>
        internal static DrawableConfig LoadDrawable(DataPath path)
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
                ShowNotification.Error("Error loading drawable",
                    $"Could not load settings from {path.Path}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Loads all drawables from a file at the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the file which shall be loaded.</param>
        /// <returns>A configuration that contains all drawable configurations of the file.</returns>
        internal static DrawablesConfigs LoadDrawables(DataPath path)
        {
            DrawablesConfigs config = new();
            try
            {
                using ConfigReader stream = new(path.Path);
                Dictionary<string, object> attributes = stream.Read();
                if (!config.Restore(attributes))
                {
                    DrawableConfig singleConfig = new();
                    if (singleConfig.Restore(attributes))
                    {
                        config.Drawables.Add(singleConfig);
                    }
                    else
                    {
                        ShowNotification.Warn(
                            "Error loading drawables",
                            "Not all drawables attributes were loaded correctly.");
                    }
                }
            }
            catch (Exception e)
            {
                ShowNotification.Error("Error loading drawable",
                    $"Could not load settings from {path.Path}: {e.Message}");
                throw e;
            }
            return config;
        }

        /// <summary>
        /// Loads a drawable from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to be loaded.</param>
        /// <returns>The loaded drawable configuraion.</returns>
        internal static DrawableConfig LoadDrawable(string fileName)
        {
            EnsureDrawableDirectoryExists(SingleConfPath);
            return LoadDrawable(new DataPath(SingleConfPath + fileName + Filenames.DrawableConfigExtension));
        }

        /// <summary>
        /// Loads all drawables from a file.
        /// </summary>
        /// <param name="fileName">The file name without the extension of the file to load.</param>
        /// <returns>The loaded configuration of the drawables from the file.</returns>
        internal static DrawablesConfigs LoadDrawables(string fileName)
        {
            EnsureDrawableDirectoryExists(MultipleConfPath);
            return LoadDrawables(new DataPath(MultipleConfPath + fileName + Filenames.DrawableConfigExtension));
        }

        /// <summary>
        /// Creates a <see cref="DrawablesConfigs"/> that holds all the <paramref name="drawables"/> configurations.
        /// Then saves the <see cref="DrawablesConfigs"/> to the <paramref name="filePath"/>.
        /// The method checks if the <paramref name="filePath"/> has the correct extension.
        /// If not, it will be set.
        /// </summary>
        /// <param name="drawables">The drawables that should be saved.</param>
        /// <param name="filePath">The file path where the save file should be placed.</param>
        internal static void SaveDrawables(GameObject[] drawables, DataPath filePath)
        {
            EnsureDrawableDirectoryExists(Path.GetDirectoryName(filePath.Path));
            if (!Path.HasExtension(filePath.Path))
            {
                filePath = new DataPath(filePath.Path + Filenames.DrawableConfigExtension);
            }
            else if (Path.GetExtension(filePath.Path) != Filenames.DrawableConfigExtension)
            {
                Path.ChangeExtension(filePath.Path, Filenames.DrawableConfigExtension);
            }
            using ConfigWriter writer = new(filePath.Path);
            if (drawables.Length > 1)
            {
                DrawablesConfigs configs = GetDrawablesConfigs(drawables);
                configs.Save(writer);
            } else
            {
                DrawableConfig config = GetDrawableConfig(drawables[0]);
                config.Save(writer);
            }
            Debug.Log($"Saved drawable configuration to file {filePath.Path}.\n");
        }

        /// <summary>
        /// Deletes the <see cref="DrawableConfig"/> or <see cref="DrawablesConfigs"/> file with the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the file to delete.</param>
        internal static void DeleteDrawables(DataPath path)
        {
            FileIO.DeleteIfExists(path.Path);
        }

        /// <summary>
        /// Creates a new drawable config instance from the given drawable.
        /// </summary>
        /// <param name="surface">The drawable surface for which a configuration is to be created.</param>
        /// <returns>The created <see cref="DrawableConfig"/>.</returns>
        internal static DrawableConfig GetDrawableConfig(GameObject surface)
        {
            if (surface.CompareTag(Tags.Drawable))
            {
                Transform transform = surface.transform;
                if (GameFinder.GetDrawableSurfaceParentName(surface) != "")
                {
                    transform = surface.transform.parent;
                }

                /// Get the order in layering for drawables.
                /// Only needed for sticky notes.
                int order = 0;
                if (transform.GetComponent<OrderInLayerValueHolder>() != null)
                {
                    order = transform.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
                }
                else if (transform.GetComponentInParent<OrderInLayerValueHolder>() != null)
                {
                    order = transform.GetComponentInParent<OrderInLayerValueHolder>().OrderInLayer;
                }
                bool lighting = false;
                if (surface.GetRootParent().transform.GetComponentInChildren<Light>() != null)
                {
                    lighting = surface.GetRootParent().transform.GetComponentInChildren<Light>().enabled;
                }

                bool visibility = surface.GetRootParent().activeInHierarchy;

                DrawableHolder holder = surface.GetComponent<DrawableHolder>();

                /// Creates the <see cref="DrawableConfig"/> with the corresponding values.
                DrawableConfig config = new()
                {
                    ID = surface.name,
                    ParentID = GameFinder.GetDrawableSurfaceParentName(surface),
                    Position = transform.position,
                    Rotation = transform.eulerAngles,
                    Scale = transform.localScale,
                    Color = surface.GetComponent<MeshRenderer>().material.color,
                    Order = order,
                    Lighting = lighting,
                    OrderInLayer = holder.OrderInLayer,
                    Description = holder.Description,
                    Visibility = visibility,
                    CurrentPage = holder.CurrentPage,
                    MaxPageSize = holder.MaxPageSize,
                };

                /// Block for creating the <see cref="DrawableType"/> of the drawable.
                GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(surface);
                if (attachedObjects != null)
                {
                    /// Creates configurations for all lines of the drawable, except the Mind Map Node borders.
                    GameObject[] lines = attachedObjects.FindAllDescendantsWithTagExcludingSpecificParentTag(
                        Tags.Line, Tags.MindMapNode).ToArray();
                    foreach (GameObject line in lines)
                    {
                        LineConf lineConfig = LineConf.GetLine(line);
                        config.LineConfigs.Add(lineConfig);
                    }

                    /// Creates configurations for all texts of the drawable, except the Mind Map Node texts.
                    GameObject[] texts = attachedObjects.FindAllDescendantsWithTagExcludingSpecificParentTag(
                        Tags.DText, Tags.MindMapNode).ToArray();
                    foreach (GameObject text in texts)
                    {
                        TextConf textConfig = TextConf.GetText(text);
                        config.TextConfigs.Add(textConfig);
                    }

                    /// Creates configurations for all images of the drawable.
                    GameObject[] images = attachedObjects.FindAllDescendantsWithTag(Tags.Image).ToArray();
                    foreach (GameObject image in images)
                    {
                        ImageConf imageConf = ImageConf.GetImageConf(image);
                        config.ImageConfigs.Add(imageConf);
                    }

                    /// Creates configurations for all Mind Map nodes of the drawable.
                    IList<GameObject> nodes = attachedObjects.FindAllDescendantsWithTag(Tags.MindMapNode);
                    nodes = nodes.OrderBy(o => o.GetComponent<MMNodeValueHolder>().Layer).ToList();
                    foreach (GameObject node in nodes)
                    {
                        MindMapNodeConf nodeConf = MindMapNodeConf.GetNodeConf(node);
                        config.MindMapNodeConfigs.Add(nodeConf);
                    }
                }
                return config;
            }
            return null;
        }

        /// <summary>
        /// Creates a new <see cref="DrawablesConfigs"/> from the given drawables.
        /// </summary>
        /// <param name="drawables<">The drawables for which a configuration is to be created.</param>
        /// <returns>The created <see cref="DrawablesConfigs"/>.</returns>
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

        /// <summary>
        /// Saves the already prepared drawable configurations to the given file path.
        /// This is useful when the configuration was modified before saving,
        /// for example when only one page of a drawable surface should be saved.
        /// </summary>
        /// <param name="configs">The drawable configurations to save.</param>
        /// <param name="filePath">The file path where the save file should be placed.</param>
        internal static void SaveDrawables(DrawableConfig[] configs, DataPath filePath)
        {
            EnsureDrawableDirectoryExists(Path.GetDirectoryName(filePath.Path));
            if (!Path.HasExtension(filePath.Path))
            {
                filePath = new DataPath(filePath.Path + Filenames.DrawableConfigExtension);
            }
            else if (Path.GetExtension(filePath.Path) != Filenames.DrawableConfigExtension)
            {
                filePath = new DataPath(Path.ChangeExtension(filePath.Path, Filenames.DrawableConfigExtension));
            }

            using ConfigWriter writer = new(filePath.Path);
            if (configs.Length > 1)
            {
                DrawablesConfigs drawablesConfigs = new();
                foreach (DrawableConfig config in configs)
                {
                    drawablesConfigs.Drawables.Add(config);
                }
                drawablesConfigs.Save(writer);
            }
            else
            {
                configs[0].Save(writer);
            }

            Debug.Log($"Saved drawable configuration to file {filePath.Path}.\n");
        }

        /// <summary>
        /// Creates a drawable configuration that contains only one page of the given drawable surface.
        /// The extracted page is normalized to page 0 so that it can later be loaded onto any target page.
        /// </summary>
        /// <param name="surface">The drawable surface from which the page should be extracted.</param>
        /// <param name="page">The page that should be extracted.</param>
        /// <returns>A drawable configuration containing only the selected page.</returns>
        internal static DrawableConfig GetSinglePageConfig(GameObject surface, int page)
        {
            DrawableConfig source = GetDrawableConfig(surface);

            DrawableConfig result = new()
            {
                ID = source.ID,
                ParentID = source.ParentID,
                Position = source.Position,
                Rotation = source.Rotation,
                Scale = source.Scale,
                Color = source.Color,
                Order = source.Order,
                Lighting = source.Lighting,
                OrderInLayer = source.OrderInLayer,
                Description = source.Description,
                Visibility = source.Visibility,
                CurrentPage = 0,
                MaxPageSize = 1
            };

            foreach (LineConf line in source.LineConfigs)
            {
                if (line.AssociatedPage == page)
                {
                    LineConf clone = (LineConf)line.Clone();
                    clone.AssociatedPage = 0;
                    result.LineConfigs.Add(clone);
                }
            }

            foreach (TextConf text in source.TextConfigs)
            {
                if (text.AssociatedPage == page)
                {
                    TextConf clone = (TextConf)text.Clone();
                    clone.AssociatedPage = 0;
                    result.TextConfigs.Add(clone);
                }
            }

            foreach (ImageConf image in source.ImageConfigs)
            {
                if (image.AssociatedPage == page)
                {
                    ImageConf clone = (ImageConf)image.Clone();
                    clone.AssociatedPage = 0;
                    result.ImageConfigs.Add(clone);
                }
            }

            foreach (MindMapNodeConf node in source.MindMapNodeConfigs)
            {
                if (node.AssociatedPage == page)
                {
                    MindMapNodeConf clone = (MindMapNodeConf)node.Clone();
                    clone.AssociatedPage = 0;
                    result.MindMapNodeConfigs.Add(clone);
                }
            }

            return result;
        }

        /// <summary>
        /// Remaps all drawable types contained in the given drawable configuration to the given page.
        /// </summary>
        /// <param name="config">The drawable configuration whose contained drawable types should be remapped.</param>
        /// <param name="targetPage">The page to assign to all contained drawable types.</param>
        internal static void RemapAllTypesToPage(DrawableConfig config, int targetPage)
        {
            foreach (DrawableType type in config.GetAllDrawableTypes())
            {
                type.AssociatedPage = targetPage;
            }

            config.CurrentPage = targetPage;
            config.MaxPageSize = Mathf.Max(config.MaxPageSize, targetPage + 1);
        }
    }
}