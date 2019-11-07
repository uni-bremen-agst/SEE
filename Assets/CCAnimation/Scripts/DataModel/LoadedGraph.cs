using SEE;
using SEE.DataModel;
using SEE.Layout;

/// <summary>
/// TODO flo doc
/// </summary>
public class LoadedGraph
{
    private readonly Graph graph;
    private readonly AbstractCCALayout layout;
    private readonly GraphSettings graphSettings;

    /// <summary>
    /// TODO flo doc
    /// </summary>
    public Graph Graph => graph;

    /// <summary>
    /// TODO flo doc
    /// </summary>
    public AbstractCCALayout Layout => layout;

    /// <summary>
    /// TODO flo doc
    /// </summary>
    public GraphSettings Settings => graphSettings;

    /// <summary>
    /// TODO flo doc
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="layout"></param>
    /// <param name="graphSettings"></param>
    public LoadedGraph(Graph graph, AbstractCCALayout layout, GraphSettings graphSettings)
    {
        this.graph = graph.AssertNotNull("graph");
        this.layout = layout.AssertNotNull("layout");
        this.graphSettings = graphSettings.AssertNotNull("graphSettings");
    }

    
}
