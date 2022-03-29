using System;
using System.Collections.Generic;
using OdinSerializer;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// The kinds of node layouts available.
    /// </summary>
    public enum NodeLayoutKind : byte
    {
        EvoStreets,
        Balloon,
        RectanglePacking,
        Treemap,
        CirclePacking,
        Manhattan,
        CompoundSpringEmbedder,
        FromFile
    }

    /// <summary>
    /// The kinds of edge layouts available.
    /// </summary>
    public enum EdgeLayoutKind : byte
    {
        None,
        Straight,
        Spline,
        Bundling
    }

    /// <summary>
    /// How leaf graph nodes should be depicted.
    /// </summary>
    public enum LeafNodeKinds : byte
    {
        Blocks
    }

    /// <summary>
    /// How inner graph nodes should be depicted.
    /// </summary>
    public enum InnerNodeKinds : byte
    {
        Blocks,
        Rectangles,
        Donuts,
        Circles,
        Empty,
        Cylinders
    }

    /// <summary>
    /// Abstract common super class for all settings influencing the visual
    /// appearance of game objects drawn in the scene.
    /// </summary>
    [Serializable]
    public abstract class VisualAttributes
    {
        /// <summary>
        /// Saves the settings in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public abstract void Save(ConfigWriter writer, string label);
        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        public abstract void Restore(Dictionary<string, object> attributes, string label);
    }

    /// <summary>
    /// Abstract common super class for all settings influencing the visual
    /// appearance of leaf or inner nodes.
    /// </summary>
    [Serializable]
    public abstract class VisualNodeAttributes : VisualAttributes
    {
        /// <summary>
        /// The name of the metric determining the height.
        /// </summary>
        public string HeightMetric = "";
        /// <summary>
        /// The name of the metric determining the style (color) of a node.
        /// </summary>
        public string ColorMetric = "";
        /// <summary>
        /// The range of colors for the style metric.
        /// </summary>
        public ColorRange ColorRange = new ColorRange(Color.white, Color.red, 10);
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
        /// Saves the settings in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(HeightMetric, HeightMetricLabel);
            writer.Save(ColorMetric, StyleMetricLabel);
            ColorRange.Save(writer, ColorRangeLabel);
            LabelSettings.Save(writer, LabelSettingsLabel);
            AntennaSettings.Save(writer, AntennaSettingsLabel);
            writer.Save(OutlineWidth, OutlineWidthLabel);
            SaveAdditionalAttributes(writer);
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

                ConfigIO.Restore(values, HeightMetricLabel, ref HeightMetric);
                ConfigIO.Restore(values, StyleMetricLabel, ref ColorMetric);
                ColorRange.Restore(values, ColorRangeLabel);
                LabelSettings.Restore(values, LabelSettingsLabel);
                AntennaSettings.Restore(values, AntennaSettingsLabel);
                ConfigIO.Restore(values, OutlineWidthLabel, ref OutlineWidth);
            }
        }

        /// <summary>
        /// Saves the additional attributes of subclasses. The enclosing group
        /// is already opened. Implementations must not call writer.BeginGroup(label)
        /// and writer.EndGroup().
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        protected abstract void SaveAdditionalAttributes(ConfigWriter writer);

        /// <summary>
        /// Label in the configuration file for the kind of object drawn for a node.
        /// It must be visible to the subclasses.
        /// </summary>
        protected const string NodeKindsLabel = "Kind";
        /// <summary>
        /// Label in the configuration file for a color range.
        /// </summary>
        private const string ColorRangeLabel = "ColorRange";
        /// <summary>
        /// Label in the configuration file for a node style (color actually).
        /// </summary>
        private const string StyleMetricLabel = "StyleMetric";
        /// <summary>
        /// Label in the configuration file for a height metric.
        /// </summary>
        private const string HeightMetricLabel = "HeightMetric";
        /// <summary>
        /// Label in the configuration file for the label settings for leaf and inner nodes.
        /// </summary>
        private const string LabelSettingsLabel = "LabelSettings";
        /// <summary>
        /// Label in the configuration file for the antenna settings for leaf and inner nodes.
        /// </summary>
        private const string AntennaSettingsLabel = "AntennnaSettings";
        /// <summary>
        /// Label in the configuration file for the width of the outline for leaf and inner nodes.
        /// </summary>
        private const string OutlineWidthLabel = "OutlineWidth";
    }

    /// <summary>
    /// The settings of leaf nodes of a specific kind.
    /// </summary>
    [Serializable]
    public class LeafNodeAttributes : VisualNodeAttributes
    {
        /// <summary>
        /// How a leaf node should be drawn.
        /// </summary>
        public LeafNodeKinds Kind = LeafNodeKinds.Blocks;
        /// <summary>
        /// Name of the metric defining the width.
        /// </summary>
        public string WidthMetric = NumericAttributeNames.Number_Of_Tokens.Name();
        /// <summary>
        /// Name of the metric defining the depth.
        /// </summary>
        public string DepthMetric = NumericAttributeNames.LOC.Name();
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
        /// Saves the settings specific to this class in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        protected override void SaveAdditionalAttributes(ConfigWriter writer)
        {
            writer.Save(Kind.ToString(), NodeKindsLabel);
            writer.Save(WidthMetric, WidthMetricLabel);
            writer.Save(DepthMetric, DepthMetricLabel);
            writer.Save(MinimalBlockLength, MinimalBlockLengthLabel);
            writer.Save(MaximalBlockLength, MaximalBlockLengthLabel);
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
            base.Restore(attributes, label);

            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, NodeKindsLabel, ref Kind);
                ConfigIO.Restore(values, WidthMetricLabel, ref WidthMetric);
                ConfigIO.Restore(values, DepthMetricLabel, ref DepthMetric);
                ConfigIO.Restore(values, MinimalBlockLengthLabel, ref MinimalBlockLength);
                ConfigIO.Restore(values, MaximalBlockLengthLabel, ref MaximalBlockLength);
            }
        }

        /// <summary>
        /// Label in the configuration file for the width metric.
        /// </summary>
        private const string WidthMetricLabel = "WidthMetric";
        /// <summary>
        /// Label in the configuration file for the depth metric.
        /// </summary>
        private const string DepthMetricLabel = "DepthMetric";
        /// <summary>
        /// Label in the configuration file for the minimal block length of a node.
        /// </summary>
        private const string MinimalBlockLengthLabel = "MinimalBlockLength";
        /// <summary>
        /// Label in the configuration file for the maximal block length of a node.
        /// </summary>
        private const string MaximalBlockLengthLabel = "MaximalBlockLength";
    }

    /// <summary>
    /// The setting for inner nodes of a specific kind. They may be unique per <see cref="Node.NodeDomain"/>.
    /// </summary>
    [Serializable]
    public class InnerNodeAttributes : VisualNodeAttributes
    {
        /// <summary>
        /// How an inner node should be drawn.
        /// </summary>
        public InnerNodeKinds Kind = InnerNodeKinds.Blocks;

        /// <summary>
        /// The metric to be put in the inner circle of a Donut chart.
        /// </summary>
        public string InnerDonutMetric = NumericAttributeNames.IssuesTotal.Name(); // serialized by Unity

        /// <summary>
        /// If true, persistent text labels will be added to inner nodes.
        /// </summary>
        public bool ShowNames = false;

        /// <summary>
        /// Saves the settings specific to this class in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        protected override void SaveAdditionalAttributes(ConfigWriter writer)
        {
            writer.Save(Kind.ToString(), NodeKindsLabel);
            writer.Save(ShowNames, ShowNamesLabel);
            writer.Save(InnerDonutMetric, InnerDonutMetricLabel);
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
            base.Restore(attributes, label);
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, NodeKindsLabel, ref Kind);
                ConfigIO.Restore(values, ShowNamesLabel, ref ShowNames);
                ConfigIO.Restore(values, InnerDonutMetricLabel, ref InnerDonutMetric);
            }
        }

        private const string InnerDonutMetricLabel = "InnerDonutMetric";
        private const string ShowNamesLabel = "ShowNames";
    }

    /// <summary>
    /// The settings of one antenna section specifying the metric that
    /// determines the length and color of the section.
    /// </summary>
    [Serializable]
    public class AntennaSection : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// The metric which should determine the length of the section.
        /// </summary>
        public string Metric;
        /// <summary>
        /// The color in which the section should be drawn.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metric">the metric that should determine the length of the section</param>
        /// <param name="color">color in the section should be drawn</param>
        public AntennaSection(string metric, Color color)
        {
            Metric = metric;
            Color = color;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public AntennaSection() : this(string.Empty, Color.white) { }

        /// <summary>
        /// Label in the configuration file for the <see cref="Metric"/>.
        /// </summary>
        private const string MetricLabel = "Metric";
        /// <summary>
        /// Label in the configuration file for the <see cref="Color"/>.
        /// </summary>
        private const string ColorLabel = "Color";

        /// <summary>
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Metric, MetricLabel);
            writer.Save(Color, ColorLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Restore(Dictionary{string, object}, string)"/>.
        /// </summary>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            Dictionary<string, object> values;
            if (string.IsNullOrEmpty(label))
            {
                // no label given => attributes contains already the data to be restored
                values = attributes;
            }
            else if (attributes.TryGetValue(label, out object dictionary))
            {
                // label was given => attributes is a dictionary where we need to look up the data
                // using the label
                values = dictionary as Dictionary<string, object>;
            }
            else
            {
                // label was given, but attributes does not know it
                // => no data; we cannot restore the object
                return false;
            }

            bool metricRestored = ConfigIO.Restore(values, MetricLabel, ref Metric);
            return ConfigIO.Restore(values, ColorLabel, ref Color) || metricRestored;
        }
    }

    /// <summary>
    /// Specifies how metrics are to be rendered as an antenna above the blocks.
    /// </summary>
    [Serializable]
    public class AntennaAttributes : ConfigIO.PersistentConfigItem
    {
        /// <summary>
        /// This parameter determines the sections of the antenna.
        /// </summary>
        [SerializeField]
        public List<AntennaSection> AntennaSections = new List<AntennaSection>(1);

        /// <summary>
        /// The width of an antenna.
        /// </summary>
        public float AntennaWidth = 0.1f;

        /// <summary>
        /// Saves the settings in the configuration file.
        ///
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(AntennaWidth, AntennaWidthLabel);
            writer.Save(AntennaSections, AntennaSectionsLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        ///
        /// Implements <see cref="ConfigIO.PersistentConfigItem.Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        /// <returns>true if at least one attribute was successfully restored</returns>
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                bool result = false;
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                ConfigIO.Restore(values, AntennaWidthLabel, ref AntennaWidth);
                if (values.TryGetValue(AntennaSectionsLabel, out object antennaSections))
                {
                    if (!(antennaSections is IList<object> objects))
                    {
                        throw new InvalidCastException($"Value to be cast {antennaSections} is expected to be a list. Actual type is {antennaSections.GetType().Name}");
                    }
                    foreach (object anObject in objects)
                    {
                        if (!(anObject is Dictionary<string, object> antennaSection))
                        {
                            throw new InvalidCastException($"Value to be cast {anObject} is expected to be a dictionary. Actual type is {anObject.GetType().Name}");
                        }
                        AntennaSection section = new AntennaSection();
                        result = section.Restore(antennaSection) || result;
                        AddAntennaSection(section);
                    }
                }
                return result;
            }
            else
            {
                return false;
            }

            // If AntennaSections already has an antenna section for the metric in newSection,
            // it will be removed. Then the newSection is added to it.
            void AddAntennaSection(AntennaSection newSection)
            {
                AntennaSections.RemoveAll(section => section.Metric == newSection.Metric);
                AntennaSections.Add(newSection);
            }
        }

        /// <summary>
        /// Label in the configuration file for the antenna width.
        /// </summary>
        private const string AntennaWidthLabel = "Width";

        /// <summary>
        /// Label in the configuration file for the antenna sections.
        /// </summary>
        private const string AntennaSectionsLabel = "AntennaSections";
    }

    /// <summary>
    /// Common super class for settings of node and edge layouts.
    /// </summary>
    [Serializable]
    public abstract class LayoutSettings : VisualAttributes
    {
    }

    /// <summary>
    /// The settings for the layout of the nodes.
    /// </summary>
    [Serializable]
    public class NodeLayoutAttributes : LayoutSettings
    {
        /// <summary>
        /// How to layout the nodes.
        /// </summary>
        public NodeLayoutKind Kind = NodeLayoutKind.Balloon;

        /// <summary>
        /// The path for the layout file containing the node layout information.
        /// If the file extension is <see cref="Filenames.GVLExtension"/>, the layout is expected
        /// to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates.
        /// Otherwise our own layout format SDL is expected, which saves the complete Transform
        /// data of a game object.
        /// </summary>
        [OdinSerialize]
        public DataPath LayoutPath = new DataPath();

        private const string LayoutPathLabel = "LayoutPath";

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), NodeLayoutLabel);
            LayoutPath.Save(writer, LayoutPathLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, NodeLayoutLabel, ref Kind);
                LayoutPath.Restore(values, LayoutPathLabel);
            }
        }

        private const string NodeLayoutLabel = "NodeLayout";
    }

    /// <summary>
    /// The settings for the layout of the edges.
    /// </summary>
    [Serializable]
    public class EdgeLayoutAttributes : LayoutSettings
    {
        /// <summary>
        /// Layout for drawing edges.
        /// </summary>
        public EdgeLayoutKind Kind = EdgeLayoutKind.Bundling;
        /// <summary>
        /// The width of an edge (drawn as line).
        /// </summary>
        [Range(0.0f, float.MaxValue)]
        public float EdgeWidth = 0.01f;
        /// <summary>
        /// Orientation of the edges;
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses.
        /// </summary>
        public bool EdgesAboveBlocks = true;
        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// 0.85 is the value recommended by Holten
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float Tension = 0.85f;
        /// <summary>
        /// Determines to which extent the polylines of the generated splines are
        /// simplified. Range: [0.0, inf] (0.0 means no simplification). More precisely,
        /// stores the epsilon parameter of the RamerDouglasPeucker algorithm which
        /// is used to identify and remove points based on their distances to the line
        /// drawn between their neighbors.
        /// </summary>
        public float RDP = 0.0001f;

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Kind.ToString(), EdgeLayoutLabel);
            writer.Save(EdgeWidth, EdgeWidthLabel);
            writer.Save(EdgesAboveBlocks, EdgesAboveBlocksLabel);
            writer.Save(Tension, TensionLabel);
            writer.Save(RDP, RDPLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.RestoreEnum(values, EdgeLayoutLabel, ref Kind);
                ConfigIO.Restore(values, EdgeWidthLabel, ref EdgeWidth);
                ConfigIO.Restore(values, EdgesAboveBlocksLabel, ref EdgesAboveBlocks);
                ConfigIO.Restore(values, TensionLabel, ref Tension);
                ConfigIO.Restore(values, RDPLabel, ref RDP);
            }
        }

        private const string EdgeLayoutLabel = "EdgeLayout";
        private const string EdgeWidthLabel = "EdgeWidth";
        private const string EdgesAboveBlocksLabel = "EdgesAboveBlocks";
        private const string TensionLabel = "Tension";
        private const string RDPLabel = "RDP";
    }

    /// <summary>
    /// Attributes regarding the selection of edges.
    /// </summary>
    [Serializable]
    public class EdgeSelectionAttributes : VisualAttributes
    {
        /// <summary>
        /// Number of segments along the tubular for edge selection.
        /// </summary>
        public int TubularSegments = 50;
        /// <summary>
        /// Radius of the tubular for edge selection.
        /// </summary>
        public float Radius = 0.005f;
        /// <summary>
        /// Number of segments around the tubular for edge selection.
        /// </summary>
        public int RadialSegments = 8;
        /// <summary>
        /// Whether the edges are selectable or not.
        /// </summary>
        public bool AreSelectable = true;

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(TubularSegments, TubularSegmentsLabel);
            writer.Save(Radius, RadiusLabel);
            writer.Save(RadialSegments, RadialSegmentsLabel);
            writer.Save(AreSelectable, AreSelectableLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, TubularSegmentsLabel, ref TubularSegments);
                ConfigIO.Restore(values, RadialSegmentsLabel, ref RadialSegments);
                ConfigIO.Restore(values, RadiusLabel, ref Radius);
                ConfigIO.Restore(values, AreSelectableLabel, ref AreSelectable);
            }
        }

        private const string TubularSegmentsLabel = "TubularSegments";
        private const string RadialSegmentsLabel = "RadialSegments";
        private const string RadiusLabel = "Radius";
        private const string AreSelectableLabel = "AreSelectable";
    }

    /// <summary>
    /// Axivion's software erosion issues shown as icons above nodes.
    /// </summary>
    [Serializable]
    public class ErosionAttributes : VisualAttributes
    {
        /// <summary>
        /// Whether erosions should be visible above inner node blocks.
        /// </summary>
        public bool ShowInnerErosions = false;

        /// <summary>
        /// Whether erosions should be visible above leaf node blocks.
        /// </summary>
        public bool ShowLeafErosions = false;

        /// <summary>
        /// Whether metrics shall be retrieved from the dashboard.
        /// This includes erosion data.
        /// </summary>
        public bool LoadDashboardMetrics = false;

        /// <summary>
        /// If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.
        /// </summary>
        public string IssuesAddedFromVersion = "";

        /// <summary>
        /// Whether metrics retrieved from the dashboard shall override existing metrics.
        /// </summary>
        public bool OverrideMetrics = true;
        /// <summary>
        /// Factor by which erosion icons shall be scaled.
        /// </summary>
        [Range(0.0f, float.MaxValue)]
        public float ErosionScalingFactor = 1.5f;

        /// <summary>
        /// The attribute name of the metric representing architecture violations.
        /// </summary>
        public string ArchitectureIssue = NumericAttributeNames.Architecture_Violations.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing duplicated code.
        /// </summary>
        public string CloneIssue = NumericAttributeNames.Clone.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing cylces.
        /// </summary>
        public string CycleIssue = NumericAttributeNames.Cycle.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing dead code.
        /// </summary>
        public string Dead_CodeIssue = NumericAttributeNames.Dead_Code.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing metric violations.
        /// </summary>
        public string MetricIssue = NumericAttributeNames.Metric.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing code-style violations.
        /// </summary>
        public string StyleIssue = NumericAttributeNames.Style.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing other kinds of violations.
        /// </summary>
        public string UniversalIssue = NumericAttributeNames.Universal.Name(); // serialized by Unity

        //-----------------------------------------------------------------------
        // Software erosion issues shown as icons on Donut charts for inner nodes
        //-----------------------------------------------------------------------
        public const string SUM_Postfix = "_SUM";
        /// <summary>
        /// The attribute name of the metric representing the sum of all architecture violations
        /// for an inner node.
        /// </summary>
        public string ArchitectureIssue_SUM = NumericAttributeNames.Architecture_Violations.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all clones
        /// for an inner node.
        /// </summary>
        public string CloneIssue_SUM = NumericAttributeNames.Clone.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all cycles
        /// for an inner node.
        /// </summary>
        public string CycleIssue_SUM = NumericAttributeNames.Cycle.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all dead entities
        /// for an inner node.
        /// </summary>
        public string Dead_CodeIssue_SUM = NumericAttributeNames.Dead_Code.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all metric violations
        /// for an inner node.
        /// </summary>
        public string MetricIssue_SUM = NumericAttributeNames.Metric.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all style violations
        /// for an inner node.
        /// </summary>
        public string StyleIssue_SUM = NumericAttributeNames.Style.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all other kinds of
        /// software erosions for an inner node.
        /// </summary>
        public string UniversalIssue_SUM = NumericAttributeNames.Universal.Name() + SUM_Postfix; // serialized by Unity

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ShowInnerErosions, ShowInnerErosionsLabel);
            writer.Save(ShowLeafErosions, ShowLeafErosionsLabel);
            writer.Save(LoadDashboardMetrics, LoadDashboardMetricsLabel);
            writer.Save(OverrideMetrics, OverrideMetricsLabel);
            writer.Save(IssuesAddedFromVersion, IssuesFromVersionLabel);
            writer.Save(ErosionScalingFactor, ErosionScalingFactorLabel);

            writer.Save(StyleIssue, StyleIssueLabel);
            writer.Save(UniversalIssue, UniversalIssueLabel);
            writer.Save(MetricIssue, MetricIssueLabel);
            writer.Save(Dead_CodeIssue, Dead_CodeIssueLabel);
            writer.Save(CycleIssue, CycleIssueLabel);
            writer.Save(CloneIssue, CloneIssueLabel);
            writer.Save(ArchitectureIssue, ArchitectureIssueLabel);

            writer.Save(StyleIssue_SUM, StyleIssue_SUMLabel);
            writer.Save(UniversalIssue_SUM, UniversalIssue_SUMLabel);
            writer.Save(MetricIssue_SUM, MetricIssue_SUMLabel);
            writer.Save(Dead_CodeIssue_SUM, Dead_CodeIssue_SUMLabel);
            writer.Save(CycleIssue_SUM, CycleIssue_SUMLabel);
            writer.Save(CloneIssue_SUM, CloneIssue_SUMLabel);
            writer.Save(ArchitectureIssue_SUM, ArchitectureIssue_SUMLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, ShowInnerErosionsLabel, ref ShowInnerErosions);
                ConfigIO.Restore(values, ShowLeafErosionsLabel, ref ShowLeafErosions);
                ConfigIO.Restore(values, LoadDashboardMetricsLabel, ref LoadDashboardMetrics);
                ConfigIO.Restore(values, OverrideMetricsLabel, ref OverrideMetrics);
                ConfigIO.Restore(values, IssuesFromVersionLabel, ref IssuesAddedFromVersion);
                ConfigIO.Restore(values, ErosionScalingFactorLabel, ref ErosionScalingFactor);

                ConfigIO.Restore(values, StyleIssueLabel, ref StyleIssue);
                ConfigIO.Restore(values, UniversalIssueLabel, ref UniversalIssue);
                ConfigIO.Restore(values, MetricIssueLabel, ref MetricIssue);
                ConfigIO.Restore(values, Dead_CodeIssueLabel, ref Dead_CodeIssue);
                ConfigIO.Restore(values, CycleIssueLabel, ref CycleIssue);
                ConfigIO.Restore(values, CloneIssueLabel, ref CloneIssue);
                ConfigIO.Restore(values, ArchitectureIssueLabel, ref ArchitectureIssue);

                ConfigIO.Restore(values, StyleIssue_SUMLabel, ref StyleIssue_SUM);
                ConfigIO.Restore(values, UniversalIssue_SUMLabel, ref UniversalIssue_SUM);
                ConfigIO.Restore(values, MetricIssue_SUMLabel, ref MetricIssue_SUM);
                ConfigIO.Restore(values, Dead_CodeIssue_SUMLabel, ref Dead_CodeIssue_SUM);
                ConfigIO.Restore(values, CycleIssue_SUMLabel, ref CycleIssue_SUM);
                ConfigIO.Restore(values, CloneIssue_SUMLabel, ref CloneIssue_SUM);
                ConfigIO.Restore(values, ArchitectureIssue_SUMLabel, ref ArchitectureIssue_SUM);
            }
        }

        private const string ShowLeafErosionsLabel = "ShowLeafErosions";
        private const string ShowInnerErosionsLabel = "ShowInnerErosions";
        private const string ErosionScalingFactorLabel = "ErosionScalingFactor";
        private const string LoadDashboardMetricsLabel = "LoadDashboardMetrics";
        private const string IssuesFromVersionLabel = "IssuesAddedFromVersion";
        private const string OverrideMetricsLabel = "OverrideMetrics";

        private const string StyleIssueLabel = "StyleIssue";
        private const string UniversalIssueLabel = "UniversalIssue";
        private const string MetricIssueLabel = "MetricIssue";
        private const string Dead_CodeIssueLabel = "Dead_CodeIssue";
        private const string CycleIssueLabel = "CycleIssue";
        private const string CloneIssueLabel = "CloneIssue";
        private const string ArchitectureIssueLabel = "ArchitectureIssue";

        private const string StyleIssue_SUMLabel = "StyleIssue_SUM";
        private const string UniversalIssue_SUMLabel = "UniversalIssue_SUM";
        private const string MetricIssue_SUMLabel = "MetricIssue_SUM";
        private const string Dead_CodeIssue_SUMLabel = "Dead_CodeIssue_SUM";
        private const string CycleIssue_SUMLabel = "CycleIssue_SUM";
        private const string CloneIssue_SUMLabel = "CloneIssue_SUM";
        private const string ArchitectureIssue_SUMLabel = "ArchitectureIssue_SUM";
    }
}
