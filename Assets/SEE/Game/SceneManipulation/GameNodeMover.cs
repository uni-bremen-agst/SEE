using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// and will have <c>null</c> as its game-object parent.
        ///
        /// Precondition: <paramref name="child"/> and <paramref name="newParent"/> must
        /// be game nodes associated with a graph node.
        ///
        /// Postcondition: <paramref name="child"/> is a child of <paramref name="newParent"/>
        /// in the hierarchy of game objects if <paramref name="newParent"/> is different
        /// from <c>null</c>. If <paramref name="newParent"/> is <c>null</c>, <paramref name="child"/>
        /// will have a <c>null</c> parent.
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
        /// Places <paramref name="child"/> on top of the roof of <paramref name="parent"/>.
        ///
        /// This method will NOT make sure that <paramref name="child"/> fits completely into the
        /// roof rectangle of <paramref name="parent"/>.
        ///
        /// The <paramref name="child"/> will be an immediate child of <paramref name="parent"/> in the
        /// game-object hierarchy afterwards.
        ///
        /// Precondition: <paramref name="parent"/> is not null.
        /// </summary>
        /// <param name="child">child to be put on <paramref name="parent"/></param>
        /// <param name="parent">parent the <paramref name="child"/> is put on</param>
        /// <param name="topPadding">Additional amount of empty space on the Y-axis that should be between
        /// <paramref name="parent"/> and <paramref name="child"/> in absolute world-space terms</param>
        ///
        public static void PlaceOn(Transform child, GameObject parent, float topPadding = 0.0001f)
        {
            // This assignment must take place before we set the parent of child to null
            // because a newly created node operator attempts to derive its code city.
            NodeOperator nodeOperator = child.gameObject.NodeOperator();

            // Release child from its current parent so that local position and scale
            // and world-space position and scale are the same, respectively.
            // The child will receive its new parent at the very end of this method.
            child.SetParent(null);

            // Where to move the child.
            Vector3 targetWorldPosition = child.position;
            Vector3 childWorldExtent = child.lossyScale / 2;
            targetWorldPosition.y = parent.GetRoof() + childWorldExtent.y + topPadding;

            // Make sure mappingTarget stays within the roof of parent.
            {
                Vector3 parentWorldExtent = parent.transform.lossyScale / 2;

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
            child.SetParent(parent.transform);
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
        /// Moves <paramref name="gameObject"/> (assumed to represent a node) to <paramref name="targetPosition"/>
        /// through some animation. All existing animations are cancelled.
        /// </summary>
        /// <param name="gameObject">game node to be moved</param>
        /// <param name="targetPosition">target position in world space</param>
        /// <param name="factor">the factor by which the animation duration is multiplied</param>
        internal static void MoveTo(GameObject gameObject, Vector3 targetPosition, float factor)
        {
            gameObject.NodeOperator().MoveTo(targetPosition, factor);
        }
    }
}
