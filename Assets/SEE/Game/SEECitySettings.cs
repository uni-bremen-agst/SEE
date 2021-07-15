using OdinSerializer;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.Game
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
    /// Global attributes, that every city defines.
    /// </summary>
    public class GlobalCityAttributes
    {
        /// <summary>
        /// The screen relative height to use for the culling a game node [0-1].
        /// If the game node uses less than this percentage it will be culled.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float lodCulling = 0.01f;

        /// <summary>
        /// The path for the layout file containing the node layout information.
        /// If the file extension is <see cref="Filenames.GVLExtension"/>, the layout is expected
        /// to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates.
        /// Otherwise is our own layout format SDL is expected, which saves the complete Transform
        /// data of a game object.
        /// </summary>
        [OdinSerialize]
        public DataPath layoutPath = new DataPath();
    }

    /// <summary>
    /// The settings of leaf nodes of a specific kind. They may be unique per <see cref="Node.NodeDomain"/>.
    /// </summary>
    public class LeafNodeAttributes
    {
        public LeafNodeKinds kind = LeafNodeKinds.Blocks;
        public string widthMetric = NumericAttributeNames.Number_Of_Tokens.Name();
        public string heightMetric = NumericAttributeNames.Clone_Rate.Name();
        public string depthMetric = NumericAttributeNames.LOC.Name();
        public string styleMetric = NumericAttributeNames.Complexity.Name();
        public ColorRange colorRange = new ColorRange(Color.white, Color.red, 10);

        [OdinSerialize]
        public LabelSettings labelSettings = new LabelSettings();
    }

    /// <summary>
    /// The setting for inner nodes of a specific kind. They may be unique per <see cref="Node.NodeDomain"/>.
    /// </summary>
    public class InnerNodeAttributes
    {
        public InnerNodeKinds kind = InnerNodeKinds.Blocks;
        public string heightMetric = "";
        public string styleMetric = NumericAttributeNames.IssuesTotal.Name();
        public ColorRange colorRange = new ColorRange(Color.white, Color.yellow, 10);

        [OdinSerialize]
        public LabelSettings labelSettings = new LabelSettings();
    }

    /// <summary>
    /// The settings for the layout of the nodes.
    /// </summary>
    public class NodeLayoutSettings
    {
        public NodeLayoutKind kind = NodeLayoutKind.Balloon;

        /// <summary>
        /// Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        /// for range [0, max-value] is used, where max-value is the maximum value of a metric.
        /// </summary>
        public bool zScoreScale = true;

        /// <summary>
        /// Whether erosions should be visible above inner node blocks.
        /// </summary>
        public bool showInnerErosions = false;
        
        /// <summary>
        /// Whether erosions should be visible above leaf node blocks.
        /// </summary>
        public bool showLeafErosions = false;
        
        /// <summary>
        /// Whether metrics shall be retrieved from the dashboard.
        /// This includes erosion data.
        /// </summary>
        public bool loadDashboardMetrics = true;

        /// <summary>
        /// If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.
        /// </summary>
        public string issuesAddedFromVersion = "";
        
        /// <summary>
        /// Whether metrics retrieved from the dashboard shall override existing metrics.
        /// </summary>
        public bool overrideMetrics = true;

        /// <summary>
        /// Factor by which erosion icons shall be scaled.
        /// </summary>
        [Range(0.0f, float.MaxValue)]
        public float erosionScalingFactor = 1.5f;
    }

    /// <summary>
    /// The settings for the layout of the edges.
    /// </summary>
    public class EdgeLayoutSettings
    {
        public EdgeLayoutKind kind = EdgeLayoutKind.Bundling;

        [Range(0.0f, float.MaxValue)]
        public float edgeWidth = 0.01f;

        /// <summary>
        /// Orientation of the edges;
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses;
        /// </summary>
        public bool edgesAboveBlocks = true;

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// 0.85 is the value recommended by Holten
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float tension = 0.85f;

        /// <summary>
        /// Determines to which extent the polylines of the generated splines are
        /// simplified. Range: [0.0, inf] (0.0 means no simplification). More precisely,
        /// stores the epsilon parameter of the Ramer–Douglas–Peucker algorithm which
        /// is used to identify and remove points based on their distances to the line
        /// drawn between their neighbors.
        /// </summary>
        public float rdp = 0.0001f;

        public int tubularSegments = 50; // Number of segments along the tubular.
        public float radius = 0.005f; // Radius of the tubular.
        public int radialSegments = 8; // Number of segments around the tubular.
        public bool isEdgeSelectable = true; // Whether the edges are selectable or not.
    }
}
