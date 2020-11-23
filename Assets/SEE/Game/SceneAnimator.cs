using UnityEngine;
using SEE.DataModel;
using SEE.GO;
using SEE.DataModel.DG;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SEE.Game
{
    [ExecuteAlways]
    public class SceneAnimator : MonoBehaviour
    {        
        public SEECity CodeCity;

        //------------------
        // Edge types 
        //------------------

        public static readonly HashSet<string> ReflexionEdges = new HashSet<string>()
        {
            "Absence",             
            "Convergence",
            "Divergence",
        };

        public static readonly HashSet<string> ArchitectureEdges = new HashSet<string>()
        {
            "Source_Dependency",
        };

        public static readonly HashSet<string> AllEdges = new HashSet<string>();

        private HashSet<string> implementationEdges = new HashSet<string>();

        public HashSet<string> ImplementationEdges
        {
            get
            {
                if (implementationEdges.Count == 0)
                {
                    implementationEdges = ImplementationEdgeTypes();
                    foreach (string type in implementationEdges)
                    {
                        Debug.LogFormat("implementation edge type {0}\n", type);
                    }
                }
                return implementationEdges;
            }
        }

        private static HashSet<string> ImplementationEdgeTypes()
        {
            HashSet<string> result = new HashSet<string>();
            foreach (GameObject go in GetAllEdges())
            {
                string type = GetGraphEdge(go).Type;
                if (!ReflexionEdges.Contains(type) && !ArchitectureEdges.Contains(type))
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
                if (CodeCity.LoadedGraph == null)
                {
                    CodeCity.LoadData();
                }
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
            if (parent.TryGetComponent<NodeRef>(out NodeRef _))
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
                        SetColor(parent, color);
                        childColor = Darker(color, 0.35f);
                    }
                    foreach (Transform child in parent.transform)
                    {
                        ColorNodes(child.gameObject, architectureNode, childColor);
                    }
                }
            }
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                Material material = renderer.sharedMaterial;
                material.SetColor("_Color", color);
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
                if (go.TryGetComponent<EdgeRef>(out EdgeRef edgeRef) && edgeRef.edge != null)
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
                return edgeRef.edge;
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
                if (go.TryGetComponent<NodeRef>(out NodeRef nodeRef) && nodeRef.node != null)
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
                return nodeRef.node;
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
                    SetEdgeVisiblity(ImplementationEdges, implementationEdgesVisible);
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
                    SetLine(ImplementationEdges, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
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
                    SetLine(ImplementationEdges, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
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
                    SetLine(ImplementationEdges, implementationEdgesStartColor, implementationEdgesEndColor, implementationEdgesWidth);
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
                    SetEdgeVisiblity(ReflexionEdges, reflexionEdgesVisible);
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
                    SetEdgeVisiblity(ArchitectureEdges, architectureEdgesVisible);
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
                    SetLine(ArchitectureEdges, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
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
                    SetLine(ArchitectureEdges, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
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
                    SetLine(ArchitectureEdges, architectureEdgesStartColor, architectureEdgesEndColor, architectureEdgesWidth);
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
                if (types == AllEdges || types.Contains(GetGraphEdge(go).Type))
                {
                    SetVisibility(go, show);
                }
            }
        }

        public void SetLine(HashSet<string> types, Color startColor, Color endColor, float width)
        {
            foreach (GameObject go in GetAllEdges())
            {
                if (types == AllEdges || types.Contains(GetGraphEdge(go).Type))
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

        private List<GameObject> ImplementationNodes()
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

        private void SetNodeVisibility(ICollection<GameObject> nodes, bool show)
        {
            foreach (GameObject go in nodes)
            {
                SetVisibility(go, show);
            }
        }

        private static void SetVisibility(GameObject gameObject, bool show)
        {
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.enabled = show;
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
                SetVisibility(gameNode, false);
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
                int indexA = Random.Range(0, gameObjects.Length - 1);
                int indexB = Random.Range(0, gameObjects.Length - 1);
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
                    int howManyRevisions = Random.Range(MinNodesPerRevision, MaxNodesPerRevision);
                    for (int i = 1; i <= howManyRevisions; i++)
                    {
                        SetVisibility(implementationGameNodes[revision], true);
                        revision++;
                    }
                }
            }
        }

        // -------------------------------------------
        // Dynamic call-graph animation
        // -------------------------------------------

        public int NumberOfEdges = 1000;

        public float CallDuration = 2.0f;

        private class ExecutedEdge
        {
            public readonly GameObject source;
            public readonly GameObject target;
            public readonly GameObject edge;
            private readonly Color originalTargetColor;

            public ExecutedEdge(GameObject source,
                                GameObject target,
                                GameObject edge)
            {
                this.source = source;
                this.target = target;
                this.edge = edge;
                if (target.TryGetComponent<Renderer>(out Renderer renderer))
                {
                    Material material = renderer.sharedMaterial;
                    originalTargetColor = material.GetColor("_Color");
                }
                else
                {
                    originalTargetColor = Color.white;
                }
            }

            // For the intial call of Main out of nowhere.
            public ExecutedEdge(GameObject target) : this(null, target, null)
            {
            }

            public void Call()
            {
                if (edge != null)
                {
                    SetVisibility(edge, true);
                }
                Color color = Color.cyan;
                SetColorOfTarget(color);
            }

            private void SetColorOfTarget(Color color)
            {
                if (target.TryGetComponent<Renderer>(out Renderer renderer))
                {
                    Material material = renderer.sharedMaterial;
                    material.SetColor("_Color", color);
                }
            }

            public void Return()
            {
                if (edge != null)
                {
                    SetVisibility(edge, false);
                }
                SetColorOfTarget(originalTargetColor);
            }
        }

        private float executionTimer = 0.0f;

        private Stack<ExecutedEdge> callStack;

        public void CallGraph()
        {
            state = State.runningCallGraph;
            executionTimer = 0.0f;
            callStack = CreateCallStack();
        }

        private Stack<ExecutedEdge> CreateCallStack()
        {
            Stack <ExecutedEdge> callStack = new Stack<ExecutedEdge>();
            GameObject n1 = GameObject.Find("SEEEditor");
            GameObject n2 = GameObject.Find("SEE.Tools");
            GameObject n3 = GameObject.Find("SEE.CameraPaths");
            GameObject n4 = GameObject.Find("SEE.Utils");

            GameObject e1 = GameObject.Find("Absence#SEEEditor#SEE.Tools");
            GameObject e2 = GameObject.Find("Absence#SEEEditor#SEE.CameraPaths");
            GameObject e3 = GameObject.Find("Absence#SEEEditor#SEE.Utils");

            callStack.Push(new ExecutedEdge(n1));
            callStack.Push(new ExecutedEdge(n1, n2, e1));
            callStack.Push(new ExecutedEdge(n1, n3, e2));
            callStack.Push(new ExecutedEdge(n1, n4, e3));

            return callStack;
        }

        private void UpdateCallGraph()
        {
            if (callStack.Count > 0)
            {
                executionTimer -= Time.deltaTime;
                if (executionTimer <= 0)
                {
                    executionTimer = CallDuration;
                    Show(callStack);                    
                    callStack.Pop();
                }
            }
            else if (lastExecutedEdge != null)
            {
                executionTimer -= Time.deltaTime;
                if (executionTimer <= 0)
                {
                    lastExecutedEdge.Return();
                }
            }
        }

        private ExecutedEdge lastExecutedEdge = null;

        private void Show(Stack<ExecutedEdge> callStack)
        {
            if (lastExecutedEdge != null)
            {
                lastExecutedEdge.Return();
            }
            foreach (ExecutedEdge edge in callStack.ToArray())
            {
                edge.Call();
            }
            lastExecutedEdge = callStack.Peek();
        }
    }
}