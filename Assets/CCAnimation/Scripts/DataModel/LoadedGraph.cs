using SEE;
using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadedGraph
{
    private readonly Graph graph;
    private readonly CCAAbstracLayout layout;
    private readonly GraphSettings graphSettings;

    public LoadedGraph(Graph graph, CCAAbstracLayout layout, GraphSettings graphSettings)
    {
        this.graph = graph;
        this.layout = layout;
        this.graphSettings = graphSettings;
    }

    public Graph Graph => graph;

    public CCAAbstracLayout Layout => layout;

    public GraphSettings Settings => graphSettings;
}
