using Assets.SEE.Game.Drawable;
using SEE.Game.HolisticMetrics;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class can persist the configurations for a list of drawables.
    /// </summary>
    public class DrawablesConfigs
    {
        /// <summary>
        /// A list of drawables that should be saved or loaded.
        /// </summary>
        public List<DrawableConfig> Drawables = new();

        private const string DrawablesLabel = "DrawablesConfigs";

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.BeginList(DrawablesLabel);
            foreach (DrawableConfig drawableConfig in Drawables)
            {
                writer.BeginGroup();
                drawableConfig.Save(writer);
                writer.EndGroup();
            }
            writer.EndList();
        }

        /// <summary>
        /// Loads the given attributes into this instance of the class <see cref="DrawablesConfigs"/>.
        /// </summary>
        /// <param name="attributes">The attributes to load in the format created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the attributes were loaded without any errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errorFree = true;
            if (attributes.TryGetValue(DrawablesLabel, out object drawableList))
            {
                foreach (object item in (List<object>)drawableList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    DrawableConfig config = new DrawableConfig();
                    errorFree = config.Restore(dict);
                    Drawables.Add(config);
                }
            }

            return errorFree;
        }
    }

    /// <summary>
    /// This class can hold all the information that is needed to configure a drawable.
    /// </summary>
    public class DrawableConfig
    {
        /// <summary>
        /// The name of this drawable.
        /// </summary>
        public string DrawableName;

        /// <summary>
        /// The parent name of this drawable.
        /// </summary>
        public string DrawableParentName;

        /// <summary>
        /// The position of this drawable / or of his parent.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The orientation of this drawable / or of his parent.
        /// </summary>
        public Vector3 Rotation;

        /// <summary>
        /// All the lines that should be displayed on this drawable.
        /// </summary>
        public List<Line> LineConfigs = new();

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
        /// The label for the group of line configurations in the configuration file.
        /// </summary>
        private const string LineConfigsLabel = "LineConfigs";

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.Save(DrawableName, DrawableNameLabel);
            writer.Save(DrawableParentName, DrawableParentNameLabel);
            writer.Save(Position, PositionLabel);
            writer.Save(Rotation, RotationLabel);

            if (LineConfigs != null && LineConfigs.Count > 0)
            {
                writer.BeginList(LineConfigsLabel);
                foreach (Line lineConfig in LineConfigs)
                {
                    lineConfig.Save(writer);
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
            if (attributes.TryGetValue(DrawableNameLabel, out object name))
            {
                DrawableName = (string)name;
            }
            else
            {
                errorFree = false;
            }
            if (attributes.TryGetValue(DrawableParentNameLabel, out object pName))
            {
                DrawableParentName = (string)pName;
            }
            else
            {
                errorFree = false;
            }
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
            
            if (attributes.TryGetValue(LineConfigsLabel, out object lineList))
            {
                foreach (object item in (List<object>)lineList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    Line config = new Line();
                    config.Restore(dict);
                    LineConfigs.Add(config);
                }
            }
            
            return errorFree;
        }
    }
}