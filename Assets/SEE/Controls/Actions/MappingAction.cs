using System;
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
    public class MappingAction : Observer, ReversibleAction
    {
        #region Fields

        private Graph mapping;
        //private Reflexion reflexion;

        #endregion

        #region Constructors

        private MappingAction()
        {
            mapping = new Graph();
            Graph impl = (Graph)SceneQueries.FindImplementation().LoadedGraph.Clone();
            Graph arch = (Graph)SceneQueries.FindArchitecture().LoadedGraph.Clone();
            string mappingName = "Mapping#" + impl.Name + "#" + arch.Name;
            mapping.Name = mappingName;
            foreach (Node n in impl.Nodes())
            {
                n.flags |= Node.Flags.Implementation;
            }
            foreach (Node n in arch.Nodes())
            {
                n.flags |= Node.Flags.Architecture;
            }
            Node mappingRoot = new Node
            {
                ID = mappingName,
                flags = Node.Flags.Mapping,
                Type = "Mapping",
                Parent = null
            };
            List<Node> implRoots = impl.GetRoots();
            List<Node> archRoots = arch.GetRoots();
            Assert.IsTrue(implRoots.Count == 1);
            Assert.IsTrue(archRoots.Count == 1);
            mappingRoot.AddChild(implRoots[0]);
            mappingRoot.AddChild(archRoots[0]);

            void _AddNodesToMappingRecursively(Node node)
            {
                node.ItsGraph = null;
                mapping.AddNode(node);
                foreach (Node child in node.Children())
                {
                    _AddNodesToMappingRecursively(child);
                }
            }
            _AddNodesToMappingRecursively(mappingRoot);
        }

        private MappingAction(MappingAction mappingAction)
        {
            mapping = (Graph)mappingAction.mapping.Clone();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a new instance of <see cref="MappingAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MappingAction"/></returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            MappingAction result = new MappingAction();
            return result;
        }

        /// <summary>
        /// Returns a new copy of <see cref="this"/>.
        /// </summary>
        /// <returns>new copy of this</returns>
        public ReversibleAction NewInstance()
        {
            MappingAction result = new MappingAction(this);
            return result;
        }

        public void Awake()
        {
#if false
            // Setup reflexion
            Reflexion = new Reflexion(implGraph, archGraph, mapping);
            Reflexion.Register(this);
            // An initial run is necessary to set up the necessary data structures.
            Reflexion.Run();
#endif
            InteractableObject.AnyHoverIn += AnyHoverIn;
            InteractableObject.AnyHoverOut += AnyHoverOut;
            InteractableObject.AnySelectIn += AnySelectIn;
            InteractableObject.AnySelectOut += AnySelectOut;
        }

        public bool Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) // TODO(torben): Bindings
            {
                mapping.FinalizeNodeHierarchy();
                SEECity city = SceneQueries.GetMapp();
                city.LoadedGraph = mapping;
                city.ReDrawGraph();
            }
            return true;
        }

        private void FixedUpdate()
        {
#if false
            bool synchronize = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit raycastHit);
            Vector3 hit = raycastHit.point;

#region Mapping

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
                        o.go.transform.position = p + o.offset;
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
                                    go = CreateGhost(e.Current),
                                    offset = e.Current.transform.position - moveState.mainDraggedObj.transform.position
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
                        o.go.transform.position = newPosition + o.offset;
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

                UnityEngine.Object.Destroy(moveState.mainDraggedObj);
                foreach (_MoveState.AdditionalDraggedObject o in moveState.additionalDraggedObjs)
                {
                    UnityEngine.Object.Destroy(o.go);
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
                                //edgeToMappingEdges.Remove(mapped.Outgoings[0]);
                                Reflexion.Delete_From_Mapping(mapped.Outgoings[0]);
                            }
                            Reflexion.Add_To_Mapping(from, to);
                        }
                    }
                }
            }

#endregion

            if (actionState.startShowDiff)
            {
                if (!actionState.stopShowDiff)
                {
                    foreach (Edge edge in archGraph.Edges())
                    {
                        if (EdgeRef.TryGet(edge, out EdgeRef edgeRef))
                        {
                            edgeRef.GetComponent<LineRenderer>().enabled = false;
                        }
                    }
                    foreach (LineRenderer r in edgeToStateEdges.Values)
                    {
                        r.enabled = true;
                    }
                }
            }
#endif
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
        public void Start()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Stop"/>.
        /// </summary>
        public void Stop()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public void Undo()
        {
            Debug.Log("UNDO MAPPING");
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public void Redo()
        {
            Debug.Log("REDO MAPPING");
        }

        //----------------------------------------------------------------
        // Events
        //----------------------------------------------------------------

        private void AnyHoverIn(InteractableObject toObj, bool isOwner)
        {
        }

        private void AnyHoverOut(InteractableObject toObj, bool isOwner)
        {
        }

        private void AnySelectIn(InteractableObject fromObj, bool isOwner)
        {
        }

        private void AnySelectOut(InteractableObject fromObj, bool isOwner)
        {
        }

        /// <summary>
        /// Called by incremental reflexion for every change in the reflexion model
        /// by way of the observer protocol as a callback. Dispatches the event to
        /// the appropriate handling function.
        /// 
        /// </summary>
        /// <param name="changeEvent">additional information about the change in the reflexion model</param>
        public void Update(ChangeEvent changeEvent)
        {
            switch (changeEvent)
            {
                case EdgeChange changedEvent:
                    HandleEdgeChange(changedEvent);
                    break;
                case PropagatedEdgeAdded changedEvent:
                    HandlePropagatedEdgeAdded(changedEvent);
                    break;
                case PropagatedEdgeRemoved changedEvent:
                    HandlePropagatedEdgeRemoved(changedEvent);
                    break;
                case MapsToEdgeAdded changedEvent:
                    HandleMapsToEdgeAdded(changedEvent);
                    break;
                case MapsToEdgeRemoved changedEvent:
                    HandleMapsToEdgeRemoved(changedEvent);
                    break;
                default:
                    Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent);
                    break;
            }
        }

        /// <summary>
        /// <see cref="ReversibleAction.HadEffect"/>
        /// </summary>
        /// <returns>true if this action has had already some effect that would need to be undone</returns>
        public bool HadEffect()
        {
            return false; // FIXME
        }

        /// <summary>
        /// Handles every state change of an existing edge.
        /// </summary>
        /// <param name="edgeChange"></param>
        private void HandleEdgeChange(EdgeChange edgeChange)
        {
#if false
            Color c = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            switch (edgeChange.newState)
            {
                case State.divergent:
                    c = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    break;
                case State.absent:
                    c = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                    break;
                case State.convergent:
                    c = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    break;
            }

            if (c.a != 0.0f)
            {
                if (!edgeToStateEdges.TryGetValue(edgeChange.edge, out LineRenderer r))
                {
                    r = CreateStateEdge(edgeChange.edge.Source, edgeChange.edge.Target);
                    edgeToStateEdges[edgeChange.edge] = r;
                }
                LineFactory.SetColor(r, c);
            }

            Debug.LogFormat("edge of type {0} from {1} to {2} changed its state from {3} to {4}.\n",
                            edgeChange.edge.Type, edgeChange.edge.Source.ID, edgeChange.edge.Target.ID,
                            edgeChange.oldState, edgeChange.newState);
#endif
#if false // TODO(torben): we might want to still use some of these decorations
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
#endif
        }

        private void HandlePropagatedEdgeRemoved(PropagatedEdgeRemoved propagatedEdgeRemoved)
        {
            Debug.Log(propagatedEdgeRemoved.ToString());
        }

        private void HandlePropagatedEdgeAdded(PropagatedEdgeAdded propagatedEdgeAdded)
        {
            Debug.Log(propagatedEdgeAdded.ToString());
        }

        private void HandleMapsToEdgeAdded(MapsToEdgeAdded mapsToEdgeAdded)
        {
            Debug.Log(mapsToEdgeAdded.ToString());

            Edge edge = mapsToEdgeAdded.mapsToEdge;
            //LineRenderer lineRenderer = CreateFinalizedHoverEdge(edge.Source.ID, edge.Target.ID);
            //edgeToFinalizedMappingEdges[edge] = lineRenderer;
        }

        private void HandleMapsToEdgeRemoved(MapsToEdgeRemoved mapsToEdgeRemoved)
        {
            Debug.Log(mapsToEdgeRemoved.ToString());

            Edge edge = mapsToEdgeRemoved.mapsToEdge;
            //LineRenderer r = edgeToFinalizedMappingEdges[edge];
            //edgeToFinalizedMappingEdges.Remove(edge);
            //UnityEngine.Object.Destroy(r.gameObject);
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns>the <see cref="ActionStateType"/> of this action</returns>
        public ActionStateType GetActionStateType()
        {
            return ActionStateType.Map;
        }

#endregion
    }
}