#define SEE_MANUAL_RENDERING
//#define SEE_RENDER_BACKFACES

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using SEE.Tools;
using SEE.DataModel;
using SEE.DataModel.IO;
using SEE.GO;
using SEE.Utils;

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
                    return loadedGraph;
                }
                else
                {
                    return RelevantGraph(loadedGraph);
                }
            }
        }

        /// <summary>
        /// Loads the graph from GXLPath() and sets all NodeRef components to the
        /// loaded nodes if GXLPath() yields a valid filename. This "deserializes"
        /// the graph to make it available at runtime.
        /// </summary>
        protected void Awake()
        {
            string filename = GXLPath();
            if (loadedGraph == null && !string.IsNullOrEmpty(filename))
            {
                loadedGraph = LoadGraph(filename);
                if (loadedGraph != null)
                {
                    SetNodeRefs(loadedGraph, gameObject);
                }
            }

            Materials.SetGlobalUniforms();
            UpdateMaterialProperties(gameObject);

#if SEE_MANUAL_RENDERING

            if (transform.childCount != 0)
            {
                DisableAllMeshRenderersOfNodes(gameObject);

                // Prepare all directories
                Transform rootTransform = transform.GetChild(0);
                int maxDirLevel = GetMaxDirectoryLevel(rootTransform);

                RenderCommand rootCommand;
                rootCommand.transform = rootTransform;
                rootCommand.mesh = rootTransform.GetComponent<MeshFilter>().mesh;
                rootCommand.material = rootTransform.GetComponent<MeshRenderer>().material;

                List<RenderCommand[]> dirCommandsList = new List<RenderCommand[]>(maxDirLevel)
                {
                    new RenderCommand[1] { rootCommand }
                };

                Color rootColor = rootCommand.material.GetColor("_Color");
                rootColor.a = 0.5f;
                rootCommand.material.SetColor("_Color", rootColor);

                for (int i = 1; i < maxDirLevel; i++)
                {
                    List<RenderCommand> commands = new List<RenderCommand>();
                    foreach (RenderCommand lastDir in dirCommandsList[i - 1])
                    {
                        Transform lastDirTransform = lastDir.transform;
                        for (int j = 0; j < lastDirTransform.childCount; j++)
                        {
                            Transform dirTransform = lastDirTransform.GetChild(j);
                            NodeRef nodeRef = dirTransform.GetComponent<NodeRef>();
                            if (nodeRef != null && nodeRef.node != null && nodeRef.node.Type.Equals("Directory"))
                            {
                                RenderCommand command;
                                command.transform = dirTransform;
                                command.mesh = dirTransform.GetComponent<MeshFilter>().mesh;
                                command.material = dirTransform.GetComponent<MeshRenderer>().material;

                                Color color = command.material.GetColor("_Color");
                                color.a = 0.5f;
                                command.material.SetColor("_Color", color);

                                commands.Add(command);
                            }
                        }
                    }
                    dirCommandsList.Add(commands.ToArray());
                }
                dirCommands = dirCommandsList.ToArray();

                // Prepare all files
                List<RenderCommand> fileCommandsList = new List<RenderCommand>();
                List<short> fileIndicesList = new List<short>();

                NodeRef[] nodeRefs = FindObjectsOfType<NodeRef>();

                for (int i = 0; i < nodeRefs.Length; i++)
                {
                    NodeRef nodeRef = nodeRefs[i];
                    if (nodeRef.node != null && nodeRef.node.Type.Equals("File"))
                    {
                        UnityEngine.Assertions.Assert.IsTrue(i <= short.MaxValue);
                        fileIndicesList.Add((short)fileIndicesList.Count);

                        RenderCommand command;
                        command.transform = nodeRef.transform;
                        command.mesh = nodeRef.GetComponent<MeshFilter>().mesh;
                        command.material = nodeRef.GetComponent<MeshRenderer>().material;

                        Color color = command.material.GetColor("_Color");
                        color.a = 0.5f;
                        command.material.SetColor("_Color", color);

                        fileCommandsList.Add(command);
                    }
                }
                fileCommands = fileCommandsList.ToArray();
                fileIndices = fileIndicesList.ToArray();

                lastMaterial = null;
            }

#endif
        }

#if SEE_MANUAL_RENDERING

        private struct RenderCommand
        {
            internal Transform transform;
            internal Mesh mesh;
            internal Material material;
        }

        private RenderCommand[][] dirCommands;
        private short[] fileIndices;
        private RenderCommand[] fileCommands;
        private Material lastMaterial;

        private void OnRenderObject()
        {
            if (transform.childCount != 0)
            {
                // Render all directories
                for (int i = 0; i < dirCommands.Length; i++)
                {
                    RenderCommand[] dirComms = dirCommands[i];

                    for (int j = 0; j < dirComms.Length; j++)
                    {
                        if (dirComms[j].material != lastMaterial)
                        {
                            lastMaterial = dirComms[j].material;
                            dirComms[j].material.SetPass(0);
                        }
                        Graphics.DrawMeshNow(dirComms[j].mesh, dirComms[j].transform.localToWorldMatrix);
                    }
                }

                // Render all files
                Vector3 cameraPosition = Camera.main.transform.position;
                int CompareRenderCommandIndices(short i0, short i1)
                {
                    return (fileCommands[i1].transform.position - cameraPosition).sqrMagnitude.CompareTo((fileCommands[i0].transform.position - cameraPosition).sqrMagnitude);
                }
                Array.Sort(fileIndices, CompareRenderCommandIndices);
                for (int i = 0; i < fileIndices.Length; i++)
                {
#if SEE_RENDER_BACKFACES
                    fileRenderCommands[fileRenderCommandIndices[i]].material.SetPass(0);
                    fileRenderCommands[fileRenderCommandIndices[i]].material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                    Graphics.DrawMeshNow(fileRenderCommands[fileRenderCommandIndices[i]].mesh, fileRenderCommands[fileRenderCommandIndices[i]].transform.localToWorldMatrix);
                    fileRenderCommands[fileRenderCommandIndices[i]].material.SetPass(0);
                    fileRenderCommands[fileRenderCommandIndices[i]].material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
                    Graphics.DrawMeshNow(fileRenderCommands[fileRenderCommandIndices[i]].mesh, fileRenderCommands[fileRenderCommandIndices[i]].transform.localToWorldMatrix);
#else
                    if (fileCommands[fileIndices[i]].material != lastMaterial)
                    {
                        lastMaterial = fileCommands[fileIndices[i]].material;
                        fileCommands[fileIndices[i]].material.SetPass(0);
                    }
                    Graphics.DrawMeshNow(fileCommands[fileIndices[i]].mesh, fileCommands[fileIndices[i]].transform.localToWorldMatrix);
#endif
                }

                lastMaterial = null;
            }
        }

        private void DisableAllMeshRenderersOfNodes(GameObject go)
        {
            if (go.tag.Equals(Tags.Node))
            {
                NodeRef nodeRef = go.GetComponent<NodeRef>();
                if (nodeRef != null)
                {
                    MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
                    if (meshRenderer)
                    {
                        meshRenderer.enabled = false;
                    }
                }
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                DisableAllMeshRenderersOfNodes(go.transform.GetChild(i).gameObject);
            }
        }

        private int GetMaxDirectoryLevel(Transform t)
        {
            int maxLevel = 0;

            NodeRef nodeRef = t.GetComponent<NodeRef>();
            if (nodeRef != null)
            {
                for (int i = 0; i < t.childCount; i++)
                {
                    int level = 1 + GetMaxDirectoryLevel(t.GetChild(i));
                    if (level > maxLevel)
                    {
                        maxLevel = level;
                    }
                }
            }

            return maxLevel;
        }

#endif

        /// <summary>
        /// Sets all NodeRefs for this city to the nodes they correspond to.
        /// We assume that the game objects with a NodeRef required to be defined to be
        /// immediate children of this SEECity. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node.
        /// </summary>
        /// <param name="graph">graph giving us the nodes who should be the
        /// target of the NodeRefs</param>
        protected void SetNodeRefs(Graph graph, GameObject parent)
        {
            foreach (Transform childTransform in parent.transform)
            {
                GameObject child = childTransform.gameObject;
                NodeRef nodeRef = child.GetComponent<NodeRef>();
                if (nodeRef != null)
                {
                    nodeRef.node = graph.GetNode(child.name);
                    if (nodeRef.node == null)
                    {
                        Debug.LogWarningFormat("Could not resolve node reference {0}.\n", child.name);
                    }
                }
                SetNodeRefs(graph, child);
            }
        }

        protected void UpdateMaterialProperties(GameObject parent)
        {
            foreach (Transform childTransform in parent.transform)
            {
                GameObject child = childTransform.gameObject;
                if (child.tag.Equals(Tags.Node))
                {
                    Materials.SetProperties(child.GetComponent<MeshRenderer>().material);
                }
                UpdateMaterialProperties(child);
            }
        }

        /// Clone graph with one directory and two files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\two_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\two_files.csv";

        /// Clone graph with one directory and three files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\three_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\three_files.csv";

        /// Very tiny clone graph with single root, one child as a leaf and 
        /// two more children with two children each to experiment with.
        //public string gxlPath = "..\\Data\\GXL\\micro_clones.gxl";
        //public string csvPath = "..\\Data\\GXL\\micro_clones.csv";

        /// Tiny clone graph with single root to experiment with.
        //public string gxlPath = "..\\Data\\GXL\\minimal_clones.gxl";
        //public string csvPath = "..\\Data\\GXL\\minimal_clones.csv";

        /// Tiny clone graph with single roots to check edge bundling.
        //public string gxlPath = "..\\Data\\GXL\\controlPoints.gxl";
        //public string csvPath = "..\\Data\\GXL\\controlPoints.csv";

        // Smaller clone graph with single root (Linux directory "fs").
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\fs.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\fs.csv";

        // Smaller clone graph with single root (Linux directory "net").

        /// <summary>
        /// The relative path for the GXL file containing the graph data.
        /// </summary>
        public string gxlPath = "..\\Data\\GXL\\linux-clones\\net.gxl";
        /// <summary>
        /// The relative path for the CSV file containing the node metrics.
        /// </summary>
        public string csvPath = "..\\Data\\GXL\\linux-clones\\net.csv";

        // Larger clone graph with single root (Linux directory "drivers"): 16.920 nodes, 10583 edges.
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\drivers.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\drivers.csv";

        // Medium size include graph with single root (OpenSSL).
        //public string gxlPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.gxl";
        //public string csvPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.csv";

        // Examples for dynamic call graphs
        //public string gxlPath = "..\\Data\\GXL\\dynamic-tests\\example_02.gxl";
        //public string csvPath = "..\\Data\\GXL\\dynamic-tests\\empty.csv";
        //public string dynPath = "..\\Data\\DYN\\example_02.dyn";

        /// <summary>
        /// Returns the concatenation of pathPrefix and gxlPath. That is the complete
        /// absolute path to the GXL file containing the graph data.
        /// </summary>
        /// <returns>concatenation of pathPrefix and gxlPath</returns>
        public string GXLPath()
        {
            return PathPrefix + gxlPath;
        }

        /// <summary>
        /// Returns the concatenation of pathPrefix and csvPath. That is the complete
        /// absolute path to the CSV file containing the additional metric values.
        /// </summary>
        /// <returns>concatenation of pathPrefix and csvPath</returns>
        public string CSVPath()
        {
            return PathPrefix + csvPath;
        }

        /// <summary>
        /// Loads the metrics from CSVPath() and aggregates and adds them to the graph.
        /// Precondition: graph must have been loaded before.
        /// </summary>
        private void LoadMetrics()
        {
            int numberOfErrors = MetricImporter.Load(LoadedGraph, CSVPath());
            if (numberOfErrors > 0)
            {
                Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", CSVPath(), numberOfErrors);
            }
            {
                MetricAggregator.AggregateSum(LoadedGraph, AllLeafIssues().ToArray<string>());
                // Note: We do not want to compute the derived metric editorSettings.InnerDonutMetric
                // when we have a single root node in the graph. This metric will be used to define the color
                // of inner circles of Donut charts. Because the color is a linear interpolation of the whole
                // metric value range, the inner circle would always have the maximal value (it is the total
                // sum over all) and hence the maximal color gradient. The color of the other nodes would be
                // hardly distinguishable. 
                // FIXME: We need a better solution. This is a kind of hack.
                MetricAggregator.DeriveSum(LoadedGraph, AllInnerNodeIssues().ToArray<string>(), InnerDonutMetric, true);
            }
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
            if (string.IsNullOrEmpty(GXLPath()))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }
                LoadedGraph = LoadGraph(GXLPath());
                LoadMetrics();
            }
        }

        /// <summary>
        /// Saves the graph data to the GXL file with GXLPath().
        /// </summary>
        public virtual void SaveData()
        {
            if (string.IsNullOrEmpty(GXLPath()))
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
                        GraphWriter.Save(GXLPath(), LoadedGraph, hierarchicalEdge);
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
                DeleteGameObjects();
                DrawGraph();
            }
        }

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
                    GraphRenderer renderer = new GraphRenderer(this);
                    // We assume here that this SEECity instance was added to a game object as
                    // a component. The inherited attribute gameObject identifies this game object.
                    renderer.Draw(visualizedSubGraph, gameObject);
                    // If CScape buildings are used, the scale of the world is larger and, hence, the camera needs to move faster.
                    // We may have cities with blocks and cities with CScape buildings in the same scene.
                    // We cannot simply alternate the speed each time when a graph is loaded.
                    // Cameras.AdjustCameraSpeed(renderer.Unit());
                }
            }
        }

        /// <summary>
        /// Saves the current layout of the city as GVL in a file name GVLPath.
        /// </summary>
        public void SaveLayout()
        {
            SEE.Layout.IO.Writer.Save(GVLPath(), loadedGraph.Name, AllNodeDescendants(gameObject));
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the node types,
        /// the underlying graph, and all game objects visualizing information about it.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            DeleteGameObjects();
            // Delete the underlying graph.
            if (loadedGraph != null)
            {
                loadedGraph.Destroy();
            }
            LoadedGraph = null;
        }

        /// <summary>
        /// Deletes all game objects that were created for rendering the graph.
        /// The underlying loaded graph is not deleted.
        /// </summary>
        private void DeleteGameObjects()
        {
            // Delete all children.
            // Note: foreach (GameObject child in transform)... would not work;
            // we really need to collect all children first and only then can destroy each.
            foreach (GameObject child in AllChildren())
            {
                Destroyer.DestroyGameObject(child);
            }
        }

        /// <summary>
        /// Returns all immediate children of the game object this SEECity is attached to.
        /// </summary>
        /// <returns>immediate children of the game object this SEECity is attached to</returns>
        private List<GameObject> AllChildren()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in transform)
            {
                result.Add(child.gameObject);
            }
            return result;
        }

        /// <summary>
        /// Returns all (transitive) descendants of <paramref name="go"/> that are tagged
        /// by Tags.Node (including <paramref name="go"/> if it is tagged by Tags.Node).
        /// </summary>
        /// <param name="go">game objects whose node descendants are required</param>
        /// <returns>all node descendants of <paramref name="go"/></returns>
        private static ICollection<GameObject> AllNodeDescendants(GameObject go)
        {
            List<GameObject> result = new List<GameObject>();
            if (go.tag == Tags.Node)
            {
                result.Add(go);
            }
            foreach (Transform child in go.transform)
            {
                ICollection<GameObject> ascendants = AllNodeDescendants(child.gameObject);
                result.AddRange(ascendants);
            }
            return result;
        }
    }
}
