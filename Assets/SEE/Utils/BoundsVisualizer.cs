using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Draws and updates a visual bounding box for debugging.
    /// <para>
    /// Enables Gizmos in the Unity Editor to see the bounding box.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    public class BoundsVisualizer : MonoBehaviour
    {
        /// <summary>
        /// The color of the bounding box.
        /// </summary>
        public Color BoundsColor = Color.green;

        /// <summary>
        /// The type of the bounding box.
        /// </summary>
        public BoundsType Type = BoundsType.SEE;

        /// <summary>
        /// Draws the gizmo for the bounding box.
        /// </summary>
        /// <remarks>Called by Unity.</remarks>
        void OnDrawGizmos()
        {
            Vector3? center, size;
            Matrix4x4 matrix = Matrix4x4.identity;

            switch (Type)
            {
                case BoundsType.Line:
                    if (!gameObject.TryGetComponent(out LineRenderer lineRenderer))
                    {
                        return;
                    }
                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);
                    Bounds lineBounds = GeometryUtils.CalculateLineBounds(lineRenderer, true);
                    center = lineBounds.center;
                    size = lineBounds.size;
                    break;
                case BoundsType.Mesh:
                    if (!gameObject.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
                    {
                        return;
                    }
                    Bounds meshBounds = meshFilter.sharedMesh.bounds;
                    center = meshBounds.center;
                    size = meshBounds.size;
                    matrix = transform.localToWorldMatrix;
                    break;
                case BoundsType.Renderer:
                    if (!gameObject.TryGetComponent(out Renderer renderer))
                    {
                        return;
                    }
                    Bounds rendererBounds = renderer.bounds;
                    center = rendererBounds.center;
                    size = rendererBounds.size;
                    break;
                case BoundsType.SEE:
                    if (!gameObject.WorldSpaceSize(out Vector3 goSize, out Vector3 goPos))
                    {
                        return;
                    }
                    size = goSize;
                    center = goPos;
                    break;
                default:
                    return;
            }

            Gizmos.color = BoundsColor;
            Gizmos.matrix = matrix;
            Gizmos.DrawWireCube(center.Value, size.Value);
        }

        /// <summary>
        /// The type of the bounding box.
        /// </summary>
        public enum BoundsType
        {
            /// <summary>
            /// Use the line renderer positions to calculate bounds.
            /// </summary>
            Line,

            /// <summary>
            /// Use bounds and center of the shared mesh.
            /// </summary>
            Mesh,

            /// <summary>
            /// Use bounds and center of the renderer.
            /// </summary>
            Renderer,

            /// <summary>
            /// Use size and position retrieved via
            /// <see cref="GameObjectExtensions.WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> as expected in
            /// SEE when the visual position and size is required.
            /// <para>
            /// The transform position, on the other hand, might be off if the mesh origin does not align with its center.
            /// Similarly, the scale does not represent the actual size for many objects except some primitives like cube.
            /// </para>
            /// </summary>
            SEE
        }
    }
}
