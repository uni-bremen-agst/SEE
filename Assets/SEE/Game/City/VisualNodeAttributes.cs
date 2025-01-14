using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.Utils.Config;

namespace SEE.Game.City
{
    /// <summary>
    /// All settings influencing the visual appearance of leaf or inner nodes.
    /// </summary>
    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public class VisualNodeAttributes : VisualAttributes
    {
        /// <summary>
        /// If true, the node should be rendered. Otherwise the node will be ignored when
        /// the graph is loaded.
        /// </summary>
        [Tooltip("Whether nodes of this type should be rendered.")]
        public bool IsRelevant = true;
        /// <summary>
        /// How a node should be drawn. Determines the kind of mesh.
        /// </summary>
        [Tooltip("The shape to be used to render a node of this type.")]
        public NodeShapes Shape = NodeShapes.Blocks;

        /// <summary>
        /// The default value for any length metric as a string. It will be used for the
        /// width, depth, and height. <seealso cref="MetricToLength"/>
        /// Using this default value means that no metric name is used but a fixed value.
        /// </summary>
        private const string defaultLengthMetricValue = "0.001";

        /// <summary>
        /// A mapping of node metric names onto lengths. The first three metrics
        /// are generally used to determine the width, height, and depth of visual
        /// node representations. More complex shapes may offer additional lengths.
        /// </summary>
        [Tooltip("Maps metric names onto lengths of the shape."), HideReferenceObjectPicker]
        [OdinSerialize]
        public IList<string> MetricToLength = new List<string>() { defaultLengthMetricValue, defaultLengthMetricValue, defaultLengthMetricValue };

        /// <summary>
        /// The index of the metric for the width within <see cref="MetricToLength"/>.
        /// Corresponds to the x axis of Unity's 3D co-ordinates.
        /// </summary>
        public const int WidthMetricIndex = 0;

        /// <summary>
        /// The name or value of the metric determining the width,
        /// i.e., <see cref="MetricToLength"/>[<see cref="WidthMetricIndex"/>].
        /// </summary>
        public string WidthMetric
        {
            get
            {
                if (WidthMetricIndex < MetricToLength.Count)
                {
                    return MetricToLength[WidthMetricIndex];
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"MetricToLength: {WidthMetricIndex} >= {MetricToLength.Count}");
                }
            }
        }

        /// <summary>
        /// The index of the metric for the height within <see cref="MetricToLength"/>.
        /// Corresponds to the y axis of Unity's 3D co-ordinates.
        /// </summary>
        public const int HeightMetricIndex = 1;

        /// <summary>
        /// The name or value of the metric determining the height,
        /// i.e., <see cref="MetricToLength"/>[<see cref="HeightMetricIndex"/>].
        /// </summary>
        public string HeightMetric
        {
            get
            {
                if (HeightMetricIndex < MetricToLength.Count)
                {
                    return MetricToLength[HeightMetricIndex];
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"MetricToLength: {HeightMetricIndex} >= {MetricToLength.Count}");
                }
            }
        }

        /// <summary>
        /// The index of the metric for the depth within <see cref="MetricToLength"/>.
        /// Corresponds to the z axis of Unity's 3D co-ordinates.
        /// </summary>
        public const int DepthMetricIndex = 2;

        /// <summary>
        /// The name or value of the metric determining the depth,
        /// i.e., <see cref="MetricToLength"/>[<see cref="DepthMetricIndex"/>].
        /// </summary>
        public string DepthMetric
        {
            get
            {
                if (DepthMetricIndex < MetricToLength.Count)
                {
                    return MetricToLength[DepthMetricIndex];
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"MetricToLength: {DepthMetricIndex} >= {MetricToLength.Count}");
                }
            }
        }

        /// <summary>
        /// How the color of a node should be determined.
        /// </summary>
        [OdinSerialize]
        [HideReferenceObjectPicker]
        [Tooltip("How the color of a node of this type should be determined.")]
        public ColorProperty ColorProperty = new ColorProperty();
        /// <summary>
        /// This parameter determines the minimal width, depth, and height of each block
        /// representing a graph node visually. Must not be greater than <see cref="MaximalBlockLength"/>.
        /// </summary>
        [Tooltip("Minimal width, depth, and height of the shape for nodes of this type.")]
        public float MinimalBlockLength = 0.001f; // serialized by Unity
        /// <summary>
        /// This parameter determines the maximal width, depth, and height of each block
        /// representing a graph node visually. Must not be smaller than <see cref="MinimalBlockLength"/>.
        /// </summary>
        [Tooltip("Maximal width, depth, and height of the shape for nodes of this type.")]
        public float MaximalBlockLength = 1.0f; // serialized by Unity
        /// <summary>
        /// Describes how metrics are mapped onto the antenna above the blocks.
        /// </summary>
        [OdinSerialize]
        [Tooltip("The antenna settings.")]
        public AntennaAttributes AntennaSettings = new();
        /// <summary>
        /// The settings for the labels appearing when a node is hovered over.
        /// </summary>
        [OdinSerialize]
        [Tooltip("The settings for labels drawn during hovering.")]
        public LabelAttributes LabelSettings = new();
        /// <summary>
        /// Width of the outline for leaf and inner nodes.
        /// </summary>
        [Tooltip("The outline width when a node is hovered.")]
        public float OutlineWidth = Controls.Interactables.Outline.DefaultWidth;
        /// <summary>
        /// If true, persistent text labels will be added to the node representation.
        /// </summary>
        [Tooltip("Whether the source name will be added to a node.")]
        public bool ShowNames = false;
        /// <summary>
        /// Defines if node type may be manually resized by users.
        /// </summary>
        [Tooltip("May users resize this type of node?")]
        public bool AllowManualResize = false;

        /// <summary>
        /// Saves the settings in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Shape.ToString(), nodeShapeLabel);
            writer.Save(IsRelevant, isRelevantLabel);
            writer.Save(MetricToLength, metricToLengthLabel);
            ColorProperty.Save(writer, colorPropertyLabel);
            writer.Save(MinimalBlockLength, minimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, maximalBlockLengthLabel);
            LabelSettings.Save(writer, labelSettingsLabel);
            AntennaSettings.Save(writer, antennaSettingsLabel);
            writer.Save(OutlineWidth, outlineWidthLabel);
            writer.Save(ShowNames, showNamesLabel);
            writer.Save(AllowManualResize, allowManualResizeLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                Restore(values);
            }
        }

        /// <summary>
        /// Restores the settings from <paramref name="values"/>.
        /// </summary>
        /// <param name="values">dictionary of attributes from which to retrieve the settings</param>
        internal virtual void Restore(Dictionary<string, object> values)
        {
            ConfigIO.RestoreEnum(values, nodeShapeLabel, ref Shape);
            ConfigIO.Restore(values, isRelevantLabel, ref IsRelevant);
            ConfigIO.RestoreStringList(values, metricToLengthLabel, ref MetricToLength);
            ColorProperty.Restore(values, colorPropertyLabel);
            ConfigIO.Restore(values, minimalBlockLengthLabel, ref MinimalBlockLength);
            ConfigIO.Restore(values, maximalBlockLengthLabel, ref MaximalBlockLength);
            LabelSettings.Restore(values, labelSettingsLabel);
            AntennaSettings.Restore(values, antennaSettingsLabel);
            ConfigIO.Restore(values, outlineWidthLabel, ref OutlineWidth);
            ConfigIO.Restore(values, showNamesLabel, ref ShowNames);
            ConfigIO.Restore(values, allowManualResizeLabel, ref AllowManualResize);
        }

        /// <summary>
        /// Label in the configuration file for <see cref="Shape"/>.
        /// </summary>
        private const string nodeShapeLabel = "Shape";
        /// <summary>
        /// Label in the configuration file for <see cref="IsRelevant"/>.
        /// </summary>
        private const string isRelevantLabel = "IsRelevant";
        /// <summary>
        /// Label in the configuration file for <see cref="MetricToLength"/>.
        /// </summary>
        private const string metricToLengthLabel = "MetricToLength";
        /// <summary>
        /// Label in the configuration file for a <see cref="ColorProperty"/>.
        /// </summary>
        private const string colorPropertyLabel = "ColorProperty";
        /// <summary>
        /// Label in the configuration file for <see cref="MinimalBlockLength"/>.
        /// </summary>
        private const string minimalBlockLengthLabel = "MinimalBlockLength";
        /// <summary>
        /// Label in the configuration file for <see cref="MaximalBlockLength"/>.
        /// </summary>
        private const string maximalBlockLengthLabel = "MaximalBlockLength";
        /// <summary>
        /// Label in the configuration file for <see cref="LabelSettings"/>.
        /// </summary>
        private const string labelSettingsLabel = "LabelSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="AntennaSettings"/>.
        /// </summary>
        private const string antennaSettingsLabel = "AntennnaSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="OutlineWidth"/>.
        /// </summary>
        private const string outlineWidthLabel = "OutlineWidth";
        /// <summary>
        /// Label in the configuration file for <see cref="ShowNames"/>.
        /// </summary>
        private const string showNamesLabel = "ShowNames";

        /// <summary>
        /// Label in the configuration file for <see cref="AllowManualResize"/>.
        /// </summary>
        private const string allowManualResizeLabel = "AllowManualResize";
    }
}
