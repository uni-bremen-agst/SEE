using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Linq;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using System;
using SEE.Game;
using SEE.Net.Actions;
using SEE.Audio;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to solve a Divergence between Implementation and
    /// Architecture by adding the Edge to the Architecture that
    /// solves the Divergence.
    /// </summary>
    internal class AcceptDivergenceAction : AbstractPlayerAction
    {
        /// <summary>
        /// The edge that was hit by the user to be accepted
        /// into the ArchitectureGraph. Set in <see cref="Update"/>.
        /// </summary>
        private GameObject selectedDivergenceEdge;

        /// <summary>
        /// Contains all Edges that were explicitly added to
        /// solve Divergences.
        ///</summary>
        private ISet<GameObject> syncedGameObjects;

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef _) != HitGraphElement.None)
            {
                // Find the edge representing the divergence that should be solved.
                selectedDivergenceEdge = raycastHit.collider.gameObject;

                // Check whether the object selected is an edge.
                if (selectedDivergenceEdge.TryGetEdge(out Edge selectedEdge))
                {
                    // Check if the selected edge represents a divergence
                    if (ReflexionGraph.IsDivergent(selectedEdge))
                    {
                        // acquire the containing ReflexionGraph
                        var graph = (ReflexionGraph)selectedEdge.ItsGraph;

                        // Find that node in the ArchitectureGraph,
                        // which the divergence's source node is
                        // explicitly or implicitly mapped to.
                        Node source = graph.MapsTo(selectedEdge.Source);

                        // Find that node in the ArchitectureGraph,
                        // which the divergence's target is explicitly
                        // or implicitly mapped to.
                        Node target = graph.MapsTo(selectedEdge.Target);

                        // We have both source and target of the edge
                        // and use a memento struct to remember which
                        // edge we have added.
                        memento = new Memento(source, target);

                        createdEdge = CreateEdge(graph, memento);
                        createdEdge.UnsetToggle(Edge.IsVirtualToggle);

                        // Add audio cue to the appearance of the new architecture edge
                        AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NEW_EDGE_SOUND);

                        return true; // the selected object is synced and this action is done
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
        /// Creates a new edge using the given <paramref name="memento"/>.
        /// In case of any error, null will be returned.
        /// </summary>
        /// <param name="memento">information needed to create the edge</param>
        /// <returns>a new edge or null</returns>
        private static Edge CreateEdge(ReflexionGraph graph, Memento memento)
        {
            // Add the new Edge to the ArhcitectureGraph
            var newArchitectureEdge = graph.ActuallyAddToArchitecture(
                memento.from,
                memento.to,
                "Source_Dependency");

            // Add the new Edge to the Game
            GameObject result = GameEdgeAdder.Draw(newArchitectureEdge);

            // Sync the Solution of the Divergence via Network
            // new AcceptDivergenceNetAction().Execute();

            return newArchitectureEdge;
        }

        /// <summary>
        /// Undoes this AcceptDivergenceAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            // remove the synced edge (info is saved in memento)
            var graph = (ReflexionGraph)createdEdge.ItsGraph;

            if (graph != null)
            {
                graph.RemoveEdge(createdEdge);
                // use netaction to remove synced edge over net
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
            // CreateEdge();
            // recreate edge from memento
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The information we need to (re-)create an edge.
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
            public Memento(Node source, Node target)
            {
                this.from = source;
                this.to = target;
            }
        }

        /// <summary>
        /// The information needed to re-create the synced edge after undo.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The edge created by this action. Can be null if no edge
        /// has been created yet or whether an Undo was called. The
        /// created edge is stored only to delete it again if Undo is
        /// called. All information to create the edge is kept in
        /// <see cref="memento"/>.
        /// </summary>
        private Edge createdEdge;

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.AcceptDivergence;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (syncedGameObjects == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>(syncedGameObjects.Select(x => x.name));
            }
        }
    }
}
