using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Part of the <see cref="EvolutionRenderer"/> taking care of deleting
    /// graph elements from the currently shown graph to the next one.
    /// The code following here implements phase (1).
    /// </summary>
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// Implements the first phase of the transition from the <paramref name="current"/> graph to
        /// the <paramref name="next"/> graph in which nodes and edges present in <paramref name="current"/>
        /// but not in <paramref name="next"/> (in other words, the deleted nodes and edges) are removed.
        /// When all animations triggered in this first phase have completed, execution will continue with
        /// <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// The mechanism to call <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/> is
        /// as follows. The <see cref="animationWatchDog"/> is set up to await the
        /// deletion of nodes and edges in <paramref name="current"/> but not in <paramref name="next"/>.
        /// Then the methods implementing this deletion will be called for each graph element to
        /// be deleted; these are <see cref="RenderRemovedNode(Node)"/> and <see cref="RenderRemovedEdge(Edge)"/>,
        /// respectively. When the animation of the deletion triggered by these has finished, each
        /// will signal the <see cref="animationWatchDog"/> its completion. The <see cref="animationWatchDog"/>
        /// awaits all outstanding deletions and then finally calls
        /// <see cref="Phase2MoveExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// <paramref name="current"/> in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="next">the next graph to be shown</param>
        private void Phase1RemoveDeletedGraphElements(LaidOutGraph next)
        {
            int deletedGraphElements = removedNodes.Count + removedEdges.Count;
            Debug.Log($"Phase 1: Removing {deletedGraphElements} graph elements.\n");
            animationWatchDog.Await(deletedGraphElements, () => Phase2MoveExistingGraphElements(next));
            if (deletedGraphElements > 0)
            {
                // Remove those edges.
                removedEdges.ForEach(RenderRemovedEdge);
                // Remove those nodes.
                removedNodes.ForEach(RenderRemovedNode);
            }
            /// Note: <see cref="Phase2MoveExistingGraphElements"/> will be called by <see cref="animationWatchDog"/>
            /// when phase 1 has completed (or skipped).
        }

        /// <summary>
        /// Removes the given node. The removal is animated by raising the
        /// node to <see cref="AbstractSEECity.SkyLevel"/> (y dimension). The node is not
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
                // AnimateToDeath() will call animationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                animationWatchDog.Finished();
            }
        }

        /// <summary>
        /// Triggers an animation indicating that the given <paramref name="gameObject"/> was
        /// deleted by moving it up to the sky. When it reaches the sky at <see cref="skyLevel"/>,
        /// it will disappear (via <see cref="OnRemoveFinishedAnimation"/>).
        /// </summary>
        /// <param name="gameObject">a game object representing either a node or edge</param>
        private void AnimateToDeath(GameObject gameObject)
        {
            gameObject.transform.SetParent(null);
            /// Let it raise to <see cref="AbstractSEECity.SkyLevel"/>.
            Vector3 newPosition = gameObject.transform.position;
            newPosition.y = AbstractSEECity.SkyLevel;

            MoveTo(gameObject, newPosition);

            void MoveTo(GameObject gameObject, Vector3 newPosition)
            {
                if (gameObject.IsNode())
                {
                    gameObject.NodeOperator()
                              .MoveTo(newPosition, AnimationLagFactor, updateEdges: false)
                              .OnComplete(() => OnRemoveFinishedAnimation(gameObject));
                }
                else if (gameObject.IsEdge())
                {
                    gameObject.EdgeOperator()
                              .FadeTo(0, AnimationLagFactor)
                              .OnComplete(() => OnRemoveFinishedAnimation(gameObject));
                }
                else
                {
                    throw new System.Exception($"Game object {gameObject.name} is expected to be a node or edge.");
                }
            }
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
                // AnimateToDeath() will call animationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                animationWatchDog.Finished();
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
            animationWatchDog.Finished();
        }
    }
}
