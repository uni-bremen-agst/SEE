using SEE;
using SEE.DataModel;
using SEE.Layout;

/// <summary>
/// DataModel containig all data generated for a graph loaded from a gxl-file
/// and the layout data
/// </summary>
public class LoadedGraph
{
    private readonly Graph graph;
    private readonly CCALayout layout;
    private readonly GraphSettings graphSettings;

    /// <summary>
    /// The loaded graph.
    /// </summary>
    public Graph Graph => graph;

    /// <summary>
    /// The calculated layout of the loaded graph.
    /// </summary>
    public CCALayout Layout => layout;

    /// <summary>
    /// The settings used for the loaded graph.
    /// </summary>
    public GraphSettings Settings => graphSettings;

    /// <summary>
    /// Creates a new LoadedGraph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="layout"></param>
    /// <param name="graphSettings"></param>
    public LoadedGraph(Graph graph, CCALayout layout, GraphSettings graphSettings)
    {
        this.graph = graph.AssertNotNull("graph");
        this.layout = layout.AssertNotNull("layout");
        this.graphSettings = graphSettings.AssertNotNull("graphSettings");
    }
}
