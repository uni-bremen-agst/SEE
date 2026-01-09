using SEE.GO;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Allows to move game nodes (game objects representing a graph node).
    /// </summary>
    public static class GameNodeMover
    {
        /// <summary>
        /// Sets the <paramref name="newParent"/> for <paramref name="child"/> both in the
        /// game-object hierarchy and in the underlying graph. If <paramref name="newParent"/>
        /// is null, the <paramref name="child"/> becomes a root in the underlying graph
        /// and will have null as its game-object parent.
        ///
        /// Precondition: <paramref name="child"/> and <paramref name="newParent"/> must
        /// be game nodes associated with a graph node.
        ///
        /// Postcondition: <paramref name="child"/> is a child of <paramref name="newParent"/>
        /// in the hierarchy of game objects if <paramref name="newParent"/> is different
        /// from null. If <paramref name="newParent"/> is null, <paramref name="child"/>
        /// will have a null parent.
        /// </summary>
        /// <param name="child">Child whose parent is to be set.</param>
        /// <param name="newParent">New parent.</param>
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
        /// Returns the new world coordinates based on <paramref name="childPosition"/> so that the child
        /// node with a size of <paramref name="childWorldScale"/> would appear on top of
        /// <paramref name="target"/> if moved there.
        /// </summary>
        /// <remarks>
        /// Keep in mind that <paramref name="child"/> might be hanging over if it is too big.
        /// </remarks>
        /// <param name="childWorldScale">The world-space scale of a node.</param>
        /// <param name="childPosition">The world position of a node.</param>
        /// <param name="target">The target node's GameObject.</param>
        /// <returns>The new world position after the correction.</returns>
        ///
        public static Vector3 GetCoordinatesOn(Vector3 childWorldScale, Vector3 childPosition, GameObject target)
        {
            Vector3 childWorldExtent = childWorldScale / 2;
            childPosition.y = target.GetRoof() + childWorldExtent.y + SpatialMetrics.Padding.y;

            // Make sure mappingTarget stays within the roof of parent.
            {
                Vector3 parentWorldExtent = target.WorldSpaceSize() / 2;

                // Fit child into x range of parent.
                if (childPosition.x + childWorldExtent.x > target.transform.position.x + parentWorldExtent.x)
                {
                    // Right corner of child must not be farther than right corner of parent.
                    childPosition.x = target.transform.position.x + parentWorldExtent.x - childWorldExtent.x;
                }
                else if (childPosition.x - childWorldExtent.x < target.transform.position.x - parentWorldExtent.x)
                {
                    // Left corner of child must be right from right corner of parent.
                    childPosition.x = target.transform.position.x - parentWorldExtent.x + childWorldExtent.x;
                }

                // Fit child into z range of parent.
                if (childPosition.z + childWorldExtent.z > target.transform.position.z + parentWorldExtent.z)
                {
                    // Front edge of child must not be farther than back edge of parent.
                    childPosition.z = target.transform.position.z + parentWorldExtent.z - childWorldExtent.z;
                }
                else if (childPosition.z - childWorldExtent.z < target.transform.position.z - parentWorldExtent.z)
                {
                    // Front edge of child must not be before front edge of parent.
                    childPosition.z = target.transform.position.z - parentWorldExtent.z + childWorldExtent.z;
                }
            }

            return childPosition;
        }

        /// <summary>
        /// Sets up a new movement action for the <see cref="movedObject"/>.
        /// Specifically, this will create a new version for the associated graph.
        ///
        /// Pre-condition: <see cref="movedObject"/> has a valid NodeRef component attached.
        /// </summary>
        /// <param name="movedObject">The object which is being moved.</param>
        public static void NewMovementVersion(GameObject movedObject)
        {
            if (movedObject.TryGetComponentOrLog(out NodeRef nodeRef))
            {
                nodeRef.Value.ItsGraph.NewVersion();
            }
        }

        /// <summary>
        /// Moves <paramref name="gameObject"/> (assumed to represent a node) to <paramref name="targetPosition"/>
        /// through some animation. All existing animations are cancelled.
        /// </summary>
        /// <param name="gameObject">Game node to be moved.</param>
        /// <param name="targetPosition">Target position in world space.</param>
        /// <param name="factor">The factor by which the animation duration is multiplied.</param>
        internal static void MoveTo(GameObject gameObject, Vector3 targetPosition, float factor)
        {
            gameObject.NodeOperator().MoveTo(targetPosition, factor);
        }
    }
}
