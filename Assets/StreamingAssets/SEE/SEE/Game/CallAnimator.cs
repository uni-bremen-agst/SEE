using System;
using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// This scene animator is intended to animate calls in a code city for capturing 
    /// videos to showcase SEE. The call chains are generated via random graph
    /// tree walks. 
    /// 
    /// Its only purpose was to create a video. Do not use this class.
    /// </summary>
    //[ExecuteAlways]
    [Obsolete("Introduced only for capturing videos.")]
    public class CallAnimator : MonoBehaviour
    {
        // -------------------------------------------
        // Dynamic call-graph animation
        // -------------------------------------------

        /// <summary>
        /// The code city to be manipulated by this component.
        /// </summary>
        [Tooltip("The code city to be manipulated by this component.")]
        private SEECity CodeCity;

        /// <summary>
        /// The time between animated calls in seconds.
        /// </summary>
        [Tooltip("The time between animated calls in seconds.")]
        public float CallDuration = 1.0f;

        /// <summary>
        /// The name (unique ID) of the node at which to start the execution.
        /// </summary>
        [Tooltip("The name (unique ID) of the node at which to start the execution.")]
        public string RootName = "R global:SEE:Game:GraphRenderer@C:/Users/raine/develop/SEECity/Temp/bin/Debug/SEE.dll^:!16+67!:Draw(!0+6!UnityEngine:GameObject@C:/Program Files/Unity/Hub/Editor/2019.4.12f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll^)_s->!0+6!System:Void@C:!130+14! (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.1/mscorlib.dll";

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

        /// <summary>
        /// Assigns <see cref="CodeCity"/>.
        /// </summary>
        private void Awake()
        {
            if (!gameObject.TryGetComponent(out CodeCity))
            {
                Debug.LogError($"Game object {name} does not have a SEECity component.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Initially all edges are hidden.
        /// </summary>
        private void Start()
        {
            foreach (GameObject edge in GetAllEdges())
            {
                edge.SetVisibility(false);
            }
            CallGraph();
        }

        private void Update()
        {
            if (SEEInput.ToggleAutomaticManualMode())
            {
                isPaused = !isPaused;
                Debug.Log($"execution pause set to {isPaused}.\n");
            }
            if (SEEInput.IncreaseAnimationSpeed())
            {
                CallDuration = Mathf.Max(0.25f, CallDuration / 2.0f);
                Debug.Log($"execution duration set to {CallDuration}.\n");
            }
            if (SEEInput.DecreaseAnimationSpeed())
            {
                CallDuration = Mathf.Max(4.0f, CallDuration * 2.0f);
                Debug.Log($"execution duration set to {CallDuration}.\n");
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
        }

        private void CallGraph()
        {
            elapsedExecutionTime = 0.0f;
            UnityEngine.Random.InitState(seed: 42);
            executionTimer = 0.0f;
            nodeToGameObject = CollectImplementationNodes();
            Debug.Log($"[CallAnimation] {nodeToGameObject.Count} many nodes.\n");
            initialNodeColors = CollectNodeColors(nodeToGameObject.Values);            
            edgeToGameObject = CollectImplementationEdges();
            Debug.Log($"[CallAnimation] {edgeToGameObject.Count} many edges.\n");
            initialEdgeColors = CollectEdgeColors(edgeToGameObject.Values);
            callGraphRoot = CodeCity.LoadedGraph.GetNode(RootName);
            if (callGraphRoot == null)
            {
                Debug.LogError($"Graph has no node with name '{RootName}'\n");
                enabled = false;
            }
            else
            {
                graphWalker = new RandomGraphWalker(callGraphRoot);
            }
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
                result[edge] = go;
            }
            return result;
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

        //-------------------
        // Edges in CodeCity
        //-------------------

        /// <summary>
        /// Returns the root node of <see cref="CodeCity"/>.
        /// </summary>
        /// <returns>root node of <see cref="CodeCity"/></returns>
        private GameObject GetCityRootNode()
        {
            return SceneQueries.GetCityRootNode(gameObject).gameObject;
        }

        /// <summary>
        /// Returns all edges in the subtree rooted by <see cref="CodeCity"/>.
        /// </summary>
        /// <returns>all edges in the subtree rooted by <see cref="CodeCity"/></returns>
        private List<GameObject> GetAllEdges()
        {
            List<GameObject> edges = new List<GameObject>();
            GameObject root = GetCityRootNode();
            AddAllEdges(root, edges);
            return edges;
        }

        /// <summary>
        /// Adds all edges in the subtree rooted by <paramref name="root"/> to <paramref name="edges"/>.
        /// </summary>
        /// <param name="root">root of the subtree to be traversed</param>
        /// <param name="edges">where to add the found edges</param>
        private void AddAllEdges(GameObject root, List<GameObject> edges)
        {
            foreach (Transform child in root.transform)
            {
                GameObject childGO = child.gameObject;
                if (childGO.CompareTag(Tags.Edge))
                {
                    if (childGO.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null)
                    {
                        edges.Add(childGO);
                    }
                    else
                    {
                        Debug.LogErrorFormat("Edge {0} has no valid edge reference.\n", childGO.name);
                    }
                }
                else if (childGO.CompareTag(Tags.Node))
                {
                    AddAllEdges(childGO, edges);
                }
            }
        }

        //------------------
        // Nodes in CodeCity
        //------------------

        /// <summary>
        /// Returns all nodes in the subtree rooted by <see cref="CodeCity"/>.
        /// </summary>
        /// <returns>all nodes in the subtree rooted by <see cref="CodeCity"/></returns>
        private List<GameObject> GetAllNodes()
        {
            GameObject root = GetCityRootNode();
            List<GameObject> nodes = new List<GameObject>() { root };
            AddAllNodes(root, nodes);
            return nodes;
        }

        /// <summary>
        /// Adds all nodes in the subtree rooted by <paramref name="root"/> to <paramref name="nodes"/>.
        /// </summary>
        /// <param name="root">root of the subtree to be traversed</param>
        /// <param name="nodes">where to add the found nodes</param>
        private void AddAllNodes(GameObject root, List<GameObject> nodes)
        {
            foreach (Transform child in root.transform)
            {
                GameObject childGO = child.gameObject;
                if (childGO.CompareTag(Tags.Node))
                {
                    if (childGO.TryGetComponent(out NodeRef nodeRef) && nodeRef.Value != null)
                    {
                        nodes.Add(childGO);
                    }
                    else
                    {
                        Debug.LogError($"Node {childGO.name} has no valid node reference.\n");
                    }
                }
                AddAllNodes(childGO, nodes);
            }
        }

        /// <summary>
        /// Returns a mapping of graph nodes onto the game object representing these graph nodes
        /// for all nodes actually present in the code city.
        /// </summary>
        /// <returns></returns>
        private Dictionary<Node, GameObject> CollectImplementationNodes()
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();
            foreach (GameObject gameObject in GetAllNodes())
            {
                if (gameObject.HasNodeRef())
                {
                    result[gameObject.GetNode()] = gameObject;
                }
            }

            // mapping of names of game nodes onto the respective game object with that name
            //Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();
            //foreach (GameObject go in GetAllNodes())
            //{
            //    gameObjects[go.name] = go;
            //}

            //foreach (Node node in CodeCity.LoadedGraph.Nodes())
            //{
            //    if (gameObjects.TryGetValue(node.ID, out GameObject target))
            //    {
            //        result[node] = target;
            //    }
            //}
            return result;
        }

        private static readonly Color ExecutionColor = Color.cyan;

        private bool isPaused = true;

        private float elapsedExecutionTime = 0.0f;

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
                    if (edgeToGameObject.TryGetValue(edge, out GameObject edgeGO))
                    {
                        edgeGO.SetVisibility(false);
                        edgeGO.SetLineColor(initialEdgeColors[edgeGO].startColor, initialEdgeColors[edgeGO].endColor);
                    }
                    // A missing edge will be reported in Show. So we will not repeat this error message here.
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
                    if (edgeToGameObject.TryGetValue(edge, out GameObject edgeGO))
                    {
                        edgeGO.SetVisibility(true);
                        edgeGO.SetLineColor(ExecutionColor, Darker(ExecutionColor));
                    }
                    else
                    {
                        Debug.LogError($"Edge {edge} does not exist.\n");
                    }
                }
            }
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
                    throw new Exception("Root should have at least one successor.");
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