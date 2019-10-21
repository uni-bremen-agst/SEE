using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CCAAbstracLayout : ILayout
{
    /// <summary>
    /// The names of the metrics for inner nodes to be put onto the 
    /// </summary>
    protected readonly string[] innerNodeMetrics;

    /// <summary>
    /// Whether erosions should be visible above the nodes.
    /// </summary>
    public bool showErosions = true;

    /// <summary>
    /// Whether donut charts should be visible in the inner circle of the nodes.
    /// </summary>
    public bool showDonuts = true;

    /// <summary>
    /// The minimal line width of a circle drawn for an inner node.
    /// </summary>
    public float minmalCircleLineWidth = 0.1f;

    /// <summary>
    /// The maximal line width of a circle drawn for an inner node.
    /// </summary>
    public float maximalCircleLineWidth = 10.0f;

    // A mapping of graph nodes onto the game objects representing them visually in the scene
    public readonly Dictionary<Node, GameObject> gameObjects = new Dictionary<Node, GameObject>();
    public readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();
    public readonly Dictionary<string, GameObject> edges = new Dictionary<string, GameObject>();


    protected readonly Dictionary<string, Vector3> nodePositions = new Dictionary<string, Vector3>();
    protected readonly Dictionary<string, Vector3> circlePositions = new Dictionary<string, Vector3>();
    protected readonly Dictionary<string, float> circleRadiuses = new Dictionary<string, float>();

    public CCAAbstracLayout(bool showEdges,
                         string widthMetric, string heightMetric, string breadthMetric,
                         SerializableDictionary<string, IconFactory.Erosion> issueMap,
                         string[] innerNodeMetrics,
                         BlockFactory blockFactory,
                         IScale scaler,
                         float edgeWidth,
                         bool showErosions,
                         bool edgesAboveBlocks)
    : base(showEdges, widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, edgeWidth, showErosions, edgesAboveBlocks)
    {
        name = "Ballon";
        this.innerNodeMetrics = innerNodeMetrics;
    }

    const float planeMeshFactor = 10.0f;

    protected Vector3 _planePosition = new Vector3();
    protected Vector3 _planeScale = new Vector3(0, planeMeshFactor, 0);

    public Vector3 PlanePositon => _planePosition;
    public Vector3 PlaneScale => _planeScale;

    public Vector3 CirclePosition(Node node)
    {
        circlePositions.TryGetValue(node.LinkName, out Vector3 position);
        return position;
    }

    public float CircleRadius(Node node)
    {
        circleRadiuses.TryGetValue(node.LinkName, out float radius);
        return radius;
    }
}
