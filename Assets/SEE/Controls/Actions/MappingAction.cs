using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.GO;
using SEE.Tools;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Implements the architectural mapping for the reflexion analysis. 
    /// This action assumes that it is attached to a game object representing
    /// the reflexion analysis during the game. 
    /// </summary>
    public class MappingAction : CityAction, Observer
    {
        private const float SelectedAlpha = 0.8f;

        private const KeyCode SaveKey = KeyCode.S;
        private const KeyCode CopyKey = KeyCode.C;
        private const KeyCode PasteKey = KeyCode.V;
        private const KeyCode ClearKey = KeyCode.X;

        [Tooltip("The game object representing the architecture.")]
        public GameObject Architecture;

        [Tooltip("The game object representing the implementation.")]
        public GameObject Implementation;

        [Tooltip("The GXL file containing the mapping from implementation onto architecture entities.")]
        public string MappingFile;

        /// <summary>
        /// The graph renderer used to draw the city. There must be a component
        /// SEECity attached to the game object this MappingAction is attached to
        /// from which we derived its graph renderer.
        /// </summary>
        private GraphRenderer architectureGraphRenderer;

        /// <summary>
        /// The graph containing the mapping from implementation onto architecture entities.
        /// </summary>
        private Graph mapping;

        /// <summary>
        /// The graph containing the architecture.
        /// </summary>
        private Graph architecture;

        /// <summary>
        /// The graph containing the impementation.
        /// </summary>
        private Graph implementation;

        /// <summary>
        /// For the reflexion analysis.
        /// </summary>
        public Reflexion Reflexion { get; private set; }

        /// <summary>
        /// Materials for the decoration of reflexion edges.
        /// </summary>
        [Tooltip("Prefab for absencenes")]
        public GameObject AbsencePrefab;
        [Tooltip("Prefab for convergences")]
        public GameObject ConvergencePrefab;
        [Tooltip("Prefab for divergences")]
        public GameObject DivergencePrefab;

        private struct Selection
        {
            internal NodeRef nodeRef;
            internal InteractableObject interactableObject; // TODO(torben): it is time to combine NodeRefs and InteractableObjects or at least have some dictionary for them...
            // Rainer: note that gameObjects with an EdgeRef instead of NodeRef now may also have a InteractableObject component.
        }

        /// <summary>
        /// The current selection of a game object and its associated graph node.
        /// </summary>
        private Selection selection;

        /// <summary>
        /// Which kind of city we are currently focusing on.
        /// </summary>
        private enum HitCity
        {
            None,
            Architecture,
            Implementation
        }

        private struct _ActionState
        {
            internal bool copy;              // copy selected object (i.e., start mapping)
            internal bool paste;             // paste (map) copied object
            internal bool clearClipboard;    // whether the clipboard of copied nodes has been cleared
            internal bool save;              // whether the current mapping should be stored
            internal HitCity hitCity;        // which city we are currently focusing on
        }

        private _ActionState actionState;

        /// <summary>
        /// The game objects that have been copied to the clipboard via Ctrl-C.
        /// </summary>
        private readonly HashSet<Selection> objectsInClipboard = new HashSet<Selection>();

        // Use this for initialization
        private void Start()
        {
            if (Architecture == null)
            {
                Debug.LogWarning("No architecture city was specified for architectural mapping.\n");
                enabled = false;
            }
            else
            {
                architecture = SceneQueries.GetGraph(Architecture);
                if (architecture == null)
                {
                    Debug.LogWarning("The architecture city has no associated graph.\n");
                    enabled = false;
                }
            }

            if (Implementation == null)
            {
                Debug.LogWarning("No implementation city was specified for architectural mapping.\n");
                enabled = false;
            }
            else
            {
                implementation = SceneQueries.GetGraph(Implementation);
                if (implementation == null)
                {
                    Debug.LogWarning("The implementation city has no associated graph.\n");
                    enabled = false;
                }
            }

            if (string.IsNullOrEmpty(MappingFile))
            {
                Debug.LogWarning("A filename for the architectural mapping should be set. Continuing with an empty mapping. Mapping cannot be saved.\n");
                mapping = new Graph();
            }
            else
            {
                mapping = LoadMapping(MappingFile);
                if (mapping == null)
                {
                    Debug.LogErrorFormat("A GXL containing the mapping could not be loaded from {0}. We are using an empty mapping instead.\n",
                                         MappingFile);
                    mapping = new Graph();
                }
                else
                {
                    Debug.LogFormat("Mapping successfully loaded from {0}\n", MappingFile);
                }
            }

            if (AbsencePrefab == null)
            {
                Debug.LogErrorFormat("No material assigned for absences.\n");
                enabled = false;
            }
            if (ConvergencePrefab == null)
            {
                Debug.LogErrorFormat("No material assigned for convergences.\n");
                enabled = false;
            }
            if (DivergencePrefab == null)
            {
                Debug.LogErrorFormat("No material assigned for divergences.\n");
                enabled = false;
            }
            if (Architecture.TryGetComponent<SEECity>(out SEECity city))
            {
                architectureGraphRenderer = city.Renderer;
                if (architectureGraphRenderer == null)
                {
                    Debug.LogErrorFormat("The SEECity component attached to the object representing the architecture has no graph renderer.\n");
                    enabled = false;
                }
            }
            else
            {
                Debug.LogErrorFormat("The object representing the architecture has no SEECity component attached to it.\n");
                enabled = false;
            }
            if (enabled)
            {
                Usage();
                SetupReflexionDecorator();
                SetupGameObjectMappings();
                SetupReflexion();
            }

            ActionState.OnStateChanged += OnStateChanged;
            if (ActionState.Value == ActionState.Type.Map)
            {
                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
            }
            else
            {
                enabled = false;
            }
        }

        private void OnStateChanged(ActionState.Type value)
        {
            if (value == ActionState.Type.Map)
            {
                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
                enabled = true;
            }
            else
            {
                InteractableObject.AnySelectIn -= AnySelectIn;
                InteractableObject.AnySelectOut -= AnySelectOut;
                enabled = false; // We don't want to waste CPU time, if Update() doesn't do anything
            }
        }

        /// <summary>
        /// Used for the visualization and decoration of reflexion edges.
        /// </summary>
        private ReflexionDecorator decorator;

        /// <summary>
        /// Sets up the reflexion decorator.
        /// </summary>
        private void SetupReflexionDecorator()
        {
            Portal.GetDimensions(Architecture, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            decorator = new ReflexionDecorator(AbsencePrefab, ConvergencePrefab, DivergencePrefab,
                                               leftFrontCorner, rightBackCorner);
        }

        /// <summary>
        /// Mapping of edge IDs onto game objects representing these edges in the architecture code city.
        /// </summary>
        private readonly Dictionary<string, GameObject> architectureEdges = new Dictionary<string, GameObject>();

        /// <summary>
        /// Mapping of node IDs onto game objects representing these nodes in the architecture code city.
        /// </summary>
        private readonly Dictionary<string, GameObject> architectureNodes = new Dictionary<string, GameObject>();

        private void SetupGameObjectMappings()
        {
            GatherNodesAndEdges(Architecture, architectureNodes, architectureEdges);
        }

        /// <summary>
        /// Adds all game nodes and edges that are reachable by <paramref name="gameObject"/> to
        /// <paramref name="nodes"/> or <paramref name="edges"/>, respectively, or by any of its
        /// descendants. Game objects representing either graph nodes or edges are recognized
        /// by either Tags.Node or Tags.Edge, respectively.
        /// </summary>
        /// <param name="gameObject">root object of the object hierarchy</param>
        /// <param name="nodes">where game objects representing graph nodes are to be added</param>
        /// <param name="edges">where game objects representing graph edges are to be added</param>
        private void GatherNodesAndEdges(GameObject gameObject, Dictionary<string, GameObject> nodes, Dictionary<string, GameObject> edges)
        {
            if (gameObject.tag == Tags.Edge)
            {
                if (gameObject.TryGetComponent<EdgeRef>(out EdgeRef edgeRef))
                {
                    Edge edge = edgeRef.edge;
                    if (edge != null)
                    {
                        edges[edge.ID] = gameObject;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Game-object edge {0} without an invalid graph edge reference.\n", gameObject.name);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Game-object edge {0} without graph edge reference.\n", gameObject.name);
                }
            }
            else if (gameObject.tag == Tags.Node)
            {
                if (gameObject.TryGetComponent<NodeRef>(out NodeRef nodeRef))
                {
                    Node node = nodeRef.Value;
                    if (node != null)
                    {
                        nodes[node.ID] = gameObject;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Game-object node {0} without an invalid graph node reference.\n", gameObject.name);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Game-object node {0} without graph node reference.\n", gameObject.name);
                }
            }
            foreach (Transform child in gameObject.transform)
            {
                GatherNodesAndEdges(child.gameObject, nodes, edges);
            }
        }

        /// <summary>
        /// Prints the keys for all actions.
        /// </summary>
        private static void Usage()
        {
            Debug.Log("Keys for architecutural mapping:\n");
            Debug.LogFormat(" copy/remove selected implementation node to/from clipboard: Ctrl-{0}\n", CopyKey);
            Debug.LogFormat(" map nodes in clipboard onto selected architecture node: Ctrl-{0}\n", PasteKey);
            Debug.LogFormat(" clear clipboard: Ctrl-{0}\n", ClearKey);
            Debug.LogFormat(" save mapping to GXL file: Ctrl-{0}\n", SaveKey);
        }

        /// <summary>
        /// Loads and returns the mapping from the given GXL <paramref name="mappingFile"/>.
        /// </summary>
        /// <param name="mappingFile"></param>
        /// <returns>the loaded graph or null</returns>
        private Graph LoadMapping(string mappingFile)
        {
            // Note: There are no hierarchical edges in a mapping graph.
            GraphReader graphReader = new GraphReader(mappingFile, new HashSet<string>());
            graphReader.Load();
            Graph graph = graphReader.GetGraph();

            HashSet<Edge> edgesToBeRemoved = new HashSet<Edge>();
            HashSet<Node> nodesToBeRemoved = new HashSet<Node>();

            foreach (Edge edge in graph.Edges())
            {
                if (edge.Type != "Maps_To")
                {
                    Debug.LogWarningFormat("Unexpected edge type {0} in mapping for edge {1}.\n", edge.Type, edge.ToString());
                    edgesToBeRemoved.Add(edge);
                }
                if (implementation.GetNode(edge.Source.ID) == null)
                {
                    Debug.LogWarningFormat("The mapping contains an implementation node that is not in the implementation graph.\n", edge.Source.ID);
                    nodesToBeRemoved.Add(edge.Source);
                }
                if (architecture.GetNode(edge.Target.ID) == null)
                {
                    Debug.LogWarningFormat("The mapping contains an architecture node that is not in the architecture graph.\n", edge.Source.ID);
                    nodesToBeRemoved.Add(edge.Target);
                }
            }
            foreach (Edge edge in edgesToBeRemoved)
            {
                graph.RemoveEdge(edge);
            }
            foreach (Node node in nodesToBeRemoved)
            {
                graph.RemoveNode(node);
            }
            return graph;
        }

        /// <summary>
        /// Saves the given <paramref name="mapping"/> in the file <paramref name="mappingFile"/> in GXL.
        /// </summary>
        /// <param name="mapping">the mapping to be saved</param>
        /// <param name="mappingFile">the GXL filename where to store the <paramref name="mapping"/></param>
        private static void SaveMapping(Graph mapping, string mappingFile)
        {
            if (!string.IsNullOrEmpty(mappingFile))
            {
                GraphWriter.Save(mappingFile, mapping, "Belongs_To");
                Debug.LogFormat("Mapping successfully saved in GXL file {0}\n", mappingFile);
            }
        }

        private struct SpinningCube
        {
            internal GameObject gameObject;
            internal MeshRenderer meshRenderer;
            internal float timer;
            internal Color c0;
            internal Color c1;
        }

        SpinningCube spinningCube;

        // Update is called once per frame
        private void Update()
        {
            // This script should be disabled, if the action state is not 'Map'
            Assert.IsTrue(ActionState.Value == ActionState.Type.Map);

            //------------------------------------------------------------------------
            // ARCHITECTURAL MAPPING
            //------------------------------------------------------------------------

            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                if (Raycasting.RaycastNodes(out RaycastHit hit, out NodeRef nodeRef)) // Select, replace or map
                {
                    Assert.IsNotNull(nodeRef);
                    Assert.IsNotNull(nodeRef.Value);

                    if (nodeRef.Value.ItsGraph == implementation) // Set or replace implementation node
                    {
                        if (selection.interactableObject != null)
                        {
                            selection.interactableObject.SetSelect(false, true);
                        }
                        nodeRef.GetComponent<InteractableObject>().SetSelect(true, true);
                    }
                    else if (selection.nodeRef != null) // Create mapping
                    {
                        Node n0 = selection.nodeRef.Value;
                        Node n1 = nodeRef.Value;
                        if (Reflexion.Is_Explicitly_Mapped(n0))
                        {
                            Node mapped = Reflexion.Get_Mapping().GetNode(n0.ID);
                            Assert.IsTrue(mapped.Outgoings.Count == 1);
                            Reflexion.Delete_From_Mapping(mapped.Outgoings[0]);
                        }
                        Reflexion.Add_To_Mapping(n0, n1);
                        selection.interactableObject.SetSelect(false, true);
                    }
                }
                else // Deselect
                {
                    selection.interactableObject?.SetSelect(false, true);
                }
            }

            if (spinningCube.gameObject != null)
            {
                const float PERIOD = 4.0f;
                spinningCube.timer += Time.deltaTime;
                while (spinningCube.timer > PERIOD)
                {
                    spinningCube.timer -= PERIOD;
                }
                float tPos = Mathf.Sin(2.0f * Mathf.PI * spinningCube.timer / PERIOD * 2.0f) * 0.5f + 0.5f; // y-range: [0.0, 1.0]
                float gr = 0.5f * MathExtensions.GoldenRatio;
                float ls = spinningCube.gameObject.transform.localScale.x;

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Physics.Raycast(ray, out RaycastHit hit);

                spinningCube.gameObject.transform.position = hit.point + new Vector3(0.0f, gr * ls + tPos * gr * ls, 0.0f);
                spinningCube.gameObject.transform.rotation = Quaternion.AngleAxis(spinningCube.timer / PERIOD * 180.0f, Vector3.up);

                float tCol = Mathf.Sin(2.0f * Mathf.PI * spinningCube.timer / PERIOD) * 0.5f + 0.5f;
                spinningCube.meshRenderer.material.color = (1.0f - tCol) * spinningCube.c0 + tCol * spinningCube.c1;
            }

#if false
            bool leftControl = LeftControlPressed();

            actionState.save = leftControl && Input.GetKeyDown(SaveKey);
            actionState.copy = leftControl && Input.GetKeyDown(CopyKey);
            actionState.paste = leftControl && Input.GetKeyDown(PasteKey);
            actionState.clearClipboard = leftControl && Input.GetKeyDown(ClearKey);

            // We can copy only from the implementation city and if there is a selected object.
            if (actionState.copy && actionState.hitCity == HitCity.Implementation && selection.go != null)
            {
                if (objectsInClipboard.Contains(selection))
                {
                    Debug.LogFormat("Removing node {0} from clipboard\n", selection.go.name);
                    objectsInClipboard.Remove(selection);
                }
                else
                {
                    Debug.LogFormat("Copying node {0} to clipboard\n", selection.go.name);
                    objectsInClipboard.Add(selection);
                }
            }
            if (actionState.clearClipboard)
            {
                Debug.Log("Node clipboard has been cleared.\n");
                objectsInClipboard.Clear();
            }
            // We can paste only into the architecture city and if we have a selected object as a target
            if (actionState.paste && actionState.hitCity == HitCity.Architecture && selection.go != null)
            {
                MapClipboardContent(selection);
            }
            // Save the mapping if requested.
            if (actionState.save && (actionState.hitCity == HitCity.Implementation || actionState.hitCity == HitCity.Implementation))
            {
                SaveMapping(mapping, MappingFile);
            }
#endif
        }

        /// <summary>
        /// Maps all nodes in <code>objectsInClipboard</code> <onto <paramref name="target"/>.
        /// Assumption: all nodes in objectsInClipboard are implementation nodes.
        /// </summary>
        /// <param name="target">architecture node to be mapped on</param>
        private void MapClipboardContent(Selection target)
        {
            foreach (Selection implementation in objectsInClipboard)
            {
                if (!Reflexion.Is_Explicitly_Mapped(implementation.nodeRef.Value))
                {
                    Debug.LogFormat("Mapping {0} -> {1}.\n", implementation.nodeRef.name, target.nodeRef.name);
                    Reflexion.Add_To_Mapping(from: implementation.nodeRef.Value, to: target.nodeRef.Value);
                }
                else
                {
                    Debug.LogWarningFormat("Node {0} is already explicitly mapped..\n", implementation.nodeRef.name);
                }
            }
            objectsInClipboard.Clear();
        }

        /// <summary>
        /// Whether the left control key was pressed.
        /// </summary>
        /// <returns>true if the left control key was pressed</returns>
        private static bool LeftControlPressed()
        {
            // Control key capturing does not really work well in the editor.
            bool leftControl = false;
#if UNITY_EDITOR
            leftControl = true;
#else
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                leftControl = true;
            }
            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                leftControl = false;
            }
#endif
            return leftControl;
        }

        private void SetupReflexion()
        {
            Reflexion = new Reflexion(implementation, architecture, mapping);
            Reflexion.Register(this);
            // An initial run is necessary to set up the necessary data structures.
            Reflexion.Run();
        }

        private void AnySelectIn(InteractableObject interactableObject, bool isOwner)
        {
            Assert.IsNull(selection.nodeRef);
            Assert.IsNull(selection.interactableObject);

            spinningCube.gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spinningCube.gameObject.name = "MappingAction.spinningCube";
            // Note: This will make raycasting ignore this object. Physics.IgnoreRaycastLayer contains the wrong value (water mask)!
            spinningCube.gameObject.layer = 2;
            float scale = 0.1f * Implementation.GetComponent<GO.Plane>().MinLengthXZ;
            spinningCube.gameObject.transform.localScale = new Vector3(scale, scale, scale);

            spinningCube.meshRenderer = spinningCube.gameObject.GetComponent<MeshRenderer>();
            spinningCube.meshRenderer.material = new Material(interactableObject.GetComponent<MeshRenderer>().material);
            spinningCube.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spinningCube.meshRenderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
            Portal.SetInfinitePortal(spinningCube.gameObject);

            spinningCube.timer = -Time.deltaTime;

            spinningCube.c0 = spinningCube.meshRenderer.material.color;
            spinningCube.c0.a = SelectedAlpha;
            spinningCube.c1 = spinningCube.c0 + new Color(0.2f, 0.2f, 0.2f, 0.0f);

            selection.nodeRef = interactableObject.GetComponent<NodeRef>();
            selection.interactableObject = interactableObject;
            SetAlpha(selection.nodeRef, SelectedAlpha);
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isOwner)
        {
            Assert.IsNotNull(selection.nodeRef);
            Assert.IsNotNull(selection.interactableObject);

            Destroy(spinningCube.gameObject);
#if UNITY_EDITOR
            spinningCube.gameObject = null;
            spinningCube.meshRenderer = null;
            spinningCube.timer = 0.0f;
            spinningCube.c0 = new Color();
            spinningCube.c1 = new Color();
#endif

            SetAlpha(selection.nodeRef, 1.0f);
            selection.interactableObject = null;
            selection.nodeRef = null;
        }

        private void SetAlpha(NodeRef nodeRef, float alpha)
        {
            MeshRenderer meshRenderer = nodeRef.GetComponent<MeshRenderer>();
            Color color = meshRenderer.material.color;
            color.a = alpha;
            meshRenderer.material.color = color;
        }

        /// <summary>
        /// Called by incremental reflexion for every change in the reflexion model
        /// by way of the observer protocol as a callback. Dispatches the event to
        /// the approriate handling function.
        /// </summary>
        /// <param name="changeEvent">additional information about the change in the reflexion model</param>
        public void Update(ChangeEvent changeEvent)
        {
            if (changeEvent is EdgeChange)
            {
                HandleEdgeChange(changeEvent as EdgeChange);
            }
            else if (changeEvent is PropagatedEdgeAdded)
            {
                HandlePropagatedEdgeAdded(changeEvent as PropagatedEdgeAdded);
            }
            else if (changeEvent is PropagatedEdgeRemoved)
            {
                HandlePropagatedEdgeRemoved(changeEvent as PropagatedEdgeRemoved);
            }
            else if (changeEvent is MapsToEdgeAdded)
            {
                HandleMapsToEdgeAdded(changeEvent as MapsToEdgeAdded);
            }
            else if (changeEvent is MapsToEdgeRemoved)
            {
                HandleMapsToEdgeRemoved(changeEvent as MapsToEdgeRemoved);
            }
            else
            {
                Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent.ToString());
            }
        }

        /// <summary>
        /// Handles every state change of an existing edge.
        /// </summary>
        /// <param name="edgeChange"></param>
        private void HandleEdgeChange(EdgeChange edgeChange)
        {
            Debug.LogFormat("edge of type {0} from {1} to {2} changed its state from {3} to {4}.\n",
                            edgeChange.edge.Type, edgeChange.edge.Source.ID, edgeChange.edge.Target.ID,
                            edgeChange.oldState, edgeChange.newState);

            // Possible edge changes:
            //  for specified architecture dependencies
            //    specified          => {absent, allowed_absent, convergent}
            //    absent             => {allowed_absent, convergent}
            //    allowed_absent     => {allowed, convergent}
            //    convergent         => {absent, allowed_absent}
            //  for implementation dependencies propagated to the architecture
            //    undefined          => {allowed, divergent, implicitly_allowed}
            //    allowed            => {divergent, implicitly_allowed}
            //    divergent          => {{allowed, implicitly_allowed}
            //    implicitly_allowed => {allowed, divergent}

            if (architectureEdges.TryGetValue(edgeChange.edge.ID, out GameObject gameEdge))
            {

                if (edgeChange.oldState != edgeChange.newState)
                {
                    switch (edgeChange.oldState)
                    {
                        //--------------------------------------
                        // Changes for architecture dependencies
                        //--------------------------------------
                        case Tools.State.specified:
                            // nothing to be done
                            break;
                        case Tools.State.absent:
                            decorator.UndecorateAbsence(gameEdge);
                            break;
                        case Tools.State.allowed_absent:
                            decorator.UndecorateAllowedAbsence(gameEdge);
                            break;
                        case Tools.State.convergent:
                            decorator.UndecorateConvergence(gameEdge);
                            break;

                        //-----------------------------------------------------------------------
                        // changes for implementation dependencies propagated to the architecture
                        //-----------------------------------------------------------------------
                        case Tools.State.divergent:
                            decorator.UndecorateDivergence(gameEdge);
                            break;
                        case Tools.State.allowed:
                            decorator.UndecorateAllowed(gameEdge);
                            break;
                        case Tools.State.implicitly_allowed:
                            decorator.UndecorateImplicitlyAllowed(gameEdge);
                            break;
                        default:
                            Debug.LogErrorFormat("UNHANDLED PREVIOUS EDGE STATE: {0}\n", edgeChange.oldState);
                            break;
                    }

                    switch (edgeChange.newState)
                    {
                        //--------------------------------------
                        // Changes for architecture dependencies
                        //--------------------------------------
                        case Tools.State.specified:
                            // nothing to be done
                            break;
                        case Tools.State.absent:
                            decorator.DecorateAbsence(gameEdge);
                            break;
                        case Tools.State.allowed_absent:
                            decorator.DecorateAllowedAbsence(gameEdge);
                            break;
                        case Tools.State.convergent:
                            decorator.DecorateConvergence(gameEdge);
                            break;

                        //-----------------------------------------------------------------------
                        // changes for implementation dependencies propagated to the architecture
                        //-----------------------------------------------------------------------
                        case Tools.State.divergent:
                            decorator.DecorateDivergence(gameEdge);
                            break;
                        case Tools.State.allowed:
                            decorator.DecorateAllowed(gameEdge);
                            break;
                        case Tools.State.implicitly_allowed:
                            decorator.DecorateImplicitlyAllowed(gameEdge);
                            break;
                        default:
                            Debug.LogErrorFormat("UNHANDLED NEW EDGE STATE: {0}\n", edgeChange.oldState);
                            break;
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("Edge {0} is unknown.\n", edgeChange.edge.ID);
            }
        }

        private void HandlePropagatedEdgeRemoved(PropagatedEdgeRemoved propagatedEdgeRemoved)
        {
            Debug.Log(propagatedEdgeRemoved.ToString());
        }

        private void HandlePropagatedEdgeAdded(PropagatedEdgeAdded propagatedEdgeAdded)
        {
            Debug.Log(propagatedEdgeAdded.ToString());

            Edge edge = propagatedEdgeAdded.propagatedEdge;
            GameObject source = architectureNodes[edge.Source.ID];
            GameObject target = architectureNodes[edge.Target.ID];
            List<GameObject> nodes = new List<GameObject> { source, target };
            // FIXME: Continue here.
            //ICollection<GameObject> edges = architectureGraphRenderer.EdgeLayout(nodes);
        }

        private void HandleMapsToEdgeAdded(MapsToEdgeAdded mapsToEdgeAdded)
        {
            Debug.Log(mapsToEdgeAdded.ToString());
        }

        private void HandleMapsToEdgeRemoved(MapsToEdgeRemoved mapsToEdgeRemoved)
        {
            Debug.Log(mapsToEdgeRemoved.ToString());
        }
    }
}