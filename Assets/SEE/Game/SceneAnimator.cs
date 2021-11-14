using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// This scene animator is intended to create code cities for capturing 
    /// videos to showcase SEE. One can show/hide nodes and edges in 
    /// the architecture, animate the evolution of the system (random
    /// graph node selection), and animate the execution (random graph
    /// tree walks). 
    /// 
    /// Its only purpose was to create a video. Do not use this class.
    /// </summary>
    [ExecuteAlways]
    [Obsolete("Introduced only for capturing videos.")]
    public class SceneAnimator : MonoBehaviour
    {        
        public SEECity CodeCity;

        //------------------
        // Edge types 
        //------------------

        public static readonly HashSet<string> ReflexionEdgeTypes = new HashSet<string>()
        {
            "Absence",             
            "Convergence",
            "Divergence",
        };

        public static readonly HashSet<string> ArchitectureEdgeTypes = new HashSet<string>()
        {
            "Source_Dependency",
        };

        public static readonly HashSet<string> AllEdgeTypes = new HashSet<string>();

        private HashSet<string> implementationEdgeTypes = new HashSet<string>();

        public HashSet<string> ImplementationEdgeTypes
        {
            get
            {
                if (implementationEdgeTypes.Count == 0)
                {
                    implementationEdgeTypes = GetImplementationEdgeTypes();
                    foreach (string type in implementationEdgeTypes)
                    {
                        Debug.LogFormat("implementation edge type {0}\n", type);
                    }
                }
                return implementationEdgeTypes;
            }
        }

        private static HashSet<string> GetImplementationEdgeTypes()
        {
            HashSet<string> result = new HashSet<string>();
            foreach (GameObject go in GetAllEdges())
            {
                string type = GetGraphEdge(go).Type;
                if (!ReflexionEdgeTypes.Contains(type) && !ArchitectureEdgeTypes.Contains(type))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        // -----------------------
        // Edge property defaults
        // -----------------------
        private static readonly Color implementationEdgesColorDefault = Color.black;
        private static readonly Color architectureEdgesColorDefault = Color.blue;
        private static readonly float implementationEdgeWidthDefault = 0.001f;
        private static readonly float architectureEdgeWidthDefault = 0.005f;
        private static readonly float reflexionEdgeWidthDefault = 0.005f;

        /// <summary>
        /// Resets the settings to their defaults. Called in editor mode when the
        /// user resets the component.
        /// </summary>
        private void Reset()
        {
            Debug.Log("SceneAnimator.Reset() was called.\n");

            implementationEdgesVisible = true;            
            implementationEdgesStartColor = Lighter(implementationEdgesColorDefault);
            implementationEdgesEndColor = implementationEdgesColorDefault;
            implementationEdgesWidth = implementationEdgeWidthDefault;

            architectureEdgesVisible = true;
            architectureEdgesStartColor = Lighter(architectureEdgesColorDefault);
            architectureEdgesEndColor = architectureEdgesColorDefault;
            architectureEdgesWidth = architectureEdgeWidthDefault;

            reflexionEdgesVisible = true;
            reflexionEdgesWidth = reflexionEdgeWidthDefault;

            architectureNodesVisible = true;
            state = State.none;
        }

        // --------------------------------------------
        // Update
        // --------------------------------------------

        private enum State
        {
            none,
            runningEvolution,
            runningCallGraph,
        }

        private State state = State.none;

        private void Update()
        {
            switch (state)
            {
                case State.runningEvolution:
                    UpdateEvolution();
                    break;
                case State.runningCallGraph:
                    UpdateCallGraph();
                    break;
            }
        }

        public void UpdateCity()
        {
            if (CodeCity != null)
            {
                CodeCity.SetNodeEdgeRefs();
                if (CodeCity.gameObject != null)
                {
                    Transform cityRootNode = SceneQueries.GetCityRootNode(CodeCity.gameObject);
                    if (cityRootNode != null)
                    {
                        GameObject root = cityRootNode.gameObject;
                        if (root != null)
                        {
                            Debug.LogFormat("Root of {0} is {1}\n", CodeCity.name, root.name);
                            ColorNodes(root);
                        }
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("Code city is null in {0}.\n", name);
            }
        }

        private void ColorNodes(GameObject root)
        {
            ColorNodes(root, false, Color.red);
            ColorNodes(root, true, Color.white);            
        }

        private void ColorNodes(GameObject parent, bool architectureNode, Color color)
        {
            // Is root a node at all? It may as well be an edge, for instance.
            if (parent.TryGetComponent<NodeRef>(out NodeRef nodeRef) && nodeRef.Value != null)
            {
                bool isArchitectureNode = IsArchitectureNode(parent);
                Color childColor = color;

                if (!isArchitectureNode && GetGraphNode(parent).IsLeaf())
                {
                    // We are dealing with an implementation node that is a leaf.
                    // We will not change the color of implementation nodes that are leaves;
                    // neither do we need to traverse any children because there are none.
                }
                else
                {
                    if ((architectureNode && isArchitectureNode) || (!architectureNode && !isArchitectureNode))
                    {
                        parent.SetColor(color);
                        childColor = Darker(color, 0.35f);
                    }
                    foreach (Transform child in parent.transform)
                    {
                        ColorNodes(child.gameObject, architectureNode, childColor);
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("Game object {0} has no valid node reference.\n", parent.name);
            }
        }

        //------------------
        // Edge in the scene
        //------------------

        private static List<GameObject> GetAllEdges()
        {
            List<GameObject> edges = new List<GameObject>();
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(Tags.Edge))
            {
                if (go.TryGetComponent<EdgeRef>(out EdgeRef edgeRef) && edgeRef.Value != null)
                {
                    edges.Add(go);
                }
                else
                {
                    Debug.LogErrorFormat("Edge {0} has no valid edge reference.\n", go.name);
                }
            }
            return edges;
        }

        private static Edge GetGraphEdge(GameObject gameEdge)
        {
            if (gameEdge.TryGetComponent<EdgeRef>(out EdgeRef edgeRef))
            {
                return edgeRef.Value;
            }
            else
            {
                Debug.LogErrorFormat("Edge {0} has no valid edge reference.\n", gameEdge.name);
                return null;
            }
        }

        //------------------
        // Node in the scene
        //------------------

        private static List<GameObject> GetAllNodes()
        {
            List<GameObject> nodes = new List<GameObject>();
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (go.TryGetComponent<NodeRef>(out NodeRef nodeRef) && nodeRef.Value != null)
                {
                    nodes.Add(go);
                }
                else
                {
                    Debug.LogErrorFormat("Node {0} has no valid node reference.\n", go.name);
                }
            }
            return nodes;
        }

        private static Node GetGraphNode(GameObject gameNode)
        {
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                return nodeRef.Value;
            }
            else
            {
                Debug.LogErrorFormat("Node {0} has no valid node reference.\n", gameNode.name);
                return null;
            }
        }

        //------------------------------------
        // Implementation edge type properties
        //------------------------------------

        private bool implementationEdgesVisible = true;
        public bool ImplementationEdgesVisible
        {
            get => implementationEdgesVisible;
            set
            {
                if (implementationEdgesVisible != value)
                {
                    implementationEdgesVisible = value;
                    SetEdgeVisiblity(ImplementationEdgeTypes, implementationEdgesVisible);
                }                
            }
        }

        private Color implementationEdgesStartColor = Lighter(implementationEdgesColorDefault);
        public Color ImplementationEdgesStartColor
        {
            get => implementationEdgesStartColor;
            set
            {
                if (implementationEdgesStartColor != value)
                {
                    implementationEdgesStartColor = value;
                    SetLine(ImplementationEdgeTypes, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
                }
            }
        }

        private Color implementationEdgesEndColor = implementationEdgesColorDefault;
        public Color ImplementationEdgesEndColor
        {
            get => implementationEdgesEndColor;
            set
            {
                if (implementationEdgesEndColor != value)
                {
                    implementationEdgesEndColor = value;
                    SetLine(ImplementationEdgeTypes, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
                }
            }
        }

        private float implementationEdgesWidth = implementationEdgeWidthDefault;
        public float ImplementationEdgesWidth
        {
            get => implementationEdgesWidth;
            set
            {
                if (implementationEdgesWidth != value)
                {
                    implementationEdgesWidth = value;
                    SetLine(ImplementationEdgeTypes, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
                }
            }
        }

        //------------------------------------
        // Reflexion edge type properties
        //------------------------------------

        private bool reflexionEdgesVisible = true;
        public bool ReflexionEdgesVisible
        {
            get => reflexionEdgesVisible;
            set
            {
                if (reflexionEdgesVisible != value)
                {
                    reflexionEdgesVisible = value;
                    SetEdgeVisiblity(ReflexionEdgeTypes, reflexionEdgesVisible);
                }
            }
        }

        private float reflexionEdgesWidth = reflexionEdgeWidthDefault;
        public float ReflexionEdgesWidth
        {
            get => reflexionEdgesWidth;
            set
            {
                if (reflexionEdgesWidth != value)
                {
                    reflexionEdgesWidth = value;
                    SetReflexionEdgeLine(reflexionEdgesWidth);
                }
            }
        }

        private void SetReflexionEdgeLine(float reflexionEdgesWidth)
        {
            SetLine(new HashSet<string>() { "Absence" },     Color.yellow, Darker(Color.yellow), reflexionEdgesWidth);
            SetLine(new HashSet<string>() { "Convergence" }, Color.green, Darker(Color.green),   reflexionEdgesWidth);
            SetLine(new HashSet<string>() { "Divergence" },  Color.red, Darker(Color.red),       reflexionEdgesWidth);
        }

        //------------------------------------
        // Architecture edge type properties
        //------------------------------------

        private bool architectureEdgesVisible = true;
        public bool ArchitectureEdgesVisible
        {
            get => architectureEdgesVisible;
            set
            {
                if (architectureEdgesVisible != value)
                {
                    architectureEdgesVisible = value;
                    SetEdgeVisiblity(ArchitectureEdgeTypes, architectureEdgesVisible);
                }
            }
        }

        private Color architectureEdgesStartColor = Lighter(Color.blue);
        public Color ArchitectureEdgesStartColor
        {
            get => architectureEdgesStartColor;
            set
            {
                if (architectureEdgesStartColor != value)
                {
                    architectureEdgesStartColor = value;
                    SetLine(ArchitectureEdgeTypes, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
                }
            }
        }

        private Color architectureEdgesEndColor = Color.blue;
        public Color ArchitectureEdgesEndColor
        {
            get => architectureEdgesEndColor;
            set
            {
                if (architectureEdgesEndColor != value)
                {
                    architectureEdgesEndColor = value;
                    SetLine(ArchitectureEdgeTypes, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
                }
            }
        }

        private float architectureEdgesWidth = architectureEdgeWidthDefault;
        public float ArchitectureEdgesWidth
        {
            get => architectureEdgesWidth;
            set
            {
                if (architectureEdgesWidth != value)
                {
                    architectureEdgesWidth = value;
                    SetLine(ArchitectureEdgeTypes, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
                }
            }
        }
        /// <summary>
        /// Set the visibility of edges to <paramref name="show"/>.
        /// If <paramref name="architecture"/> is true, this operation is executed only
        /// for reflexion edges.
        /// </summary>
        /// <param name="architecture"></param>
        /// <param name="show"></param>
        public void SetEdgeVisiblity(HashSet<string> types, bool show)
        {
            foreach (GameObject go in GetAllEdges())
            {
                if (types == AllEdgeTypes || types.Contains(GetGraphEdge(go).Type))
                {
                    go.SetVisibility(show);
                }
            }
        }

        public void SetLine(HashSet<string> types, Color startColor, Color endColor, float width)
        {
            foreach (GameObject go in GetAllEdges())
            {
                if (types == AllEdgeTypes || types.Contains(GetGraphEdge(go).Type))
                {
                    LineRenderer renderer = go.GetComponent<LineRenderer>();
                    if (renderer != null)
                    {
                        renderer.startColor = startColor;
                        renderer.endColor = endColor;
                        renderer.startWidth = width;
                        renderer.endWidth = width;
                    }
                }
            }
        }

        //------------------------------------
        // Architecture node type properties
        //------------------------------------

        private bool architectureNodesVisible = true;
        public bool ArchitectureNodesVisible
        {
            get => architectureNodesVisible;
            set
            {
                if (architectureNodesVisible != value)
                {
                    architectureNodesVisible = value;
                    SetNodeVisibility(ArchitectureNodes(), architectureNodesVisible);
                }
            }
        }

        private bool implementationNodesVisible = true;
        public bool ImplementationNodesVisible
        {
            get => implementationNodesVisible;
            set
            {
                if (implementationNodesVisible != value)
                {
                    implementationNodesVisible = value;
                    SetNodeVisibility(ImplementationNodes(), implementationNodesVisible);
                }
            }
        }

        private List<GameObject> ArchitectureNodes()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject go in GetAllNodes())
            {
                if (IsArchitectureNode(go))
                {
                    result.Add(go);
                }
            }
            return result;
        }

        private static bool IsArchitectureNode(GameObject go)
        {
            return GetGraphNode(go).Type == "Cluster";
        }

        private static List<GameObject> ImplementationNodes()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject go in GetAllNodes())
            {
                if (!IsArchitectureNode(go))
                {
                    result.Add(go);
                }
            }
            return result;
        }

        private static void SetNodeVisibility(ICollection<GameObject> nodes, bool show)
        {
            foreach (GameObject go in nodes)
            {
                go.SetVisibility(show);
            }
        }

        /// <summary>
        /// Returns given <paramref name="color"/> lightened by 50%.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <returns>given <paramref name="color"/> lightened by 50%</returns>
        private static Color Lighter(Color color)
        {
            return Color.Lerp(color, Color.white, 0.5f); // To lighten by 50 %
        }

        /// <summary>
        /// Returns given <paramref name="color"/> darkened by <paramref name="degree"/>.
        /// 
        /// Precondition: 0 <= <paramref name="degree"/> <= 1
        /// </summary>
        /// <param name="color">base color to be darkened</param>
        /// <param name="degree">degree by which to darker the given <paramref name="color"/></param>
        /// <returns>given <paramref name="color"/> darkened by <paramref name="degree"/></returns>
        private static Color Darker(Color color, float degree = 0.5f)
        {
            return Color.Lerp(color, Color.black, degree);
        }

        // -------------------------------------------
        // Evolution animation
        // -------------------------------------------

        public void Evolution()
        {
            List<GameObject> gameNodes = ImplementationNodes();
            implementationGameNodes = new GameObject[gameNodes.Count];

            // fill implementationGameNodes and make all nodes invisible initially
            int i = 0;
            foreach (GameObject gameNode in gameNodes)
            {
                implementationGameNodes[i] = gameNode;
                gameNode.SetVisibility(false);
                i++;
            }
            // now randomize the nodes
            Randomize(implementationGameNodes, RandomSwaps);

            state = State.runningEvolution;
            revision = 0;
        }

        private static void Randomize(GameObject[] gameObjects, int howManySwaps)
        {
            howManySwaps = Mathf.Clamp(howManySwaps, 0, gameObjects.Length);
            for (int i = 1; i <= howManySwaps; i++)
            {
                int indexA = UnityEngine.Random.Range(0, gameObjects.Length - 1);
                int indexB = UnityEngine.Random.Range(0, gameObjects.Length - 1);
                GameObject swap = gameObjects[indexA];
                gameObjects[indexA] = gameObjects[indexB];
                gameObjects[indexB] = swap;
            }
        }

        public float RevisionDuration = 0.25f;

        public int MinNodesPerRevision = 10;
        public int MaxNodesPerRevision = 100;
        public int RandomSwaps = 1000;

        private float evolutionTimer = 0.0f;

        private int revision = 0;

        private GameObject[] implementationGameNodes;

        private void UpdateEvolution()
        {
            if (revision < implementationGameNodes.Length)
            {
                evolutionTimer -= Time.deltaTime;
                if (evolutionTimer <= 0)
                {
                    evolutionTimer = RevisionDuration;
                    Debug.LogFormat("revision {0}/{1}\n", revision, implementationGameNodes.Length);
                    int howManyRevisions = UnityEngine.Random.Range(MinNodesPerRevision, MaxNodesPerRevision);
                    for (int i = 1; i <= howManyRevisions; i++)
                    {
                        implementationGameNodes[revision].SetVisibility(true);
                        revision++;
                    }
                }
            }
        }

        // -------------------------------------------
        // Dynamic call-graph animation
        // -------------------------------------------

        public float CallDuration = 0.5f;

        private float executionTimer = 0.0f;

        private Dictionary<Node, GameObject> nodeToGameObject;

        /// <summary>
        /// Initial colors of all game objects representing implementation nodes.
        /// </summary>
        private Dictionary<GameObject, Color> initialNodeColors;

        private struct EdgeColor
        {
            public Color startColor;
            public Color endColor;

            public EdgeColor(Color startColor, Color endColor)
            {
                this.startColor = startColor;
                this.endColor = endColor;
            }
        }

        /// <summary>
        /// Initial colors of all game objects representing implementation edges.
        /// </summary>
        private Dictionary<GameObject, EdgeColor> initialEdgeColors;

        private Dictionary<Edge, GameObject> edgeToGameObject;

        private RandomGraphWalker graphWalker;

        private Node callGraphRoot;

        public void CallGraph()
        {
            elapsedExecutionTime = 0.0f;
            UnityEngine.Random.InitState(seed: 42);
            state = State.runningCallGraph;
            executionTimer = 0.0f;
            nodeToGameObject = CollectImplementationNodes();
            initialNodeColors = CollectNodeColors(nodeToGameObject.Values);            
            edgeToGameObject = CollectImplementationEdges();
            initialEdgeColors = CollectEdgeColors(edgeToGameObject.Values);
            callGraphRoot = CodeCity.LoadedGraph.GetNode("R global:SEE:Game:GraphRenderer@C:/Users/raine/develop/SEECity/Temp/bin/Debug/SEE.dll^:!16+67!:Draw(!0+6!UnityEngine:GameObject@C:/Program Files/Unity/Hub/Editor/2019.4.12f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll^)_s->!0+6!System:Void@C:!130+14! (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.1/mscorlib.dll");           
            graphWalker = new RandomGraphWalker(callGraphRoot);
        }

        private Dictionary<GameObject, EdgeColor> CollectEdgeColors(Dictionary<Edge, GameObject>.ValueCollection gameObjects)
        {
            Dictionary<GameObject, EdgeColor> result = new Dictionary<GameObject, EdgeColor>();
            foreach (GameObject go in gameObjects)
            {
                if (go.TryGetComponent<LineRenderer>(out LineRenderer renderer))
                {
                    result[go] = new EdgeColor(renderer.startColor, renderer.endColor);
                }
                else
                {
                    result[go] = new EdgeColor(Color.white, Color.white);
                }
            }
            return result;
        }

        private Dictionary<GameObject, Color> CollectNodeColors(ICollection<GameObject> gameObjects)
        {
            Dictionary<GameObject, Color> result = new Dictionary<GameObject, Color>();
            foreach (GameObject go in gameObjects)
            {
                Color color;
                if (go.TryGetComponent<Renderer>(out Renderer renderer))
                {
                    Material material = renderer.sharedMaterial;
                    color = material.GetColor("_Color");
                }
                else
                {
                    color = Color.white;
                }
                result[go] = color;
            }
            return result;
        }

        private Dictionary<Edge, GameObject> CollectImplementationEdges()
        {
            Dictionary<Edge, GameObject> result = new Dictionary<Edge, GameObject>();
            foreach (GameObject go in GetAllEdges())
            {
                Edge edge = GetGraphEdge(go);
                if (ImplementationEdgeTypes.Contains(edge.Type))
                {
                    result[edge] = go;
                }                
            }
            return result;
        }

        private Dictionary<Node, GameObject> CollectImplementationNodes()
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();

            // mapping of names of game-object representing implementation nodes onto the 
            // respective game object with that name
            Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();
            foreach (GameObject go in ImplementationNodes())
            {
                gameObjects[go.name] = go;
            }

            foreach (Node node in CodeCity.LoadedGraph.Nodes())
            {
                if (node.Type != "Cluster")
                {
                    if (gameObjects.TryGetValue(node.ID, out GameObject target))
                    {
                        result[node] = target;
                    }
                    else
                    {
                        Debug.LogErrorFormat("graph node {0} has no corresponding game object.\n", node.ID);
                    }
                }
            }
            return result;
        }

        private static readonly Color ExecutionColor = Color.cyan;

        private bool isPaused = false;

        private float elapsedExecutionTime = 0.0f;

        private void UpdateCallGraph()
        {
            if (Input.GetKeyDown(KeyCode.Pause))
            {
                isPaused = !isPaused;
                Debug.LogFormat("execution pause set to {0}.\n", isPaused);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                CallDuration *= 0.75f;
                Debug.LogFormat("execution duration set to {0}.\n", CallDuration);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                CallDuration *= 1.25f;
                Debug.LogFormat("execution duration set to {0}.\n", CallDuration);
            }
            if (!isPaused)
            {
                elapsedExecutionTime += Time.deltaTime;
                executionTimer -= Time.deltaTime;
                if (executionTimer <= 0)
                {
                    Hide(graphWalker.CurrentPath());
                    graphWalker.Next();
                    Show(graphWalker.CurrentPath());
                    // callGraphRoot should be highlighted no matter what             
                    nodeToGameObject[callGraphRoot].SetColor(ExecutionColor);
                    executionTimer = CallDuration;                    
                }
            }
            else
            {
                Debug.LogFormat("Elapsed execution time: {0}.\n", elapsedExecutionTime);
            }
        }

        private void Hide(Edge[] edges)
        {
            foreach (Edge edge in edges)
            {
                {
                    GameObject source = nodeToGameObject[edge.Source];
                    source.SetColor(initialNodeColors[source]);
                }
                {
                    GameObject target = nodeToGameObject[edge.Source];
                    target.SetColor(initialNodeColors[target]);
                }
                {
                    GameObject edgeGO = edgeToGameObject[edge];
                    edgeGO.SetVisibility(false);
                    edgeGO.SetLineColor(initialEdgeColors[edgeGO].startColor, initialEdgeColors[edgeGO].endColor);
                }
            }
        }

        private void Show(Edge[] edges)
        {
            foreach (Edge edge in edges)
            {
                {
                    GameObject source = nodeToGameObject[edge.Source];
                    source.SetColor(ExecutionColor);
                }
                {
                    GameObject target = nodeToGameObject[edge.Source];
                    target.SetColor(ExecutionColor);
                }
                {
                    GameObject edgeGO = edgeToGameObject[edge];
                    edgeGO.SetVisibility(true);
                    edgeGO.SetLineColor(ExecutionColor, Darker(ExecutionColor));
                }
            }
        }

        // -------------------------------
        // Random graph walker
        // -------------------------------

        private class RandomGraphWalker
        {
            private readonly Node root;

            private readonly Stack<Edge> currentPath = new Stack<Edge>();

            public RandomGraphWalker(Node root)
            {
                this.root = root;
                if (root.Outgoings.Count == 0)
                {
                    throw new System.Exception("Root should have at least one successor.");
                }
            }

            public Edge[] CurrentPath()
            {
                return currentPath.ToArray();
            }

            public void Next()
            {                
                if (currentPath.Count == 0)
                {
                    // We are at the root.
                    currentPath.Push(AnySuccessor(root));
                }
                else
                {
                    Node callee = currentPath.Peek().Target;

                    int numberOfOutgoings = callee.Outgoings.Count;

                    //if (numberOfOutgoings == 0 || returnChance > Random.Range(0.0f, 1.0f))
                    if (numberOfOutgoings == 0 || UnityEngine.Random.Range(0, numberOfOutgoings) == 0)
                    {
                        // back to caller
                        currentPath.Pop();
                    }
                    else
                    {
                        currentPath.Push(AnySuccessor(callee));
                    }
                }
            }

            private Edge AnySuccessor(Node root)
            {
                int whichSuccessor = UnityEngine.Random.Range(1, root.Outgoings.Count);
                foreach (Edge outgoing in root.Outgoings)
                {
                    if (whichSuccessor > 1)
                    {
                        whichSuccessor--;
                    }
                    else
                    {
                        return outgoing;
                    }
                }
                throw new Exception("We should never arrive here.");
            }
        }
    }
}