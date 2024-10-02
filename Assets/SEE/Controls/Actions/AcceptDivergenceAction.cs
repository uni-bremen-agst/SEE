using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.GO;
using SEE.Utils.History;
using UnityEngine;
using System;
using SEE.Game.SceneManipulation;
using SEE.Net.Actions;
using SEE.Audio;
using SEE.Game;
using SEE.UI.Notification;
using SEE.Utils;

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
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/> as a <see
        /// cref="IReversibleAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
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
            /// <param name="source">the source node of the edge in the architecture graph</param>
            /// <param name="target">the target node of the edge in the architecture grpah</param>
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
        /// The edge created by this action. Can be null if no edge has been created yet or whether
        /// an Undo was called. The created edge is stored only to delete it again if Undo is
        /// called. All information to create the edge is kept in <see cref="memento"/>.
        /// </summary>
        private Edge createdEdge;

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
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
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

                            // create the edge
                            createdEdge = CreateConvergentEdge(memento);

                            // check whether edge creation was successful
                            bool divergenceSolved = createdEdge != null;

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
                return false;
            }
            else
            {
                // FIXME: Needs adaptation for VR where no mouse is available.
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

                            // create the edge
                            createdEdge = CreateConvergentEdge(memento);

                            // check whether edge creation was successful
                            bool divergenceSolved = createdEdge != null;

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
                return false;
            }
        }

        /// <summary>
        /// Undoes this AcceptDivergenceAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            // remove the synced edge (its info is saved in memento)
            ReflexionGraph graph = (ReflexionGraph)createdEdge.ItsGraph;

            if (graph != null)
            {
                // find the corresponding GameObject
                GameObject createdEdgeGO = GraphElementIDMap.Find(createdEdge.ID);
                // remove the edge's GameObject and graph representation locally and on the network
                GameEdgeAdder.Remove(createdEdgeGO);

                // propagate the new edge via network
                new DeleteNetAction(createdEdge.ID).Execute();

                // ensure the edge's GameObject gets destroyed properly
                Destroyer.Destroy(createdEdgeGO);
            }
            else
            {
                throw new Exception($"Edge {createdEdge.ID} to be removed is not contained in a graph.");
            }

            // set any edge references back to null
            createdEdge = null;
        }

        /// <summary>
        /// Redoes this AcceptDivergenceAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            // recreate the edge
            createdEdge = CreateConvergentEdge(memento);
        }

        /// <summary>
        /// Creates a new edge using the given <paramref name="memento"/>.
        /// </summary>
        /// <param name="memento">information needed to create the edge</param>
        /// <returns>the new edge</returns>
        private static Edge CreateConvergentEdge(Memento memento)
        {
            Edge newEdge = AcceptDivergence.Accept(memento.From, memento.To, memento.Type);

            // propagate the edge (including matching ID) over network
            new AcceptDivergenceNetAction(memento.From.ID, memento.To.ID, newEdge.ID, newEdge.Type).Execute();

            return newEdge;
        }

        /// <summary>
        /// Creates a new edge in the architecture to allow the given <paramref name="divergence"/>.
        /// </summary>
        /// <param name="divergence">the edge representing the divergence</param>
        /// <returns>the new edge</returns>
        public static Edge CreateConvergentEdge(Edge divergence)
        {
            ReflexionGraph graph = (ReflexionGraph)divergence.ItsGraph;
            Node source = graph.MapsTo(divergence.Source);
            Node target = graph.MapsTo(divergence.Target);
            Memento memento = new(source, target, Edge.SourceDependency);
            return CreateConvergentEdge(memento);
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.AcceptDivergence;
        }

        /// <summary>
        /// Returns all IDs of GameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of GameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (createdEdge == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.From.ID,
                    memento.To.ID,
                    createdEdge.ID
                };
            }
        }
    }
}
