using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// This class can hold all the information that is needed to configure a drawable.
    /// </summary>
    [Serializable]
    public class DrawableConfig
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
        /// Gets the current game object of this drawable.
        /// </summary>
        /// <returns>the drawable game object.</returns>
        public GameObject GetDrawable()
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

        #region Config I/O

        /// <summary>
        /// The label for the drawable name in the configuration file.
        /// </summary>
        private const string DrawableNameLabel = "DrawableName";

        /// <summary>
        /// The label for the drawable parent name in the configuration file.
        /// </summary>
        private const string DrawableParentNameLabel = "DrawableParentName";

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
            writer.Save(ID, DrawableNameLabel);
            writer.Save(ParentID, DrawableParentNameLabel);
            writer.Save(Position, PositionLabel);
            writer.Save(Rotation, RotationLabel);
            writer.Save(Scale, ScaleLabel);
            writer.Save(Color, ColorLabel);
            writer.Save(Order, OrderLabel);

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
            if (attributes.TryGetValue(DrawableNameLabel, out object name))
            {
                ID = (string)name;
            }
            else
            {
                errorFree = false;
            }

            /// Try to restore the drawable parent name.
            if (attributes.TryGetValue(DrawableParentNameLabel, out object pName))
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

        #endregion
    }
}