using SEE.Audio;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using SEE.XR;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to solve a divergence (see <see cref="ReflexionAnalysis.State"/>) between
    /// implementation and architecture by adding the exact edge to the architecture that solves
    /// this divergence.
    /// </summary>
    internal class AcceptDivergenceAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/> as a <see
        /// cref="IReversibleAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The information required to (re-)create the edge that solves the divergence.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The source of the edge.
            /// </summary>
            public Node From;
            /// <summary>
            /// The target of the edge.
            /// </summary>
            public Node To;
            /// <summary>
            /// The type of the edge.
            /// </summary>
            public string Type;
            /// <summary>
            /// Construct a new memento.
            /// </summary>
            /// <param name="source">The source node of the edge in the architecture graph.</param>
            /// <param name="target">The target node of the edge in the architecture grpah.</param>
            public Memento(Node source, Node target, string type)
            {
                this.From = source;
                this.To = target;
                this.Type = type;
            }
        }

        /// <summary>
        /// The information required to (re-)create the edge that solves the divergence.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information required to (re-)create the edges that solve the divergence
        /// via the multi-selection context menu.
        /// </summary>
        private readonly List<Memento> mementoList = new();

        /// <summary>
        /// The edges created by this action <see cref="createdEdge"/>.
        /// The list is needed for the multi-selection via context menu.
        /// </summary>
        private readonly List<Edge> createdEdgeList = new();

        /// <summary>
        /// Registers itself at <see cref="InteractableObject"/> to listen for hovering events.
        /// </summary>
        public override void Start()
        {
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// Unregisters itself from <see cref="InteractableObject"/>. Does no longer listen for
        /// hovering events.
        /// </summary>
        public override void Stop()
        {
            InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
        }

        /// <summary>
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            if (User.UserSettings.IsVR)
            {
                if (XRSEEActions.Selected
                    && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    // find the edge representing the divergence that should be solved.
                    GameObject divergentEdge = hit.collider.gameObject;

                    // check whether the object selected is an edge.
                    if (divergentEdge.TryGetEdge(out Edge selectedEdge))
                    {
                        // check if the selected edge represents a divergence
                        if (selectedEdge.ItsGraph is ReflexionGraph graph && ReflexionGraph.IsDivergent(selectedEdge))
                        {
                            // find that node in the architecture graph,
                            // which the divergence's source node is
                            // explicitly or implicitly mapped to
                            Node source = graph.MapsTo(selectedEdge.Source);

                            // find that node in the ArchitectureGraph,
                            // which the divergence's target is explicitly
                            // or implicitly mapped to
                            Node target = graph.MapsTo(selectedEdge.Target);

                            // we have both source and target of the edge and use a memento struct
                            // to remember which edge we have added
                            memento = new Memento(source, target, Edge.SourceDependency);
                            mementoList.Add(memento);
                            // create the edge
                            createdEdgeList.Add(CreateConvergentEdge(memento));

                            // check whether edge creation was successful
                            bool divergenceSolved = createdEdgeList[0] != null;

                            // add audio cue to the appearance of the new architecture edge
                            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewEdgeSound);

                            // update the current state depending on whether the divergence has been solved
                            // (required in order to register as an undo-able action)
                            CurrentState = divergenceSolved ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;

                            // the selected object is synced and this action is done
                            XRSEEActions.Selected = false;
                            return true;
                        }
                    }
                    else
                    {
                        XRSEEActions.Selected = false;
                        ShowNotification.Warn("Not an edge", $"Selected Element {divergentEdge.name} is not an edge.\n");
                    }
                }
                if (ExecuteViaContextMenu)
                {
                    bool divergenceSolved = createdEdgeList.All(e => e != null);
                    CurrentState = divergenceSolved ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;
                    return true;
                }
                return false;
            }
            else
            {
                if (Input.GetMouseButtonDown(0)
                    && Raycasting.RaycastGraphElement(
                        out RaycastHit raycastHit,
                        out GraphElementRef _) != HitGraphElement.None)
                {
                    // find the edge representing the divergence that should be solved.
                    GameObject divergentEdge = raycastHit.collider.gameObject;

                    // check whether the object selected is an edge.
                    if (divergentEdge.TryGetEdge(out Edge selectedEdge))
                    {
                        // check if the selected edge represents a divergence
                        if (selectedEdge.ItsGraph is ReflexionGraph graph && ReflexionGraph.IsDivergent(selectedEdge))
                        {
                            // find that node in the architecture graph,
                            // which the divergence's source node is
                            // explicitly or implicitly mapped to
                            Node source = graph.MapsTo(selectedEdge.Source);

                            // find that node in the ArchitectureGraph,
                            // which the divergence's target is explicitly
                            // or implicitly mapped to
                            Node target = graph.MapsTo(selectedEdge.Target);

                            // we have both source and target of the edge and use a memento struct
                            // to remember which edge we have added
                            memento = new Memento(source, target, Edge.SourceDependency);
                            mementoList.Add(memento);
                            // create the edge
                            createdEdgeList.Add(CreateConvergentEdge(memento));

                            // check whether edge creation was successful
                            bool divergenceSolved = createdEdgeList[0] != null;

                            // add audio cue to the appearance of the new architecture edge
                            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewEdgeSound);

                            // update the current state depending on whether the divergence has been solved
                            // (required in order to register as an undo-able action)
                            CurrentState = divergenceSolved ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;

                            // the selected object is synced and this action is done
                            return true;
                        }
                    }
                    else
                    {
                        ShowNotification.Warn("Not an edge", $"Selected Element {divergentEdge.name} is not an edge.\n");
                    }
                }
                if (ExecuteViaContextMenu)
                {
                    bool divergenceSolved = createdEdgeList.All(e => e != null);
                    CurrentState = divergenceSolved ? IReversibleAction.Progress.Completed : IReversibleAction.Progress.NoEffect;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Undoes this <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            foreach (Edge edge in createdEdgeList)
            {
                RemoveDivergence(edge);
            }
            createdEdgeList.Clear();
        }

        /// <summary>
        /// Removes the divergence (undo).
        /// </summary>
        /// <param name="edge">The edge divergence to remove.</param>
        /// <exception cref="Exception">If the edge not contained in a graph.</exception>
        private static void RemoveDivergence(Edge edge)
        {
            // remove the synced edge (its info is saved in memento)
            ReflexionGraph graph = (ReflexionGraph)edge.ItsGraph;

            if (graph != null)
            {
                // find the corresponding GameObject
                GameObject createdEdgeGO = GraphElementIDMap.Find(edge.ID);
                // remove the edge's GameObject and graph representation locally and on the network
                GameEdgeAdder.Remove(createdEdgeGO);

                // propagate the new edge via network
                new DeleteNetAction(edge.ID).Execute();

                // ensure the edge's GameObject gets destroyed properly
                Destroyer.Destroy(createdEdgeGO);
            }
            else
            {
                throw new Exception($"Edge {edge.ID} to be removed is not contained in a graph.");
            }
        }

        /// <summary>
        /// Redoes this <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (Memento mem in mementoList)
            {
                createdEdgeList.Add(CreateConvergentEdge(mem));
            }
        }

        /// <summary>
        /// Creates a new edge using the given <paramref name="memento"/>.
        /// </summary>
        /// <param name="memento">Information needed to create the edge.</param>
        /// <returns>The new edge.</returns>
        private static Edge CreateConvergentEdge(Memento memento)
        {
            Edge newEdge = AcceptDivergence.Accept(memento.From, memento.To, memento.Type);

            // propagate the edge (including matching ID) over network
            new AcceptDivergenceNetAction(memento.From.ID, memento.To.ID, newEdge.ID, newEdge.Type).Execute();

            return newEdge;
        }

        /// <summary>
        /// Used to execute the <see cref="AcceptDivergenceAction"/> from the context menu.
        /// Creates a new edge in the architecture to allow the given <paramref name="divergence"/>
        /// and ensures that the <see cref="Update"/> method performs the execution via context menu.
        /// </summary>
        /// <param name="divergence">The edge representing the divergence.</param>
        /// <returns>The new edge.</returns>
        public void ContextMenuExecution(Edge divergence)
        {
            ExecuteViaContextMenu = true;
            mementoList.Add(CreateMementoAndConvergentEdge(divergence));
        }

        /// <summary>
        /// Used to execute the <see cref="AcceptDivergenceAction"/> from the context menu in multiselection mode.
        /// Creates new edges in the architecture to allow the given <paramref name="divergences"/> and ensures
        /// that the <see cref="Update"/> method perfoms the execution via context menu.
        /// </summary>
        /// <param name="divergences">The edges representing the divergences.</param>
        public void ContextMenuExecution(IList<Edge> divergences)
        {
            ExecuteViaContextMenu = true;
            foreach (Edge divergence in divergences)
            {
                mementoList.Add(CreateMementoAndConvergentEdge(divergence));
            }
        }

        /// <summary>
        /// Creates the memento for restoring the edge and creates the edge.
        /// </summary>
        /// <param name="divergence">The edge representing the divergence.</param>
        /// <returns>The created memento.</returns>
        private Memento CreateMementoAndConvergentEdge(Edge divergence)
        {
            ReflexionGraph graph = (ReflexionGraph)divergence.ItsGraph;
            Node source = graph.MapsTo(divergence.Source);
            Node target = graph.MapsTo(divergence.Target);
            memento = new(source, target, Edge.SourceDependency);
            createdEdgeList.Add(CreateConvergentEdge(memento));
            return memento;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.AcceptDivergence;
        }

        /// <summary>
        /// Returns all IDs of GameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of GameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (createdEdgeList.Count == 0)
            {
                return new HashSet<string>();
            }
            else
            {
                return mementoList.Zip(createdEdgeList, (m, e) => new[] { m.From.ID, m.To.ID, e.ID }).SelectMany(x => x).ToHashSet();
            }
        }
    }
}
