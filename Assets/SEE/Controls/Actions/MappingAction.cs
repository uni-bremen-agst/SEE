using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Tools;
using SEE.Utils;
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
        /// <summary>
        /// Which kind of city we are currently focusing on.
        /// </summary>
        private enum HitCity
        {
            None,
            Architecture,
            Implementation
        }

        private struct _MoveState
        {
            internal struct AdditionalDraggedObject
            {
                internal GameObject obj; // The object to be dragged
                internal Vector3 off;    // The offset to the main dragged transform, that must is to be maintained at all times
            }

            internal Vector3 dragStartTransformPosition;
            internal Vector3 dragStartOffset;
            internal Vector3 dragCanonicalOffset;
            internal Vector3 moveVelocity;

            internal GameObject mainDraggedObj;
            internal List<AdditionalDraggedObject> additionalDraggedObjs;
        }

        private struct _ActionState
        {
            internal bool startDrag;
            internal bool dragHoveredOnly; // true, if only the element, that is hovered by the mouse should be moved instead of whole city
            internal bool drag;
            internal bool cancel;
        }

        private struct MappingEdge
        {
            internal LineRenderer lineRenderer;
            internal uint from;
            internal uint to;
        }

        const float AlphaCoefficient = 4.0f;

        [Tooltip("The game object representing the architecture.")]
        public GameObject Architecture;

        [Tooltip("The game object representing the implementation.")]
        public GameObject Implementation;

        [Tooltip("The GXL file containing the mapping from implementation onto architecture entities.")]
        public string MappingFile;

        [Tooltip("Prefab for absences")]
        public GameObject AbsencePrefab;

        [Tooltip("Prefab for convergences")]
        public GameObject ConvergencePrefab;

        [Tooltip("Prefab for divergences")]
        public GameObject DivergencePrefab;

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
        private Graph archGraph;

        /// <summary>
        /// The graph containing the impementation.
        /// </summary>
        private Graph implGraph;

        /// <summary>
        /// For the reflexion analysis.
        /// </summary>
        public Reflexion Reflexion { get; private set; }

        /// <summary>
        /// Used for the visualization and decoration of reflexion edges.
        /// </summary>
        private ReflexionDecorator decorator;

        private readonly List<MappingEdge> activeMappingEdges = new List<MappingEdge>();
        private _MoveState moveState;
        private _ActionState actionState;
        private bool moving = false;

        /// <summary>
        /// Mapping of edge IDs onto game objects representing these edges in the architecture code city.
        /// </summary>
        private readonly Dictionary<string, GameObject> architectureEdges = new Dictionary<string, GameObject>();

        /// <summary>
        /// Mapping of node IDs onto game objects representing these nodes in the architecture code city.
        /// </summary>
        private readonly Dictionary<string, GameObject> architectureNodes = new Dictionary<string, GameObject>();

        private void Start()
        {
            if (!Assertions.DisableOnCondition(this, Architecture == null, "No architecture city was specified for architectural mapping."))
            {
                archGraph = SceneQueries.GetGraph(Architecture);
                Assertions.DisableOnCondition(this, archGraph == null, "The architecture city has no associated graph.");
            }
            if (!Assertions.DisableOnCondition(this, Implementation == null, "No implementation city was specified for architectural mapping."))
            {
                implGraph = SceneQueries.GetGraph(Implementation);
                Assertions.DisableOnCondition(this, implGraph == null, "The implementation city has no associated graph.");
            }

            if (string.IsNullOrEmpty(MappingFile))
            {
                Debug.LogWarning("A filename for the architectural mapping should be set. Continuing with an empty mapping. Mapping cannot be saved.");
                mapping = new Graph();
            }
            else
            {
                mapping = LoadMapping(MappingFile);
                if (mapping == null)
                {
                    Debug.LogErrorFormat("A GXL containing the mapping could not be loaded from {0}. We are using an empty mapping instead.", MappingFile);
                    mapping = new Graph();
                }
                else
                {
                    Debug.LogFormat("Mapping successfully loaded from {0}\n", MappingFile);
                }
            }

            Assertions.DisableOnCondition(this, AbsencePrefab == null, "No material assigned for absences.");
            Assertions.DisableOnCondition(this, ConvergencePrefab == null, "No material assigned for convergences.");
            Assertions.DisableOnCondition(this, DivergencePrefab == null, "No material assigned for divergences.");

            if (!Assertions.DisableOnCondition(this, !Architecture.TryGetComponent(out SEECity city), "The object representing the architecture has no SEECity component attached to it."))
            {
                architectureGraphRenderer = city.Renderer;
                Assertions.DisableOnCondition(this, architectureGraphRenderer == null, "The SEECity component attached to the object representing the architecture has no graph renderer.");
            }

            if (enabled)
            {
                // Setup reflexion decorator
                Portal.GetDimensions(Architecture, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
                decorator = new ReflexionDecorator(AbsencePrefab, ConvergencePrefab, DivergencePrefab, leftFrontCorner, rightBackCorner);

                // Setup game object mappings
                GatherNodesAndEdges(Architecture, architectureNodes, architectureEdges);

                // Setup reflexion
                Reflexion = new Reflexion(implGraph, archGraph, mapping);
                Reflexion.Register(this);
                // An initial run is necessary to set up the necessary data structures.
                Reflexion.Run();
            }

            ActionState.OnStateChanged += OnStateChanged;
            if (!Assertions.DisableOnCondition(this, !Equals(ActionState.Value, ActionStateType.Map)))
            {
                InteractableObject.AnyHoverIn += AnyHoverIn;
                InteractableObject.AnyHoverOut += AnyHoverOut;
                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
            }

            moveState.additionalDraggedObjs = new List<_MoveState.AdditionalDraggedObject>();
        }

        private void Update()
        {
            Assert.IsTrue(ActionState.Is(ActionStateType.Map));

            bool isMouseOverGUI = Raycasting.IsMouseOverGUI();

            actionState.drag = Input.GetMouseButton(2);
            actionState.startDrag |= !isMouseOverGUI && Input.GetMouseButtonDown(2);
            actionState.dragHoveredOnly = Input.GetKey(KeyCode.LeftControl);
            actionState.cancel |= Input.GetKeyDown(KeyCode.Escape);

#if false
            //------------------------------------------------------------------------
            // ARCHITECTURAL MAPPING
            //------------------------------------------------------------------------

            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                if (Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef elementRef) == HitGraphElement.Node) 
                {
                    // Select, replace or map
                    NodeRef nodeRef = elementRef as NodeRef;

                    Assert.IsNotNull(nodeRef);
                    Assert.IsNotNull(nodeRef.Value);

                    if (nodeRef.Value.ItsGraph == implGraph) // Set or replace implementation node
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

            if (selection.material)
            {
                float t = -Mathf.Cos((2.0f * Mathf.PI * highlightTimer) / HighlightLoopTimeInSeconds) * 0.5f + 0.5f;
                selection.material.color = Color.Lerp(selection.initialColor, selection.highlightColor, t);
                materialCopy.color = selection.material.color;
            }

            if (spinningCubeData.go != null)
            {
                spinningCubeData.timer += Time.deltaTime;
                while (spinningCubeData.timer > SpinningCursorCubeLoopTimeInSeconds)
                {
                    spinningCubeData.timer -= SpinningCursorCubeLoopTimeInSeconds;
                }
                float tPos = Mathf.Sin(2.0f * Mathf.PI * spinningCubeData.timer / SpinningCursorCubeLoopTimeInSeconds * 2.0f) * 0.5f + 0.5f; // y-range: [0.0, 1.0]
                float gr = 0.5f * MathExtensions.GoldenRatio;
                float ls = spinningCubeData.go.transform.localScale.x;

                //TODO: Camera.main should be cached for Update(),
                // as it has CPU overhead comparable to GameObject.GetComponent
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Physics.Raycast(ray, out RaycastHit hit);

                spinningCubeData.go.transform.position = hit.point + new Vector3(0.0f, gr * ls + tPos * gr * ls, 0.0f);
                spinningCubeData.go.transform.rotation = Quaternion.AngleAxis(spinningCubeData.timer / SpinningCursorCubeLoopTimeInSeconds * 180.0f, Vector3.up);

                float tCol = Mathf.Sin(2.0f * Mathf.PI * spinningCubeData.timer / SpinningCursorCubeLoopTimeInSeconds) * 0.5f + 0.5f;
            }
#endif
        }

        private void FixedUpdate()
        {
            bool synchronize = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit raycastHit);
            Vector3 hit = raycastHit.point;

            if (actionState.cancel) // cancel movement
            {
                if (moving)
                {
                    moving = false;
                
                    //moveState.draggedTransform.GetComponent<InteractableObject>().SetGrab(false, true);
                
                    moveState.moveVelocity = Vector3.zero;
                    Vector3 p = moveState.dragStartTransformPosition + moveState.dragStartOffset
                        - Vector3.Scale(moveState.dragCanonicalOffset, moveState.mainDraggedObj.transform.localScale);
                    moveState.mainDraggedObj.transform.position = p;
                    foreach (_MoveState.AdditionalDraggedObject o in moveState.additionalDraggedObjs)
                    {
                        o.obj.transform.position = p + o.off;
                    }
                    moveState.mainDraggedObj = null;
                    moveState.additionalDraggedObjs.Clear();
                    synchronize = true;
                }
                else
                {
                    InteractableObject o = InteractableObject.HoveredObject;
                    if (o)
                    {
                        InteractableObject.UnselectAllInGraph(o.ItsGraph(), true);
                    }
                }
            }
            else if (actionState.drag) // start or continue movement
            {
                if (actionState.startDrag) // start movement
                {
                    if (actionState.dragHoveredOnly)
                    {
                        InteractableObject o = InteractableObject.HoveredObject;
                        if (IsValidSource(o))
                        {
                            moving = true;
                            moveState.mainDraggedObj = CreateGhost(o);
                            //o.SetGrab(true, true);
                        }
                    }
                    else
                    {
                        HashSet<InteractableObject> objs = InteractableObject.GetSelectedObjectsOfGraph(implGraph);
                        if (objs.Count > 0)
                        {
                            moving = true;
                            HashSet<InteractableObject>.Enumerator e = objs.GetEnumerator();
                            e.MoveNext();
                            moveState.mainDraggedObj = CreateGhost(e.Current);
                            //e.Current.SetGrab(true, true);
                            while (e.MoveNext())
                            {
                                _MoveState.AdditionalDraggedObject t = new _MoveState.AdditionalDraggedObject()
                                {
                                    obj = CreateGhost(e.Current),
                                    off = e.Current.transform.position - moveState.mainDraggedObj.transform.position
                                };
                                moveState.additionalDraggedObjs.Add(t);
                                //e.Current.SetGrab(true, true);
                            }
                        }
                    }

                    if (moving)
                    {
                        moveState.dragStartTransformPosition = moveState.mainDraggedObj.transform.position;
                        moveState.dragStartOffset = hit - moveState.mainDraggedObj.transform.position;
                        moveState.dragCanonicalOffset = moveState.dragStartOffset.DividePairwise(moveState.mainDraggedObj.transform.localScale);
                        moveState.moveVelocity = Vector3.zero;
                    }
                }

                if (moving) // continue movement
                {
                    Vector3 totalDragOffsetFromStart = hit - (moveState.dragStartTransformPosition + moveState.dragStartOffset);

                    Vector3 oldPosition = moveState.mainDraggedObj.transform.position;
                    Vector3 newPosition = moveState.dragStartTransformPosition + totalDragOffsetFromStart;

                    moveState.moveVelocity = (newPosition - oldPosition) / Time.fixedDeltaTime; // TODO(torben): it might be possible to determine velocity only on release (this todo comes from DesktopNavigationAction)
                    moveState.mainDraggedObj.transform.position = newPosition;
                    foreach (_MoveState.AdditionalDraggedObject o in moveState.additionalDraggedObjs)
                    {
                        o.obj.transform.position = newPosition + o.off;
                    }
                    synchronize = true;
                }
            }
            else if (moving) // finalize movement
            {
                //if (moveState.mainDraggedObj.transform != CityTransform) // only reparent non-root nodes
                //{
                //    Transform movingObject = moveState.mainDraggedObj.transform;
                //    Vector3 originalPosition = moveState.dragStartTransformPosition + moveState.dragStartOffset
                //            - Vector3.Scale(moveState.dragCanonicalOffset, movingObject.localScale);
                //
                //    GameNodeMover.FinalizePosition(movingObject.gameObject, originalPosition);
                //    synchronize = true;
                //}

                moving = false;

                //moveState.mainDraggedObj.transform.GetComponent<InteractableObject>().SetGrab(false, true);
                //if (moveState.mainDraggedObj.transform != CityTransform)
                //{
                //    moveState.moveVelocity = Vector3.zero; // TODO(torben): do we want to apply velocity to individually moved buildings or keep it like this?
                //}

                Destroy(moveState.mainDraggedObj);
                foreach (_MoveState.AdditionalDraggedObject o in moveState.additionalDraggedObjs)
                {
                    Destroy(o.obj);
                }

                moveState.mainDraggedObj = null;
                moveState.additionalDraggedObjs.Clear();
                
                if (IsValidTarget(InteractableObject.HoveredObject))
                {
                    Node to = InteractableObject.HoveredObject.GetNode();
                    foreach (InteractableObject o in InteractableObject.GetSelectedObjectsOfGraph(implGraph))
                    {
                        if (IsValidSource(o))
                        {
                            Node from = o.GetNode();
                            if (Reflexion.Is_Explicitly_Mapped(from))
                            {
                                Node mapped = Reflexion.Get_Mapping().GetNode(from.ID);
                                Assert.IsTrue(mapped.Outgoings.Count == 1);
                                Reflexion.Delete_From_Mapping(mapped.Outgoings[0]);
                            }
                            Reflexion.Add_To_Mapping(from, to);
                        }
                    }
                }
            }

            #region ResetActionState

            actionState.startDrag = false;
            actionState.cancel = false;

            #endregion

            #region Synchronize

            if (synchronize)
            {
                // TODO(torben): synchronize new edges. also sync edge color animations
            }

            #endregion
        }

        /// <summary>
        /// Adds all game nodes and edges that are reachable by <paramref name="rootGameObject"/> to
        /// <paramref name="nodes"/> or <paramref name="edges"/>, respectively, or by any of its
        /// descendants. Game objects representing either graph nodes or edges are recognized
        /// by either Tags.Node or Tags.Edge, respectively.
        /// </summary>
        /// <param name="rootGameObject">root object of the object hierarchy</param>
        /// <param name="nodes">where game objects representing graph nodes are to be added</param>
        /// <param name="edges">where game objects representing graph edges are to be added</param>
        private static void GatherNodesAndEdges(GameObject rootGameObject, IDictionary<string, GameObject> nodes, IDictionary<string, GameObject> edges)
        {
            switch (rootGameObject.tag)
            {
                case Tags.Edge when rootGameObject.TryGetComponent(out EdgeRef edgeRef):
                {
                    Edge edge = edgeRef.Value;
                    if (edge != null)
                    {
                        edges[edge.ID] = rootGameObject;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Game-object edge {0} without an invalid graph edge reference.\n", rootGameObject.name);
                    }

                    break;
                }
                case Tags.Edge:
                {
                    Debug.LogErrorFormat("Game-object edge {0} without graph edge reference.\n", rootGameObject.name);
                    break;
                }
                case Tags.Node when rootGameObject.TryGetComponent(out NodeRef nodeRef):
                {
                    Node node = nodeRef.Value;
                    if (node != null)
                    {
                        nodes[node.ID] = rootGameObject;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Game-object node {0} without an invalid graph node reference.\n", rootGameObject.name);
                    }

                    break;
                }
                case Tags.Node: Debug.LogErrorFormat("Game-object node {0} without graph node reference.\n", rootGameObject.name);
                    break;
            }

            foreach (Transform child in rootGameObject.transform)
            {
                GatherNodesAndEdges(child.gameObject, nodes, edges);
            }
        }

        /// <summary>
        /// Loads and returns the mapping from the given GXL <paramref name="mappingFile"/>.
        /// </summary>
        /// <param name="mappingFile">GXL file from which to load the mapping</param>
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
                    Debug.LogWarningFormat("Unexpected edge type {0} in mapping for edge {1}.\n", edge.Type, edge);
                    edgesToBeRemoved.Add(edge);
                }
                if (implGraph.GetNode(edge.Source.ID) == null)
                {
                    Debug.LogWarning($"The mapping contains an implementation node {edge.Source.ID} (source) that is not in the implementation graph for maps-to edge {edge}.\n");
                    nodesToBeRemoved.Add(edge.Source);
                }
                if (archGraph.GetNode(edge.Target.ID) == null)
                {
                    Debug.LogWarning($"The mapping contains an architecture node {edge.Target.ID} (target) that is not in the architecture graph for maps-to edge {edge}.\n");
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

        private LineRenderer CreateEdge(Node from, Node to)
        {
            LineRenderer result = null;

            // TODO(torben): This is way to inefficient to create an edge!!!
            NodeRef nr0 = NodeRef.Get(from);
            NodeRef nr1 = NodeRef.Get(to);
            AbstractSEECity settings = Implementation.GetComponent<SEECity>().Renderer.GetSettings();
            float minimalEdgeLevelDistance = 2.5f * settings.EdgeWidth;

            IEdgeLayout edgeLayout = new SplineEdgeLayout(settings.EdgesAboveBlocks, minimalEdgeLevelDistance, settings.RDP);
            NodeFactory nodeFactory = new CubeFactory(Materials.ShaderType.Opaque, new ColorRange(Color.white, Color.white, 1));
            EdgeFactory factory = new EdgeFactory(edgeLayout, settings.EdgeWidth, settings.TubularSegments, settings.Radius, settings.RadialSegments, settings.isEdgeSelectable);

            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            ILayoutNode fromLayoutNode = new GameNode(to_layout_node, nr0.gameObject, nodeFactory);
            ILayoutNode toLayoutNode = new GameNode(to_layout_node, nr1.gameObject, nodeFactory);
            LayoutEdge layoutEdge = new LayoutEdge(fromLayoutNode, toLayoutNode, new Edge(from, to));

            ICollection<ILayoutNode> nodes = new List<ILayoutNode> { fromLayoutNode, toLayoutNode };
            ICollection<LayoutEdge> edges = new List<LayoutEdge> { layoutEdge };

            IEnumerator<GameObject> enumerator = factory.DrawEdges(nodes, edges).GetEnumerator();
            enumerator.MoveNext();
            result = enumerator.Current.GetComponent<LineRenderer>();
            LineFactory.SetColor(result, new Color(1.0f, 1.0f, 1.0f, 0.2f));

            return result;
        }

        private GameObject CreateGhost(InteractableObject o)
        {
            Material m = o.GetComponent<MeshRenderer>().material;
            Color c = m.color;
            CubeFactory f = new CubeFactory(Materials.ShaderType.Transparent, new ColorRange(new Color(c.r, c.g, c.b, c.a / AlphaCoefficient)));
            GameObject result = f.NewBlock(0, 0);
            result.GetComponent<MeshRenderer>().material.renderQueue = m.renderQueue;
            result.transform.position = o.transform.position;
            result.transform.localScale = o.transform.lossyScale;
            result.layer = 2; // Note: This will make raycasting ignore this object. Physics.IgnoreRaycastLayer contains the wrong value (water mask)!
            return result;
        }

        private bool IsValidSource(InteractableObject o)
        {
            return o != null && o.GraphElemRef is NodeRef && o.GraphElemRef.elem.ItsGraph.Equals(implGraph);
        }

        private bool IsValidTarget(InteractableObject o)
        {
            return o != null && o.GraphElemRef is NodeRef && o.GraphElemRef.elem.ItsGraph.Equals(archGraph);
        }

        //----------------------------------------------------------------
        // Events
        //----------------------------------------------------------------

        private void OnStateChanged(ActionStateType value)
        {
            if (Equals(value, ActionStateType.Map))
            {
                InteractableObject.AnyHoverIn += AnyHoverIn;
                InteractableObject.AnyHoverOut += AnyHoverOut;
                InteractableObject.AnySelectIn += AnySelectIn;
                InteractableObject.AnySelectOut += AnySelectOut;
                enabled = true;
            }
            else
            {
                foreach (MappingEdge e in activeMappingEdges)
                {
                    Destroy(e.lineRenderer.gameObject);
                }
                activeMappingEdges.Clear();

                InteractableObject.AnyHoverIn -= AnyHoverIn;
                InteractableObject.AnyHoverOut -= AnyHoverOut;
                InteractableObject.AnySelectIn -= AnySelectIn;
                InteractableObject.AnySelectOut -= AnySelectOut;
                enabled = false;
            }
        }

        private void AnyHoverIn(InteractableObject interactableObject, bool isOwner)
        {
            Assert.IsTrue(activeMappingEdges.Count == 0);

            if (IsValidTarget(interactableObject))
            {
                Node to = interactableObject.GetNode();
                foreach (InteractableObject o in InteractableObject.GetSelectedObjectsOfGraph(implGraph))
                {
                    if (IsValidSource(o))
                    {
                        Node from = o.GetNode();
                        MappingEdge e = new MappingEdge()
                        {
                            lineRenderer = CreateEdge(from, to),
                            from = o.ID,
                            to = interactableObject.ID
                        };
                        activeMappingEdges.Add(e);
                    }
                }
            }
        }

        private void AnyHoverOut(InteractableObject interactableObject, bool isOwner)
        {
            if (IsValidTarget(interactableObject))
            {
                for (int i = activeMappingEdges.Count - 1; i >= 0; i--)
                {
                    MappingEdge e = activeMappingEdges[i];
                    if (e.to == interactableObject.ID)
                    {
                        activeMappingEdges.RemoveAt(i);
                        Destroy(e.lineRenderer.gameObject);
                    }
                }
            }
        }

        private void AnySelectIn(InteractableObject interactableObject, bool isOwner)
        {
            if (IsValidSource(interactableObject) && IsValidTarget(InteractableObject.HoveredObject))
            {
                Node from = interactableObject.GetNode();
                Node to = InteractableObject.HoveredObject.GetNode();
                MappingEdge e = new MappingEdge()
                {
                    lineRenderer = CreateEdge(from, to),
                    from = interactableObject.ID,
                    to = InteractableObject.HoveredObject.ID
                };
                activeMappingEdges.Add(e);
            }
        }

        private void AnySelectOut(InteractableObject interactableObject, bool isOwner)
        {
            if (IsValidSource(interactableObject))
            {
                for (int i = activeMappingEdges.Count - 1; i >= 0; i--)
                {
                    MappingEdge e = activeMappingEdges[i];
                    if (e.from == interactableObject.ID)
                    {
                        activeMappingEdges.RemoveAt(i);
                        Destroy(e.lineRenderer.gameObject);
                        break; // Note: There can only ever be ONE edge coming from a given selected object.
                    }
                }
            }
        }

        /// <summary>
        /// Called by incremental reflexion for every change in the reflexion model
        /// by way of the observer protocol as a callback. Dispatches the event to
        /// the appropriate handling function.
        /// </summary>
        /// <param name="changeEvent">additional information about the change in the reflexion model</param>
        public void Update(ChangeEvent changeEvent)
        {
            switch (changeEvent)
            {
                case EdgeChange changedEvent: HandleEdgeChange(changedEvent);
                    break;
                case PropagatedEdgeAdded changedEvent: HandlePropagatedEdgeAdded(changedEvent);
                    break;
                case PropagatedEdgeRemoved changedEvent: HandlePropagatedEdgeRemoved(changedEvent);
                    break;
                case MapsToEdgeAdded changedEvent: HandleMapsToEdgeAdded(changedEvent);
                    break;
                case MapsToEdgeRemoved changedEvent: HandleMapsToEdgeRemoved(changedEvent);
                    break;
                default: Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent);
                    break;
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
                        case State.specified:
                            // nothing to be done
                            break;
                        case State.absent:
                            decorator.UndecorateAbsence(gameEdge);
                            break;
                        case State.allowed_absent:
                            decorator.UndecorateAllowedAbsence(gameEdge);
                            break;
                        case State.convergent:
                            decorator.UndecorateConvergence(gameEdge);
                            break;

                        //-----------------------------------------------------------------------
                        // changes for implementation dependencies propagated to the architecture
                        //-----------------------------------------------------------------------
                        case State.divergent:
                            decorator.UndecorateDivergence(gameEdge);
                            break;
                        case State.allowed:
                            decorator.UndecorateAllowed(gameEdge);
                            break;
                        case State.implicitly_allowed:
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
                        case State.specified:
                            // nothing to be done
                            break;
                        case State.absent:
                            decorator.DecorateAbsence(gameEdge);
                            break;
                        case State.allowed_absent:
                            decorator.DecorateAllowedAbsence(gameEdge);
                            break;
                        case State.convergent:
                            decorator.DecorateConvergence(gameEdge);
                            break;

                        //-----------------------------------------------------------------------
                        // changes for implementation dependencies propagated to the architecture
                        //-----------------------------------------------------------------------
                        case State.divergent:
                            decorator.DecorateDivergence(gameEdge);
                            break;
                        case State.allowed:
                            decorator.DecorateAllowed(gameEdge);
                            break;
                        case State.implicitly_allowed:
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

            foreach (MappingEdge e in activeMappingEdges)
            {
                Color initialColor = e.lineRenderer.startColor;
                Color highlightColor = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Min(1.0f, AlphaCoefficient * initialColor.a));
                Color finalColor = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Max(0.05f, initialColor.a / AlphaCoefficient));

                EdgeAnimator.Create(e.lineRenderer.gameObject, initialColor, highlightColor, finalColor, 0.3f, 4.0f);
            }
            activeMappingEdges.Clear();
        }

        private void HandleMapsToEdgeRemoved(MapsToEdgeRemoved mapsToEdgeRemoved)
        {
            Debug.Log(mapsToEdgeRemoved.ToString());
        }
    }
}