using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Tools.ReflexionAnalysis;
using TinySpline;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// Allows to move game nodes (game objects representing a graph node).
    /// </summary>
    public static class GameNodeMover
    {
        /// <summary>
        /// Factor by which nodes should be scaled relative to their parents in <see cref="PutOn"/>.
        /// </summary>
        public const float SCALING_FACTOR = 0.2f;

        /// <summary>
        /// Sets the <paramref name="newParent"/> for <paramref name="child"/> both in the
        /// game-object hierarchy and in the underlying graph. If <paramref name="newParent"/>
        /// is null, the <paramref name="child"/> becomes a root in the underlying graph
        /// and will have <c>null</c> as its game-object parent.
        ///
        /// Precondition: <paramref name="child"/> and <paramref name="newParent"/> must
        /// be game nodes associated with a graph node.
        /// </summary>
        /// <param name="child">child whose parent is to be set</param>
        /// <param name="newParent">new parent</param>
        /// <remarks>This method changes only the parentship in the game-object hierarchy
        /// and the graph-node hierarchy. It does not change any visual attribute
        /// of either of the two nodes.</remarks>
        public static void SetParent(GameObject child, GameObject newParent)
        {
            if (newParent != null)
            {
                child.GetComponent<NodeRef>().Value.Reparent(newParent.GetComponent<NodeRef>().Value);
                child.transform.SetParent(newParent.transform);
            }
            else
            {
                child.GetComponent<NodeRef>().Value.Reparent(null);
                child.transform.SetParent(null);
            }
        }

        /// <summary>
        /// Puts <paramref name="child"/> on top of <paramref name="parent"/> and scales it down,
        /// assuming <paramref name="scaleDown"/> is true. This method makes sure that <paramref name="child"/>
        /// will be contained within the area of the roof of <paramref name="parent"/>.
        /// </summary>
        /// <param name="child">child to be put on <paramref name="parent"/></param>
        /// <param name="parent">parent the <paramref name="child"/> is put on</param>
        /// <param name="targetXZ">XZ coordinates <paramref name="child"/> should be placed at
        /// (will be center of <paramref name="parent"/> if not given)</param>
        /// <param name="topPadding">Additional amount of empty space that should be between <paramref name="parent"/>
        /// and <paramref name="child"/>, given as a percentage of the parent's height</param>
        /// <param name="setParent">Whether <paramref name="parent"/> should become a parent of
        /// <paramref name="child"/></param>
        /// <param name="scaleDown">Whether <paramref name="child"/> should be scaled down to fit into
        /// <paramref name="parent"/></param>
        /// <returns>Old scale (i.e., before the changes from this function were applied, but after its parent
        /// was changed if <paramref name="setParent"/> was true) of <paramref name="child"/></returns>
        public static Vector3 PutOn(Transform child, GameObject parent, Vector2? targetXZ = null, float topPadding = 0,
                                    bool setParent = true, bool scaleDown = false)
        {
            if (setParent)
            {
                child.SetParent(parent.transform);
            }

            NodeOperator nodeOperator = child.gameObject.AddOrGetComponent<NodeOperator>();
            Vector3 oldLocalScale = child.localScale;
            if (scaleDown)
            {
                nodeOperator.ScaleTo(ScaleDown(child, parent), 0);
            }
            // ScaleTo with animation duration = 0 has immediate effect.
            Vector3 newWorldScale = child.lossyScale;

            // Where to move the child.
            Vector3 targetWorldPosition = child.position;
            float parentRoof = parent.GetRoof();
            targetWorldPosition.y = parentRoof + child.lossyScale.y / 2.0f + topPadding * parent.transform.lossyScale.y;

            if (targetXZ.HasValue)
            {
                targetWorldPosition.x = targetXZ.Value.x;
                targetWorldPosition.z = targetXZ.Value.y;
            }

            // Make sure mappingTarget stays within the roof of parent. We do not
            // work with local position here because we cannot assume that child
            // is actually a game-object child of parent (only if setParent were true,
            // we could assume that).
            {
                Vector3 parentWorldExtent = parent.transform.lossyScale / 2;
                Vector3 childWorldExtent = newWorldScale / 2;

                // Fit child into x range of parent.
                if (targetWorldPosition.x + childWorldExtent.x > parent.transform.position.x + parentWorldExtent.x)
                {
                    // Right corner of child must not be farther than right corner of parent.
                    targetWorldPosition.x = parent.transform.position.x + parentWorldExtent.x - childWorldExtent.x;
                }
                else if (targetWorldPosition.x - childWorldExtent.x < parent.transform.position.x - parentWorldExtent.x)
                {
                    // Left corner of child must be right from right corner of parent.
                    targetWorldPosition.x = parent.transform.position.x - parentWorldExtent.x + childWorldExtent.x;
                }

                // Fit child into z range of parent.
                if (targetWorldPosition.z + childWorldExtent.z > parent.transform.position.z + parentWorldExtent.z)
                {
                    // Front edge of child must not be farther than back edge of parent.
                    targetWorldPosition.z = parent.transform.position.z + parentWorldExtent.z - childWorldExtent.z;
                }
                else if (targetWorldPosition.z - childWorldExtent.z < parent.transform.position.z - parentWorldExtent.z)
                {
                    // Front edge of child must not be before front edge of parent.
                    targetWorldPosition.z = parent.transform.position.z - parentWorldExtent.z + childWorldExtent.z;
                }
            }

            nodeOperator.MoveTo(targetWorldPosition, 0);
            return oldLocalScale;

            // Returns the target local scale of child relative to parent when child is to be put onto parent.
            static Vector3 ScaleDown(Transform child, GameObject parent)
            {
                // TODO: We need a strategy to scale down a node to the maximal size that is still
                // fitting into the area where the nodes has been placed.
                // We want to shrink only the ground area, but maintain the height.
                float localHeight = parent.transform.lossyScale.y == 0 ?
                    0 : child.transform.lossyScale.y / parent.transform.lossyScale.y;
                return new Vector3(SCALING_FACTOR, localHeight, SCALING_FACTOR);
            }
        }

        /// <summary>
        /// Sets up a new movement action for the <see cref="movedObject"/>.
        /// Specifically, this will create a new version for the associated graph.
        ///
        /// Pre-condition: <see cref="movedObject"/> has a valid NodeRef component attached.
        /// </summary>
        /// <param name="movedObject">The object which is being moved</param>
        public static void NewMovementVersion(GameObject movedObject)
        {
            if (movedObject.TryGetComponentOrLog(out NodeRef nodeRef))
            {
                nodeRef.Value.ItsGraph.NewVersion();
            }
        }

        /// <summary>
        /// Puts <paramref name="child"/> on <paramref name="newParent"/> visually. If <paramref name="newParent"/>
        /// is different from <paramref name="originalParent"/>, the <paramref name="child"/> will be scaled
        /// down so that it fits into <paramref name="newParent"/>. If instead <paramref name="newParent"/>
        /// and <paramref name="originalParent"/> are the same, <paramref name="child"/> will be scaled back
        /// to its <paramref name="originalLocalScale"/>.
        /// </summary>
        /// <remarks>Calling this method is equivalent to <see cref="PutOn(child, newParent, scaleDown: newParent != originalParent.gameObject)"/>
        /// with a previous scaling of <paramref name="child"/> to <paramref name="originalLocalScale"/> if
        /// <paramref name="newParent"/> equals <paramref name="originalParent"/>.</remarks>
        /// <param name="child">game object to be put onto <paramref name="newParent"/></param>
        /// <param name="newParent">where to put <paramref name="child"/></param>
        /// <param name="originalParent">the <paramref name="originalParent"/> of <paramref name="child"/></param>
        /// <param name="originalLocalScale">original local scale of <paramref name="child"/> relative to
        /// <paramref name="originalParent"/>; used to restore this scale if <paramref name="newParent"/>
        /// and <paramref name="originalParent"/> are the same</param>
        internal static void PutOnAndFit(Transform child, GameObject newParent,
            GameObject originalParent, Vector3 originalLocalScale)
        {
            bool scaleDown = newParent != originalParent.gameObject;
            if (!scaleDown)
            {
                // The gameObject may have already been scaled down, hence,
                // we need to restore its original scale.
                child.gameObject.AddOrGetComponent<NodeOperator>().ScaleTo(originalLocalScale, 0);
            }
            PutOn(child, newParent, scaleDown: scaleDown);
        }

        /// <summary>
        /// Moves <paramref name="gameObject"/> (assumed to represent a node) to <paramref name="targetPosition"/>
        /// through some animation. All existing animations are cancelled.
        /// </summary>
        /// <param name="gameObject">game node to be moved</param>
        /// <param name="targetPosition">target position in world space</param>
        /// <param name="duration">the duration of the animation in seconds</param>
        internal static void MoveTo(GameObject gameObject, Vector3 targetPosition, float duration)
        {
            if (gameObject.TryGetComponent(out NodeOperator nodeOperator))
            {
                // FIXME: Why is this killing necessary. Do we need to re-enable it?
                // KillActiveAnimations(nodeOperator);
                nodeOperator.MoveTo(targetPosition, duration);
                // FIXME: Isn't that meanwhile handled by the NodeOperator? Do we need to re-enable it?
                //MorphEdgesToSplines(gameObject, duration);
            }

            void KillActiveAnimations(NodeOperator nodeOperator)
            {
                if (gameObject.TryGetNodeRef(out NodeRef node))
                {
                    // We will also kill any active tweens (=> Reflexion Analysis), if necessary.
                    if (node.Value.IsInImplementation() || node.Value.IsInArchitecture())
                    {
                        // TODO: Instead of just killing animations here with this trick,
                        //       handle all movement inside the NodeOperator.
                        nodeOperator.MoveTo(nodeOperator.TargetPosition, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Morphs the incoming and outgoing edges of <see cref="gameObject"/> to simple splines.
        /// </summary>
        /// <param name="duration">the duration of the morphing animation in seconds</param>
        private static void MorphEdgesToSplines(GameObject gameObject, float duration)
        {
            // The minimal y offset for the point in between the start and end
            // of a spline through which the spline should pass.
            const float MinimalSplineOffset = 0.05f;

            // We will also "stick" the connected edges to the moved node during its movement.
            // In order to do this, we need to modify the splines of each one.
            // --------------------------------------------------------------------------------
            // FIXME: This is too simplistic. It does not handle the case of moving an inner
            // node whose descendants have connecting edges. The descendants will be moved
            // along with the inner node, but not their edges.
            // --------------------------------------------------------------------------------
            foreach ((SEESpline connectedSpline, bool nodeIsSource) hitEdge in GetConnectedEdges(gameObject))
            {
                Edge edge = hitEdge.connectedSpline.gameObject.GetComponent<EdgeRef>().Value;
                BSpline spline;
                if (hitEdge.nodeIsSource)
                {
                    spline = SplineEdgeLayout.CreateSpline(gameObject.transform.position,
                                                           edge.Target.RetrieveGameNode().transform.position,
                                                           true,
                                                           MinimalSplineOffset);
                }
                else
                {
                    spline = SplineEdgeLayout.CreateSpline(edge.Source.RetrieveGameNode().transform.position,
                                                           gameObject.transform.position,
                                                           true,
                                                           MinimalSplineOffset);
                }

                if (hitEdge.connectedSpline.gameObject.TryGetComponentOrLog(out EdgeOperator edgeOperator))
                {
                    edgeOperator.MorphTo(spline, duration);
                }
            }
        }

        /// <summary>
        /// Returns the list of <see cref="SEESpline"/>s of the incoming and outgoing edges
        /// of <paramref name="gameNode"/>. The boolean in the returned pair indicates
        /// whether the edge is outgoing (if it is false, the edge is incoming).
        /// </summary>
        private static IList<(SEESpline, bool nodeIsSource)> GetConnectedEdges(GameObject gameNode)
        {
            IList<(SEESpline, bool nodeIsSource)> ConnectedEdges = new List<(SEESpline, bool)>();
            if (gameNode.TryGetNode(out Node node))
            {
                foreach (Edge edge in node.Incomings.Union(node.Outgoings).Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
                {
                    GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
                    Assert.IsNotNull(gameEdge);
                    if (gameEdge.TryGetComponentOrLog(out SEESpline spline))
                    {
                        ConnectedEdges.Add((spline, node == edge.Source));
                    }
                }
            }
            return ConnectedEdges;
        }
    }
}