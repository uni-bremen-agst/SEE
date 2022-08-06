using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

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
        /// Constructor.
        /// </summary>
        /// <param name="typeName">name of the node type for which these settings are intended</param>
        public VisualNodeAttributes(string typeName)
        {
            NodeType = typeName;
        }

        public VisualNodeAttributes()
        {
            NodeType = "A Node Type";
        }

        /// <summary>
        /// The name of the node type that is specified. See <see cref="NodeType"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private string nodeType = string.Empty;
        /// <summary>
        /// The name of the node type that is specified.
        /// </summary>
        [HideInInspector]
        public string NodeType { get => nodeType; private set => nodeType = value; }
        /// <summary>
        /// If true, the node should be rendered. Otherwise the node will be ignored when
        /// the graph is loaded.
        /// </summary>
        public bool IsRelevant = true;
        /// <summary>
        /// How a node should be drawn. Determines the kind of mesh.
        /// </summary>
        public NodeShapes Shape = NodeShapes.Blocks;

        private const string DefaultLengthMetricValue = "0.001";

        /// <summary>
        /// A mapping of node metric names onto colors. The first three metrics
        /// are generally used to determine the width, height, and depth of visual
        /// node representations.
        /// </summary>
        [Tooltip("Maps metric names onto lengths of the shape."), HideReferenceObjectPicker]
        [NonSerialized, OdinSerialize]
        public IList<string> MetricToLength = new List<string>() { DefaultLengthMetricValue, DefaultLengthMetricValue, DefaultLengthMetricValue };

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
        public ColorProperty ColorProperty = new ColorProperty();
        /// <summary>
        /// This parameter determines the minimal width, breadth, and height of each block
        /// representing a graph node visually. Must not be greater than <see cref="MaximalBlockLength"/>.
        /// </summary>
        public float MinimalBlockLength = 0.001f; // serialized by Unity
        /// <summary>
        /// This parameter determines the maximal width, breadth, and height of each block
        /// representing a graph node visually. Must not be smaller than <see cref="MinimalBlockLength"/>.
        /// </summary>
        public float MaximalBlockLength = 1.0f; // serialized by Unity
        /// <summary>
        /// Describes how metrics are mapped onto the antenna above the blocks.
        /// </summary>
        [OdinSerialize]
        public AntennaAttributes AntennaSettings = new AntennaAttributes();
        /// <summary>
        /// The settings for the labels appearing when a node is hovered over.
        /// </summary>
        [OdinSerialize]
        public LabelAttributes LabelSettings = new LabelAttributes();
        /// <summary>
        /// Width of the outline for leaf and inner nodes.
        /// </summary>
        public float OutlineWidth = Controls.Interactables.Outline.DefaultWidth;
        /// <summary>
        /// If true, persistent text labels will be added to the node representation.
        /// </summary>
        public bool ShowNames = false;

        /// <summary>
        /// Saves the settings in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(NodeType, NodeTypeLabel);
            writer.Save(Shape.ToString(), NodeShapeLabel);
            writer.Save(IsRelevant, IsRelevantLabel);
            writer.Save(MetricToLength, MetricToLengthLabel);
            ColorProperty.Save(writer, ColorPropertyLabel);
            writer.Save(MinimalBlockLength, MinimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, MaximalBlockLengthLabel);
            LabelSettings.Save(writer, LabelSettingsLabel);
            AntennaSettings.Save(writer, AntennaSettingsLabel);
            writer.Save(OutlineWidth, OutlineWidthLabel);
            writer.Save(ShowNames, ShowNamesLabel);
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
            ConfigIO.Restore(values, NodeTypeLabel, ref nodeType);
            ConfigIO.RestoreEnum(values, NodeShapeLabel, ref Shape);
            ConfigIO.Restore(values, IsRelevantLabel, ref IsRelevant);
            ConfigIO.RestoreStringList(values, MetricToLengthLabel, ref MetricToLength);
            ColorProperty.Restore(values, ColorPropertyLabel);
            ConfigIO.Restore(values, MinimalBlockLengthLabel, ref MinimalBlockLength);
            ConfigIO.Restore(values, MaximalBlockLengthLabel, ref MaximalBlockLength);
            LabelSettings.Restore(values, LabelSettingsLabel);
            AntennaSettings.Restore(values, AntennaSettingsLabel);
            ConfigIO.Restore(values, OutlineWidthLabel, ref OutlineWidth);
            ConfigIO.Restore(values, ShowNamesLabel, ref ShowNames);
        }

        /// <summary>
        /// Label in the configuration file for <see cref="NodeType"/>.
        /// </summary>
        private const string NodeTypeLabel = "NodeType";
        /// <summary>
        /// Label in the configuration file for <see cref="Shape"/>.
        /// </summary>
        private const string NodeShapeLabel = "Shape";
        /// <summary>
        /// Label in the configuration file for <see cref="IsRelevant"/>.
        /// </summary>
        private const string IsRelevantLabel = "IsRelevant";
        /// <summary>
        /// Label in the configuration file for <see cref="MetricToLength"/>.
        /// </summary>
        private const string MetricToLengthLabel = "MetricToLength";
        /// <summary>
        /// Label in the configuration file for a <see cref="ColorProperty"/>.
        /// </summary>
        private const string ColorPropertyLabel = "ColorProperty";
        /// <summary>
        /// Label in the configuration file for <see cref="MinimalBlockLength"/>.
        /// </summary>
        private const string MinimalBlockLengthLabel = "MinimalBlockLength";
        /// <summary>
        /// Label in the configuration file for <see cref="MaximalBlockLength"/>.
        /// </summary>
        private const string MaximalBlockLengthLabel = "MaximalBlockLength";
        /// <summary>
        /// Label in the configuration file for <see cref="LabelSettings"/>.
        /// </summary>
        private const string LabelSettingsLabel = "LabelSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="AntennaSettings"/>.
        /// </summary>
        private const string AntennaSettingsLabel = "AntennnaSettings";
        /// <summary>
        /// Label in the configuration file for <see cref="OutlineWidth"/>.
        /// </summary>
        private const string OutlineWidthLabel = "OutlineWidth";
        /// <summary>
        /// Label in the configuration file for <see cref="ShowNames"/>.
        /// </summary>
        private const string ShowNamesLabel = "ShowNames";
    }
}
