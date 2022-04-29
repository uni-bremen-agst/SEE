using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Tools.ReflexionAnalysis;
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

        /// <summary>
        /// The mapping graph, which may be modified during gameplay.
        /// </summary>
        private readonly Graph mapping;

        private readonly InteractableObject lastSelection;
        //private Reflexion reflexion;

        #endregion

        #region Constructors

        private MappingAction()
        {
            // FIXME
            //mapping = new Graph();
            //Graph impl = (Graph)SceneQueries.FindImplementation().LoadedGraph.Clone();
            //Graph arch = (Graph)SceneQueries.FindArchitecture().LoadedGraph.Clone();
            //string mappingName = "Mapping#" + impl.Name + "#" + arch.Name;
            //mapping.Name = mappingName;
            //foreach (Node n in impl.Nodes())
            //{
            //    n.Domain |= Node.NodeDomain.Implementation;
            //}
            //foreach (Node n in arch.Nodes())
            //{
            //    n.Domain |= Node.NodeDomain.Architecture;
            //}
            //Node mappingRoot = new Node
            //{
            //    ID = mappingName,
            //    SourceName = mappingName,
            //    Domain = Node.NodeDomain.Mapping,
            //    Type = "Mapping",
            //    Parent = null
            //};
            //List<Node> implRoots = impl.GetRoots();
            //List<Node> archRoots = arch.GetRoots();
            //Assert.IsTrue(implRoots.Count == 1);
            //Assert.IsTrue(archRoots.Count == 1);
            //mappingRoot.AddChild(implRoots[0]);
            //mappingRoot.AddChild(archRoots[0]);

            //void _AddNodesToMappingRecursively(Node node)
            //{
            //    node.ItsGraph = null;
            //    mapping.AddNode(node);
            //    foreach (Node child in node.Children())
            //    {
            //        _AddNodesToMappingRecursively(child);
            //    }
            //}
            //_AddNodesToMappingRecursively(mappingRoot);

            lastSelection = null;
        }

        private MappingAction(MappingAction mappingAction)
        {
            mapping = (Graph)mappingAction.mapping?.Clone();
            lastSelection = mappingAction.lastSelection;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a new instance of <see cref="MappingAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MappingAction"/></returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            return new MappingAction();
        }

        /// <summary>
        /// Returns a new copy of <see cref="this"/>.
        /// </summary>
        /// <returns>new copy of this</returns>
        public ReversibleAction NewInstance()
        {
            return new MappingAction(this);
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
        }

        public bool Update()
        {
            if (SEEInput.Mapping())
            {
                SEECity city = SceneQueries.FindMapping();
                city.LoadedGraph = mapping;
                city.ReDrawGraph();
            }
            return true;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
        public void Start()
        {
            InteractableObject.ReplaceSelect += ReplaceSelect;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Stop"/>.
        /// </summary>
        public void Stop()
        {
            InteractableObject.ReplaceSelect -= ReplaceSelect;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Undo"/>.
        /// </summary>
        public void Undo()
        {
            // TODO
            Debug.Log("UNDO MAPPING");
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Redo"/>.
        /// </summary>
        public void Redo()
        {
            // TODO
            Debug.Log("REDO MAPPING");
        }

        //----------------------------------------------------------------
        // Events
        //----------------------------------------------------------------

        private void ReplaceSelect(List<InteractableObject> replaced, List<InteractableObject> by, bool isInitiator)
        {
            // TODO(torben): for now!
            Assert.IsTrue(replaced.Count <= 1);
            Assert.IsTrue(by.Count <= 1);

            if (replaced.Count == 1 && by.Count == 1)
            {
                if (replaced[0].TryGetNode(out Node srcNode) && by[0].TryGetNode(out Node dstNode))
                {
                    // FIXME
                    //Node.NodeDomain srcKind = srcNode.Domain;
                    //Node.NodeDomain dstKind = dstNode.Domain;
                    //// Note: Arch elems should not be moved into impl elems
                    //if (!srcNode.ID.Equals(dstNode.ID) && !(srcKind == Node.NodeDomain.Architecture && dstKind == Node.NodeDomain.Implementation) && !srcNode.IsRoot())
                    //{
                    //    Node src = mapping.GetNode(srcNode.ID);
                    //    Node dst = mapping.GetNode(dstNode.ID);
                    //    src.Reparent(dst);
                    //    SEECity city = SceneQueries.FindMapping();
                    //    city.LoadedGraph = mapping;
                    //    city.ReDrawGraph();
                    //}
                }
            }
        }

        /// <summary>
        /// Called by incremental reflexion for every change in the reflexion model
        /// by way of the observer protocol as a callback. Dispatches the event to
        /// the appropriate handling function.
        /// </summary>
        /// <param name="changeEvent">additional information about the change in the reflexion model</param>
        public void NewChange(ChangeEvent changeEvent)
        {
            switch (changeEvent)
            {
                case EdgeChange changedEvent:
                    HandleEdgeChange(changedEvent);
                    break;
                case PropagatedEdgeEvent changedEvent:
                    HandlePropagatedEdge(changedEvent);
                    break;
                case EdgeEvent changedEvent:
                    if (changedEvent.Affected == ReflexionSubgraph.Mapping)
                    {
                        HandleMapsToEdgeAdded(changedEvent);
                    }
                    break;
                default:
                    Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent);
                    break;
            }
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

        private void HandlePropagatedEdge(PropagatedEdgeEvent propagatedEdgeEvent)
        {
            Debug.Log(propagatedEdgeEvent.ToString());
        }

        private void HandleMapsToEdgeAdded(EdgeEvent mapsToEdgeEvent)
        {
            Debug.Log(mapsToEdgeEvent.ToString());

            Edge edge = mapsToEdgeEvent.Edge;
            // Added:
            //LineRenderer lineRenderer = CreateFinalizedHoverEdge(edge.Source.ID, edge.Target.ID);
            //edgeToFinalizedMappingEdges[edge] = lineRenderer;
            
            // Removed: 
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

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>.
        /// </summary>
        /// <returns>The set of the IDs of all game objects changed by this action</returns>
        public HashSet<string> GetChangedObjects()
        {
            // TODO
            return new HashSet<string>();
        }

        /// <summary>
        /// The unique ID of an action.
        /// </summary>
        private readonly string id = Guid.NewGuid().ToString();

        /// <summary>
        /// The unique ID of this action.
        /// <see cref="ReversibleAction.GetId"/>.
        /// </summary>
        /// <returns>unique ID</returns>
        public string GetId()
        {
            return id;
        }

        public ReversibleAction.Progress CurrentProgress()
        {
            // FIXME
            return ReversibleAction.Progress.NoEffect;
        }

        #endregion
    }
}