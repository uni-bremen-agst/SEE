using SEE.GO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attribute of an author edge connecting an <see cref="AuthorSphere"/>
    /// to <see cref="AuthorRef"/>.
    /// </summary>
    /// <remarks>This component will be attached to connections between authors and their edited files.</remarks>
    public class AuthorEdge : SerializedMonoBehaviour
    {
        /// <summary>
        /// Reference to the target node this edge connects to.
        /// </summary>
        public AuthorRef FileNode;

        /// <summary>
        /// Reference to the <see cref="GameObjects.AuthorSphere"/> that this edge originates from.
        /// </summary>
        public AuthorSphere AuthorSphere;

        /// <summary>
        /// The width of the edge.
        /// </summary>
        public float Width;

        /// <summary>
        /// Draws the edge between the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        internal void Draw()
        {
            // The edge inherits the material of the AuthorSphere.
            Material authorMaterial = AuthorSphere.gameObject.GetComponent<Renderer>().sharedMaterial;

            Vector3[] linePoints = new Vector3[2];
            linePoints[0] = AuthorSphere.gameObject.transform.position;
            linePoints[1] = FileNode.gameObject.GetRoofCenter();

            LineFactory.Draw(gameObject, linePoints, Width, authorMaterial);
        }

        /// <summary>
        /// Sets the visibility of the edge.
        /// </summary>
        /// <param name="show">whether to show the line</param>
        internal void ShowOrHide(bool show)
        {
            if (TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.enabled = show;
            }
        }
    }
}
