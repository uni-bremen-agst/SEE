using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.MindMap
{
    /// <summary>
    /// Provides transform-related handling for Mind Map node hierarchies.
    /// </summary>
    public static class GameMindMapTransform
    {
        /// <summary>
        /// Prepares a Mind Map node for a move or rotation operation.
        /// If children should be included, they are temporarily parented to the
        /// transformed node. Otherwise, temporary collision components of child
        /// nodes are removed when required.
        /// </summary>
        /// <param name="node">The node to prepare.</param>
        /// <param name="includeChildren">
        /// Whether child nodes should be included in the transformation.
        /// </param>
        /// <param name="rotationSetMode">
        /// Whether the preparation is performed for directly setting a rotation.
        /// </param>
        /// <param name="setMode">
        /// Whether the preparation is performed for directly setting a transform value.
        /// </param>
        public static void PrepareForTransform(
            GameObject node,
            bool includeChildren,
            bool rotationSetMode = false,
            bool setMode = false)
        {
            if (includeChildren)
            {
                PrepareNodeChildren(
                    node,
                    setMode,
                    rotationSetMode);

                return;
            }

            if (!node.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            if (!setMode)
            {
                GameObject surface =
                    GameFinder.GetDrawableSurface(node);

                string surfaceParentName =
                    GameFinder.GetDrawableSurfaceParentName(surface);

                new RbAndCCDestroyerNetAction(
                    surface.name,
                    surfaceParentName,
                    node.name).Execute();

                foreach (KeyValuePair<GameObject, GameObject> pair
                         in valueHolder.GetAllChildren())
                {
                    if (pair.Key.GetComponent<Rigidbody>() != null)
                    {
                        Destroyer.Destroy(
                            pair.Key.GetComponent<Rigidbody>());

                        Destroyer.Destroy(
                            pair.Key.GetComponent<CollisionController>());
                    }
                }
            }
        }

        /// <summary>
        /// Finishes a move or rotation operation for a Mind Map node.
        /// Child nodes that were temporarily parented to the transformed node are
        /// restored to the drawable hierarchy. If children were not included,
        /// only the affected branch lines are redrawn.
        /// </summary>
        /// <param name="node">The transformed node.</param>
        /// <param name="includeChildren">
        /// Whether child nodes were included in the transformation.
        /// </param>
        public static void FinishTransform(
            GameObject node,
            bool includeChildren)
        {
            if (includeChildren)
            {
                PostProcessNode(node);
            }
            else if (node.CompareTag(Tags.MindMapNode))
            {
                GameMindMapBranch.ReDrawBranchLines(node);
            }
        }

        /// <summary>
        /// Destroys all rigid bodies and collision controllers of the children
        /// of the given Mind Map node.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        public static void DestroyRigidBodiesAndCollisionControllersOfChildren(
            GameObject node)
        {
            if (!node.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            foreach (KeyValuePair<GameObject, GameObject> pair
                     in valueHolder.GetAllChildren())
            {
                if (pair.Key.GetComponent<Rigidbody>() != null)
                {
                    Destroyer.Destroy(
                        pair.Key.GetComponent<Rigidbody>());
                }

                if (pair.Key.GetComponent<CollisionController>() != null)
                {
                    Destroyer.Destroy(
                        pair.Key.GetComponent<CollisionController>());
                }
            }
        }

        /// <summary>
        /// Temporarily parents all child nodes to the given Mind Map node so that
        /// they participate in the same transformation.
        /// </summary>
        /// <param name="node">The parent Mind Map node.</param>
        /// <param name="setMode">
        /// Whether the preparation is performed for directly setting a transform value.
        /// </param>
        /// <param name="rotationSetMode">
        /// Whether child nodes should adopt the current rotation of the parent.
        /// </param>
        private static void PrepareNodeChildren(
            GameObject node,
            bool setMode,
            bool rotationSetMode = false)
        {
            if (!node.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            foreach (KeyValuePair<GameObject, GameObject> pair
                     in valueHolder.GetAllChildren())
            {
                if (rotationSetMode)
                {
                    pair.Key.transform.localEulerAngles =
                        node.transform.localEulerAngles;
                }

                pair.Key.transform.SetParent(node.transform);

                if (!setMode
                    && pair.Key.GetComponent<Rigidbody>() == null)
                {
                    pair.Key.AddComponent<Rigidbody>().isKinematic = true;
                    pair.Key.AddComponent<CollisionController>();
                }
            }
        }

        /// <summary>
        /// Restores child nodes to the drawable hierarchy after a transformation
        /// and redraws the affected branch lines.
        /// </summary>
        /// <param name="node">The transformed Mind Map node.</param>
        private static void PostProcessNode(GameObject node)
        {
            if (!node.CompareTag(Tags.MindMapNode))
            {
                return;
            }

            GameMindMapBranch.ReDrawParentBranchLine(node);

            GameObject attachedObjects =
                GameFinder.GetAttachedObjectsObject(node);

            MMNodeValueHolder valueHolder =
                node.GetComponent<MMNodeValueHolder>();

            foreach (KeyValuePair<GameObject, GameObject> pair
                     in valueHolder.GetAllChildren())
            {
                pair.Key.transform.SetParent(
                    attachedObjects.transform);

                pair.Value.transform.SetParent(
                    attachedObjects.transform);

                GameMindMapBranch.ReDrawParentBranchLine(
                    pair.Key);
            }
        }
    }
}
