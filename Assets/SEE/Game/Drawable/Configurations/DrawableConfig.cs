using SEE.Net.Actions.Drawable;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// This class can hold all the information that is needed to configure a drawable.
    /// </summary>
    [Serializable]
    public class DrawableConfig : ICloneable
    {
        /// <summary>
        /// The name of this drawable.
        /// </summary>
        public string ID;

        /// <summary>
        /// The parent name of this drawable.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The position of this drawable / or of his parent.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The orientation of this drawable / or of his parent.
        /// </summary>
        public Vector3 Rotation;

        /// <summary>
        /// The scale of this drawable / or of his parent.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The color of this drawable.
        /// </summary>
        public Color Color;

        /// <summary>
        /// The order of the drawable.
        /// (Only necessary for sticky notes)
        /// </summary>
        public int Order;

        /// <summary>
        /// The lighting state of the drawable.
        /// </summary>
        public bool Lighting;

        /// <summary>
        /// The current order in layer of the drawable.
        /// </summary>
        public int OrderInLayer;

        /// <summary>
        /// The description of the drawable.
        /// </summary>
        public string Description;

        /// <summary>
        /// The visibility of the drawable.
        /// </summary>
        public bool Visibility;

        /// <summary>
        /// The current page of the drawable.
        /// </summary>
        public int CurrentPage;

        /// <summary>
        /// The maximum page size.
        /// </summary>
        public int MaxPageSize;

        /// <summary>
        /// All the lines that should be displayed on this drawable.
        /// </summary>
        public List<LineConf> LineConfigs = new();

        /// <summary>
        /// All the texts that should be displayed on this drawable.
        /// </summary>
        public List<TextConf> TextConfigs = new();

        /// <summary>
        /// All the images that should be displayed on this drawable.
        /// </summary>
        public List<ImageConf> ImageConfigs = new();

        /// <summary>
        /// All the mind map nodes that should be displayed on this drawable.
        /// </summary>
        public List<MindMapNodeConf> MindMapNodeConfigs = new();

        /// <summary>
        /// Gets the surface game object of this drawable.
        /// </summary>
        /// <returns>The drawable game object.</returns>
        public GameObject GetDrawableSurface()
        {
            return GameFinder.FindDrawableSurface(ID, ParentID);
        }

        /// <summary>
        /// Gets all drawable type configs of this drawable config.
        /// </summary>
        /// <returns>A list that contains all drawable type configs of this drawable.</returns>
        public List<DrawableType> GetAllDrawableTypes()
        {
            List<DrawableType> list = new(LineConfigs);
            list.AddRange(TextConfigs);
            list.AddRange(ImageConfigs);
            list.AddRange(MindMapNodeConfigs);
            return list;
        }

        /// <summary>
        /// Restores the changes of a configuration.
        /// </summary>
        /// <param name="surface">The surface to be changed.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The <paramref name="surface"/> GameObject after applying the configuration.</returns>
        public static GameObject Restore(GameObject surface, DrawableConfig config)
        {
            if (surface.CompareTag(Tags.Drawable))
            {
                if (GameFinder.IsStickyNote(surface))
                {
                    GameStickyNoteManager.Change(surface, config);
                    new StickyNoteChangeNetAction(config).Execute();
                } else
                {
                    GameDrawableManager.Change(surface, config);
                    new DrawableChangeNetAction(config).Execute();
                }
            }
            return surface;
        }

        #region Config I/O

        /// <summary>
        /// The label for the drawable surface name in the configuration file.
        /// </summary>
        private const string SurfaceNameLabel = "SurfaceName";

        /// <summary>
        /// The label for the drawable surface parent name in the configuration file.
        /// </summary>
        private const string SurfaceParentNameLabel = "SurfaceParentName";

        /// <summary>
        /// The label for the position of a drawable in the configuration file.
        /// </summary>
        private const string PositionLabel = "Position";

        /// <summary>
        /// The label for the rotation of a drawable in the configuration file.
        /// </summary>
        private const string RotationLabel = "Rotation";

        /// <summary>
        /// The label for the scale of a drawable in the configuration file.
        /// </summary>
        private const string ScaleLabel = "Scale";

        /// <summary>
        /// The label for the color of a drawable in the configuration file.
        /// </summary>
        private const string ColorLabel = "Color";

        /// <summary>
        /// The label for the order of a drawable in the configuration file.
        /// </summary>
        private const string OrderLabel = "Order";

        /// <summary>
        /// The label for the lighting of a drawable in the configuration file.
        /// </summary>
        private const string LightingLabel = "Lighting";

        /// <summary>
        /// The label for the order in layer of a drawable in the configuration file.
        /// </summary>
        private const string OrderInLayerLabel = "OrderInLayer";

        /// <summary>
        /// The label for the description of a drawable in the configuration file.
        /// </summary>
        private const string DescriptionLabel = "Description";

        /// <summary>
        /// The label for the visibility of a drawable in the configuration file.
        /// </summary>
        private const string VisibilityLabel = "Visibility";

        /// <summary>
        /// The label for the current selected page of a drawable in the configuration file.
        /// </summary>
        private const string CurrentPageLabel = "CurrentPage";

        /// <summary>
        /// The label for the maximum page size of a drawable in the configuration file.
        /// </summary>
        private const string MaxPageSizeLabel = "MaxPageSize";

        /// <summary>
        /// The label for the group of line configurations in the configuration file.
        /// </summary>
        private const string LineConfigsLabel = "LineConfigs";

        /// <summary>
        /// The label for the group of text configurations in the configuration file.
        /// </summary>
        private const string TextConfigsLabel = "TextConfigs";

        /// <summary>
        /// The label for the group of image configurations in the configuration file.
        /// </summary>
        private const string ImageConfigsLabel = "ImageConfigs";

        /// <summary>
        /// The label for the group of mind map node configurations in the configuration file.
        /// </summary>
        private const string MindMapNodeConfigsLabel = "MindMapNodeConfigs";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.Save(ID, SurfaceNameLabel);
            writer.Save(ParentID, SurfaceParentNameLabel);
            writer.Save(Position, PositionLabel);
            writer.Save(Rotation, RotationLabel);
            writer.Save(Scale, ScaleLabel);
            writer.Save(Color, ColorLabel);
            writer.Save(Order, OrderLabel);
            writer.Save(Lighting, LightingLabel);
            writer.Save(OrderInLayer, OrderInLayerLabel);
            writer.Save(Description, DescriptionLabel);
            writer.Save(Visibility, VisibilityLabel);
            writer.Save(CurrentPage, CurrentPageLabel);
            writer.Save(MaxPageSize, MaxPageSizeLabel);

            if (LineConfigs != null && LineConfigs.Count > 0)
            {
                writer.BeginList(LineConfigsLabel);
                foreach (LineConf lineConfig in LineConfigs)
                {
                    lineConfig.Save(writer);
                }
                writer.EndList();
            }

            if (TextConfigs != null && TextConfigs.Count > 0)
            {
                writer.BeginList(TextConfigsLabel);
                foreach (TextConf textConfig in TextConfigs)
                {
                    textConfig.Save(writer);
                }
                writer.EndList();
            }

            if (ImageConfigs != null && ImageConfigs.Count > 0)
            {
                writer.BeginList(ImageConfigsLabel);
                foreach (ImageConf imageConfig in ImageConfigs)
                {
                    imageConfig.Save(writer);
                }
                writer.EndList();
            }

            if (MindMapNodeConfigs != null && MindMapNodeConfigs.Count > 0)
            {
                writer.BeginList(MindMapNodeConfigsLabel);
                foreach (MindMapNodeConf nodeConfigs in MindMapNodeConfigs)
                {
                    nodeConfigs.Save(writer);
                }
                writer.EndList();
            }
        }

        /// <summary>
        /// Loads the given attributes into this instance of the class <see cref="DrawableConfig"/>.
        /// </summary>
        /// <param name="attributes">The attributes to load in the format created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the attributes were loaded without any errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errorFree = true;
            /// Try to restore the drawable name.
            if (attributes.TryGetValue(SurfaceNameLabel, out object name))
            {
                ID = (string)name;
            }
            else
            {
                errorFree = false;
            }

            /// Try to restore the drawable parent name.
            if (attributes.TryGetValue(SurfaceParentNameLabel, out object pName))
            {
                ParentID = (string)pName;
            }
            else
            {
                errorFree = false;
            }

            /// Try to restore the position.
            Vector3 position = Vector3.zero;
            if (ConfigIO.Restore(attributes, PositionLabel, ref position))
            {
                Position = position;
            }
            else
            {
                Position = Vector3.zero;
                errorFree = false;
            }

            /// Try to restore the rotation.
            Vector3 rotation = Vector3.zero;
            if (ConfigIO.Restore(attributes, RotationLabel, ref rotation))
            {
                Rotation = rotation;
            }
            else
            {
                Rotation = Vector3.zero;
                errorFree = false;
            }

            /// Try to restore the scale.
            Vector3 scale = Vector3.zero;
            if (ConfigIO.Restore(attributes, ScaleLabel, ref scale))
            {
                Scale = scale;
            }
            else
            {
                Scale = Vector3.zero;
                errorFree = false;
            }

            /// Try to restore the color.
            Color color = Color.black;
            if (ConfigIO.Restore(attributes, ColorLabel, ref color))
            {
                Color = color;
            }
            else
            {
                Color = Color.black;
                errorFree = false;
            }

            /// Try to restore the order.
            if (!ConfigIO.Restore(attributes, OrderLabel, ref Order))
            {
                errorFree = false;
            }

            /// Try to restore the lighting status.
            if (attributes.TryGetValue(LightingLabel, out object status))
            {
                Lighting = (bool)status;
            }
            else
            {
                Lighting = false;
                errorFree = false;
            }

            /// Try to restore the order in layer.
            if (!ConfigIO.Restore(attributes, OrderInLayerLabel, ref OrderInLayer))
            {
                errorFree = false;
            }

            /// Try to restore the description of the drawable.
            if (attributes.TryGetValue(DescriptionLabel, out object description))
            {
                Description = (string)description;
            }
            else
            {
                errorFree = false;
            }

            /// Try to restore the lighting status.
            if (attributes.TryGetValue(VisibilityLabel, out object visibility))
            {
                Visibility = (bool)visibility;
            }
            else
            {
                Visibility = true;
                errorFree = false;
            }

            /// Try to restore the current selected page.
            if (!ConfigIO.Restore(attributes, CurrentPageLabel, ref CurrentPage))
            {
                errorFree = false;
            }

            /// Try to restore the maximum page size.
            if (!ConfigIO.Restore(attributes, MaxPageSizeLabel, ref MaxPageSize))
            {
                errorFree = false;
            }

            /// Try to restore the lines that are displayed on this drawable.
            if (attributes.TryGetValue(LineConfigsLabel, out object lineList))
            {
                foreach (object item in (List<object>)lineList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    LineConf config = new();
                    config.Restore(dict);
                    LineConfigs.Add(config);
                }
            }

            /// Try to restore the texts that are displayed on this drawable.
            if (attributes.TryGetValue(TextConfigsLabel, out object textList))
            {
                int i = 0;
                foreach (object item in (List<object>)textList)
                {
                    i++;
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    TextConf config = new();
                    config.Restore(dict);
                    TextConfigs.Add(config);
                }
            }

            /// Try to restore the images that are displayed on this drawable.
            if (attributes.TryGetValue(ImageConfigsLabel, out object imageList))
            {
                int i = 0;
                foreach (object item in (List<object>)imageList)
                {
                    i++;
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    ImageConf config = new();
                    config.Restore(dict);
                    ImageConfigs.Add(config);
                }
            }

            /// Try to restore the mind map nodes that are displayed on this drawable.
            if (attributes.TryGetValue(MindMapNodeConfigsLabel, out object nodeList))
            {
                int i = 0;
                foreach (object item in (List<object>)nodeList)
                {
                    i++;
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    MindMapNodeConf config = new();
                    config.Restore(dict);
                    MindMapNodeConfigs.Add(config);
                }
            }

            return errorFree;
        }

        /// <summary>
        /// Returns a clone of this <see cref="DrawableConfig"/>.
        /// </summary>
        /// <returns>A new <see cref="DrawableConfig"/> with the values of this object.</returns>
        public object Clone()
        {
            return new DrawableConfig
            {
                ID = this.ID,
                ParentID = this.ParentID,
                Position = this.Position,
                Rotation = this.Rotation,
                Scale = this.Scale,
                Color = this.Color,
                Order = this.Order,
                Lighting = this.Lighting,
                OrderInLayer = this.OrderInLayer,
                Description = this.Description,
                Visibility = this.Visibility,
                CurrentPage = this.CurrentPage,
                MaxPageSize = this.MaxPageSize,
                LineConfigs = this.LineConfigs.ToList(),
                TextConfigs = this.TextConfigs.ToList(),
                ImageConfigs = this.ImageConfigs.ToList(),
                MindMapNodeConfigs = this.MindMapNodeConfigs.ToList(),
            };
        }

        #endregion
    }
}