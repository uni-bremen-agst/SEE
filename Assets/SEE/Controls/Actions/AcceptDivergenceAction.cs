using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using System;
using SEE.Game.SceneManipulation;
using SEE.Net.Actions;
using SEE.Audio;
using SEE.Game;

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
        public static ReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/> as a <see
        /// cref="ReversibleAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The graph that the edge which was hit by the user to be accepted into the graph is
        /// in. Set in <see cref="Update"/>.
        /// </summary>
        private ReflexionGraph graph;

        /// <summary>
        /// The information required to (re-)create the edge that solves the divergence.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The source of the edge.
            /// </summary>
            public Node from;
            /// <summary>
            /// The target of the edge.
            /// </summary>
            public Node to;
            /// <summary>
            /// The type of the edge.
            /// </summary>
            public string type;
            /// <summary>
            /// Construct a new memento.
            /// </summary>
            /// <param name="source">the source node of the edge in the architecture graph</param>
            /// <param name="target">the target node of the edge in the architecture grpah</param>
            public Memento(Node source, Node target, string type)
            {
                this.from = source;
                this.to = target;
                this.type = type;
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
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // indicates whether the divergence has been solved ("true" means "solved")
            bool divergenceSolved = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef _) != HitGraphElement.None)
            {
                // find the edge representing the divergence that should be solved.
                GameObject selectedDivergenceEdge = raycastHit.collider.gameObject;

                // check whether the object selected is an edge.
                if (selectedDivergenceEdge.TryGetEdge(out Edge selectedEdge))
                {
                    // check if the selected edge represents a divergence
                    if (selectedEdge.IsInImplementation() && ReflexionGraph.IsDivergent(selectedEdge))
                    {
                        // acquire the containing ReflexionGraph
                        graph = (ReflexionGraph)selectedEdge.ItsGraph;

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
                        createdEdge = CreateEdge(memento);

                        // check whether edge creation was successfull
                        divergenceSolved = createdEdge != null;

                        // add audio cue to the appearance of the new architecture edge
                        AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NEW_EDGE_SOUND);

                        // update the current state depending on whether the divergence has been solved
                        // (required in order to register as an undo-able action)
                        currentState = divergenceSolved ? ReversibleAction.Progress.Completed : ReversibleAction.Progress.NoEffect;

                        // the selected object is synced and this action is done
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning($"Selected Element {selectedDivergenceEdge.name} is not an edge.\n");
                }
            }
            return false;
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
            createdEdge = CreateEdge(memento);
        }

        /// <summary>
        /// Creates a new edge using the given <paramref
        /// name="memento"/>.  In case of any error, null will be
        /// returned.
        /// </summary>
        /// <param name="memento">information needed to create the edge</param>
        /// <returns>the new edge's GameObject and a reference to itself, or both null</returns>
        private Edge CreateEdge(Memento memento)
        {
            // create the edge beforehand
            Edge newEdge = new(memento.from, memento.to, memento.type);

            // add the already created edge to the architecture graph
            graph.AddToArchitecture(newEdge);

            // (re)draw the new edge
            GameEdgeAdder.Draw(newEdge);

            // propagate the edge (including matching ID) over network
            new AcceptDivergenceNetAction(memento.from.ID, memento.to.ID, newEdge.ID, newEdge.Type).Execute();

            return newEdge;
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
                    memento.from.ID,
                    memento.to.ID,
                    createdEdge.ID
                };
            }
        }
    }
}
