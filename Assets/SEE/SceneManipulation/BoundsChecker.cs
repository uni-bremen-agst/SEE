using System;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Provides utility methods to compute and evaluate <see cref="Bounds"/>
    /// of <see cref="GameObject"/>s, particulary for checking spatial relationships in the XZ-plane.
    /// </summary>
    public static class BoundsChecker
    {
        /// <summary>
        /// Calculates a bounding volume that encapsulates all <see cref="Renderer"/> components
        /// attached to the given <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The GameObject whose renderers will be considered.</param>
        /// <returns>
        /// A <see cref="Bounds"/> structure that encompasses all renderer bounds of the GameObject.
        /// If no renderers are found, returns a zero-size bounds centered at the GameObject's position.
        /// </returns>
        public static Bounds GetCombinedBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponents<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.zero);
            }

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }

            return combined;
        }

        /// <summary>
        /// Checks whether all four bottom corner points of the given <paramref name="childBounds"/>
        /// are fully contained within the XZ-projection of the <paramref name="parentBounds"/>.
        /// </summary>
        /// <param name="childBounds">The bounds of the object to be tested (e.g., a child).</param>
        /// <param name="parentBounds">The bounds to test against (e.g., a parent or container).</param>
        /// <returns>
        /// True if all four XZ corners of <paramref name="childBounds"/> are within
        /// the XZ range of <paramref name="parentBounds"/>; otherwise, false.
        /// </returns>
        public static bool AreCornersInsideXZ(Bounds childBounds, Bounds parentBounds)
        {
            Vector3[] corners = new Vector3[4];

            Vector3 min = childBounds.min;
            Vector3 max = childBounds.max;

            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(min.x, min.y, max.z);
            corners[2] = new Vector3(max.x, min.y, min.z);
            corners[3] = new Vector3(max.x, min.y, max.z);

            foreach (Vector3 corner in corners)
            {
                if (!IsInsideXZ(corner, parentBounds))
                {
                    return false;
                }
            }
            return true;

            static bool IsInsideXZ(Vector3 point, Bounds bounds)
            {
                return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                       point.z >= bounds.min.z && point.z <= bounds.max.z;
            }
        }

        /// <summary>
        /// Checks whether all child nodes renderers are fully contained within the combined bounds
        /// of the specified <paramref name="parent"/> (in the XZ-plane).
        /// </summary>
        /// <param name="parent">The parent whose GameObject bounds changes.</param>
        /// <param name="onFailure">An action to execute if at least one child's bounds are not fully contained
        /// within the parent bounds in XZ. This can be used to reset transforms or handle rollback.</param>
        /// <returns>True if all child bounds are within the parent's combined bounds (XZ-plane);
        /// otherwise, false and <paramref name="onFailure"/> is invoked.</returns>
        public static bool ValidateChildrenInBounds(Node parent, Action onFailure)
        {
            Bounds newBounds = GetCombinedBounds(parent.GameObject(true));

            foreach (Node child in parent.Children())
            {
                Transform trans = child.GameObject().transform;
                Renderer r = trans.GetComponent<Renderer>();
                if (r != null && !AreCornersInsideXZ(r.bounds, newBounds))
                {
                    onFailure?.Invoke();
                    return false;
                }
            }
            return true;
        }
    }
}
