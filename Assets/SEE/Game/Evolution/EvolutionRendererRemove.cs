using SEE.DataModel.DG;
using SEE.Layout;
using SEE.Utils;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of deleting
    /// graph elements from the currently shown graph to the next one.
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// A watchdog awaiting all animations of the first phase to be finished. The first
        /// phase is dedicated to the deletion of graph elements not present in the next graph.
        /// When all deletion animations have completed, <see cref="Phase2AddNewAndExistingGraphElements"/>
        /// will be called.
        /// </summary>
        private class Phase1AnimationWatchDog : AnimationWatchDog
        {
            protected override string Name { get => nameof(Phase1AnimationWatchDog); }

            /// <summary>
            /// The next graph to be shown.
            /// </summary>
            private LaidOutGraph next;

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase1AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// Sets the <paramref name="numberOfAnimations"/> to be waited for until the
            /// <see cref="Phase2AddNewAndExistingGraphElements"/> should be called.
            /// </summary>
            /// <param name="numberOfAnimations">the number of animations to be awaited</param>
            public void Await(int numberOfAnimations, LaidOutGraph next)
            {
                this.next = next;
                Await(numberOfAnimations);
            }

            /// <summary>
            /// If there are no more other animations to be awaited, <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called.
            /// </summary>
            protected override void Continue()
            {
                evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
            }

            /// <summary>
            /// Tells this <see cref="Phase1AnimationWatchDog"/> to skip the waiting for
            /// outstanding animations. <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called immediately. <paramref name="next"/> will be passed as
            /// argument to <see cref="Phase2AddNewAndExistingGraphElements"/>.
            /// </summary>
            /// <param name="next">the next graph to be shown</param>
            public void Skip(LaidOutGraph next)
            {
                this.next = next;
                Skip();
            }
        }

        /// <summary>
        /// Implements the first phase of the transition from the <paramref name="current"/> graph to
        /// the <paramref name="next"/> graph in which nodes and edges present in <paramref name="current"/>
        /// but not in <paramref name="next"/> (in other words, the deleted nodes and edges) are removed.
        /// When all animations triggered in this first phase have completed, execution will continue with
        /// <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// The mechanism to call <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/> is
        /// as follows. The <see cref="phase1AnimationWatchDog"/> is set up to await the
        /// deletion of nodes and edges in <paramref name="current"/> but not in <paramref name="next"/>.
        /// Then the methods implementing this deletion will be called for each graph element to
        /// be deleted; these are <see cref="RenderRemovedNode(Node)"/> and <see cref="RenderRemovedEdge(Edge)"/>,
        /// respectively. When the animation of the deletion triggered by these has finished, each
        /// will signal the <see cref="phase1AnimationWatchDog"/> its completion. The <see cref="phase1AnimationWatchDog"/>
        /// awaits all outstanding deletions and then finally calls
        /// <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// <paramref name="current"/> in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="current">the currently shown graph</param>
        /// <param name="next">the next graph to be shown</param>
        private void Phase1RemoveDeletedGraphElements(LaidOutGraph current, LaidOutGraph next)
        {
            if (OLD)
            {
                if (current != null && current.Graph != null)
                {
                    // The set of nodes of the current graph not in the other graph, in other words,
                    // the set of deleted nodes.
                    // Note: The comparison is based on the IDs of the nodes because nodes between
                    // two graphs must be different even if they denote the "logically same" node.
                    IList<Node> deletedNodes = current.Graph.Nodes().Except(next.Graph.Nodes(), nodeEqualityComparer).ToList();

                    // The set of edges of the current graph not in the next graph; that is, all
                    // edges removed. As above, edges are compared by their IDs.
                    IList<Edge> deletedEdges = current.Graph.Edges().Except(next.Graph.Edges(), edgeEqualityComparer).ToList();

                    int deletedGraphElements = deletedNodes.Count + deletedEdges.Count;
                    if (deletedGraphElements > 0)
                    {
                        phase1AnimationWatchDog.Await(deletedGraphElements, next);

                        // Remove those nodes.
                        deletedNodes.ForEach(RenderRemovedNode);
                        // Remove those edges.
                        deletedEdges.ForEach(RenderRemovedEdge);
                    }
                    else
                    {
                        // To trigger the next phase where new and existing edges are to be drawn.
                        phase1AnimationWatchDog.Skip(next);
                    }
                }
                else
                {
                    // To trigger the next phase where new and existing edges are to be drawn.
                    phase1AnimationWatchDog.Skip(next);
                }
                /// Note: <see cref="Phase2AddNewAndExistingGraphElements"/> will be called by <see cref="phase1AnimationWatchDog"/>
                /// when phase 1 has completed (or skipped).
            }
            else
            {
                int deletedGraphElements = removedNodes.Count + removedEdges.Count;
                if (deletedGraphElements > 0)
                {
                    phase1AnimationWatchDog.Await(deletedGraphElements, next);

                    // Remove those nodes.
                    removedNodes.ForEach(RenderRemovedNode);
                    // Remove those edges.
                    removedEdges.ForEach(RenderRemovedEdge);
                }
                else
                {
                    // To trigger the next phase where new and existing edges are to be drawn.
                    phase1AnimationWatchDog.Skip(next);
                }
                /// Note: <see cref="Phase2AddNewAndExistingGraphElements"/> will be called by <see cref="phase1AnimationWatchDog"/>
                /// when phase 1 has completed (or skipped).
            }
        }

        /// <summary>
        /// Removes the given node. The removal is animated by raising the
        /// node to <see cref="SkyLevel"/> (y dimension). The node is not
        /// destroyed when the animation has finished.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        private void RenderRemovedNode(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject nodeObject))
            {
                Assert.IsNotNull(nodeObject);
                markerFactory.MarkDead(nodeObject);
                AnimateToDeath(nodeObject);
                // AnimateToDeath() will call phase1AnimationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                phase1AnimationWatchDog.Finished();
            }
        }

        /// <summary>
        /// Triggers an animation indicating that the given <paramref name="gameObject"/> was
        /// deleted by moving it up to the sky. When it reaches the sky at <see cref="SkyLevel"/>,
        /// it will disappear (via <see cref="OnRemoveFinishedAnimation"/>).
        /// </summary>
        /// <param name="gameObject">a game object representing either a node or edge</param>
        private void AnimateToDeath(GameObject gameObject)
        {
            gameObject.transform.SetParent(null);
            /// Let it raise to <see cref="SkyLevel"/>.
            Vector3 newPosition = gameObject.transform.position;
            newPosition.y = SkyLevel;
            ILayoutNode nodeTransform = new AnimationNode(newPosition, gameObject.transform.localScale);
            moveAnimator.AnimateTo(gameObject, nodeTransform, OnRemoveFinishedAnimation);
        }

        /// <summary>
        /// Removes the given edge. The edge is not destroyed immediately, however.
        /// Its destruction is postponed by <see cref="AnimateToDeath(GameObject)"/>
        /// to the point in time when the animation is finished in which case
        /// <see cref="OnRemoveFinishedAnimation"/> will be called.
        /// </summary>
        /// <param name="edge">removed edge</param>
        private void RenderRemovedEdge(Edge edge)
        {
            if (edgesAreDrawn && objectManager.RemoveEdge(edge, out GameObject edgeObject))
            {
                AnimateToDeath(edgeObject);
                // AnimateToDeath() will call phase1AnimationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                phase1AnimationWatchDog.Finished();
            }
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node or edge to be
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed (can be a node or edge)</param>
        private void OnRemoveFinishedAnimation(object gameObject)
        {
            GameObject go = gameObject as GameObject;
            Destroyer.Destroy(go);
            phase1AnimationWatchDog.Finished();
        }
    }
}
