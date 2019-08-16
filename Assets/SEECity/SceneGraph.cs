using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// A container for nodes and edges of the graph represented in a scene where the 
/// nodes and edges are GameObjects.
/// </summary>
public class SceneGraph : MonoBehaviour
{
    [Tooltip("The tag of all buildings")]
    public string houseTag = "House";

    [Tooltip("The tag of all connections")]
    public string edgeTag = "Edge";

    [Tooltip("The relative path to the connection preftab")]
    public string linePreftabPath = "Assets/Prefabs/Line.prefab";

    [Tooltip("The relative path to the building preftab")]
    public string housePrefabPath = "Assets/Prefabs/House.prefab";

    [Tooltip("The path to the graph data")]
    public string graphPath = "";

    [Tooltip("The name of the edge type of hierarchical edges")]
    public string hierarchicalEdgeType = "Enclosing";

    // The list of graph nodes indexed by their unique linkname
    private Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();

    // The list of graph edges.
    private List<GameObject> edges = new List<GameObject>();

    /// <summary>
    /// Called by Unity once before any Update messages is sent.
    /// Loads the graph data.
    /// 
    /// Note: This mechanism works for classes derived from MonoBehaviour.
    /// For other runtime classes, we could use the attribute 
    /// RuntimeInitializeOnLoadMethod; see
    /// https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute-ctor.html
    /// 
    /// Example: 
    /// [RuntimeInitializeOnLoadMethod]
    /// static void OnRuntimeMethodLoad()
    /// {
    /// Debug.Log("After Scene is loaded and game is running.\n");
    /// }
    /// </summary>
    private void Start()
    {
        Load();
    }

    // the unique SceneGraph instance
    private static SceneGraph instance;

    /// <summary>
    /// Returns the unique SceneGraph instance. If a GameObject with name
    /// SceneGraph exists already, that will be the instance. If no such
    /// GameObject exists yet, a new one will be created with a new 
    /// SceenGraph instance attached to it. The latter is returned then.
    /// </summary>
    /// <returns>unique SceneGraph instance</returns>
    public static SceneGraph GetInstance()
    {
        if (instance == null)
        {
            const string sceneGraphTag = "SceneGraph";
            GameObject gameObjectForSceneGraph = GameObject.Find(sceneGraphTag);
            if (gameObjectForSceneGraph != null)
            {
                instance = gameObjectForSceneGraph.GetComponent<SceneGraph>();
            }
            else
            {
                // Note: Instantiate(gameObjectForSceneGraph) will not work here, because
                // it attempts to create a copy of the given game object passed as parameter,
                // which in our case is null.
                gameObjectForSceneGraph = new GameObject
                {
                    name = sceneGraphTag
                };
                instance = gameObjectForSceneGraph.AddComponent<SceneGraph>();
            }
        }
        return instance;
    }

    /// <summary>
    /// Deletes the nodes of the graph.
    /// </summary>
    private void DeleteNodes()
    {
        foreach (KeyValuePair<string, GameObject> entry in nodes)
        {
            UnityEngine.Object.DestroyImmediate(entry.Value);
        }
        nodes.Clear();
    }

    /// <summary>
    /// Deletes the edges of the graph.
    /// </summary>
    private void DeleteEdges()
    {
        DeleteGameObjects(edges);
        edges.Clear();
    }

    /// <summary>
    /// Deletes all scene nodes and edges of the graph as well 
    /// as the graph data themselves.
    /// </summary>
    public void Delete()
    {
        DeleteNodes();
        DeleteEdges();
        graph = null;
    }

    /// <summary>
    /// Deletes all given objects immediately.
    /// </summary>
    /// <param name="objects"></param>
    private void DeleteGameObjects(List<GameObject> objects)
    {
        foreach (GameObject o in objects)
        {
            UnityEngine.Object.DestroyImmediate(o);
        }
    }

    /// <summary>
    /// The number of nodes in the scene.
    /// </summary>
    /// <returns></returns>
    public int NodeCount()
    {
        return nodes.Count;
    }

    /// <summary>
    /// Returns the scene node having the given linkname. Throws an Exception
    /// if no such node exists.
    /// </summary>
    /// <param name="linkname">unique ID of the node to be retrieved</param>
    /// <returns></returns>
    public GameObject GetGameNode(string linkname)
    {
        if (!nodes.TryGetValue(linkname, out GameObject result))
        {
            throw new Exception("Unknown node id " + linkname);
        }
        return result;
    }

    /// <summary>
    /// Returns the graph node having the given linkname. Throws an Exception
    /// if no such node exists.
    /// </summary>
    /// <param name="linkname">unique ID of the node to be retrieved</param>
    /// <returns></returns>
    public INode GetNode(string linkname)
    {
        if (graph.TryGetNode(linkname, out INode node))
        {
            return node;
        }
        else
        {
            Debug.Log("graph node number: " + graph.NodeCount + "\n");
            throw new Exception("Unknown node id " + linkname);
        }
    }

    // The underlying graph whose nodes and edges are to be visualized.
    // Note: When the game is started, this attribute initialization will be executed
    // even though the GameObject this SceneGraph is contained in continues to exist.
    // TODO: WE NEED TO PRESERVE THE GRAPH DATA.
    private IGraph graph;

    /// <summary>
    /// Loads the graph from the given file and creates the GameObjects representing
    /// its nodes and edges. Sets the graphPath attribute.
    /// </summary>
    /// <param name="filename"></param>
    public void LoadAndDraw(string filename)
    {
        graphPath = filename;
        Load();
        {
            Performance p = Performance.Begin("drawing graph");
            Draw();
            p.End();
        }
    }

    /// <summary>
    /// Loads the graph from graphPath (if set), but does not actually create the GameObjects 
    /// representing its nodes and edges.
    /// </summary>
    public void Load()
    {
        if (!string.IsNullOrEmpty(graphPath))
        {
            graph = new Graph();
            HashSet<string> hierarchicalEdges = new HashSet<string>();
            hierarchicalEdges.Add(hierarchicalEdgeType);
            GraphCreator graphCreator = new GraphCreator(graphPath, graph, hierarchicalEdges, new Logger());
            {
                Performance p = Performance.Begin("loading graph data");
                graphCreator.Load();
                p.End();
            }
            
            Debug.Log("Number of nodes loaded: " + graph.NodeCount + "\n");
            Debug.Log("Number of edges loaded: " + graph.EdgeCount + "\n");
        }
        else
        {
            Debug.LogError("No graph path given.\n");
        }
    }

    const string widthMetric = "Metric.Number_of_Tokens";
    const string heightMetric = "Metric.Clone_Rate";
    const string breadthMetric = "Metric.LOC";

    /// <summary>
    /// Draws the scene graph. 
    /// Precondition: graph data must have been loaded.
    /// </summary>
    public void Draw()
    {
        if (graph != null)
        {
            ILayout layout = new GridLayout(housePrefabPath, linePreftabPath, widthMetric, heightMetric, breadthMetric);
            layout.Draw(graph, nodes, edges);
        }
        else
        {
            Debug.LogError("No graph loaded.\n");
        }
    }
}
