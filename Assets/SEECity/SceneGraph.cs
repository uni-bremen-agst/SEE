using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SEE;

namespace SEEEditor
{
    /// <summary>
    /// A container for nodes and edges of the graph represented in a scene where the 
    /// nodes and edges are GameObjects. It will be created at design time and used at
    /// runtime to relate graph objects of the underlying data model to the GameObjects
    /// in the scene.
    /// </summary>
    public class SceneGraph : MonoBehaviour
    {
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
            Debug.Log("SceneGraph.Start started\n");
            Load(settings);
            Debug.Log("SceneGraph.Start ended\n");
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
        private SEE.IGraph graph;

        // Path to the graph containing the graph data. It must be loaded at runtime
        // so that GameObjects (which are generated at design time) and graph data
        // can be connected. Only the GameObjects are created and preserved when the
        // system is built, but not the data of the underlying graph.

        private SEEEditor.EditorSettings settings;

        /// <summary>
        /// Loads the graph from graphPath (if set), but does not actually create the GameObjects 
        /// representing its nodes and edges.
        /// </summary>
        public void Load(SEEEditor.EditorSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.graphPath))
            {
                this.settings = settings;
                graph = new Graph();
                HashSet<string> hierarchicalEdges = new HashSet<string>
                {
                    settings.hierarchicalEdgeType
                };
                SEE.GraphCreator graphCreator = new SEE.GraphCreator(settings.graphPath, graph, hierarchicalEdges, new SEELogger());
                {
                    SEE.Performance p = SEE.Performance.Begin("loading graph data");
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
                SEE.ILayout layout = new SEE.GridLayout(settings.nodePrefabPath, settings.edgePreftabPath, widthMetric, heightMetric, breadthMetric);
                layout.Draw(graph, nodes, edges);
            }
            else
            {
                Debug.LogError("No graph loaded.\n");
            }
        }

    }
}