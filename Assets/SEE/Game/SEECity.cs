using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Manages settings of the graph data showing a single version of a software
    /// system needed at runtime.
    /// </summary>
    public class SEECity : AbstractSEECity
    {
        /// <summary>
        /// The graph that is visualized in the scene and whose visualization settings are 
        /// managed here.
        /// We do not want to serialize it using Unity or Odin because both frameworks are
        /// insufficient for the highly recursive structure of all the graph objects.
        /// There are different points in time in which the underlying graph is created:
        /// (1) in the editor mode or (2) during the game. If the graph is created in the
        /// editor mode by a graph renderer, the graph renderer will attached the NodeRefs
        /// to the game objects representing the nodes. So all the information about nodes
        /// is available, for instance for the layouts or the inspector. During the game 
        /// there are two different possible scenarios: (a) this SEECity is is created 
        /// and configured at runtime or (b) the SEECity was created in the editor and then
        /// the game is started. For scenario (a), we expect the graph to be loaded and
        /// all NodeRefs be defined accordingly. For scenario (b), the graph attribute
        /// will not be serialized and, hence, be null. In that case, we load the graph
        /// from the GXL file, i.e., the GXL file is our persistent serialization we 
        /// use to re-create the graph. We need, however, to set the NodeRefs at runtime.
        /// All that is being done in Start() below.
        /// </summary>
        [NonSerialized]
        private Graph loadedGraph = null;

        /// <summary>
        /// The graph underlying this SEE city that was loaded from disk. May be null.
        /// If a new graph is assigned to this property, the selected node types will
        /// be updated, too.
        /// </summary>
        public Graph LoadedGraph
        {
            get => loadedGraph;
            set
            {
                loadedGraph = value;
                InspectSchema(loadedGraph);
            }
        }

        /// <summary>
        /// The graph to be visualized. It may be a subgraph of the loaded graph
        /// containing only nodes with relevant node types or the original LoadedGraph
        /// if all node types are relevant. It is null if no graph has been loaded yet.
        /// </summary>
        public Graph VisualizedSubGraph
        {
            get
            {
                if (loadedGraph == null)
                {
                    return null;
                }
                else
                {
                    Graph graph = RelevantGraph(loadedGraph);
                    LoadDataForGraphListing(graph);
                    return graph;
                }
            }
        }

        private static readonly Dictionary<string, SEECity> dict = new Dictionary<string, SEECity>();
        public static SEECity GetByGraph(Graph graph)
        {
            SEECity result = null;

            if (graph.Path != null)
            {
                if (!dict.TryGetValue(graph.Path, out result))
                {
                    if (graph.Name != null)
                    {
                        dict.TryGetValue(graph.Name, out result);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Loads the graph from GXLPath() and sets all NodeRef components to the
        /// loaded nodes if GXLPath() yields a valid filename. This "deserializes"
        /// the graph to make it available at runtime.
        /// </summary>
        protected void Awake()
        {
            string filename = GXLPath;
            if (loadedGraph != null)
            {
                Debug.Log("SEECity.Awake: graph is already loaded.\n");
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                loadedGraph = LoadGraph(filename);
                if (loadedGraph != null)
                {
                    LoadMetrics();
                    SetNodeEdgeRefs(loadedGraph, gameObject);
                }
                else
                {
                    Debug.LogErrorFormat("SEECity.Awake: Could not load GXL file {0}.\n", filename);
                }
            }
            else
            {
                Debug.LogError("SEECity.Awake: GXL file is undefined.\n");
            }

            if (loadedGraph != null)
            {
                if (dict.ContainsKey(filename))
                {
                    Debug.LogWarning("Graph seems to exists twice!");
                }
                else
                {
                    dict.Add(filename, this);
                }
            }

#if true
            RemoveTransparency();
#endif
        }

        /// <summary>
        /// All game objects representing a graph node or edge in the current scene will be made
        /// opaque (no transparency).
        /// </summary>
        private static void RemoveTransparency()
        {
            // Remove transparency of all nodes and edges
            foreach (NodeRef nodeRef in FindObjectsOfType<NodeRef>())
            {
                MeshRenderer meshRenderer = nodeRef.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    Material material = meshRenderer.material;
                    Color color = material.GetColor("_Color");
                    color.a = 1.0f;
                    material.SetColor("_Color", color);
                }
            }
            foreach (EdgeRef edgeRef in FindObjectsOfType<EdgeRef>())
            {
                LineRenderer lineRenderer = edgeRef.gameObject.GetComponent<LineRenderer>();
                if (lineRenderer)
                {
                    Material material = lineRenderer.material;
                    Color color = material.GetColor("_Color");
                    color.a = 1.0f;
                    material.SetColor("_Color", color);
                }
            }
        }

        /// <summary>
        /// Sets all NodeRefs and EdgeRefs for this city to the nodes and edges, respectively,
        /// they correspond to. This will take place for the graph elements of the
        /// <see cref="LoadedGraph"/> only.
        /// We assume that the game objects with a NodeRef/EdgeRef required to be
        /// defined to be immediate children of this SEECity. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node/edge.
        /// </summary>
        public void SetNodeEdgeRefs()
        {
            if (loadedGraph != null)
            {
                SetNodeEdgeRefs(loadedGraph, gameObject);
                Debug.LogFormat("Node and edge references for {0} are resolved.\n", gameObject.name);
            }
            else
            {
                Debug.LogError("No graph loaded.\n");
            }
        }

        /// <summary>
        /// Sets all NodeRefs and EdgeRefs for this city to the nodes and edges,
        /// respectively, they correspond to.
        /// We assume that the game objects with a NodeRef/EdgeRef required to be
        /// defined to be immediate children of this SEECity. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node/edge.
        /// </summary>
        /// <param name="graph">graph giving us the nodes/edges who should be the
        /// target of the NodeRefs and EdgeRefs, respectively</param>
        protected void SetNodeEdgeRefs(Graph graph, GameObject parent)
        {
            foreach (Transform childTransform in parent.transform)
            {
                GameObject child = childTransform.gameObject;
                if (child.TryGetComponent<NodeRef>(out NodeRef nodeRef))
                {
                    nodeRef.node = graph.GetNode(child.name);
                    if (nodeRef.node == null)
                    {
                        Debug.LogWarningFormat("Could not resolve node reference {0}.\n", child.name);
                    }
                }
                else if (child.TryGetComponent<EdgeRef>(out EdgeRef edgeRef))
                {
                    edgeRef.edge = graph.GetEdge(child.name);
                    if (edgeRef.edge == null)
                    {
                        Debug.LogWarningFormat("Could not resolve edge reference {0}.\n", child.name);          
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Game object {0} has neither node nor edge reference.\n", child.name);
                }
                SetNodeEdgeRefs(graph, child);
            }
        }

        /// <summary>
        /// The relative path for the GXL file containing the graph data.
        /// </summary>
        public string gxlPath = "..\\Data\\GXL\\minimal_clones.gxl";
        /// <summary>
        /// The relative path for the CSV file containing the node metrics.
        /// </summary>
        public string csvPath = "..\\Data\\GXL\\minimal_clones.csv";

        /// <summary>
        /// Returns the concatenation of pathPrefix and gxlPath. That is the complete
        /// absolute path to the GXL file containing the graph data.
        /// </summary>
        /// <returns>concatenation of pathPrefix and gxlPath</returns>
        public string GXLPath => PathPrefix + gxlPath;

        /// <summary>
        /// Returns the concatenation of pathPrefix and csvPath. That is the complete
        /// absolute path to the CSV file containing the additional metric values.
        /// </summary>
        /// <returns>concatenation of pathPrefix and csvPath</returns>
        public string CSVPath => PathPrefix + csvPath;

        /// <summary>
        /// Loads the metrics from CSVPath() and aggregates and adds them to the graph.
        /// Precondition: graph must have been loaded before.
        /// </summary>
        private void LoadMetrics()
        {
            string filename = CSVPath;
            Performance p = Performance.Begin("loading metric data data from CSV file " + filename);
            int numberOfErrors = MetricImporter.Load(LoadedGraph, filename);
            if (numberOfErrors > 0)
            {
                Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", filename, numberOfErrors);
            }
            p.End();
        }

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath() and then draws it. Equivalent to:
        ///   LoadData();
        ///   DrawGraph();
        /// </summary>
        public virtual void LoadAndDrawGraph()
        {
            LoadData();
            DrawGraph();
        }

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath(). Afterwards, DrawGraph() can be used
        /// to actually render the graph data.
        /// </summary>
        public virtual void LoadData()
        {
            if (string.IsNullOrEmpty(GXLPath))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }
                LoadedGraph = LoadGraph(GXLPath);
                LoadMetrics();
            }
        }

        /// <summary>
        /// Saves the graph data to the GXL file with GXLPath().
        /// </summary>
        public virtual void SaveData()
        {
            if (string.IsNullOrEmpty(GXLPath))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    // This loop runs only once for the first hierarchical edge type
                    // we encounter. There is no simple method to retrieve an 
                    // arbitrary element from a HashSet (the type of HierarchicalEdges).
                    foreach (string hierarchicalEdge in HierarchicalEdges)
                    {
                        GraphWriter.Save(GXLPath, LoadedGraph, hierarchicalEdge);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Re-draws the graph without deleting the underlying loaded graph.
        /// Only the game objects generated for the nodes are deleted first.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        public void ReDrawGraph()
        {
            if (loadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                DeleteGraphGameObjects();
                DrawGraph();
            }
        }

        /// <summary>
        /// The graph renderer used to draw the city.
        /// </summary>
        private GraphRenderer graphRenderer;

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        public void DrawGraph()
        {
            if (loadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                Graph visualizedSubGraph = VisualizedSubGraph;
                if (ReferenceEquals(visualizedSubGraph, null))
                {
                    Debug.LogError("No graph loaded.\n");
                }
                else
                {
                    graphRenderer = new GraphRenderer(this, visualizedSubGraph);
                    // We assume here that this SEECity instance was added to a game object as
                    // a component. The inherited attribute gameObject identifies this game object.
                    graphRenderer.Draw(gameObject);
                }
            }
        }

        /// <summary>
        /// Yields a graph renderer that can draw this city.
        /// </summary>
        public GraphRenderer Renderer
        {
            get
            {
                if (graphRenderer == null)
                {
                    return new GraphRenderer(this, VisualizedSubGraph);
                }
                return graphRenderer;
            }
        }

        /// <summary>
        /// Saves the current layout of the city in a file named <see cref="LayoutPath"/>.
        /// The format of the written file depends upon the file extension. If the extension
        /// is <see cref="Filenames.GVLExtension"/> it is saved in the GVL format; otherwise
        /// the file is saved in the SLD format.
        /// </summary>
        public void SaveLayout()
        {
            string path = LayoutPath;
            Debug.LogFormat("Saving layout data to {0}.\n", path);
            if (Filenames.HasExtension(path, Filenames.GVLExtension))
            {
                Layout.IO.GVLWriter.Save(LayoutPath, loadedGraph.Name, AllNodeDescendants(gameObject));
            }
            else
            {
                Layout.IO.SLDWriter.Save(LayoutPath, AllNodeDescendants(gameObject));
            }
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the selected node types,
        /// the underlying graph, and all game objects visualizing information about it.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            if (loadedGraph != null)
            {
                loadedGraph.Destroy();
            }
            LoadedGraph = null;
            Measurements.Clear();
        }
    }
}