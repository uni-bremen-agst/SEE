using SEE.DataModel.DG;
using SEE.Layout;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of deleting
    /// graph elements from the currently shown graph to the next one.
    ///
    /// The transition from the current graph to the next one is organized in
    /// the following phases:
    ///
    /// (1) Remove deleted nodes and edges from the scene.
    /// (2) Move existing nodes and edges to their new position in the scene.
    /// (3) Adjust existing changed nodes and edges in the scene.
    /// (4) Add newly created nodes and edges to the scene.
    ///
    /// The code following here implements phase (1).
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// Watchdog triggering <see cref="Phase2MoveExistingGraphElements"/> when phase 1 has been
        /// completed, in which the necessary nodes and edges are deleted.
        /// </summary>
        private Utils.CountingJoin phase1AnimationWatchDog;

        /// <summary>
        /// Implements the first phase of the transition from the <paramref name="current"/> graph to
        /// the <paramref name="next"/> graph in which nodes and edges present in <paramref name="current"/>
        /// but not in <paramref name="next"/> (in other words, the deleted nodes and edges) are removed.
        /// When all animations triggered in this first phase have completed, execution will continue with
        /// <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// The mechanism to call <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/> is
        /// as follows. The <see cref="phase1AnimationWatchDog"/> is set up to await the
        /// deletion of nodes and edges in <paramref name="current"/> but not in <paramref name="next"/>.
        /// Then the methods implementing this deletion will be called for each graph element to
        /// be deleted; these are <see cref="RenderRemovedNode(Node)"/> and <see cref="RenderRemovedEdge(Edge)"/>,
        /// respectively. When the animation of the deletion triggered by these has finished, each
        /// will signal the <see cref="phase1AnimationWatchDog"/> its completion. The <see cref="phase1AnimationWatchDog"/>
        /// awaits all outstanding deletions and then finally calls
        /// <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// <paramref name="current"/> in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="current">the currently shown graph</param>
        /// <param name="next">the next graph to be shown</param>
        private void Phase1RemoveDeletedGraphElements(LaidOutGraph current, LaidOutGraph next)
        {
            int deletedGraphElements = removedNodes.Count + removedEdges.Count;
            phase1AnimationWatchDog.Await(deletedGraphElements, () => Phase2MoveExistingGraphElements(next));
            if (deletedGraphElements > 0)
            {
                // Remove those nodes.
                removedNodes.ForEach(RenderRemovedNode);
                // Remove those edges.
                removedEdges.ForEach(RenderRemovedEdge);
            }
            else
            {
                // To trigger the next phases where new and existing nodes/edges are to be drawn.
                phase1AnimationWatchDog.Skip();
            }
            /// Note: <see cref="Phase2MoveExistingGraphElements"/> will be called by <see cref="phase1AnimationWatchDog"/>
            /// when phase 1 has completed (or skipped).
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
